using Consilium.Domain.Models;
using Consilium.Application.Dtos;

namespace Consilium.Application.Interfaces;

public interface IMessageRepository
{
    Task<Message> Create(Message message);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetAll(string? search, int page, int limit, string? sortBy, string? sortOrder);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetByProcessId(Guid processId, int page, int limit);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetByLawyerId(Guid lawyerId, int page, int limit);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetByClientId(Guid clientId, int page, int limit);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetByProcessName(string processName, int page, int limit);
    Task<Message?> UpdateLawyer(int messageId, Guid newLawyerId);
    Task MarkMessagesAsRead(Guid processId, Guid recipientId);
    Task<int> GetUnreadCount(Guid userId);
    Task<IEnumerable<UnreadProcessStats>> GetUnreadCountsByProcess(Guid userId);
}
