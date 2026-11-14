using ClosedXML.Excel;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class ItemImportTests
{
    // a new Excel file with the template's header row
    private static XLWorkbook CreateXLWorkbook()
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Items");
        string[] headers =
        {
            "Name", "Category", "Responsible email", "Start date", "Expiration date", "Cost", "Provider",
            "Reference number", "Description"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        return workbook;
    }
    
    // writes one item into the given row
    private static void AddRow(IXLWorksheet sheet, int row, string name, string category, string email, DateTime startDate, DateTime expirationDate, decimal? cost = null)
    {
        sheet.Cell(row, 1).Value = name;
        sheet.Cell(row, 2).Value = category;
        sheet.Cell(row, 3).Value = email;
        sheet.Cell(row, 4).Value = startDate;
        sheet.Cell(row, 5).Value = expirationDate;
        if (cost != null)
        {
            sheet.Cell(row, 6).Value = cost.Value;
        }
    }
    
    // saves the workbook into memory
    private static MemoryStream ToStream(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;  
        return stream;
    }
    
    // file with 2 valid rows creates both items
    [Fact]
    public async Task Import_ValidFile_CreatesAllItems()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email="manager@test.com", RoleId = 3};
        Item existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        DateTime today = DateTime.UtcNow.Date;
        
        using var workbook = CreateXLWorkbook();
        var sheet = workbook.Worksheet(1);
        AddRow(sheet, 2, "Office licence", existing.Category.Name, existing.ResponsibleUser.Email, today, today.AddDays(100));
        AddRow(sheet, 3, "Support contract", existing.Category.Name, existing.ResponsibleUser.Email, today, today.AddDays(200), cost: 500);
        using var file = ToStream(workbook);

        var service = new ItemService(db);
        var result = await service.ImportAsync(manager.Id, file);
        
        Assert.Null(result.Error);
        Assert.Empty(result.RowErrors);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(3, db.Items.Count());
    }

    [Fact]
    public async Task Import_OneInvalidRow_NothingSaved()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email="manager@test.com", RoleId = 3};
        Item existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        DateTime today = DateTime.UtcNow.Date;
        
        using var workbook = CreateXLWorkbook();
        var sheet = workbook.Worksheet(1);
        AddRow(sheet, 2, "Office licence", existing.Category.Name, "testmail@test.com", today, today.AddDays(100));
        AddRow(sheet, 3, "Support contract", existing.Category.Name, existing.ResponsibleUser.Email, today, today.AddDays(200), cost: 500);
        using var file = ToStream(workbook);
        
        var service = new ItemService(db);
        var result = await service.ImportAsync(manager.Id, file);
        
        Assert.NotEmpty(result.RowErrors);
        Assert.Single(result.RowErrors);
        Assert.Contains("Row 2", result.RowErrors[0]);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, db.Items.Count());
    }
    
    [Fact]
    public async Task Import_EmployeeFromOtherDepartment_RowError()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email="manager@test.com", RoleId = 3};
        Item existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        Item other = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        DateTime today = DateTime.UtcNow.Date;
        
        using var workbook = CreateXLWorkbook();
        var sheet = workbook.Worksheet(1);
        AddRow(sheet, 2, "Office licence", existing.Category.Name, other.ResponsibleUser.Email, today, today.AddDays(100));
        AddRow(sheet, 3, "Support contract", existing.Category.Name, existing.ResponsibleUser.Email, today, today.AddDays(200), cost: 500);
        using var file = ToStream(workbook);
        
        var service = new ItemService(db);
        var result = await service.ImportAsync(manager.Id, file);
        
        Assert.NotEmpty(result.RowErrors);
        Assert.Single(result.RowErrors);
        Assert.Contains("department", result.RowErrors[0]);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(2, db.Items.Count());
    }
    
    [Fact]
    public async Task Import_WrongHeader_Refused()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email="manager@test.com", RoleId = 3};
        Item existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        DateTime today = DateTime.UtcNow.Date;
        
        using var workbook = CreateXLWorkbook();
        var sheet = workbook.Worksheet(1);
        sheet.Cell(1, 1).Value = "Title";
        AddRow(sheet, 2, "Office licence", existing.Category.Name, existing.ResponsibleUser.Email, today, today.AddDays(100));
        using var file = ToStream(workbook);
        
        var service = new ItemService(db);
        var result = await service.ImportAsync(manager.Id, file);
        
        Assert.NotNull(result.Error);
        Assert.Contains("template", result.Error);
        Assert.Empty(result.RowErrors);
        Assert.Equal(1, db.Items.Count());
    }

    [Fact]
    public async Task Import_NotExcelFile_Refused()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email="manager@test.com", RoleId = 3};
        Item existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        DateTime today = DateTime.UtcNow.Date;

        using var file = new MemoryStream(new byte[] {1,2,3,4,5});
        var service = new ItemService(db);
        var result = await service.ImportAsync(manager.Id, file);
        
        Assert.NotNull(result.Error);
        Assert.Contains("not a valid Excel file", result.Error);
        Assert.Empty(result.RowErrors);
        Assert.Equal(1, db.Items.Count());
    }
}