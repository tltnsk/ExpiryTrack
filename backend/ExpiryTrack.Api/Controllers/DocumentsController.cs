using System.Security.Claims;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    
    public DocumentsController(AppDbContext db)
    {
        _db = db;
    }
    // helper method for getting the user id from the authentication claims
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser()
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == GetCurrentUserId());
    }
    
    // POST api/items/{itemId:int}/documents 
    // upload a PDF to an item
    [HttpPost("items/{itemId:int}/documents")]
    [Authorize(Roles = "Employee,DepartmentManager")]
    public async Task<IActionResult> UploadForItem(int itemId, IFormFile file)
    {
        User user = await LoadCurrentUser();
        Item? item = await _db.Items
            .Include(i => i.CurrentPeriod)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || !ItemsController.CanSee(item, user))
        {
            return NotFound("Unable to find item.");
        }

        // the item must be active or expiring soon
        if (item.LifecycleState != LifecycleState.Active && item.LifecycleState != LifecycleState.ExpiringSoon)
        {
            return BadRequest("You can only upload files to active and expiring soon items.");
        }

        // a pdf has at least 5 bytes (%PDF-)
        if (file.Length < 5)
        {
            return BadRequest("File is empty.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest("The file must be 10 MB or smaller.");
        }
        
        // check that the file is a PDF file
        // a PDF file always begins with %PDF- 
        byte[] header = new byte[5];

        using (Stream stream = file.OpenReadStream())
        {
            await stream.ReadExactlyAsync(header);
        }
        
        // compare the header 
        if (System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
        {
            return BadRequest("You can only upload PDF files.");
        }
        
        // save the file in the Uploads folder with a unique name
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        Directory.CreateDirectory(folder);
        string storedName = Guid.NewGuid() + ".pdf";
        string path = Path.Combine(folder, storedName);

        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        Document document = new Document
        {
            OriginalFileName = file.FileName,
            StoredFileName = storedName,
            SizeBytes = (int)file.Length,
            UploadedAt = DateTime.UtcNow,
            ItemId = itemId,
            UploadedByUserId = user.Id
        };

        _db.Documents.Add(document);
        
        // if saving in the database fails, delete the file so it is not left without a row
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            System.IO.File.Delete(path);
            throw;
        }

        return Ok(new DocumentResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            SizeBytes = document.SizeBytes,
            UploadedAt = document.UploadedAt,
            UploadedByUserName = user.FirstName + " " + user.LastName
        });
    }
}