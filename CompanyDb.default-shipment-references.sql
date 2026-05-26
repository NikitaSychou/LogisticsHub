-- LogisticsHub manual data patch
-- Database: CompanyDb
-- Purpose: ensure stable bootstrap/backfill Company/Address references for legacy shipments.
-- Data: inserts only the fixed records below when they do not already exist.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON;
GO

DECLARE @DefaultSenderCompanyId uniqueidentifier = '11111111-1111-4111-8111-111111111111';
DECLARE @DefaultSenderAddressId uniqueidentifier = '22222222-2222-4222-8222-222222222222';
DECLARE @DefaultReceiverCompanyId uniqueidentifier = '33333333-3333-4333-8333-333333333333';
DECLARE @DefaultReceiverAddressId uniqueidentifier = '44444444-4444-4444-8444-444444444444';
DECLARE @Now datetime2(7) = sysutcdatetime();

IF NOT EXISTS (SELECT 1 FROM [dbo].[companies] WHERE [id] = @DefaultSenderCompanyId)
BEGIN
    INSERT INTO [dbo].[companies] (
        [id],
        [name],
        [external_code],
        [status],
        [created_at_utc],
        [updated_at_utc])
    VALUES (
        @DefaultSenderCompanyId,
        N'LogisticsHub Bootstrap Sender Warehouse',
        N'BOOTSTRAP-SENDER-WAREHOUSE',
        N'Active',
        @Now,
        NULL);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[companies] WHERE [id] = @DefaultReceiverCompanyId)
BEGIN
    INSERT INTO [dbo].[companies] (
        [id],
        [name],
        [external_code],
        [status],
        [created_at_utc],
        [updated_at_utc])
    VALUES (
        @DefaultReceiverCompanyId,
        N'LogisticsHub Bootstrap Legacy Receiver',
        N'BOOTSTRAP-LEGACY-RECEIVER',
        N'Active',
        @Now,
        NULL);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[company_addresses] WHERE [id] = @DefaultSenderAddressId)
BEGIN
    INSERT INTO [dbo].[company_addresses] (
        [id],
        [company_id],
        [address_type],
        [country_code],
        [city],
        [postal_code],
        [line1],
        [line2],
        [created_at_utc],
        [updated_at_utc])
    VALUES (
        @DefaultSenderAddressId,
        @DefaultSenderCompanyId,
        N'Warehouse',
        N'US',
        N'Bootstrap Warehouse',
        NULL,
        N'Bootstrap sender warehouse address for legacy shipment backfill',
        NULL,
        @Now,
        NULL);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[company_addresses] WHERE [id] = @DefaultReceiverAddressId)
BEGIN
    INSERT INTO [dbo].[company_addresses] (
        [id],
        [company_id],
        [address_type],
        [country_code],
        [city],
        [postal_code],
        [line1],
        [line2],
        [created_at_utc],
        [updated_at_utc])
    VALUES (
        @DefaultReceiverAddressId,
        @DefaultReceiverCompanyId,
        N'Shipping',
        N'US',
        N'Legacy Receiver',
        NULL,
        N'Bootstrap legacy receiver address for shipment backfill',
        NULL,
        @Now,
        NULL);
END
GO
