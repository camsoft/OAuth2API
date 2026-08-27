# SQL Scripts vs. EF Core Migrations

This folder contains plain SQL scripts that create the `WebAPICourseDb` database,
the `Products` table, and seed it with starter data. They are provided so you can
see exactly what the database schema looks like without needing to understand EF Core.

## Two ways to get the database set up

You only need to use **one** of the two options below - they both end up creating
the same database and data.

### Option 1: Run the SQL scripts manually (recommended for learning)

1. Open SQL Server Object Explorer in Visual Studio (View > SQL Server Object Explorer),
   or connect to `(localdb)\mssqllocaldb` using SSMS/Azure Data Studio.
2. Run the scripts **in order**:
   - `01_CreateDatabase.sql`
   - `02_CreateProductsTable.sql`
   - `03_SeedProducts.sql`
3. Run the app. Since the database and table already exist, `dbContext.Database.Migrate()`
   in `Program.cs` will simply record that the "InitialCreate" migration has already been
   applied (EF Core tracks this in a `__EFMigrationsHistory` table) and won't try to
   recreate anything.

### Option 2: Let EF Core Migrations do it for you

The `Migrations` folder (generated via `dotnet ef migrations add InitialCreate`) contains
C# code that describes the same schema as these SQL scripts. When the app starts,
`Program.cs` calls `dbContext.Database.Migrate()`, which will:

1. Create the `WebAPICourseDb` database if it doesn't exist.
2. Create the `Products` table if it doesn't exist.
3. Insert the seed data configured in `AppDbContext.OnModelCreating`.

You don't need to run anything manually - just start the app (F5) and the database
will be ready.

## Why show both?

Understanding the raw SQL helps you understand *what* EF Core is doing for you.
Once you're comfortable with the SQL, using Migrations is much faster for day-to-day
development because EF Core generates and tracks these scripts automatically as your
model changes (e.g., adding a new property to `Product` and running
`dotnet ef migrations add AddedNewProperty`).

## Useful EF Core CLI commands

Run these from the `WebAPICourse` project folder:

```powershell
# Install the EF Core CLI tool once per machine (if not already installed)
dotnet tool install --global dotnet-ef

# Add a new migration after changing your models
dotnet ef migrations add <MigrationName>

# Apply pending migrations to the database
dotnet ef database update

# Remove the last migration (if not yet applied to a database)
dotnet ef migrations remove
```
