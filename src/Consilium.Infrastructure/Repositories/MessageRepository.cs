using Consilium.Domain.Models;
using Consilium.Application.Interfaces;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessagesForUserAsync(Guid userId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.RecipientId == user2Id) ||
                            (m.SenderId == user2Id && m.RecipientId == user1Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesByProcessIdAsync(Guid processId)
        {
            return await _context.Messages
                .Where(m => m.ProcessId == processId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
