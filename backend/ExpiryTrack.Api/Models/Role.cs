namespace ExpiryTrack.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    // One role can be assigned to many users
    public ICollection<User> Users { get; set; } = new List<User>();
}