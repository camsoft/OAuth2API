using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using WebAPICourse.Models;

namespace WebAPICourse.Data
{
    // The DbContext is the bridge between our C# objects (like Product) and the database.
    // EF Core uses this class to know which tables exist, how to query them, and how to save changes.
    //
    // Think of DbContext as representing a single "session" with the database - it tracks
    // changes to the entities you load, and translates LINQ queries into SQL for you.
    public class AppDbContext : DbContext
    {
        // The constructor takes DbContextOptions, which is configured in Program.cs
        // (this is where we tell EF Core which database provider and connection string to use).
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // A DbSet<T> represents a table in the database. EF Core will map this to a
        // "Products" table by convention (pluralized class name).
        public DbSet<Product> Products => Set<Product>();

        // A second table for our new Category entity.
        public DbSet<Category> Categories => Set<Category>();

        // A table of user accounts used to authenticate against the API and issue JWTs.
        // Unlike Products/Categories, demo users are NOT seeded here via HasData - a
        // password hash needs to be generated at runtime (PasswordHasher uses a random
        // salt), so seeding happens once at app startup instead (see Program.cs).
        public DbSet<User> Users => Set<User>();

        // OnModelCreating lets us fine-tune how EF Core maps our classes to database tables.
        // This is also where we can provide "seed data" - rows that should always exist
        // whenever the database is created via a migration.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Product entity/table explicitly (optional, but good practice
            // for beginners to see how column types/constraints can be configured).
            //
            // NOTE: The Product class also has Data Annotations (e.g. [Key], [Required],
            // [MaxLength]) applied directly to its properties - see Models/Product.cs.
            // This is intentional so you can compare both configuration styles side by
            // side. Where the same thing is configured in both places (like here), the
            // Fluent API below takes precedence.
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
            });

            // Configure the Category entity/table.
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(500);
            });

            // Configure the User entity/table.
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            });

            // Configure the one-to-many relationship: a Category has many Products,
            // and each Product belongs to exactly one Category (required FK).
            //
            // OnDelete(DeleteBehavior.Restrict) prevents deleting a Category while it
            // still has Products pointing at it - the database will reject the delete
            // instead of silently cascading and removing all its Products too.
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data for Categories - inserted before Products so the FK values
            // referenced below (CategoryId) are valid.
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Peripherals", Description = "Input devices such as mice and keyboards" },
                new Category { Id = 2, Name = "Accessories", Description = "Add-ons and connectivity accessories" },
                new Category { Id = 3, Name = "Storage", Description = "Devices for storing and transferring data" }
            );

            // Seed data: these rows will be inserted automatically when the "InitialCreate"
            // migration is applied. This mirrors the SQL scripts in the SqlScripts folder,
            // so whether you use EF Migrations or run the .sql scripts manually, you end up
            // with the same starting data.
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 24.99m, StockQuantity = 150, CategoryId = 1 },
                new Product { Id = 2, Name = "Mechanical Keyboard", Description = "RGB backlit mechanical keyboard", Price = 89.99m, StockQuantity = 75, CategoryId = 1 },
                new Product { Id = 3, Name = "USB-C Hub", Description = "7-in-1 USB-C hub", Price = 39.99m, StockQuantity = 200, CategoryId = 2 }
            );

            // Registers the entity sets OpenIddict needs (applications, authorizations,
            // scopes, tokens) so its Authorization Server can persist OAuth 2.0 clients,
            // grants, and tokens in this same database via EF Core.
            modelBuilder.UseOpenIddict();
        }
    }
}
