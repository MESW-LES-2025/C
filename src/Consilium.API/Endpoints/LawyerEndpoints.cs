using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Domain.Enums;
using Consilium.API.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Consilium.API.Endpoints;

public static class LawyerEndpoints
{
    #region Route Mapping

    public static void MapLawyerEndpoints(this WebApplication app)
    {
        // Removi o .WithName("Lawyers") daqui para evitar o conflito global
        var group = app.MapGroup("/api/lawyers")
            .WithOpenApi()
            .RequireAuthorization("AdminOrLawyer");

        group.MapPost("/", CreateLawyer)
            .WithName("CreateLawyer"); // Nome único

        group.MapGet("/", GetAllLawyers)
            .WithName("GetAllLawyers"); // Nome único

        group.MapGet("/{id:guid}", GetLawyerById)
            .WithName("GetLawyerById");

        group.MapPatch("/{id:guid}", UpdateLawyer)
            .WithName("UpdateLawyer");

        group.MapDelete("/{id:guid}", DeleteLawyer)
            .WithName("DeleteLawyer");
    }

    #endregion

    #region Action Handlers - CRUD Operations

    /// <summary>
    /// Handles the registration of a new lawyer, including user account and phone numbers.
    /// </summary>
    private static async Task<IResult> CreateLawyer(
        CreateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher,
        ClaimsPrincipal userClaims)
    {

        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { message = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.NIF) || request.NIF.Length != 9)
            return Results.BadRequest(new { message = "Invalid NIF" });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hasher.HashPassword(request.Password),
            Name = request.Name,
            NIF = request.NIF,
            IsActive = true
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.Phones.Add(new Phone
            {
                ID = Guid.NewGuid(),
                Number = request.PhoneNumber,
                CountryCode = request.PhoneCountryCode ?? 351,
                IsMain = true
            });
        }

        var lawyer = new Lawyer { ProfessionalRegister = request.ProfessionalRegister };
        var newLawyer = await repo.Create(user, lawyer, editorId);

        return Results.Created($"/api/lawyers/{newLawyer.ID}", MapToLawyerResponse(newLawyer));
    }

    /// <summary>
    /// Retrieves a paginated list of all lawyers with optional search and status filtering.
    /// </summary>
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
        var response = lawyers.Select(MapToLawyerResponse);

        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    /// <summary>
    /// Retrieves a single lawyer profile. This triggers the profile-view audit log.
    /// </summary>
    private static async Task<IResult> GetLawyerById(
        Guid id,
        ILawyerRepository repo,
        ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        try
        {
            // Using GetLawyerProfileById to ensure the access is audited
            var lawyer = await repo.GetLawyerProfileById(id, editorId);
            return Results.Ok(MapToLawyerResponse(lawyer!));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing lawyer's details. Modification history is tracked in the repository.
    /// </summary>
    private static async Task<IResult> UpdateLawyer(
        Guid id,
        UpdateLawyerRequest request,
        ILawyerRepository repo,
        IPasswordHasher hasher,
        ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        if (!IsUpdateDataProvided(request))
            return Results.BadRequest(new { message = "No data provided for update" });

        var (userUpdates, lawyerUpdates) = MapUpdateEntities(request, hasher);

        var updatedLawyer = await repo.UpdateLawyerAndUser(id, lawyerUpdates, userUpdates, editorId, request.IsActive);

        return updatedLawyer is null
            ? Results.NotFound(new { message = $"Lawyer {id} not found" })
            : Results.Ok(MapToLawyerResponse(updatedLawyer));
    }

    /// <summary>
    /// Permanently deletes a lawyer and their associated data.
    /// </summary>
    private static async Task<IResult> DeleteLawyer(Guid id, ILawyerRepository repo, ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        try
        {
            await repo.Delete(id, editorId);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Maps the Domain model to the Response DTO for the Client/Frontend.
    /// </summary>
    private static LawyerResponse MapToLawyerResponse(Lawyer lawyer)
    {
        var mainPhone = lawyer.User?.Phones?.FirstOrDefault(p => p.IsMain);

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

    /// <summary>
    /// Extracts the user ID from the JWT access token claims.
    /// </summary>
    private static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Prepares domain entities with provided update data.
    /// </summary>
    private static (User userUpdates, Lawyer lawyerUpdates) MapUpdateEntities(UpdateLawyerRequest request, IPasswordHasher hasher)
    {
        var userUpdates = new User
        {
            Name = request.Name ?? string.Empty,
            Email = request.Email ?? string.Empty,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password) ? hasher.HashPassword(request.Password) : string.Empty,
            NIF = request.NIF ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            userUpdates.Phones.Add(new Phone { Number = request.PhoneNumber, IsMain = true });
        }

        var lawyerUpdates = new Lawyer { ProfessionalRegister = request.ProfessionalRegister ?? string.Empty };
        return (userUpdates, lawyerUpdates);
    }

    /// <summary>
    /// Validates if at least one field is being updated.
    /// </summary>
    private static bool IsUpdateDataProvided(UpdateLawyerRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Name) || !string.IsNullOrWhiteSpace(request.Email) ||
               request.IsActive.HasValue || !string.IsNullOrWhiteSpace(request.ProfessionalRegister) ||
               !string.IsNullOrWhiteSpace(request.Password) || !string.IsNullOrWhiteSpace(request.PhoneNumber);
    }

    #endregion
}