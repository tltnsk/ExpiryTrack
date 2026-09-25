namespace ExpiryTrack.Api.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool isActive { get; set; }

    // A department has one manager
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    // a department can have many users that work there 
    public ICollection<User> Users { get; set; } = new List<User>();

    // a department can be responsible for many items
    public ICollection<Item> Items { get; set; } = new List<Item>();
}