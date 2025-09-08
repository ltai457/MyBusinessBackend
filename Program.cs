using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Build connection string with support for environment variables and RDS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Support for environment variables (better for production/containerization)
if (string.IsNullOrEmpty(connectionString))
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "radiatorstockdb";
    var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    
    connectionString = $"Host={host};Database={database};Username={username};Password={password};Port={port};SSL Mode=Require";
}

// Add services to the container
builder.Services.AddDbContext<RadiatorDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IRadiatorService, RadiatorService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISalesService, SalesService>();

// Add health checks for monitoring
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RadiatorDbContext>("database")
    .AddCheck("api", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

// Add CORS with enhanced configuration for network access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow any origin for easier testing
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // In production, use specific origins
            var allowedOrigins = new List<string>();
            
            // Add production URLs from environment variables
            var prodOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') 
                ?? Array.Empty<string>();
            allowedOrigins.AddRange(prodOrigins.Where(o => !string.IsNullOrWhiteSpace(o)));
            
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = jwtSettings["Secret"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT Secret is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add controllers and API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "RadiatorStock API", 
        Version = "v1",
        Description = "API for managing radiator inventory and sales"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RadiatorStock API v1");
        c.RoutePrefix = "swagger";
    });
}

// Security headers
app.UseSecurityHeaders();

// Enable CORS before authentication - THIS IS CRITICAL
app.UseCors("AllowFrontend");

// Authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Enhanced health check endpoint with CORS information
app.MapGet("/health", async (HttpContext httpContext, RadiatorDbContext context, IWebHostEnvironment env) =>
{
    var dbHealthy = false;
    var dbError = "";
    
    try
    {
        // Test database connection
        await context.Database.CanConnectAsync();
        dbHealthy = true;
    }
    catch (Exception ex)
    {
        dbError = ex.Message;
    }
    
    var response = new
    {
        status = dbHealthy ? "healthy" : "unhealthy",
        timestamp = DateTime.UtcNow,
        environment = env.EnvironmentName,
        api_version = "v1",
        database_status = dbHealthy ? "connected" : "disconnected",
        database_error = dbError,
        cors_policy = env.IsDevelopment() ? "AllowAnyOrigin (Development)" : "Restricted (Production)",
        checks = new[]
        {
            new
            {
                name = "database",
                status = dbHealthy ? "healthy" : "unhealthy",
                description = dbHealthy ? "Database connection successful" : $"Database connection failed: {dbError}"
            },
            new
            {
                name = "api",
                status = "healthy",
                description = "API is running"
            },
            new
            {
                name = "cors",
                status = "healthy", 
                description = env.IsDevelopment() ? "CORS allows any origin in development" : "CORS configured for production"
            }
        }
    };
    
    httpContext.Response.ContentType = "application/json";
    httpContext.Response.StatusCode = dbHealthy ? 200 : 503;
    
    // Add CORS headers manually for health check
    httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    httpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    httpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
    
    await httpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = true
    }));
}).AllowAnonymous();

// Add simple ping endpoint for load balancers
app.MapGet("/ping", (IWebHostEnvironment env) => Results.Ok(new { 
    status = "ok", 
    timestamp = DateTime.UtcNow,
    server = Environment.MachineName,
    environment = env.EnvironmentName,
    cors = env.IsDevelopment() ? "open" : "restricted"
})).AllowAnonymous();

// Add API info endpoint
app.MapGet("/api/v1/info", (IWebHostEnvironment env) => Results.Ok(new {
    name = "RadiatorStock API",
    version = "1.0.0",
    environment = env.EnvironmentName,
    timestamp = DateTime.UtcNow,
    cors_policy = env.IsDevelopment() ? "Development (Any Origin)" : "Production (Restricted)",
    endpoints = new {
        health = "/health",
        swagger = env.IsDevelopment() ? "/swagger" : null,
        auth = "/api/v1/auth",
        radiators = "/api/v1/radiators",
        warehouses = "/api/v1/warehouses",
        customers = "/api/v1/customers",
        sales = "/api/v1/sales"
    }
})).AllowAnonymous();

// Database migration and seeding with enhanced error handling
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RadiatorDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    
    logger.LogInformation("Starting database migration and seeding...");
    
    // Test connection first
    var canConnect = await context.Database.CanConnectAsync();
    if (!canConnect)
    {
        throw new InvalidOperationException("Cannot connect to database");
    }
    
    // Apply pending migrations
    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
        await context.Database.MigrateAsync();
        logger.LogInformation("✅ Database migrations applied successfully");
    }
    else
    {
        logger.LogInformation("✅ Database is up to date, no migrations needed");
    }
    
    // Seed initial data
    await SeedData.Initialize(context);
    logger.LogInformation("✅ Database seeding completed successfully");
    
    // Log connection info (without sensitive details)
    var connectionInfo = context.Database.GetConnectionString();
    var maskedConnection = connectionInfo?.Split(';')
        .Where(part => !part.ToLower().Contains("password"))
        .Aggregate((a, b) => $"{a};{b}") ?? "Not available";
    
    logger.LogInformation("✅ Connected to database: {ConnectionInfo}", maskedConnection);
    
    Console.WriteLine("🚀 RadiatorStock API started successfully!");
    Console.WriteLine($"📊 Environment: {env.EnvironmentName}");
    Console.WriteLine($"🌐 CORS Policy: {(env.IsDevelopment() ? "Allow Any Origin (Development)" : "Restricted (Production)")}");
    Console.WriteLine($"🔗 API Documentation: {(env.IsDevelopment() ? "http://localhost:5128" : "")}/swagger");
    Console.WriteLine($"💚 Health Check: /health");
    Console.WriteLine($"📍 API Info: /api/v1/info");
    Console.WriteLine($"🏓 Ping: /ping");
    
    // Show network information
    Console.WriteLine("🌍 Network Access:");
    Console.WriteLine($"   - Localhost: http://localhost:5128");
    Console.WriteLine($"   - Network: http://{Environment.MachineName}:5128");
    Console.WriteLine($"   - IP Access: http://[your-ip]:5128");
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "❌ Failed to initialize database");
    Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    
    // In production, you might want to exit gracefully instead of throwing
    if (app.Environment.IsDevelopment())
    {
        Console.WriteLine("⚠️ Development mode: API will start anyway for debugging");
        Console.WriteLine("🔍 Check your database connection string and ensure PostgreSQL is running");
        Console.WriteLine("🌐 CORS is configured to allow any origin in development");
    }
    else
    {
        Console.WriteLine("⚠️ Production mode: Starting API without database - some features may not work");
    }
}

app.Run();

// Extension method for security headers
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            
            await next();
        });
    }
}