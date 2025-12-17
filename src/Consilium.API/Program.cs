using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Consilium.API.Endpoints;
using Consilium.API.Services;
using Consilium.Application.Interfaces;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Repositories;
using Consilium.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

// 1. Global Configurations
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 2. Environment Configurations
if (builder.Environment.IsDevelopment())
{
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// 3. Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ConsiliumSecretKeyForDevelopment_MustBeAtLeast32CharactersLong";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost:8080";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:4200";

// 4. Database Connection (Ignored if Test)
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}

// 5. JSON Serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 6. Dependency Injection (Services and Repositories)
RegisterApplicationServices(builder.Services);

// 7. Authentication Configuration (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; 
    options.SaveToken = true;
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? string.Empty)),
        RoleClaimType = "role",       
        NameClaimType = "username",   
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = LogAuthFailure,
        OnTokenValidated = LogTokenSuccess
    };
});

// 8. Authorization Configuration
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrLawyer", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Admin", "Lawyer"));
    
    options.AddPolicy("OnlyAdmin", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Admin"));

    options.AddPolicy("Any", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Admin", "Lawyer", "Client"));
});

// 9. Swagger and Antiforgery
ConfigureSwagger(builder.Services);
builder.Services.AddAntiforgery();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => 
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:8080",
            "https://consilium-web-staging.onrender.com", 
            "https://consilium-web-prod-ea6s.onrender.com")
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

// 10. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// 9. Error Pipeline (Logic moved to auxiliary function)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(HandleCustomExceptions);
});

// 10. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAntiforgery();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", message = "This is cool" }));

// 11. Endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapClientEndpoints();
app.MapLawyerEndpoints();
app.MapAdminEndpoints();
app.MapProcessEndpoints();
app.MapMessageEndpoints();
app.MapDocumentEndpoints();
app.MapLookupEndpoints();

app.Run();

// --- AUXILIARY FUNCTIONS ---
void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IClientRepository, ClientRepository>();
    services.AddScoped<ILawyerRepository, LawyerRepository>();
    services.AddScoped<IAdminRepository, AdminRepository>();
    services.AddScoped<IProcessRepository, ProcessRepository>();
    services.AddScoped<IMessageRepository, MessageRepository>();
    services.AddScoped<IPasswordHasher, PasswordHasher>();
    services.AddScoped<JwtTokenService>();
    services.AddEndpointsApiExplorer();
}

void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
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
}

Task LogAuthFailure(AuthenticationFailedContext context)
{
    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
    var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
    var maskedHeader = string.IsNullOrEmpty(authHeader) ? "(none)" : (authHeader.Length <= 20 ? authHeader : authHeader.Substring(0, 20) + "...[truncated]");
    logger?.LogWarning(context.Exception, "JWT authentication failed. Authorization header (masked): {MaskedHeader}", maskedHeader);
    return Task.CompletedTask;
}

Task LogTokenSuccess(TokenValidatedContext context)
{
    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
    var name = context.Principal?.Identity?.Name;
    foreach (var c in context.Principal?.Claims ?? Array.Empty<Claim>())
    {
        logger?.LogDebug("JWT claim: {ClaimType} = {ClaimValue}", c.Type, c.Value);
    }
    var roleClaim = context.Principal?.FindFirst("role")?.Value;
    logger?.LogInformation("JWT validated for user '{name}' with role '{role}'", name, roleClaim);
    return Task.CompletedTask;
}

async Task HandleCustomExceptions(HttpContext context)
{
    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
    var exception = exceptionHandlerPathFeature?.Error;

    if (exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx)
    {
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

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { message = "An error occurred" });
}

public partial class Program { }