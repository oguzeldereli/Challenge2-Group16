using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Middlewares;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Net.WebSockets;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(80); // HTTP
            options.ListenAnyIP(443, listenOptions =>
            {
                listenOptions.UseHttps(); // HTTPS
            });
        });

        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
                });
        });

        builder.Services.AddControllers();
        builder.Services.AddControllersWithViews();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add database
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()  // Logs to Console
            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)  // Logs to File
            .CreateLogger();

        // Add Identity with jwt token
        builder.Services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
                };
            });

        // Singleton Services
        builder.Services.AddSingleton<SseClientService>();

        // Scoped Services
        builder.Services.AddScoped<PacketService>();
        builder.Services.AddScoped<RegisteredClientService>();
        builder.Services.AddScoped<DataService>();
        builder.Services.AddScoped<PacketHandlingService>();
        builder.Services.AddScoped<PacketManagingService>();
        builder.Services.AddScoped<WebSocketManagerService>();
        builder.Services.AddScoped<WebSocketHandlerService>();
        builder.Services.AddScoped<DeviceService>();
        builder.Services.AddScoped<AuthService>();

        builder.Host.UseSerilog();


        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseCors(MyAllowSpecificOrigins);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();
        app.UseWebSocketMiddleware();
        app.MapControllers();

        try
        {
            Log.Information("Starting Web API...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "The application failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}