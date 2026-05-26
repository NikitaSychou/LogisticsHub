-- LogisticsHub manual schema patch
-- Database: ShipmentDb
-- Purpose: add nullable sender/receiver Company/Address references for future ShipmentService work.
-- Data: no table data is inserted or modified.

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.shipments', N'sender_company_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        ADD [sender_company_id] [uniqueidentifier] NULL;
END
GO

IF COL_LENGTH(N'dbo.shipments', N'sender_address_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        ADD [sender_address_id] [uniqueidentifier] NULL;
END
GO

IF COL_LENGTH(N'dbo.shipments', N'receiver_company_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        ADD [receiver_company_id] [uniqueidentifier] NULL;
END
GO

IF COL_LENGTH(N'dbo.shipments', N'receiver_address_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        ADD [receiver_address_id] [uniqueidentifier] NULL;
END
GO
