using System.Text;
using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class DocumentsControllerTests
{
    // a fake uploaded file
    private static IFormFile MakeFile(string content, string fileName = "contract.pdf")
    {
        var bytes = Encoding.ASCII.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static void DeleteUploadedFiles(AppDbContext db)
    {
        foreach (var document in db.Documents)
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Uploads", document.StoredFileName));
        }
    }

    [Fact]
    public async Task UploadForItem_PdfFromResponsibleEmployee_Saved()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.UploadForItem(item.Id, MakeFile("%PDF-1.7 test"));
        
        Assert.IsType<OkObjectResult>(result);
        var document = Assert.Single(db.Documents);
        Assert.Equal(item.Id, document.Id);
        Assert.Equal("contract.pdf", document.OriginalFileName);
        Assert.NotEqual("contract.pdf", document.StoredFileName);
        
        DeleteUploadedFiles(db);
    }
    
    [Fact]
    public async Task UploadForItem_NotAPdf_BadRequest()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.UploadForItem(item.Id, MakeFile("hello world"));
        
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("You can only upload PDF files.", bad.Value);
        Assert.Empty(db.Documents);
    }
    
    [Fact]
    public async Task UploadForItem_EmptyFile_BadRequest()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.UploadForItem(item.Id, MakeFile(""));
        
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File is empty.", bad.Value);
    }   
    
    [Fact]
    public async Task UploadForItem_FileOver10MB_BadRequest()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);
        var bigFile = new FormFile(new MemoryStream(), 0, 11 * 1024 * 1024, "file", "big.pdf");
        var result = await controller.UploadForItem(item.Id, bigFile);
        
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The file must be 10 MB or smaller.", bad.Value);
    }

    [Fact]
    public async Task UploadForItem_CancelledItem_BadRequest()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Cancelled, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);
        
        var result = await controller.UploadForItem(item.Id, MakeFile("%PDF-1.7 test"));
        
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task UploadForItem_OtherEmployee_NotFound()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var otherItem = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, otherItem.ResponsibleUserId);
        
        var result = await controller.UploadForItem(item.Id, MakeFile("%PDF-1.7 test"));
        
        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.Documents);
    }

    [Fact]
    public async Task Download_ExistingDocument_ReturnsThePdf()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, 100);
        var controller = new DocumentsController(db);
        TestData.SetUser(controller, item.ResponsibleUserId);
        await controller.UploadForItem(item.Id, MakeFile("%PDF-1.7 test"));
        var document = Assert.Single(db.Documents);
        
        var result = await controller.DownloadDocument(document.Id);

        var file = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("contract.pdf", file.FileDownloadName);
        
        DeleteUploadedFiles(db);
    }
}