namespace ExpiryTrack.Api.DTO;

public class DepartmentItemCount
{
    public string DepartmentName { get; set; } = "";
    public int ItemCount { get; set; }
}

public class DashboardItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public DateOnly ExpirationDate { get; set; }
}

public class AdminDashboardResponse
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int ExpiringSoonItems { get; set; }
    public int ExpiredItems { get; set; }
    public int CancelledItems { get; set; }
    public List<DepartmentItemCount> ItemsByDepartment { get; set; } = new();
    public List<DashboardItem> UpcomingExpirations { get; set; } = new();
}