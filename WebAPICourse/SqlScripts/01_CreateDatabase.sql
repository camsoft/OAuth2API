-- ============================================================================
-- 01_CreateDatabase.sql
--
-- Creates the WebAPICourseDb database used by this project.
-- Run this script against your SQL Server LocalDB instance (or SQL Server)
-- using SQL Server Management Studio (SSMS), Azure Data Studio, or the
-- "SQL Server Object Explorer" inside Visual Studio.
--
-- Connect to: (localdb)\mssqllocaldb
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'WebAPICourseDb')
BEGIN
	CREATE DATABASE WebAPICourseDb;
END
GO
