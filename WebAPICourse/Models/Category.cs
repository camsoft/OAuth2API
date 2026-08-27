using System.ComponentModel.DataAnnotations;

namespace WebAPICourse.Models
{
    // Category represents the "one" side of a one-to-many relationship with Product:
    // one Category can have many Products, but each Product belongs to exactly one Category.
    //
    // Like Product, this model uses Data Annotations here, with the relationship itself
    // configured via Fluent API in AppDbContext.OnModelCreating.
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Navigation property: the collection of Products that belong to this Category.
        // This isn't a database column - EF Core uses it (together with the Fluent API
        // configuration) to let you write queries like category.Products.
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
