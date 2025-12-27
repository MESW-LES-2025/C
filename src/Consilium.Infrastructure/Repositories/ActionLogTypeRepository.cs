using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories
{
    public class ActionLogTypeRepository : IActionLogTypeRepository
    {
        private readonly AppDbContext _context;

        public ActionLogTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        // Search by name using case-insensitive comparison
        public async Task<ActionLogType?> GetByName(string name)
        {
            return await _context.ActionLogTypes
                .FirstOrDefaultAsync(alt => alt.Name.ToLower() == name.ToLower());
        }

        // Standard lookup by primary key
        public async Task<ActionLogType?> GetById(int id)
        {
            return await _context.ActionLogTypes.FindAsync(id);
        }
    }
}