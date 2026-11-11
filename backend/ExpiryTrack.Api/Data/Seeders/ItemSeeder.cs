using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Data.Seeders;

public static class ItemSeeder
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static readonly Dictionary<string, ((string Name, string Provider)[] Products, int MinCost, int MaxCost)>
        Catalog = new()
        {
            ["Software Licence"] = (new[]
            {
                ("Microsoft 365", "Microsoft"),
                ("Adobe Creative Cloud", "Adobe"),
                ("JetBrains All Products", "JetBrains"),
                ("Slack Business+", "Slack"),
                ("Zoom Workplace", "Zoom"),
                ("ESET Endpoint Security", "ESET"),
                ("Jira Software", "Atlassian"),
                ("SSL certificate", "DigiCert"),
            }, 150, 1000),
            ["Contract"] = (new[]
            {
                ("IT support contract", "TechServ Ltd."),
                ("Cleaning services contract", "CleanPro Services Ltd."),
                ("Security services contract", "SafeGuard Security"),
                ("Catering contract", "FoodCo"),
                ("Consulting agreement", "BrightPath Consulting"),
            }, 2000, 10000),
            ["Insurance"] = (new[]
            {
                ("Car insurance", "Allianz"),
                ("Property insurance", "DZI"),
                ("Liability insurance", "Bulstrad"),
                ("Health insurance", "Allianz"),
                ("Equipment insurance", "Generali"),
            }, 500, 6000),
            ["Lease"] = (new[]
            {
                ("Office lease", "Imoti Properties"),
                ("Car lease", "Porsche Leasing"),
                ("Laptop lease", "Lenovo Financial Services"),
                ("Printer lease", "Canon"),
                ("Warehouse lease", "Logistic Park"),
            }, 3000, 20000),
            ["Certificate"] = (new[]
            {
                ("SSL certificate", "DigiCert"),
                ("Wildcard SSL Certificate", "GlobalSign"),
                ("Code signing certificate", "Sectigo"),
                ("Domain name", "GoDaddy"),
            }, 50, 1500),
        };

    public static async Task<List<Item>> SeedDemoItemsAsync(AppDbContext db)
    {
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name);
        var employee = await db.Users.SingleAsync(u => u.Email == "employee@test.com");
        int? managerId = (await db.Departments.SingleAsync(d => d.Id == employee.DepartmentId)).ManagerId;

        var periods = new List<LifecyclePeriod>();

        // daysLeft is counted from today: negative means already expired
        AddItem(db, periods, "Microsoft 365 Business", "Microsoft", categories["Software Licence"], employee, managerId, daysLeft: 12, cost: 2400);
        AddItem(db, periods, "Office lease Sofia HQ", "Imoti Properties", categories["Lease"], employee, managerId, daysLeft: 40, cost: 36000);
        AddItem(db, periods, "Company car insurance", "Allianz", categories["Insurance"], employee, managerId, daysLeft: 20, cost: 1850);
        AddItem(db, periods, "SSL certificate for company website", "DigiCert", categories["Software Licence"], employee, managerId, daysLeft: 5, cost: 300);
        AddItem(db, periods, "Cloud backup service", "Backblaze", categories["Contract"], employee, managerId, daysLeft: -10, cost: 1200);
        AddItem(db, periods, "SSL Certificate", "DigiCert", categories["Certificate"], employee, managerId, daysLeft: 25, cost: 950);
        AddItem(db, periods, "Domain name expirytrack.com", "GoDaddy", categories["Software Licence"], employee, managerId, daysLeft: 3, cost: 40);
        AddItem(db, periods, "IT support contract", "TechServ Ltd.", categories["Contract"], employee, managerId, daysLeft: 55, cost: 12000);
        AddItem(db, periods, "Laptop leasing agreement", "Lenovo Financial Services", categories["Lease"], employee, managerId, daysLeft: -30, cost: 8400);
        AddItem(db, periods, "Adobe Creative Cloud", "Adobe", categories["Software Licence"], employee, managerId, daysLeft: 150, cost: 1300);
        AddItem(db, periods, "JetBrains All Products Pack", "JetBrains", categories["Software Licence"], employee, managerId, daysLeft: 200, cost: 2900);
        AddItem(db, periods, "Server room insurance", "DZI", categories["Insurance"], employee, managerId, daysLeft: 100, cost: 2200);
        AddItem(db, periods, "Zoom Workplace", "Zoom", categories["Software Licence"], employee, managerId, daysLeft: 80, cost: 900);
        AddItem(db, periods, "Slack Business+", "Slack", categories["Software Licence"], employee, managerId, daysLeft: 300, cost: null);
        AddItem(db, periods, "Antivirus licence", "ESET", categories["Software Licence"], employee, managerId, daysLeft: 60, cost: 700, cancelled: true);

        await SaveItemsAsync(db, periods);

        var demoItems = new List<Item>();
        foreach (var period in periods)
        {
            demoItems.Add(period.Item);
        }
        return demoItems;
    }
    // creates one item with its first period and its state history
    private static Item AddItem(AppDbContext db, List<LifecyclePeriod> periods, string name, string provider,
        Category category, User employee, int? managerId, int daysLeft, decimal? cost, bool cancelled = false)
    {
        var now = DateTime.UtcNow;
        DateOnly expirationDate = Today.AddDays(daysLeft);
        LifecycleState state = cancelled ? LifecycleState.Cancelled : StateFor(daysLeft, category);

        var item = new Item
        {
            Name = name,
            Provider = provider,
            CategoryId = category.Id,
            DepartmentId = employee.DepartmentId!.Value,
            ResponsibleUserId = employee.Id,
            LifecycleState = state,
            StateChangedAt = now,
            CreatedAt = now,
        };

        // the period points to the item through the navigation property
        // so EF Core fills in ItemId when the item is saved
        var period = new LifecyclePeriod
        {
            Item = item,
            PeriodNumber = 1,
            StartDate = expirationDate.AddYears(-1),
            ExpirationDate = expirationDate,
            Cost = cost,
            CreatedAt = now,
        };

        db.Items.Add(item);
        db.LifecyclePeriods.Add(period);
        periods.Add(period);

        // every item starts as Active then moves to its current state
        db.ItemStateHistory.Add(new ItemStateHistory
        {
            Item = item,
            FromState = null,
            ToState = LifecycleState.Active,
            ChangedByUserId = employee.Id,
            Reason = "Item created",
            ChangedAt = now,
        });

        if (state == LifecycleState.ExpiringSoon || state == LifecycleState.Expired)
        {
            // the background job marks the item as expiring soon
            AddHistory(db, item, LifecycleState.Active, LifecycleState.ExpiringSoon, null, "Item expiring soon.", now);
        }
        if (state == LifecycleState.Expired)
        {
            AddHistory(db, item, LifecycleState.ExpiringSoon, LifecycleState.Expired, null, "Item expired without being renewed.", now);
        }
        if (state == LifecycleState.Cancelled)
        {
            // items are cancelled by the department manager
            AddHistory(db, item, LifecycleState.Active, LifecycleState.Cancelled, managerId, "Item cancelled", now);
        }

        return item;
    }

    // generated items spread over all departments and employees
    public static async Task SeedGeneratedItemsAsync(AppDbContext db, Random random, int count)
    {
        var categories = await db.Categories.ToListAsync();

        var employees = await db.Users
            .Include(u => u.Department)
            .Where(u => u.Role.Name == "Employee" && u.Email != "employee@test.com" && u.Email != "emily.moore@test.com")
            .ToListAsync();

        var periods = new List<LifecyclePeriod>();

        for (int i = 0; i < count; i++)
        {
            var category = categories[random.Next(categories.Count)];
            var catalog = Catalog[category.Name];
            var product = catalog.Products[random.Next(catalog.Products.Length)];
            var employee = employees[random.Next(employees.Count)];

            // between 60 days ago and a year from today
            int daysLeft = random.Next(-60, 366);

            // about 1 in 10 items has no cost, the rest are rounded to tens
            decimal? cost = null;
            if (random.Next(10) != 0)
            {
                cost = random.Next(catalog.MinCost / 10, catalog.MaxCost / 10) * 10;
            }

            // about 1 in 25 items is cancelled
            bool cancelled = random.Next(25) == 0;

            string name = product.Name + " - " + employee.Department!.Name;

            AddItem(db, periods, name, product.Provider, category, employee, employee.Department.ManagerId, daysLeft, cost, cancelled);
        }

        await SaveItemsAsync(db, periods);
    }

    private static void AddHistory(AppDbContext db, Item item, LifecycleState from, LifecycleState to, int? userId, string reason, DateTime changedAt)
    {
        db.ItemStateHistory.Add(new ItemStateHistory
        {
            Item = item,
            FromState = from,
            ToState = to,
            ChangedByUserId = userId,
            Reason = reason,
            ChangedAt = changedAt,
        });
    }

    // saves the items, periods and history in two steps
    private static async Task SaveItemsAsync(AppDbContext db, List<LifecyclePeriod> periods)
    {
        // items, periods and history together, EF Core inserts the items first
        await db.SaveChangesAsync();

        // now the periods have Ids, so each item can point to its current period
        foreach (var period in periods)
        {
            period.Item.CurrentPeriod = period;
        }
        await db.SaveChangesAsync();
    }

    // the state that matches the expiration date, same rule as ItemService and ExpirationService
    private static LifecycleState StateFor(int daysLeft, Category category)
    {
        if (daysLeft < 0)
            return LifecycleState.Expired;
        if (daysLeft <= category.WarningPeriodDays)
            return LifecycleState.ExpiringSoon;
        return LifecycleState.Active;
    }
}