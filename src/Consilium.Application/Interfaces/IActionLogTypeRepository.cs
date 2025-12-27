using Consilium.Domain.Models;

namespace Consilium.Application.Interfaces
{
    public interface IActionLogTypeRepository
    {
        // Retrieves the log type by its unique name
        Task<ActionLogType?> GetByName(string name);

        // Retrieves the log type by its integer primary key
        Task<ActionLogType?> GetById(int id);
    }
}