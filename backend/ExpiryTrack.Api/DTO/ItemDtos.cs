using System.ComponentModel.DataAnnotations;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.DTO;


// what the user sends when creating an item
public class CreateItemRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(150)]
    public string? Provider { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }


    [Required]
    public DateOnly ExpirationDate { get; set; }

    [Range(0.01, 9999999.99)]
    public decimal? Cost { get; set; }

    // only managers send it 
    // for employees use their own id 
    public int? ResponsibleUserId { get; set; }
}

// what the user sends when updating an item 
// a user can update only the descriptive fields
// name, description, provider, and reference number
public class UpdateItemRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(150)]
    public string? Provider { get; set; }

    [MaxLength(150)]
    public string? ReferenceNumber { get; set; }
}

public class ItemResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public string? Provider { get; set; }

    public string? ReferenceNumber { get; set; }

    public LifecycleState LifecycleState { get; set; }

    public DateTime StateChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = "";

    public int ResponsibleUserId { get; set; }

    public string ResponsibleUserName { get; set; } = "";

    public int PeriodNumber { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public decimal? Cost { get; set; }
}
