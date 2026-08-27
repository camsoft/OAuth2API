using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPICourse.Models
{
    // This model is configured TWO ways for teaching purposes:
    //   1. Data Annotations (attributes below, e.g. [Key], [Required]) - quick and
    //      convenient, live right next to the property they describe.
    //   2. Fluent API (see AppDbContext.OnModelCreating) - configured separately from
    //      the model, more powerful/flexible, and the recommended approach for larger
    //      or more complex configurations.
    //
    // If both are used on the same property and they conflict, Fluent API wins -
    // it always takes precedence over Data Annotations.
    public class Product
    {
        // [Key] marks this as the primary key. EF Core would actually infer this
        // automatically because the property is named "Id", but it's shown here
        // explicitly so students can see how to mark a key when the convention
        // doesn't apply (e.g., a property named "ProductId" instead).
        [Key]
        public int Id { get; set; }

        // [Required] means this column cannot be NULL in the database.
        // [MaxLength] / [StringLength] map to nvarchar(200) instead of nvarchar(max).
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        // [Column] lets you control the exact database column type, similar to
        // HasColumnType(...) in the Fluent API.
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        // Foreign key property: every Product must belong to exactly one Category.
        // By convention, EF Core recognizes "CategoryId" as the FK for the Category
        // navigation property below. It's also configured explicitly via Fluent API
        // in AppDbContext.OnModelCreating.
        [Required]
        public int CategoryId { get; set; }

        // Navigation property: lets you write product.Category.Name instead of a
        // separate lookup query. This is null until explicitly loaded, e.g. via
        // .Include(p => p.Category) in the repository.
        public Category? Category { get; set; }
    }

}
