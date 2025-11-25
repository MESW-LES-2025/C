using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Domain.Enums;
using Consilium.API.Dtos;
using Microsoft.AspNetCore.Mvc;
using Consilium.Infrastructure.Services;

namespace Consilium.API.Endpoints;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients")
            .WithName("Clients")
            .WithOpenApi();
        group.RequireAuthorization("AdminOrLawyer");

        group.MapGet("/", GetAllClients)
            .WithName("GetAllClients")
            .WithDescription("Retrieve all clients");

        group.MapGet("/{id:guid}", GetClientById)
            .WithName("GetClientById")
            .WithDescription("Retrieve a client by ID");

        group.MapPost("/", CreateClient)
            .WithName("CreateClient")
            .WithDescription("Create a new client");

        group.MapDelete("/{id:guid}", DeleteClient)
            .WithName("DeleteClient")
            .WithDescription("Inactivate a client");

        group.MapPatch("/{id:guid}", UpdateClient)
            .WithName("UpdateClient")
            .WithDescription("Update client and user information");
    }

    private static async Task<IResult> GetAllClients(
        IClientRepository repo,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        var (clients, totalCount) = await repo.GetAll(search, status, page, limit, sortBy, sortOrder);

        var response = clients.Select(c => {
            var mainPhone = c.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
            var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;
            return new ClientResponse(
                Id: c.ID,
                Email: c.User?.Email ?? string.Empty,
                Name: c.User?.Name ?? string.Empty,
                Status: c.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
                NIF: c.User?.NIF ?? string.Empty,
                Address: c.Address,
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


    private static async Task<IResult> GetClientById(Guid id, IClientRepository repo)
    {
        var client = await repo.GetById(id);
        
        if (client is null)
            return Results.NotFound(new { message = $"Client with ID {id} not found" });

        var mainPhone = client.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var phoneStr = mainPhone != null ? mainPhone.Number : string.Empty;

        var response = new ClientResponse(
            Id: client.ID,
            Email: client.User?.Email ?? string.Empty,
            Name: client.User?.Name ?? string.Empty,
            Status: client.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: client.User?.NIF ?? string.Empty,
            Address: client.Address,
            Phone: phoneStr,
            PhoneCountryCode: mainPhone != null ? mainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateClient(
        CreateClientRequest request,
        IClientRepository repo,
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

        // Create user and client
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

        var client = new Client
        {
            Address = request.Address ?? string.Empty
        };

        // Save to database
        var newClient = await repo.Create(user, client);

        // Prepare response (include main phone if present)
        var createdMainPhone = newClient.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var createdPhoneStr = createdMainPhone != null ? createdMainPhone.Number : string.Empty;

        var response = new ClientResponse(
            Id: newClient.ID,
            Email: newClient.User?.Email ?? string.Empty,
            Name: newClient.User?.Name ?? string.Empty,
            Status: UserStatus.ACTIVE,
            NIF: newClient.User?.NIF ?? string.Empty,
            Address: newClient.Address,
            Phone: createdPhoneStr,
            PhoneCountryCode: createdMainPhone != null ? createdMainPhone.CountryCode : (short?)null
        );

        return Results.Created($"/api/clients/{newClient.ID}", response);
    }

    private static async Task<IResult> DeleteClient(Guid id, IClientRepository repo)
    {
        try
        {
            await repo.Delete(id);
            //await auditLog.AddDeleteClientLogAsync(id, id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { message = $"Client with ID {id} not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("active"))
        {
            return Results.Conflict(new { message = "Client has active/open cases and cannot be deleted" });
        }
    }

    private static async Task<IResult> UpdateClient(
        Guid id,
        UpdateClientRequest request,
        IClientRepository repo,
        IPasswordHasher hasher)
    {
        // Validate input - at least one field should be provided
        if (string.IsNullOrWhiteSpace(request.Name) && 
            string.IsNullOrWhiteSpace(request.Email) && 
            string.IsNullOrWhiteSpace(request.Password) && 
            string.IsNullOrWhiteSpace(request.Address) &&
            string.IsNullOrWhiteSpace(request.NIF) &&
            string.IsNullOrWhiteSpace(request.PhoneNumber) &&
            !request.IsActive.HasValue)
        {
            return Results.BadRequest(new { message = "At least one field must be provided for update" });
        }

        // If NIF is provided, ensure basic length validation
        if (!string.IsNullOrWhiteSpace(request.NIF) && request.NIF.Length != 9)
        {
            return Results.BadRequest(new { message = "NIF must be a 9-character string" });
        }

        // Prepare the update data
        var userUpdates = new User
        {
            Name = request.Name ?? string.Empty,
            Email = request.Email ?? string.Empty,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password) ? hasher.HashPassword(request.Password) : string.Empty,
            NIF = request.NIF ?? string.Empty
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

        var clientUpdates = new Client
        {
            Address = request.Address ?? string.Empty
        };

        // Fetch existing state for "before" snapshot and build old snapshot BEFORE update
        var existingClient = await repo.GetById(id);
        if (existingClient == null)
            return Results.NotFound(new { message = $"Client with ID {id} not found" });
        // Build old snapshot from existing state (pre-update)
        var oldSnapshot = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            ClientID = existingClient.ID,
            UserID = existingClient.User?.ID,
            UserName = existingClient.User?.Name,
            UserEmail = existingClient.User?.Email,
            UserNIF = existingClient.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = existingClient.User?.IsActive,
            ClientAddress = existingClient.Address,
            Phones = existingClient.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        // Update in the repository
        var updatedClient = await repo.UpdateClientAndUser(id, clientUpdates, userUpdates, request.IsActive);

        if (updatedClient == null)
            return Results.NotFound(new { message = $"Client with ID {id} not found" });


        var newSnapshot = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            ClientID = updatedClient.ID,
            UserID = updatedClient.User?.ID,
            UserName = updatedClient.User?.Name,
            UserEmail = updatedClient.User?.Email,
            UserNIF = updatedClient.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = updatedClient.User?.IsActive,
            ClientAddress = updatedClient.Address,
            Phones = updatedClient.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        //await auditLog.AddUpdateClientLogAsync(id, id, oldSnapshot, newSnapshot);

        // Prepare response (include main phone if present)
        var updatedMainPhone = updatedClient.User?.Phones?.FirstOrDefault(p => p.IsMain == true);
        var updatedPhoneStr = updatedMainPhone != null ? updatedMainPhone.Number : string.Empty;

        var response = new ClientResponse(
            Id: updatedClient.ID,
            Email: updatedClient.User?.Email ?? string.Empty,
            Name: updatedClient.User?.Name ?? string.Empty,
            Status: updatedClient.User?.IsActive == true ? UserStatus.ACTIVE : UserStatus.INACTIVE,
            NIF: updatedClient.User?.NIF ?? string.Empty,
            Address: updatedClient.Address,
            Phone: updatedPhoneStr,
            PhoneCountryCode: updatedMainPhone != null ? updatedMainPhone.CountryCode : (short?)null
        );

        return Results.Ok(response);
    }
}
