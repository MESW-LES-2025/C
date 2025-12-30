using Consilium.Domain.Models;

namespace Consilium.Application.Interfaces;

public interface IProcessRepository
{
    #region CRUD Operations

    /// <summary>
    /// Persists a new process and records the creation audit log.
    /// </summary>
    Task<Process> Create(Process process, Guid editorId);

    /// <summary>
    /// Updates an existing process and records the differential audit log.
    /// </summary>
    Task Update(Process process, Guid editorId);

    /// <summary>
    /// Removes a process and records the final state before deletion.
    /// </summary>
    Task Delete(Guid id, Guid editorId);

    /// <summary>
    /// Retrieves a paginated list of processes with search and sorting support.
    /// </summary>
    Task<(List<Process> Processes, int TotalCount)> GetAll(
        string? search, int page, int limit, string? sortBy, string? sortOrder);

    /// <summary>
    /// Lightweight retrieval for internal system checks. No audit log.
    /// </summary>
    Task<Process?> GetProcessById(Guid id);

    /// <summary>
    /// Full detail retrieval including all relations and document metadata.
    /// Triggers a READ audit log.
    /// </summary>
    Task<Process?> GetProcessDetailById(Guid id, Guid editorId);

    #endregion

    #region Relationship Operations

    /// <summary>
    /// Mass updates processes to remove lawyer association with batch auditing.
    /// </summary>
    Task DissociateProcessesFromLawyer(Guid id, Guid editorId);

    /// <summary>
    /// Retrieves processes for a specific client.
    /// Optionally includes document metadata projection.
    /// </summary>
    Task<List<Process>> GetProcessesByClient(Guid clientId, bool includeDocuments = false);

    /// <summary>
    /// Retrieves processes assigned to a specific lawyer.
    /// Optionally includes document metadata projection.
    /// </summary>
    Task<List<Process>> GetProcessesByLawyer(Guid lawyerId, bool includeDocuments = false);

    /// <summary>
    /// Paged retrieval for client-specific process lists.
    /// </summary>
    Task<(List<Process> Processes, int TotalCount)> GetProcessesByClientId(
        Guid clientId, string? search, int page, int limit, string? sortBy, string? sortOrder);

    /// <summary>
    /// Paged retrieval for lawyer-specific process lists.
    /// </summary>
    Task<(List<Process> Processes, int TotalCount)> GetProcessesByLawyerId(
        Guid lawyerId, string? search, int page, int limit, string? sortBy, string? sortOrder);

    #endregion
}