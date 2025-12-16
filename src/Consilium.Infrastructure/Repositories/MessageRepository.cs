using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Data;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Message> Create(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        
        // Reload to get navigation properties
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .FirstAsync(m => m.Id == message.Id);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetAll(string? search, int page, int limit, string? sortBy, string? sortOrder)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(m => 
                m.Subject.ToLower().Contains(search) || 
                m.Body.ToLower().Contains(search) ||
                m.Sender.Name.ToLower().Contains(search) ||
                m.Recipient.Name.ToLower().Contains(search) ||
                m.Process.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        // Basic sorting
        query = sortBy?.ToLower() switch
        {
            "subject" => sortOrder == "desc" ? query.OrderByDescending(m => m.Subject) : query.OrderBy(m => m.Subject),
            "date" => sortOrder == "desc" ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetByClientId(Guid clientId, int page, int limit)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .Where(m => m.SenderId == clientId || m.RecipientId == clientId) // Assuming Client ID matches User ID for now, logic might need adjustment if they are distinct
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetByLawyerId(Guid lawyerId, int page, int limit)
    {
         var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .Where(m => m.SenderId == lawyerId || m.RecipientId == lawyerId)
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetByProcessId(Guid processId, int page, int limit)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .Where(m => m.ProcessId == processId)
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

        return (items, totalCount);
    }
    
    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetByProcessName(string processName, int page, int limit)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .Where(m => m.Process.Name.ToLower().Contains(processName.ToLower()))
            .OrderByDescending(m => m.CreatedAt);
            
         var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

        return (items, totalCount);
    }

    public async Task<Message?> UpdateLawyer(int messageId, Guid newLawyerId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Process)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return null;

        // Process logic to determine which participant is the lawyer
        // The message participants are Sender and Recipient. One should match the keys.
        // Also need to check if the NEW lawyer exists (repo check can be done here or in endpoint, doing here for simpler transaction)
        
        // Actually, update the Message's SenderId or RecipientId depending on which one WAS the old lawyer
        // But first we should identify WHICH field corresponds to the lawyer
        // We can check against the Process.LawyerId (old value) or check roles?
        // Simpler: Check against message.Process.LawyerId
        
        // IMPORTANT: The requirements imply we are changing the lawyer OF THE MESSAGE.
        // Assuming the message was constrained to be between Process.Client and Process.Lawyer.
        // If we change the lawyer ID in the message, it might violate the constraint if the Process.LawyerId hasn't changed?
        // OR the user instruction implies just changing the ID on the message record itself.
        // "alter the lawyerID of a message to another lawyerID"
        
        // Let's check which participant ID matches the Process.LawyerId (the "old" lawyer)
        
        bool senderIsLawyer = message.SenderId == message.Process.LawyerId;
        bool recipientIsLawyer = message.RecipientId == message.Process.LawyerId;
        
        if (senderIsLawyer)
        {
            message.SenderId = newLawyerId;
        }
        else if (recipientIsLawyer)
        {
            message.RecipientId = newLawyerId;
        }
        else
        {
            // Fallback: Check if the sender or recipient is NOT the client
            if (message.SenderId != message.Process.ClientId)
                 message.SenderId = newLawyerId;
            else
                 message.RecipientId = newLawyerId;
        }
        
        await _context.SaveChangesAsync();
        
        // Reload to get new lawyer name
        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        await _context.Entry(message).Reference(m => m.Recipient).LoadAsync();
        
        return message;
    }

    public async Task MarkMessagesAsRead(Guid processId, Guid recipientId)
    {
        var unreadMessages = await _context.Messages
            .Where(m => m.ProcessId == processId && m.RecipientId == recipientId && m.ReadAt == null)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCount(Guid userId)
    {
        return await _context.Messages
            .CountAsync(m => m.RecipientId == userId && m.ReadAt == null);
    }

    public async Task<IEnumerable<UnreadProcessStats>> GetUnreadCountsByProcess(Guid userId)
    {
        return await _context.Messages
            .Where(m => m.RecipientId == userId && m.ReadAt == null)
            .GroupBy(m => new { m.ProcessId, m.Process.Name, m.Process.Number })
            .Select(g => new UnreadProcessStats(g.Key.ProcessId, g.Key.Name, g.Key.Number, g.Count()))
            .ToListAsync();
    }
}
