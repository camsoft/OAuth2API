using WebAPICourse.Models;
using WebAPICourse.Repositories;

namespace WebAPICourse.Services
{
    // Services/CategoryService.cs
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ServiceResult<Category>> CreateCategoryAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return ServiceResult<Category>.Fail("Category name is required.");
            }

            var created = await _repository.CreateAsync(category);
            return ServiceResult<Category>.Ok(created);
        }
    }
}
