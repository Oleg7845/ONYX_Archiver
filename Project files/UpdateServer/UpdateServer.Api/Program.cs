using Microsoft.EntityFrameworkCore;
using UpdateServer.Application.Services;
using UpdateServer.Domain.Repositories;
using UpdateServer.Infrastructure.Data;
using UpdateServer.Infrastructure.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Service Container Configuration (DI) ---

builder.Services.AddControllers();

// Configure OpenAPI (Swagger/Scalar) for API documentation.
// Standard for production to allow frontend/mobile/agent developers to explore endpoints.
builder.Services.AddOpenApi();

// Setup PostgreSQL connection. 
// Standard practice: Connection strings should be managed via Environment Variables or KeyVault in Production.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Pattern implementation for data persistence.
builder.Services.AddScoped<IUpdateRepository, UpdateRepository>();

// Domain logic service for handling update-related business rules.
builder.Services.AddScoped<UpdateService>();

var app = builder.Build();

// --- 2. HTTP Request Pipeline Configuration (Middleware) ---

// Development-specific tools like API documentation UI.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 3. Static Files Strategy (Files Server) ---

// Define the root directory for update packages (.zip, .exe, etc.).
var filesPath = Path.Combine(builder.Environment.ContentRootPath, "Files");

// Ensure the storage directory exists on the server's file system.
if (!Directory.Exists(filesPath))
{
    Directory.CreateDirectory(filesPath);
}

// Configure the Static File Middleware to serve update files.
// RequestPath "/files" maps to the physical "Files" folder on the server.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesPath),
    RequestPath = "/files"
});

// Enforce HTTPS for secure communication, preventing MITM attacks during update downloads.
app.UseHttpsRedirection();

// Standard Authorization middleware (can be extended with JWT/API-Key validation).
app.UseAuthorization();

// Route requests to Controller actions based on [Route] attributes.
app.MapControllers();

// Execute the application.
app.Run();