using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Domain.Enums;
using Consilium.API.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Consilium.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admins")
            .WithName("Admins")
            .WithOpenApi();
        group.RequireAuthorization("OnlyAdmin");

        group.MapGet("/", GetAllAdmins)
            .WithName("GetAllAdmins")
            .WithDescription("Retrieve all admins");

        group.MapGet("/{id:guid}", GetAdminById)
            .WithName("GetAdminById")
            .WithDescription("Retrieve an admin by ID");

        group.MapPost("/", CreateAdmin)
            .WithName("CreateAdmin")
            .WithDescription("Create a new admin");

        group.MapDelete("/{id:guid}", DeleteAdmin)
            .WithName("DeleteAdmin")
            .WithDescription("Delete an admin");

        group.MapPatch("/{id:guid}", UpdateAdmin)
            .WithName("UpdateAdmin")
            .WithDescription("Update admin and user information");
    }

    private static async Task<IResult> GetAllAdmins(
        IAdminRepository repo,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        var admins = await repo.GetAll();

        var query = admins.AsQueryable();

        // Text search across Name, Email, and NIF
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.User.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.User.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.User.NIF.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by status (IsActive boolean)
        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
            query = query.Where(a => a.User.IsActive == isActive);
        }

        // Count before pagination
        var totalCount = query.Count();

        // Sorting
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = sortBy.ToLower();
            sortOrder = sortOrder?.ToLower() ?? "asc";

            if (sortBy == "nif")
                query = sortOrder == "desc" ? query.OrderByDescending(a => a.User.NIF) : query.OrderBy(a => a.User.NIF);
            else
                query = sortOrder == "desc" ? query.OrderByDescending(a => a.User.Name) : query.OrderBy(a => a.User.Name);
        }

        // Pagination
        var paginatedAdmins = query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        var response = paginatedAdmins.Select(a => {
            var mainPhone = a.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
            var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;
            return new AdminResponse(
                Id: a.ID,
                Email: a.User?.Email ?? string.Empty,
                Name: a.User?.Name ?? string.Empty,
                Status: a.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
                NIF: a.User?.NIF ?? string.Empty,
                StartedAt: a.StartedAt,
                Phone: phoneStr,
                PhoneCountryCode: mainPhone != null ? mainPhone.CountryCode : (short?)null
            );
        });

        return Results.Ok(new {
            data = response,
            meta = new {
                totalCount,
                page,
                limit
            }
        });
    }

    private static async Task<IResult> GetAdminById(Guid id, IAdminRepository repo)
    {
        var admin = await repo.GetById(id);
        
        if (admin is null)
            return Results.NotFound(new { message = $"Admin with ID {id} not found" });

        var mainPhone = admin.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;

        var response = new AdminResponse(
            Id: admin.ID,
            Email: admin.User?.Email ?? string.Empty,
            Name: admin.User?.Name ?? string.Empty,
            Status: admin.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: admin.User?.NIF ?? string.Empty,
            StartedAt: admin.StartedAt,
            Phone: phoneStr,
            PhoneCountryCode: mainPhone != null ? mainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateAdmin(
        CreateAdminRequest request,
        IAdminRepository repo,
        IPasswordHasher hasher)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { message = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { message = "Password is required" });

        if (string.IsNullOrWhiteSpace(request.NIF) || request.NIF.Length != 9)
            return Results.BadRequest(new { message = "NIF must be a 9-character string" });

        // Hash the password
        var hashedPassword = hasher.HashPassword(request.Password);

        // Create user and admin
        var user = new User
        {
            Email = request.Email,
            PasswordHash = hashedPassword,
            Name = request.Name,
            NIF = request.NIF,
            IsActive = true
        };

        // If phone information is provided, attach it to the User so EF will persist it
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = new Phone
            {
                ID = Guid.NewGuid(),
                Number = request.PhoneNumber,
                CountryCode = request.PhoneCountryCode ?? 351,
                IsMain = request.PhoneIsMain ?? true,
                User = user
            };
            user.Phones.Add(phone);
        }

        var admin = new Admin
        {
            StartedAt = DateTime.UtcNow
        };

        // Save to database
            var newAdmin = await repo.Create(user, admin);

            // Log the creation
            //.AddCreateAdminLogAsync(newAdmin.ID, newAdmin.ID);

        // Prepare response (include main phone if present)
        var createdMainPhone = newAdmin.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var createdPhoneStr = createdMainPhone != null ? createdMainPhone.Number : string.Empty;

        var response = new AdminResponse(
            Id: newAdmin.ID,
            Email: newAdmin.User?.Email ?? string.Empty,
            Name: newAdmin.User?.Name ?? string.Empty,
            Status: UserStatus.ACTIVE,
            NIF: newAdmin.User?.NIF ?? string.Empty,
            StartedAt: newAdmin.StartedAt,
            Phone: createdPhoneStr,
            PhoneCountryCode: createdMainPhone != null ? createdMainPhone.CountryCode : (short?)null
        );

        return Results.Created($"/api/admins/{newAdmin.ID}", response);
    }

    private static async Task<IResult> DeleteAdmin(Guid id, IAdminRepository repo)
    {
        try
        {
                await repo.Delete(id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { message = $"Admin with ID {id} not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("active"))
        {
            return Results.Conflict(new { message = "Admin has active/open cases and cannot be deleted" });
        }
    }

    private static async Task<IResult> UpdateAdmin(
        Guid id,
        UpdateAdminRequest request,
        IAdminRepository repo,
        IPasswordHasher hasher)
    {
        // Validate input - at least one field should be provided
        if (string.IsNullOrWhiteSpace(request.Name) && 
            string.IsNullOrWhiteSpace(request.Email) && 
            string.IsNullOrWhiteSpace(request.Password) &&
            string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return Results.BadRequest(new { message = "At least one field must be provided for update" });
        }

        // Prepare the update data
        var userUpdates = new User
        {
            Name = request.Name ?? string.Empty,
            Email = request.Email ?? string.Empty,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password) ? hasher.HashPassword(request.Password) : string.Empty
        };

        // If phone info is present in the request, attach a Phone object to the userUpdates
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneUpd = new Phone
            {
                ID = Guid.NewGuid(),
                Number = request.PhoneNumber,
                CountryCode = request.PhoneCountryCode ?? 351,
                IsMain = request.PhoneIsMain ?? true
            };
            userUpdates.Phones.Add(phoneUpd);
        }

        var adminUpdates = new Admin();

        // Update in the repository
        var updatedAdmin = await repo.UpdateAdminAndUser(id, adminUpdates, userUpdates);

        if (updatedAdmin == null)
            return Results.NotFound(new { message = $"Admin with ID {id} not found" });

        // Prepare response (include main phone if present)
        var updatedMainPhone = updatedAdmin.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var updatedPhoneStr = updatedMainPhone != null ? updatedMainPhone.Number : string.Empty;

        var response = new AdminResponse(
            Id: updatedAdmin.ID,
            Email: updatedAdmin.User?.Email ?? string.Empty,
            Name: updatedAdmin.User?.Name ?? string.Empty,
            Status: updatedAdmin.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: updatedAdmin.User?.NIF ?? string.Empty,
            StartedAt: updatedAdmin.StartedAt,
            Phone: updatedPhoneStr,
            PhoneCountryCode: updatedMainPhone != null ? updatedMainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }
}
