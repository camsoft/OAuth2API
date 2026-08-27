-- ============================================================================
-- 02_CreateProductsTable.sql
--
-- Creates the Products table. Run this AFTER 01_CreateDatabase.sql.
-- This mirrors exactly what the "InitialCreate" EF Core migration generates,
-- so students can see what EF Core is doing "under the hood".
-- ============================================================================

USE WebAPICourseDb;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
	CREATE TABLE dbo.Products
	(
		Id            INT IDENTITY(1,1) NOT NULL,
		Name          NVARCHAR(200)     NOT NULL,
		Description   NVARCHAR(1000)    NULL,
		Price         DECIMAL(10,2)     NOT NULL,
		StockQuantity INT               NOT NULL,

		CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id ASC)
	);
END
GO
