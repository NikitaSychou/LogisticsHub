-- LogisticsHub manual schema patch
-- Database: ShipmentDb
-- Purpose: add sender/receiver Company/Address reference columns before backfill/enforcement.
-- Data: no table data is inserted or modified.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

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
