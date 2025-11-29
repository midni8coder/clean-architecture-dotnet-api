using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using MediatR;
using FluentValidation;

using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Caching;
using Infrastructure.Authentication;
using Infrastructure.Security;
using Infrastructure.Email;
using Infrastructure.BackgroundJobs;
using Application.Behaviors;
using Application.Mappers;
using API.Middleware;

// Set environment to Development
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Explicitly set to Development environment
builder.Environment.EnvironmentName = "Development";

// Load configuration
var configuration = builder.Configuration;

Console.WriteLine($"[CONFIG] Environment: {builder.Environment.EnvironmentName}");

// Database
builder.Services.AddDbContext<CleanArchDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=CleanArchDb;Trusted_Connection=true;",
        sqlOptions => sqlOptions.CommandTimeout(3))
);

// Redis - non-blocking (OPTIONAL)
IConnectionMultiplexer? redisConnection = null;
var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false;

try
{
    redisConnection = ConnectionMultiplexer.Connect(redisOptions);
    if (redisConnection != null)
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        Console.WriteLine("[SUCCESS] Redis connected successfully");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARNING] Redis connection failed: {ex.Message}");
    Console.WriteLine("[INFO] Continuing without Redis - caching disabled");
}

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Security
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Caching - always register (will use mock if Redis unavailable)
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// JWT Authentication
var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Email Service
builder.Services.AddSingleton<IEmailService, MockEmailService>();

// Background Services
builder.Services.AddHostedService<EmailDispatcherBackgroundService>();

// Health Checks
builder.Services.AddHealthChecks();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Clean Architecture API",
        Version = "v1",
        Description = "Production-ready .NET 8 API with Clean Architecture",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Development Team",
        }
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

Console.WriteLine("""
===========================================================
Clean Architecture .NET 8 Web API
===========================================================

Application initializing...
Environment: Development
""");

// Middleware pipeline - DEVELOPMENT SPECIFIC
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("[INFO] Development environment detected");
    Console.WriteLine("[INFO] Swagger enabled at /swagger");
    
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CleanArch API v1");
    });
    
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health");

// Startup message middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.Request.Method == "GET")
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Clean Architecture .NET 8 Web API</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; display: flex; align-items: center; padding: 20px; }
        .container { max-width: 1000px; width: 100%; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 20px 60px rgba(0,0,0,0.3); }
        h1 { color: #333; margin-bottom: 10px; font-size: 28px; }
        .subtitle { color: #666; margin-bottom: 30px; font-size: 16px; }
        .status { padding: 15px 20px; border-radius: 8px; margin: 20px 0; background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin: 30px 0; }
        @media (max-width: 768px) { .grid { grid-template-columns: 1fr; } }
        .card { background: #f8f9fa; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; }
        .card h3 { color: #667eea; margin-bottom: 15px; font-size: 18px; }
        .card p { color: #555; line-height: 1.6; margin: 8px 0; }
        .endpoint { margin: 15px 0; padding: 15px; background: #f8f9fa; border-left: 4px solid #667eea; border-radius: 4px; }
        .endpoint strong { color: #667eea; display: block; margin-bottom: 5px; }
        .code-block { background: #2d2d2d; color: #f8f8f2; padding: 10px; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 12px; margin: 8px 0; overflow-x: auto; }
        a { color: #667eea; text-decoration: none; font-weight: 600; }
        a:hover { text-decoration: underline; }
        .badge { display: inline-block; background: #667eea; color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: bold; margin-left: 10px; }
        h2 { color: #333; margin-top: 30px; margin-bottom: 15px; font-size: 22px; border-bottom: 2px solid #667eea; padding-bottom: 10px; }
        ol { margin-left: 20px; color: #555; line-height: 1.8; }
        hr { margin: 30px 0; border: none; border-top: 1px solid #e0e0e0; }
        .footer { color: #999; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; }
        .success-icon { color: #28a745; font-weight: bold; }
    </style>
</head>
<body>
    <div class="container">
        <div style="margin-bottom: 20px;">
            <h1>Clean Architecture .NET 8 Web API <span class="badge">DEVELOPMENT</span></h1>
            <p class="subtitle">Production-ready backend with Clean Architecture & SOLID principles</p>
        </div>
        
        <div class="status">
            <span class="success-icon">[SUCCESS] Application started successfully!</span>
        </div>
        
        <div class="grid">
            <div class="card">
                <h3>Quick Links</h3>
                <p><strong>Swagger UI (API Testing):</strong></p>
                <p><a href="http://localhost:5000/swagger" target="_blank">http://localhost:5000/swagger</a></p>
                <p style="margin-top: 15px;"><strong>Health Check:</strong></p>
                <div class="code-block">http://localhost:5000/health</div>
            </div>
            
            <div class="card">
                <h3>Server Configuration</h3>
                <p><strong>Environment:</strong> Development</p>
                <p><strong>Server Port:</strong> 5000 (HTTP)</p>
                <p><strong>Database:</strong> SQL Server (LocalDB)</p>
                <p><strong>Caching:</strong> Redis (Optional)</p>
            </div>
        </div>
        
        <h2>Available API Endpoints</h2>
        
        <div class="endpoint">
            <strong>POST /api/users</strong>
            <p>Create a new user (no authentication required)</p>
            <div class="code-block">{"email":"user@example.com","firstName":"John","lastName":"Doe","password":"SecurePassword123!"}</div>
        </div>
        
        <div class="endpoint">
            <strong>POST /api/auth/login</strong>
            <p>Login and receive JWT tokens</p>
            <div class="code-block">{"email":"user@example.com","password":"SecurePassword123!"}</div>
        </div>
        
        <div class="endpoint">
            <strong>GET /api/users/{id}</strong>
            <p>Get user by ID (requires JWT authentication)</p>
            <div class="code-block">Authorization: Bearer YOUR_JWT_TOKEN</div>
        </div>
        
        <div class="endpoint">
            <strong>POST /api/auth/refresh</strong>
            <p>Refresh expired access token using refresh token</p>
        </div>
        
        <h2>Testing Workflow</h2>
        <ol>
            <li><strong>Create a user:</strong> POST to /api/users with user credentials</li>
            <li><strong>Login:</strong> POST to /api/auth/login to receive tokens</li>
            <li><strong>Copy JWT:</strong> Extract accessToken from login response</li>
            <li><strong>Call Protected API:</strong> GET /api/users/{id} with Authorization header</li>
        </ol>
        
        <h2>Documentation & Resources</h2>
        <div class="card" style="border-left-color: #28a745; margin-bottom: 20px;">
            <p><a href="http://localhost:5000/swagger" target="_blank">Swagger/OpenAPI Documentation</a> - Interactive API testing & exploration</p>
            <p style="margin-top: 10px;"><strong>Project Files:</strong></p>
            <ul style="margin-left: 20px;">
                <li><code>ARCHITECTURE.md</code> - Complete architecture documentation</li>
                <li><code>RUNNING.md</code> - How to run the application</li>
                <li><code>QUICKSTART.md</code> - Quick setup guide</li>
                <li><code>TROUBLESHOOTING.md</code> - Common issues & solutions</li>
            </ul>
        </div>
        
        <div class="footer">
            Built with .NET 8 | Clean Architecture | SOLID Principles | Domain-Driven Design
        </div>
    </div>
</body>
</html>
""");
        return;
    }
    await next();
});

Console.WriteLine("""

[SUCCESS] Application ready!
[INFO] Swagger: http://localhost:5000/swagger
[INFO] Health: http://localhost:5000/health

Press Ctrl+C to stop the server.
===========================================================
""");

app.Run();
