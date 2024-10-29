using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Challenge2_Group16_GUI_WebAPI.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }


    public DbSet<RegisteredClient> Clients { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AccessTokenBlacklistEntry> AccessTokenBlacklist { get; set; }

    public DbSet<TempData> TempData { get; set; }
    public DbSet<pHData> pHData { get; set; }
    public DbSet<StirringData> StirringData { get; set; }
    public DbSet<DeviceStatusData> DeviceStatusData { get; set; }
    public DbSet<ErrorData> ErrorData { get; set; }

    public DbSet<TempAggregateData> TempAggregateData { get; set; }
    public DbSet<pHAggregateData> pHAggregateData { get; set; }
    public DbSet<StirringAggregateData> StirringAggregateData { get; set; }
    public DbSet<DeviceStatusAggregateData> DeviceStatusAggregateData { get; set; }
    public DbSet<ErrorAggregateData> ErrorAggregateData { get; set; }
}

public static class ApplicationDbContextExtensions
{
    public static DbSet<T>? GetDbSet<T>(this ApplicationDbContext context) where T : class
    {
        // Find a property in the ApplicationDbContext that matches the type T
        var dbSetProperty = typeof(ApplicationDbContext)
            .GetProperties()
            .FirstOrDefault(prop => prop.PropertyType == typeof(DbSet<T>));

        // If found, return the property value as DbSet<T>
        return dbSetProperty?.GetValue(context) as DbSet<T>;
    }
}