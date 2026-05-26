-- LogisticsHub manual schema/data patch
-- Database: ShipmentDb
-- Purpose: backfill sender/receiver Company/Address references and enforce NOT NULL.
-- Data: updates only existing shipment rows with missing sender/receiver references.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.shipments', N'sender_company_id') IS NULL
    OR COL_LENGTH(N'dbo.shipments', N'sender_address_id') IS NULL
    OR COL_LENGTH(N'dbo.shipments', N'receiver_company_id') IS NULL
    OR COL_LENGTH(N'dbo.shipments', N'receiver_address_id') IS NULL
BEGIN
    THROW 51000, 'Shipment company/address reference columns must exist before enforcing required references.', 1;
END
GO

DECLARE @DefaultSenderCompanyId uniqueidentifier = '11111111-1111-4111-8111-111111111111';
DECLARE @DefaultSenderAddressId uniqueidentifier = '22222222-2222-4222-8222-222222222222';
DECLARE @DefaultReceiverCompanyId uniqueidentifier = '33333333-3333-4333-8333-333333333333';
DECLARE @DefaultReceiverAddressId uniqueidentifier = '44444444-4444-4444-8444-444444444444';

UPDATE [dbo].[shipments]
SET
    [sender_company_id] = COALESCE([sender_company_id], @DefaultSenderCompanyId),
    [sender_address_id] = COALESCE([sender_address_id], @DefaultSenderAddressId),
    [receiver_company_id] = COALESCE([receiver_company_id], @DefaultReceiverCompanyId),
    [receiver_address_id] = COALESCE([receiver_address_id], @DefaultReceiverAddressId)
WHERE [sender_company_id] IS NULL
    OR [sender_address_id] IS NULL
    OR [receiver_company_id] IS NULL
    OR [receiver_address_id] IS NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.shipments')
        AND [name] = N'sender_company_id'
        AND [is_nullable] = 1)
BEGIN
    ALTER TABLE [dbo].[shipments] ALTER COLUMN [sender_company_id] [uniqueidentifier] NOT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.shipments')
        AND [name] = N'sender_address_id'
        AND [is_nullable] = 1)
BEGIN
    ALTER TABLE [dbo].[shipments] ALTER COLUMN [sender_address_id] [uniqueidentifier] NOT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.shipments')
        AND [name] = N'receiver_company_id'
        AND [is_nullable] = 1)
BEGIN
    ALTER TABLE [dbo].[shipments] ALTER COLUMN [receiver_company_id] [uniqueidentifier] NOT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.shipments')
        AND [name] = N'receiver_address_id'
        AND [is_nullable] = 1)
BEGIN
    ALTER TABLE [dbo].[shipments] ALTER COLUMN [receiver_address_id] [uniqueidentifier] NOT NULL;
END
GO
