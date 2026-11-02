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
    
     // POST api/requests/{requestId:int}/documents 
    // upload a PDF to an request
    [HttpPost("requests/{requestId:int}/documents")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UploadForRequest(int requestId, IFormFile file)
    {
        User user = await LoadCurrentUser();
        RenewalRequest? request = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        
        if (request == null || !RenewalRequestsController.CanSeeRequest(request, user))
        {
            return NotFound("Unable to find request.");
        }

        if (request.RequestedByUserId != user.Id)
        {
            return Forbid();
        }
        
        // documents can only be attached when submitting the request
        // so the number of actions for that request needs to be checked
        int actionCount = await _db.RequestActions.CountAsync(a => a.RequestId == requestId);

        if (actionCount > 1)
        {
            return BadRequest("You can only add documents when submitting the request.");
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
            RequestId = requestId,
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
    
    // GET api/items/{itemId:int}/documents
    // get the item's documents
    [HttpGet("items/{itemId:int}/documents")]
    public async Task<IActionResult> GetItemDocuments(int itemId)
    {
        User user = await LoadCurrentUser();
        Item? item = await _db.Items
            .Include(i => i.CurrentPeriod)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || !ItemsController.CanSee(item, user))
        {
            return NotFound("Unable to find item.");
        }

        var documents = await _db.Documents
            .Where(d => d.ItemId == item.Id)
            .Include(d => d.UploadedByUser)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();
        
        var responses = new List<DocumentResponse>();
        foreach (var document in documents)
        {
            var response = ToResponse(document);
            responses.Add(response);
        }
        
        return Ok(responses);
    }
    
    // GET api/requests/{requestId:int}/documents
    // get the request's documents
    [HttpGet("requests/{requestId:int}/documents")]
    public async Task<IActionResult> GetRequestDocuments(int requestId)
    {
        User user = await LoadCurrentUser();
        RenewalRequest? request = await  _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || !RenewalRequestsController.CanSeeRequest(request, user))
        {
            return NotFound("Unable to find request.");
        }

        var documents = await _db.Documents
            .Where(d => d.RequestId == requestId)
            .Include(d => d.UploadedByUser)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();
        
        var responses = new List<DocumentResponse>();
        foreach (var document in documents)
        {
            var response = ToResponse(document);
            responses.Add(response);
        }
        
        return Ok(responses);
    }
    
    // GET api/documents/{id:int}/download
    // downloading a document 
    [HttpGet("documents/{id:int}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        User user = await LoadCurrentUser();
        
        Document? document = await _db.Documents
            .Include(d => d.Item)
            .ThenInclude(i => i!.CurrentPeriod)
            .Include(d => d.Request)
            .ThenInclude(r => r!.Period)
            .ThenInclude(p => p.Item)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound("Unable to find document.");
        }
        
        // if the document is attached to item
        if (document.ItemId != null)
        {
            if (!ItemsController.CanSee(document.Item!, user))
            {
                return NotFound("Unable to find document.");
            }
        }

        if (document.RequestId != null)
        {
            if (!RenewalRequestsController.CanSeeRequest(document.Request!, user))
            {
                return NotFound("Unable to find request.");
            }
        }

        if (!System.IO.File.Exists(document.OriginalFileName))
        {
            return NotFound("Unable to find the file.");
        }
        
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", document.StoredFileName);
        
        return PhysicalFile(path, "application/pdf", document.OriginalFileName);
    }
    
    private static DocumentResponse ToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            SizeBytes = document.SizeBytes,
            UploadedAt = document.UploadedAt,
            UploadedByUserName = document.UploadedByUser.FirstName + " " + document.UploadedByUser.LastName
        };
    }
}