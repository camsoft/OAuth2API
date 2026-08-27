namespace WebAPICourse.Models
{
    // Models/ProductResponseDto.cs
    //
    // DTO = "Data Transfer Object". Instead of returning the raw Product entity
    // (which has a Category navigation property that could point back to a
    // collection of Products - a circular reference that breaks JSON serialization),
    // we return this flattened, purpose-built shape from our API endpoints.
    //
    // This is also a chance to control exactly what data is exposed to clients -
    // for example, we only expose the Category's Name here, not the whole Category
    // object with its own list of Products.
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
