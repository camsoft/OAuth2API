using Microsoft.EntityFrameworkCore;
using WebAPICourse.Data;
using WebAPICourse.Models;

namespace WebAPICourse.Repositories
{
    // Repositories/CategoryRepository.cs
    //
    // Follows the same Repository pattern as ProductRepository: it's the only place
    // in the app that talks directly to AppDbContext for Category data.
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }
    }
}
