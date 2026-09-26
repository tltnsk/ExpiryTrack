using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<LifecyclePeriod> LifecyclePeriods => Set<LifecyclePeriod>();
    public DbSet<ItemDependency> ItemDependencies => Set<ItemDependency>();
    public DbSet<RenewalRequest> RenewalRequests => Set<RenewalRequest>();
    public DbSet<RequestAction> RequestActions => Set<RequestAction>();
    public DbSet<ItemStateHistory> ItemStateHistory => Set<ItemStateHistory>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}