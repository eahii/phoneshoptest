using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Api;
using Backend.Data;
using Shared.Models;
using Microsoft.OpenApi.Models; // Added for OpenApiInfo

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Used Phones API", Version = "v1" });
});

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5058") // Blazor frontend URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add controllers (if any)
builder.Services.AddControllers();

// Add Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireCustomerRole", policy => policy.RequireRole("Customer"));
});

// Register other necessary services
builder.Services.AddScoped<DatabaseInitializer>(); // Ensure DatabaseInitializer is non-static

var app = builder.Build();

// Use CORS policy before registering API endpoints
app.UseCors("AllowBlazorClient");

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Used Phones API V1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.Initialize();
}

// Use Authentication and Authorization middlewares
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

// Map API endpoints
app.MapAuthApi();
app.MapUserApi();
app.MapCartApi();
app.MapOfferApi();
// app.MapOtherApis();

app.Run();