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

// Add CORS with environment-specific configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string> 
        { 
            "http://localhost:5173", 
            "http://localhost:3000",
            "http://localhost:4200"  // Angular dev server
        };
        
        // Add production URLs
        if (builder.Environment.IsProduction())
        {
            // Add your production frontend URLs here
            var prodOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ?? Array.Empty<string>();
            allowedOrigins.AddRange(prodOrigins);
            
            // Add Elastic Beanstalk URLs if using AWS
            allowedOrigins.Add("http://radiator-api-prod.eba-fsuk46hv.us-east-1.elasticbeanstalk.com");
            allowedOrigins.Add("https://radiator-api-prod.eba-fsuk46hv.us-east-1.elasticbeanstalk.com");
        }
        
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add JWT Authentication with environment variable support
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                builder.Configuration["JWT:Secret"] ?? 
                throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "RadiatorStockAPI";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "RadiatorStockAPI-Users";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add logging configuration
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    
    // Add AWS CloudWatch logging in production
    if (builder.Environment.IsProduction())
    {
        // Add AWS logging provider if needed
        // loggingBuilder.AddAWSProvider();
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Radiator Stock API",
        Version = "v1.0",
        Description = "API for managing car radiator stock across warehouses with JWT authentication",
        Contact = new OpenApiContact
        {
            Name = "RadiatorStock NZ",
            Email = "support@radiatorstock.co.nz"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only. Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Radiator Stock API v1");
    c.DocumentTitle = "Radiator Stock API Documentation";
    
    // Serve Swagger UI at root only in development
    if (app.Environment.IsDevelopment())
    {
        c.RoutePrefix = string.Empty;
    }
});

// Only use HTTPS redirection in development
// AWS Elastic Beanstalk and other cloud providers handle HTTPS at the load balancer level
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Use CORS - IMPORTANT: This must be before Authentication and Authorization
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add root endpoint that provides API information
app.MapGet("/", () => Results.Ok(new 
{
    service = "RadiatorStock API",
    version = "v1.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    swagger = app.Environment.IsDevelopment() ? "/" : "/swagger",
    health = "/health",
    endpoints = new
    {
        auth = "/api/v1/auth",
        radiators = "/api/v1/radiators",
        warehouses = "/api/v1/warehouses",
        customers = "/api/v1/customers",
        sales = "/api/v1/sales"
    }
})).AllowAnonymous();

// Add comprehensive health check endpoint for AWS ELB and monitoring
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        // Custom health check that tests database connectivity
        bool dbHealthy = false;
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RadiatorDbContext>();
            dbHealthy = await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            dbHealthy = false;
        }

        var response = new
        {
            status = dbHealthy ? "healthy" : "unhealthy",
            timestamp = DateTime.UtcNow,
            checks = new[]
            {
                new
                {
                    name = "database",
                    status = dbHealthy ? "healthy" : "unhealthy",
                    description = dbHealthy ? "Database connection successful" : "Database connection failed"
                },
                new
                {
                    name = "api",
                    status = "healthy",
                    description = "API is running"
                }
            },
            environment = app.Environment.EnvironmentName
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = dbHealthy ? 200 : 503;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        }));
    }
}).AllowAnonymous();

// Add simple health check for load balancers
app.MapGet("/ping", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow })).AllowAnonymous();

// Database migration and seeding
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RadiatorDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting database migration and seeding...");
    
    // Apply pending migrations
    await context.Database.MigrateAsync();
    logger.LogInformation("✅ Database migrations applied successfully");
    
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
    Console.WriteLine($"📊 Environment: {app.Environment.EnvironmentName}");
    Console.WriteLine($"🔗 API Documentation: {(app.Environment.IsDevelopment() ? "http://localhost:5128" : "")}/swagger");
    Console.WriteLine($"💚 Health Check: /health");
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "❌ Failed to initialize database");
    Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    
    // In production, you might want to exit gracefully instead of throwing
    if (app.Environment.IsDevelopment())
    {
        throw;
    }
    else
    {
        Console.WriteLine("⚠️ Starting API without database - some features may not work");
    }
}

app.Run();