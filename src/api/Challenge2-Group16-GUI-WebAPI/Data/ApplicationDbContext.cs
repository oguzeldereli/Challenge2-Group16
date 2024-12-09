using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Challenge2_Group16_GUI_WebAPI.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    private static int _instanceCount = 0;

    public static int InstanceCount => _instanceCount;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) 
    {
        Interlocked.Increment(ref _instanceCount);
        // Console.WriteLine($"DbContext created. Total instances: {_instanceCount}");


      
    }

    public override void Dispose()
    {
        base.Dispose();
        Interlocked.Decrement(ref _instanceCount);
        // Console.WriteLine($"DbContext disposed. Total instances: {_instanceCount}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Interlocked.Decrement(ref _instanceCount);
        // Console.WriteLine($"DbContext disposed async. Total instances: {_instanceCount}");
    }

    public DbSet<RegisteredClient> Clients { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AccessTokenBlacklistEntry> AccessTokenBlacklist { get; set; }

    public DbSet<TempData> TempData { get; set; }
    public DbSet<pHData> pHData { get; set; }
    public DbSet<StirringData> StirringData { get; set; }
    public DbSet<DeviceStatusData> DeviceStatusData { get; set; }
    public DbSet<LogData> LogData { get; set; }
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