using Consilium.Domain.Models;

namespace Consilium.Application.Interfaces;

public interface ILawyerRepository
{
    #region CRUD Operations

    /// <summary>
    /// Registers a new lawyer and their associated user account.
    /// </summary>
    Task<Lawyer> Create(User user, Lawyer lawyer, Guid editorId);

    /// <summary>
    /// Performs a simple database lookup for internal validations without audit logs.
    /// </summary>
    Task<Lawyer?> GetLawyerById(Guid id);

    /// <summary>
    /// Retrieves the full profile for display and records the access in audit logs.
    /// </summary>
    Task<Lawyer?> GetLawyerProfileById(Guid id, Guid editorId);

    /// <summary>
    /// Updates both Lawyer and User records, tracking changes via editorId.
    /// </summary>
    Task<Lawyer?> UpdateLawyerAndUser(
        Guid id,
        Lawyer lawyerUpdates,
        User userUpdates,
        Guid editorId,
        bool? isActive);

    /// <summary>
    /// Permanently removes a lawyer and their associated user data from the system.
    /// </summary>
    Task Delete(Guid id, Guid editorId);

    #endregion

    #region Search Operations

    /// <summary>
    /// Returns a paginated list of lawyers with optional search and status filters.
    /// </summary>
    Task<(IEnumerable<Lawyer> lawyers, int totalCount)> GetAll(
        string? search,
        string? status,
        int page,
        int limit,
        string? sortBy,
        string? sortOrder);

    #endregion
}