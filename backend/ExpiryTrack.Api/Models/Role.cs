namespace ExpiryTrack.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    // One role has many users and each user has exactly one role 
    public ICollection<User> Users { get; set; } = new List<User>();
}