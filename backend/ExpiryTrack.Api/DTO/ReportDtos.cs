using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.DTO;

public class CategoryReportRow
{
    public string CategoryName { get; set; } = "";
    public int ItemCount { get; set; }
    public decimal? TotalCost { get; set; }
}

public class ExpiringReport
{
    public int Within30Days { get; set; }
    public int Within60Days { get; set; }
    public int Within90Days { get; set; }
}

public class TopReportRow
{
    public string Name { get; set; } = "";
    public int ItemCount { get; set; }
}

// everything the page gets in one response
public class ReportResponse
{
    public ExpiringReport Expiring { get; set; } = new();
    public List<CategoryReportRow> Categories { get; set; } = new();
    public List<TopReportRow> TopProviders { get; set; } = new();
    public List<TopReportRow> TopEmployees { get; set; } = new();
}