using System.ComponentModel.DataAnnotations;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.DTOs;

// what the admin sends to create a user
public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = "";


    // the initial password which is set by the admin
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";


    [Required]
    public string Role { get; set; } = "";

    // employee and department manager belong to a department
    // finance officer and administrator don't
    public int? DepartmentId { get; set; }
}


// what the admin sends to update a user
public class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = "";

    [Required]
    public string Role { get; set; } = "";

    // employee and department manager belong to a department
    // finance officer and administrator don't
    public int? DepartmentId { get; set; }
}

// what the admin sends to activate or deactivate a user
public class SetUserActiveRequest
{
    public bool IsActive { get; set; }
}

// what the server sends back about a user
public class UserResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }

    public static UserResponse FromUser(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            IsActive = user.IsActive
        };
    }
}