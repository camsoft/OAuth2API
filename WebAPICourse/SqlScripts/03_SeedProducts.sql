-- ============================================================================
-- 03_SeedProducts.sql
--
-- Inserts starter data into the Products table. Run this AFTER
-- 02_CreateProductsTable.sql. This matches the seed data configured in
-- AppDbContext.OnModelCreating via HasData(), so the app behaves the same
-- way whether the database was created via these scripts or via EF Migrations.
--
-- SET IDENTITY_INSERT is required because we are explicitly supplying Id
-- values instead of letting the database auto-generate them.
-- ============================================================================

USE WebAPICourseDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
	SET IDENTITY_INSERT dbo.Products ON;

	INSERT INTO dbo.Products (Id, Name, Description, Price, StockQuantity)
	VALUES
		(1, N'Wireless Mouse', N'Ergonomic wireless mouse', 24.99, 150),
		(2, N'Mechanical Keyboard', N'RGB backlit mechanical keyboard', 89.99, 75),
		(3, N'USB-C Hub', N'7-in-1 USB-C hub', 39.99, 200);

	SET IDENTITY_INSERT dbo.Products OFF;
END
GO
