using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Data.Seeders;

namespace ExpiryTrack.Api.Data;

// fills an empty databasee with test data
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, int generatedItems)
    {
        // only seed an empty DB
        if (await db.Roles.AnyAsync())
            return;

        // same random numbers so that data is same after reset
        var random = new Random(42);

        await RoleSeeder.SeedAsync(db);
        await DepartmentSeeder.SeedAsync(db);
        await UserSeeder.SeedAsync(db);
        await CategorySeeder.SeedAsync(db);

        List<Item> demoItems = await ItemSeeder.SeedDemoItemsAsync(db);
        await ItemSeeder.SeedGeneratedItemsAsync(db, random, generatedItems);

        await RenewalRequestSeeder.SeedAsync(db, demoItems);
    }
}