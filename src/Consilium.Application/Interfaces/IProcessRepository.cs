using Consilium.Domain.Models;

namespace Consilium.Application.Interfaces
{
    public interface IProcessRepository
    {
        #region CRUD Operations
        // Methods in this region follow the CRUD order: Create, Read (All then Single), Update, Delete.

        // Creates a new process record in the database
        Task<Process> Create(Process process);

        // Retrieves all processes with support for search, pagination, and dynamic sorting
        Task<(List<Process> Processes, int TotalCount)> GetAll(
            string? search,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder);

        // Retrieves a specific process by its unique identifier
        Task<Process?> GetById(Guid id);

        // Updates an existing process record
        Task Update(Process process);

        // Deletes a process record by its unique identifier
        Task Delete(Guid id);
        #endregion

        #region Relationship Based Operations
        // Methods in this region are sorted alphabetically.

        // Dissociates all processes currently assigned to a specific lawyer
        Task DissociateProcessesFromLawyer(Guid id, Guid editorId);

        // Retrieves processes for a specific client with search, pagination, and sorting
        Task<(List<Process> Processes, int TotalCount)> GetProcessesByClientId(
            Guid clientId,
            string? search,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder);

        // Retrieves processes for a specific client including their document metadata
        Task<List<Process>> GetProcessesByClientIdWithDocuments(Guid clientId);

        // Retrieves processes for a specific lawyer with search, pagination, and sorting
        Task<(List<Process> Processes, int TotalCount)> GetProcessesByLawyerId(
            Guid lawyerId,
            string? search,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder);

        // Retrieves processes for a specific lawyer including their document metadata
        Task<List<Process>> GetProcessesByLawyerIdWithDocuments(Guid lawyerId);
        #endregion
    }
}