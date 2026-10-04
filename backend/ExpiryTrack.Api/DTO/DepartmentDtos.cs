using System.ComponentModel.DataAnnotations;

namespace ExpiryTrack.Api.DTO;

// what the user sends
public class DepartmentRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

// what the backend responds
public class DepartmentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
}

// assigning a manager to a department 
public class AssignManagerRequest
{
    public int? UserId { get; set; }
}