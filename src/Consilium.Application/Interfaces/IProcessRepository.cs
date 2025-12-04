using Consilium.Domain.Models;
namespace Consilium.Application.Interfaces;

public interface IProcessRepository
{
    Task<Process?> GetById(Guid id);

    Task<(List<Process> Processes, int TotalCount)> GetAll(
        string? search,
        int page,
        int limit,
        string? sortBy,
        string? sortOrder);

    Task<Process> Create(Process process);

    Task Update(Process process);

    Task Delete(Guid id);

    Task<(List<Process> Processes, int TotalCount)> GetByClientId(
        Guid clientId,
        string? search,
        int page,
        int limit,
        string? sortBy,
        string? sortOrder);

    Task<(List<Process> Processes, int TotalCount)> GetByLawyerId(
        Guid lawyerId,
        string? search,
        int page,
        int limit,
        string? sortBy,
        string? sortOrder);

    Task<List<Process>> GetByClientIdWithDocuments(Guid clientId);

    Task<List<Process>> GetByLawyerIdWithDocuments(Guid lawyerId);
}
