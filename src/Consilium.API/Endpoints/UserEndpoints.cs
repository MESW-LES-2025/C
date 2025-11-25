using Consilium.Application.Interfaces;
using Consilium.API.Dtos;
using Consilium.Domain.Enums;

namespace Consilium.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithName("Users")
            .WithOpenApi();
        group.RequireAuthorization("AdminOrLawyer");

        group.MapGet("/", GetAllUsers)
            .WithName("GetAllUsers")
            .WithDescription("Retrieve all users");

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithDescription("Retrieve a user by ID");

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .WithDescription("Delete a user and anonymize their audit logs (GDPR right to be forgotten)");
    }

    private static async Task<IResult> GetAllUsers(IUserRepository repo)
    {
        var users = await repo.GetAll();
        var response = users.Select(u => new UserResponse(
            Id: u.ID,
            Email: u.Email,
            Name: u.Name,
            Status: u.IsActive ? UserStatus.ACTIVE : UserStatus.INACTIVE
        ));
        return Results.Ok(response);
    }

    private static async Task<IResult> GetUserById(Guid id, IUserRepository repo)
    {
        var user = await repo.GetById(id);

        if (user is null)
            return Results.NotFound(new { message = $"User with ID {id} not found" });

        var response = new UserResponse(
            Id: user.ID,
            Email: user.Email,
            Name: user.Name,
            Status: user.IsActive ? UserStatus.ACTIVE : UserStatus.INACTIVE
        );

        return Results.Ok(response);
    }

    /// <summary>
    /// Deletes a user and all related data (Client, Phone records).
    /// This method is reusable by User, Advocate, and Admin delete endpoints.
    /// </summary>
    private static async Task<IResult> DeleteUserAsync(Guid id, IClientRepository clientRepo)
    {
        try
        {
            // Use the ClientRepository delete method which handles User deletion
            // and cascade deletes to Client and Phone records
            await clientRepo.Delete(id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { message = $"User with ID {id} not found" });
        }
    }

    /// <summary>
    /// Reusable delete method that can be called from User, Advocate, and Admin endpoints.
    /// Handles the deletion logic consistently across all entity types.
    /// </summary>
    public static async Task<IResult> DeleteUserAndDependents(Guid id, IClientRepository clientRepo)
    {
        return await DeleteUserAsync(id, clientRepo);
    }

    private static async Task<IResult> DeleteUser(Guid id, IClientRepository clientRepo)
    {
        // TODO: anonymize logs before deletion per GDPR right to be forgotten
        return await DeleteUserAndDependents(id, clientRepo);
    }
}
