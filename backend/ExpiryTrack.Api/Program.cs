using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ExpiryTrack.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// login with encrypted cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ExpiryTrack.Auth";
        options.Cookie.HttpOnly = true;                    // prevent XSS: prevents cookie being accessed by client-side JavaScript
        options.Cookie.SameSite = SameSiteMode.Strict;     // prevent CSRF: prevent cookies from being sent with requests from other sites 
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;                  // stays valid while the user is active

        // invalid credentials 
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        //  forbidden access
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ExpiryTrack API"));

    // fill an empty database with roles and test users
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedRolesAsync(db);
    await DbSeeder.SeedTestUsersAsync(db);
    await DbSeeder.SeedTestCategoriesAsync(db);
    await DbSeeder.SeedItemsAsync(db);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();