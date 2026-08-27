using WebAPICourse.Models;

namespace WebAPICourse.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<ServiceResult<Category>> CreateCategoryAsync(Category category);
    }
}
