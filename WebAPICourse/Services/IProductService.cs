using WebAPICourse.Models;

namespace WebAPICourse.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync();
        Task<ProductResponseDto?> GetProductByIdAsync(int id);
        Task<ServiceResult<ProductResponseDto>> CreateProductAsync(Product product);
        Task<ServiceResult<bool>> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
    }
}
