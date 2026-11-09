using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.DTO;

public class  ReportResponse
{
    public int ExpiringIn30 { get; set; }
    public int ExpiringIn60 { get; set; }
    public int ExpiringIn90 { get; set; }
    public List<int> NumberOfItemsPerCategory { get; set; } = new();
    public List<int> TotalCostPerCategory { get; set; } = new();
    public List<Provider> TopProviders { get; set; } = new();
    public List<User> TopEmployees { get; set; } = new();
}

public class Provider
{
    public string ProviderName {get; set;} = "";
}
