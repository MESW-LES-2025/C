using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Consilium.Infrastructure.Data;
using Consilium.Application.Interfaces;
using Consilium.Infrastructure.Repositories;
using Consilium.Infrastructure.Services;
using Consilium.API.Endpoints;
using Consilium.API.Services;
using Npgsql;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// --- Register Services (Dependency Injection) ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ILawyerRepository, LawyerRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuditLogFacade>();

// --- Add API Services ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Configure Exception Handling Middleware ---
app.UseExceptionHandler((exceptionHandlerApp) =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        // Handle DbUpdateException for unique constraint violations
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            if (dbEx.InnerException is Npgsql.PostgresException pgEx)
            {
                // Handle unique constraint violations
                if (pgEx.SqlState == "23505") // Unique violation
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    context.Response.ContentType = "application/json";
                    
                    var message = pgEx.ConstraintName switch
                    {
                        "uk_user_01_nif" => "A client with this NIF already exists",
                        "uk_user_02_email" => "A user with this email already exists",
                        _ => "A record with this value already exists"
                    };
                    
                    await context.Response.WriteAsJsonAsync(new { message });
                    return;
                }
            }
        }

        // Default error response
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "An error occurred" });
    });
});

// --- Configure HTTP Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", message = "This is cool" }));

// --- Map API Endpoints ---
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapClientEndpoints();
app.MapLawyerEndpoints();
app.MapAdminEndpoints();

app.Run();