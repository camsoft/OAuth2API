using WebAPICourse.Models;
using WebAPICourse.Repositories;
using WebAPICourse.Services;
using System.Collections.Generic;
using System.Linq;

namespace WebAPICourse.Services
{
    // Services/ProductService.cs
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
        {
            var products = await _repository.GetAllAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            return product is null ? null : MapToDto(product);
        }

        public async Task<ServiceResult<ProductResponseDto>> CreateProductAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return ServiceResult<ProductResponseDto>.Fail("Product name is required.");
            }

            if (product.Price <= 0)
            {
                return ServiceResult<ProductResponseDto>.Fail("Product price must be greater than zero.");
            }

            if (product.StockQuantity < 0)
            {
                return ServiceResult<ProductResponseDto>.Fail("Stock quantity cannot be negative.");
            }

            var created = await _repository.CreateAsync(product);

            // Re-fetch with the Category included so the returned DTO has CategoryName
            // populated, instead of relying on the (possibly not-loaded) navigation
            // property on the just-created entity.
            var withCategory = await _repository.GetByIdAsync(created.Id);
            return ServiceResult<ProductResponseDto>.Ok(MapToDto(withCategory!));
        }

        public async Task<ServiceResult<bool>> UpdateProductAsync(int id, Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return ServiceResult<bool>.Fail("Product name is required.");
            }

            if (product.Price <= 0)
            {
                return ServiceResult<bool>.Fail("Product price must be greater than zero.");
            }

            product.Id = id;
            var updated = await _repository.UpdateAsync(product);

            return updated
                ? ServiceResult<bool>.Ok(true)
                : ServiceResult<bool>.Fail($"Product with ID {id} was not found.");
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Maps a Product entity (with its Category navigation property loaded) to the
        // flattened ProductResponseDto shape returned by the API. Centralizing the
        // mapping here means every method that returns products stays consistent.
        private static ProductResponseDto MapToDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty
            };
        }

    }

}
