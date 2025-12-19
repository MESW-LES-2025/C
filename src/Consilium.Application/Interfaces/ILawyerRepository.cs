using Consilium.Domain.Models;

namespace Consilium.Application.Interfaces
{
    public interface ILawyerRepository
    {
        Task<Lawyer?> GetById(Guid id);

        Task<(List<Lawyer> Lawyers, int TotalCount)> GetAll(
            string? search,
            string? status,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder);

        Task<Lawyer> Create(User user, Lawyer lawyer);
        Task Update(Lawyer lawyer);
        Task<Lawyer?> UpdateLawyerAndUser(Guid lawyerId, Lawyer lawyerUpdates, User userUpdates, bool? isActive = null, Guid? editorId = null);
        Task Delete(Guid id);
    }
}
