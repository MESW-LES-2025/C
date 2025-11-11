using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Domain.Enums;
using Consilium.API.Dtos;
using Consilium.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Consilium.API.Endpoints;

public static class LawyerEndpoints
{
    public static void MapLawyerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/lawyers")
            .WithName("Lawyers")
            .WithOpenApi();

        group.MapGet("/", GetAllLawyers)
            .WithName("GetAllLawyers")
            .WithDescription("Retrieve all lawyers");

        group.MapGet("/{id:guid}", GetLawyerById)
            .WithName("GetLawyerById")
            .WithDescription("Retrieve a lawyer by ID");

        group.MapPost("/", CreateLawyer)
            .WithName("CreateLawyer")
            .WithDescription("Create a new lawyer");

        group.MapDelete("/{id:guid}", DeleteLawyer)
            .WithName("DeleteLawyer")
            .WithDescription("Delete a lawyer");

        group.MapPatch("/{id:guid}", UpdateLawyer)
            .WithName("UpdateLawyer")
            .WithDescription("Update lawyer and user information");
    }

    private static async Task<IResult> GetAllLawyers(
        ILawyerRepository repo,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        var (lawyers, totalCount) = await repo.GetAll(search, status, page, limit, sortBy, sortOrder);

        var response = lawyers.Select(l => {
            var mainPhone = l.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
            var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;
            return new LawyerResponse(
                Id: l.ID,
                Email: l.User?.Email ?? string.Empty,
                Name: l.User?.Name ?? string.Empty,
                Status: l.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
                NIF: l.User?.NIF ?? string.Empty,
                ProfessionalRegister: l.ProfessionalRegister,
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

    private static async Task<IResult> GetLawyerById(Guid id, ILawyerRepository repo)
    {
        var lawyer = await repo.GetById(id);
        
        if (lawyer is null)
            return Results.NotFound(new { message = $"Lawyer with ID {id} not found" });

        var mainPhone = lawyer.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;

        var response = new LawyerResponse(
            Id: lawyer.ID,
            Email: lawyer.User?.Email ?? string.Empty,
            Name: lawyer.User?.Name ?? string.Empty,
            Status: lawyer.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: lawyer.User?.NIF ?? string.Empty,
            ProfessionalRegister: lawyer.ProfessionalRegister,
            Phone: phoneStr,
            PhoneCountryCode: mainPhone != null ? mainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateLawyer(
        CreateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher,
        AuditLogFacade auditLog)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { message = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { message = "Password is required" });

        if (string.IsNullOrWhiteSpace(request.NIF) || request.NIF.Length != 9)
            return Results.BadRequest(new { message = "NIF must be a 9-character string" });

        if (string.IsNullOrWhiteSpace(request.ProfessionalRegister))
            return Results.BadRequest(new { message = "Professional register is required" });

        // Hash the password
        var hashedPassword = hasher.HashPassword(request.Password);

        // Create user and lawyer
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

        var lawyer = new Lawyer
        {
            ProfessionalRegister = request.ProfessionalRegister
        };

        // Save to database
        var newLawyer = await repo.Create(user, lawyer);

        // Log the creation
        await auditLog.AddCreateLawyerLogAsync(newLawyer.ID, newLawyer.ID);

        // Prepare response (include main phone if present)
        var createdMainPhone = newLawyer.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var createdPhoneStr = createdMainPhone != null ? createdMainPhone.Number : string.Empty;

        var response = new LawyerResponse(
            Id: newLawyer.ID,
            Email: newLawyer.User?.Email ?? string.Empty,
            Name: newLawyer.User?.Name ?? string.Empty,
            Status: UserStatus.ACTIVE,
            NIF: newLawyer.User?.NIF ?? string.Empty,
            ProfessionalRegister: newLawyer.ProfessionalRegister,
            Phone: createdPhoneStr,
            PhoneCountryCode: createdMainPhone != null ? createdMainPhone.CountryCode : (short?)null
        );

        return Results.Created($"/api/lawyers/{newLawyer.ID}", response);
    }

    private static async Task<IResult> DeleteLawyer(Guid id, ILawyerRepository repo, AuditLogFacade auditLog)
    {
        try
        {
            await repo.Delete(id);
            // Log the deletion
            await auditLog.AddDeleteLawyerLogAsync(id, id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { message = $"Lawyer with ID {id} not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("active"))
        {
            return Results.Conflict(new { message = "Lawyer has active/open cases and cannot be deleted" });
        }
    }

    private static async Task<IResult> UpdateLawyer(
        Guid id,
        UpdateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher,
        AuditLogFacade auditLog)
    {
        // Validate input - at least one field should be provided
        if (string.IsNullOrWhiteSpace(request.Name) && 
            string.IsNullOrWhiteSpace(request.Email) && 
            string.IsNullOrWhiteSpace(request.Password) && 
            string.IsNullOrWhiteSpace(request.ProfessionalRegister) &&
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

        var lawyerUpdates = new Lawyer
        {
            ProfessionalRegister = request.ProfessionalRegister ?? string.Empty
        };

        // Update in the repository
        var updatedLawyer = await repo.UpdateLawyerAndUser(id, lawyerUpdates, userUpdates);

        if (updatedLawyer == null)
            return Results.NotFound(new { message = $"Lawyer with ID {id} not found" });

        // Log the update
        var oldValues = new Dictionary<string, object?>
        {
            { "email", request.Email },
            { "name", request.Name },
            { "professionalRegister", request.ProfessionalRegister }
        };
        var newValues = new Dictionary<string, object?>
        {
            { "email", updatedLawyer.User?.Email },
            { "name", updatedLawyer.User?.Name },
            { "professionalRegister", updatedLawyer.ProfessionalRegister }
        };
        await auditLog.AddUpdateLawyerLogAsync(id, id, oldValues, newValues);

        // Prepare response (include main phone if present)
        var updatedMainPhone = updatedLawyer.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var updatedPhoneStr = updatedMainPhone != null ? updatedMainPhone.Number : string.Empty;

        var response = new LawyerResponse(
            Id: updatedLawyer.ID,
            Email: updatedLawyer.User?.Email ?? string.Empty,
            Name: updatedLawyer.User?.Name ?? string.Empty,
            Status: updatedLawyer.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: updatedLawyer.User?.NIF ?? string.Empty,
            ProfessionalRegister: updatedLawyer.ProfessionalRegister,
            Phone: updatedPhoneStr,
            PhoneCountryCode: updatedMainPhone != null ? updatedMainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }
}
