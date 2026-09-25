namespace ExpiryTrack.Api.Models;

public class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // foreign key 
    public int RoleId { get; set; }

    // EF core navigation property 
    public Role Role { get; set; } = null!;

    // null for administrators and finance officers 
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Department Managers
    public Department? ManagedDepartment { get; set; }

    // Items this user is responsible for
    public ICollection<Item> ResponsibleItems { get; set; } = new List<Item>();
}