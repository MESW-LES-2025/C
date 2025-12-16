using Consilium.API.Dtos;
using Consilium.Application.Dtos;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Consilium.API.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/messages")
            .WithName("Messages")
            .WithOpenApi();

        group.MapGet("/", GetAllMessages)
            .WithName("GetAllMessages")
            .WithDescription("Retrieve all messages")
            .RequireAuthorization("Any");

        group.MapGet("/process/{processId:guid}", GetMessagesByProcess)
            .WithName("GetMessagesByProcess")
            .WithDescription("Retrieve all messages for a specific process")
            .RequireAuthorization("Any");

        group.MapGet("/lawyer/{lawyerId:guid}", GetMessagesByLawyer)
            .WithName("GetMessagesByLawyer")
            .WithDescription("Retrieve all messages for a specific lawyer")
            .RequireAuthorization("Any");

        group.MapGet("/client/{clientId:guid}", GetMessagesByClient)
            .WithName("GetMessagesByClient")
            .WithDescription("Retrieve all messages for a specific client")
            .RequireAuthorization("Any");

        group.MapGet("/process-name/{processName}", GetMessagesByProcessName)
            .WithName("GetMessagesByProcessName")
            .WithDescription("Retrieve all messages for a process by searching its name")
            .RequireAuthorization("Any");

        group.MapPost("/", CreateMessage)
            .WithName("CreateMessage")
            .WithDescription("Create a new message")
            .RequireAuthorization("Any");

        group.MapPatch("/{id:int}/lawyer", UpdateMessageLawyer)
            .WithName("UpdateMessageLawyer")
            .WithDescription("Update the lawyer of a message")
            .RequireAuthorization("Any");

        group.MapPut("/process/{processId:guid}/read", MarkMessagesAsRead)
            .WithName("MarkMessagesAsRead")
            .WithDescription("Mark all messages in a process as read for a recipient")
            .RequireAuthorization("Any");

        group.MapGet("/unread-count/{userId:guid}", GetUnreadCount)
            .WithName("GetUnreadCount")
            .WithDescription("Get count of unread messages for a user")
            .RequireAuthorization("Any");
    }

    private static async Task<IResult> GetAllMessages(
        IMessageRepository repo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortOrder = "desc")
    {
        var (messages, totalCount) = await repo.GetAll(search, page, limit, sortBy, sortOrder);
        var response = MapToResponse(messages);
        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    private static async Task<IResult> GetMessagesByProcess(
        Guid processId,
        IMessageRepository repo,
        IProcessRepository processRepo,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var process = await processRepo.GetById(processId);
        if (process == null)
            return Results.NotFound(new { message = $"Process with ID {processId} not found" });

        var (messages, totalCount) = await repo.GetByProcessId(processId, page, limit);
        var response = MapToResponse(messages);
        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    private static async Task<IResult> GetMessagesByLawyer(
        Guid lawyerId,
        IMessageRepository repo,
        ILawyerRepository lawyerRepo,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var lawyer = await lawyerRepo.GetById(lawyerId);
        if (lawyer == null)
            return Results.NotFound(new { message = $"Lawyer with ID {lawyerId} not found" });

        var (messages, totalCount) = await repo.GetByLawyerId(lawyerId, page, limit);
        var response = MapToResponse(messages);
        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    private static async Task<IResult> GetMessagesByClient(
        Guid clientId,
        IMessageRepository repo,
        IClientRepository clientRepo,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var client = await clientRepo.GetById(clientId);
        if (client == null)
            return Results.NotFound(new { message = $"Client with ID {clientId} not found" });

        var (messages, totalCount) = await repo.GetByClientId(clientId, page, limit);
        var response = MapToResponse(messages);
        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    private static async Task<IResult> GetMessagesByProcessName(
        string processName,
        IMessageRepository repo,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var (messages, totalCount) = await repo.GetByProcessName(processName, page, limit);
        var response = MapToResponse(messages);
        return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
    }

    private static async Task<IResult> CreateMessage(
        CreateMessageRequest request, 
        IMessageRepository repo, 
        IProcessRepository processRepo,
        IUserRepository userRepo)
    {
        // Validate that the process exists
        var process = await processRepo.GetById(request.ProcessId);
        if (process == null)
            return Results.BadRequest(new { message = $"Process with ID {request.ProcessId} not found" });

        // Validate that the sender exists
        var sender = await userRepo.GetById(request.SenderId);
        if (sender == null)
            return Results.BadRequest(new { message = $"Sender with ID {request.SenderId} not found" });

        // Validate that the recipient exists
        var recipient = await userRepo.GetById(request.RecipientId);
        if (recipient == null)
            return Results.BadRequest(new { message = $"Recipient with ID {request.RecipientId} not found" });

        // Validate that the sender and recipient are the Lawyer and Client of the process
        bool isSenderLawyer = request.SenderId == process.LawyerId;
        bool isSenderClient = request.SenderId == process.ClientId;
        bool isRecipientLawyer = request.RecipientId == process.LawyerId;
        bool isRecipientClient = request.RecipientId == process.ClientId;

        // The message participants must be the Process's Lawyer and Client (in either direction)
        // Case 1: Lawyer -> Client
        // Case 2: Client -> Lawyer
        bool isValid = (isSenderLawyer && isRecipientClient) || (isSenderClient && isRecipientLawyer);

        if (!isValid)
        {
            return Results.BadRequest(new { 
                message = "Message participants must be the Lawyer and Client associated with this process.",
                details = $"Process ClientId: {process.ClientId}, Process LawyerId: {process.LawyerId}. Request Sender: {request.SenderId}, Request Recipient: {request.RecipientId}"
            });
        }

        var message = new Message
        {
            SenderId = request.SenderId,
            RecipientId = request.RecipientId,
            ProcessId = request.ProcessId,
            Subject = request.Subject,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow
        };

        try 
        {
            var created = await repo.Create(message);
            return Results.Created($"/api/messages/{created.Id}", MapToResponseSingle(created));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = "Failed to create message", details = ex.Message });
        }
    }

    private static async Task<IResult> UpdateMessageLawyer(
        int id,
        UpdateMessageLawyerRequest request,
        IMessageRepository repo,
        ILawyerRepository lawyerRepo)
    {
        // 1. Verify existence of new lawyer
        var newLawyer = await lawyerRepo.GetById(request.NewLawyerId);
        if (newLawyer == null)
            return Results.BadRequest(new { message = $"New lawyer with ID {request.NewLawyerId} not found" });

        // 2. Call Repo Update
        var updatedMessage = await repo.UpdateLawyer(id, request.NewLawyerId);

        if (updatedMessage == null)
            return Results.NotFound(new { message = $"Message with ID {id} not found" });

        return Results.Ok(MapToResponseSingle(updatedMessage));
    }

    private static async Task<IResult> MarkMessagesAsRead(
        Guid processId,
        [FromBody] MarkAsReadRequest request,
        IMessageRepository repo)
    {
        await repo.MarkMessagesAsRead(processId, request.RecipientId);
        return Results.Ok(new { message = "Messages marked as read" });
    }

    private static async Task<IResult> GetUnreadCount(
        Guid userId,
        IMessageRepository repo)
    {
        var total = await repo.GetUnreadCount(userId);
        var byProcess = await repo.GetUnreadCountsByProcess(userId);
        return Results.Ok(new { total, byProcess });
    }

    private static IEnumerable<MessageResponse> MapToResponse(IEnumerable<Message> messages)
    {
        return messages.Select(MapToResponseSingle);
    }

    private static MessageResponse MapToResponseSingle(Message m)
    {
        return new MessageResponse(
            Id: m.Id,
            SenderId: m.SenderId,
            SenderName: m.Sender?.Name ?? "Unknown",
            RecipientId: m.RecipientId,
            RecipientName: m.Recipient?.Name ?? "Unknown",
            ProcessId: m.ProcessId,
            ProcessName: m.Process?.Name ?? "Unknown",
            Subject: m.Subject,
            Body: m.Body,
            CreatedAt: m.CreatedAt,
            ReadAt: m.ReadAt
        );
    }
}
