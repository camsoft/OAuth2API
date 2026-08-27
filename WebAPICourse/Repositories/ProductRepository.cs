using Microsoft.EntityFrameworkCore;
using WebAPICourse.Data;
using WebAPICourse.Models;

namespace WebAPICourse.Repositories
{
    // Repositories/ProductRepository.cs
    //
    // This repository now talks to a real SQL Server database via EF Core's AppDbContext,
    // instead of an in-memory List<Product>. Notice the shape of the interface hasn't changed -
    // that's the benefit of the Repository pattern: the Service/Controller layers don't need
    // to know or care that the data source changed.
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        // The DbContext is injected by the built-in dependency injection container.
        // It's registered as "Scoped" in Program.cs, meaning one instance is created
        // per HTTP request - this is important because DbContext is not thread-safe
        // and is designed to be short-lived.
        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            // ToListAsync() executes the query against the database asynchronously
            // and materializes the results into a List<Product>.
            //
            // .Include(p => p.Category) tells EF Core to eager-load the related
            // Category in the same query (via a SQL JOIN), instead of leaving the
            // Category navigation property null. Without this, Category would only
            // be populated if it happened to already be tracked in memory.
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            // FindAsync can't be combined with Include, so we use FirstOrDefaultAsync
            // here instead to eager-load the related Category.
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            // AddAsync stages the new entity for insertion; nothing is sent to the database
            // until SaveChangesAsync is called. EF Core will populate product.Id automatically
            // once saved, because Id is configured as the primary key (auto-increment by default).
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing is null)
            {
                return false;
            }

            // Because "existing" is being tracked by the DbContext, changing its properties
            // is enough - EF Core will detect the changes and generate the correct UPDATE
            // statement when SaveChangesAsync is called.
            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.CategoryId = product.CategoryId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing is null)
            {
                return false;
            }

            _context.Products.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
