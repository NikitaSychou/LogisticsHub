-- LogisticsHub manual schema patch
-- Database: ShipmentDb
-- Purpose: remove legacy destination text columns from shipments.
-- Data: drops only obsolete columns. Sender/receiver Company/Address reference columns remain unchanged.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.shipments', N'destination_name') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        DROP COLUMN [destination_name];
END
GO

IF COL_LENGTH(N'dbo.shipments', N'destination_address') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[shipments]
        DROP COLUMN [destination_address];
END
GO
