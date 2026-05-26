-- LogisticsHub schema export
-- Server: localhost\SQLEXPRESS
-- Database: ShipmentDb
-- Generated: 2026-05-25T14:56:04.2714622Z
-- Schema only. No table data is included.
-- Manually updated to include nullable sender/receiver Company/Address references.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[shipment_inbox_messages](
	[id] [uniqueidentifier] NOT NULL,
	[event_id] [uniqueidentifier] NOT NULL,
	[type] [nvarchar](512) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[processed_at_utc] [datetime2](7) NOT NULL,
	[created_at_utc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_shipment_inbox_messages] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_shipment_inbox_messages_event_id] ON [dbo].[shipment_inbox_messages]
(
	[event_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[shipment_inbox_messages] ADD  CONSTRAINT [DF_shipment_inbox_messages_created_at_utc]  DEFAULT (sysutcdatetime()) FOR [created_at_utc]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[shipment_items](
	[shipment_id] [uniqueidentifier] NOT NULL,
	[sku] [nvarchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[quantity] [int] NOT NULL,
 CONSTRAINT [PK_shipment_items] PRIMARY KEY CLUSTERED 
(
	[shipment_id] ASC,
	[sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

SET ANSI_PADDING ON

GO

CREATE NONCLUSTERED INDEX [IX_shipment_items_sku] ON [dbo].[shipment_items]
(
	[sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[shipment_items]  WITH CHECK ADD  CONSTRAINT [FK_shipment_items_shipments] FOREIGN KEY([shipment_id])
REFERENCES [dbo].[shipments] ([id])
GO

ALTER TABLE [dbo].[shipment_items] CHECK CONSTRAINT [FK_shipment_items_shipments]
GO

ALTER TABLE [dbo].[shipment_items]  WITH CHECK ADD  CONSTRAINT [CK_shipment_items_quantity_positive] CHECK  (([quantity]>(0)))
GO

ALTER TABLE [dbo].[shipment_items] CHECK CONSTRAINT [CK_shipment_items_quantity_positive]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[shipment_outbox_messages](
	[id] [uniqueidentifier] NOT NULL,
	[occurred_at_utc] [datetime2](7) NOT NULL,
	[type] [nvarchar](512) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[routing_key] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[payload] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[processed_at_utc] [datetime2](7) NULL,
	[error] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[retry_count] [int] NOT NULL,
	[created_at_utc] [datetime2](7) NOT NULL,
	[locked_by] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[locked_at_utc] [datetime2](7) NULL,
	[next_attempt_at_utc] [datetime2](7) NULL,
	[failed_at_utc] [datetime2](7) NULL,
 CONSTRAINT [PK_shipment_outbox_messages] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE NONCLUSTERED INDEX [IX_shipment_outbox_messages_unprocessed] ON [dbo].[shipment_outbox_messages]
(
	[processed_at_utc] ASC,
	[occurred_at_utc] ASC
)
INCLUDE([routing_key],[type],[retry_count]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[shipment_outbox_messages] ADD  CONSTRAINT [DF_shipment_outbox_messages_retry_count]  DEFAULT ((0)) FOR [retry_count]
GO

ALTER TABLE [dbo].[shipment_outbox_messages] ADD  CONSTRAINT [DF_shipment_outbox_messages_created_at_utc]  DEFAULT (sysutcdatetime()) FOR [created_at_utc]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[shipment_status_history](
	[id] [uniqueidentifier] NOT NULL,
	[shipment_id] [uniqueidentifier] NOT NULL,
	[old_status] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[new_status] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[reason] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[changed_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_shipment_status_history] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE NONCLUSTERED INDEX [IX_shipment_status_history_shipment_id_changed_at] ON [dbo].[shipment_status_history]
(
	[shipment_id] ASC,
	[changed_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[shipment_status_history] ADD  CONSTRAINT [DF_shipment_status_history_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[shipment_status_history] ADD  CONSTRAINT [DF_shipment_status_history_changed_at]  DEFAULT (sysutcdatetime()) FOR [changed_at]
GO

ALTER TABLE [dbo].[shipment_status_history]  WITH CHECK ADD  CONSTRAINT [FK_shipment_status_history_shipments] FOREIGN KEY([shipment_id])
REFERENCES [dbo].[shipments] ([id])
GO

ALTER TABLE [dbo].[shipment_status_history] CHECK CONSTRAINT [FK_shipment_status_history_shipments]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[shipments](
	[id] [uniqueidentifier] NOT NULL,
	[shipment_number] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[status] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[reservation_id] [uniqueidentifier] NULL,
	[reservation_failure_reason] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[sender_company_id] [uniqueidentifier] NULL,
	[sender_address_id] [uniqueidentifier] NULL,
	[receiver_company_id] [uniqueidentifier] NULL,
	[receiver_address_id] [uniqueidentifier] NULL,
	[destination_name] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[destination_address] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[comment] [nvarchar](1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[dispatched_at] [datetime2](3) NULL,
	[cancelled_at] [datetime2](3) NULL,
 CONSTRAINT [PK_shipments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
 CONSTRAINT [UQ_shipments_shipment_number] UNIQUE NONCLUSTERED 
(
	[shipment_number] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_shipments_reservation_id_not_null] ON [dbo].[shipments]
(
	[reservation_id] ASC
)
WHERE ([reservation_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[shipments] ADD  CONSTRAINT [DF_shipments_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[shipments] ADD  CONSTRAINT [DF_shipments_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO

ALTER TABLE [dbo].[shipments] ADD  CONSTRAINT [DF_shipments_updated_at]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO

ALTER TABLE [dbo].[shipments]  WITH CHECK ADD  CONSTRAINT [CK_shipments_status] CHECK  (([status]=N'Cancelled' OR [status]=N'Dispatched' OR [status]=N'ReservationFailed' OR [status]=N'Reserved' OR [status]=N'ReservationRequested' OR [status]=N'Created'))
GO

ALTER TABLE [dbo].[shipments] CHECK CONSTRAINT [CK_shipments_status]
GO
