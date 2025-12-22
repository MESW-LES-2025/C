using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Domain.Enums;
using Consilium.API.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Consilium.API.Endpoints;

public static class LawyerEndpoints
{
    public static void MapLawyerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/lawyers")
            .WithName("Lawyers")
            .WithOpenApi();
        group.RequireAuthorization("AdminOrLawyer");

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

        var response = lawyers.Select(l =>
        {
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

        return Results.Ok(new
        {
            data = response,
            meta = new
            {
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
        {
            return Results.NotFound(new { message = $"Lawyer with ID {id} not found" });
        }

        // Using the existing private mapping method to standardize the response
        return Results.Ok(MapToLawyerResponse(lawyer));
    }

    private static async Task<IResult> CreateLawyer(
        CreateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher)
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

    private static async Task<IResult> DeleteLawyer(Guid id, ILawyerRepository repo)
    {
        try
        {
            await repo.Delete(id);
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

    private static async Task<(Guid id, IResult? error)> ValidateEditor(
        ClaimsPrincipal userClaims,
        ILawyerRepository repo)
    {
        // 1. Extract the user ID from the Token claims.
        // Use .Trim() to ensure no accidental whitespace causes a parsing failure.
        var userIdClaim = userClaims?.FindFirst("user_id")?.Value?.Trim();

        // 2. Check if the claim exists at all
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return (Guid.Empty, Results.Unauthorized());
        }

        // 3. Check if the value is a valid Guid format
        if (!Guid.TryParse(userIdClaim, out var editorId))
        {
            return (Guid.Empty, Results.BadRequest(new { message = "Operation aborted: Malformed User ID in Token." }));
        }

        // Return the editor ID and null for the error if everything is valid
        return (editorId, null);
    }


    private static IResult? ValidateUpdateRequest(UpdateLawyerRequest request)
    {
        // 1. Check if at least one field is provided for update
        bool hasData = !string.IsNullOrWhiteSpace(request.Name) ||
                       !string.IsNullOrWhiteSpace(request.Email) ||
                       !string.IsNullOrWhiteSpace(request.Password) ||
                       !string.IsNullOrWhiteSpace(request.ProfessionalRegister) ||
                       !string.IsNullOrWhiteSpace(request.NIF) ||
                       !string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                       request.IsActive.HasValue;

        if (!hasData)
        {
            return Results.BadRequest(new { message = "At least one field must be provided for update" });
        }

        // 2. If NIF is provided, ensure basic length validation
        if (!string.IsNullOrWhiteSpace(request.NIF) && request.NIF.Length != 9)
        {
            return Results.BadRequest(new { message = "NIF must be a 9-character string" });
        }

        // Return null if all validations pass
        return null;
    }


    private static (User userUpdates, Lawyer lawyerUpdates) MapUpdateEntities(UpdateLawyerRequest request, IPasswordHasher hasher)
    {
        // 1. Create the User update container
        var userUpdates = new User
        {
            Name = request.Name ?? string.Empty,
            Email = request.Email ?? string.Empty,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                ? hasher.HashPassword(request.Password)
                : string.Empty,
            NIF = request.NIF ?? string.Empty
        };

        // 2. Attach phone info if provided in the request
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            userUpdates.Phones.Add(new Phone
            {
                ID = Guid.NewGuid(),
                Number = request.PhoneNumber,
                CountryCode = request.PhoneCountryCode ?? 351,
                IsMain = request.PhoneIsMain ?? true
            });
        }

        // 3. Create the Lawyer update container
        var lawyerUpdates = new Lawyer
        {
            ProfessionalRegister = request.ProfessionalRegister ?? string.Empty
        };

        return (userUpdates, lawyerUpdates);
    }


    private static LawyerResponse MapToLawyerResponse(Lawyer lawyer)
    {
        // Identify the main phone record if it exists
        var mainPhone = lawyer.User?.Phones?.FirstOrDefault(p => p.IsMain == true);

        // Map the domain entity to the DTO (Data Transfer Object)
        return new LawyerResponse(
            Id: lawyer.ID,
            Email: lawyer.User?.Email ?? string.Empty,
            Name: lawyer.User?.Name ?? string.Empty,
            Status: lawyer.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: lawyer.User?.NIF ?? string.Empty,
            ProfessionalRegister: lawyer.ProfessionalRegister,
            Phone: mainPhone?.Number ?? string.Empty,
            PhoneCountryCode: mainPhone?.CountryCode
        );
    }



    private static async Task<IResult> UpdateLawyer(
        Guid id,
        UpdateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher,
        ClaimsPrincipal userClaims)
    {

        // Validate editor user from token claims
        var (editorId, errorResult) = await ValidateEditor(userClaims, repo);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Validate input request
        var validationError = ValidateUpdateRequest(request);
        if (validationError != null)
        {
            return validationError;
        }

        // Prepare entities for update
        var (userUpdates, lawyerUpdates) = MapUpdateEntities(request, hasher);

        // Update in the repository (pass through optional IsActive flag)
        var updatedLawyer = await repo.UpdateLawyerAndUser(id, lawyerUpdates, userUpdates, request.IsActive, editorId);
        if (updatedLawyer == null)
        {
            return Results.NotFound(new { message = $"Lawyer with ID {id} not found" });
        }

        // Return the formatted response
        return Results.Ok(MapToLawyerResponse(updatedLawyer));
    }
}
