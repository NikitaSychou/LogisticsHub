-- LogisticsHub schema export
-- Server: localhost\SQLEXPRESS
-- Database: InventoryDb
-- Generated: 2026-05-25T14:56:01.1163316Z
-- Schema only. No table data is included.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[inventory_inbox_messages](
	[id] [uniqueidentifier] NOT NULL,
	[event_id] [uniqueidentifier] NOT NULL,
	[type] [nvarchar](512) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[processed_at_utc] [datetime2](7) NOT NULL,
	[created_at_utc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_inventory_inbox_messages] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_inventory_inbox_messages_event_id] ON [dbo].[inventory_inbox_messages]
(
	[event_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[inventory_inbox_messages] ADD  CONSTRAINT [DF_inventory_inbox_messages_created_at_utc]  DEFAULT (sysutcdatetime()) FOR [created_at_utc]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[inventory_outbox_messages](
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
 CONSTRAINT [PK_inventory_outbox_messages] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[items](
	[id] [uniqueidentifier] NOT NULL,
	[sku] [nvarchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[name] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_items] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
 CONSTRAINT [UQ_items_sku] UNIQUE NONCLUSTERED 
(
	[sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

SET ANSI_PADDING ON

GO

CREATE NONCLUSTERED INDEX [IX_items_name] ON [dbo].[items]
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[items] ADD  CONSTRAINT [DF_items_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[items] ADD  CONSTRAINT [DF_items_is_active]  DEFAULT ((1)) FOR [is_active]
GO

ALTER TABLE [dbo].[items] ADD  CONSTRAINT [DF_items_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO

ALTER TABLE [dbo].[items] ADD  CONSTRAINT [DF_items_updated_at]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[stock_balances](
	[item_id] [uniqueidentifier] NOT NULL,
	[on_hand] [int] NOT NULL,
	[reserved] [int] NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[row_version] [timestamp] NOT NULL,
 CONSTRAINT [PK_stock_balances] PRIMARY KEY CLUSTERED 
(
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

ALTER TABLE [dbo].[stock_balances] ADD  CONSTRAINT [DF_stock_balances_on_hand]  DEFAULT ((0)) FOR [on_hand]
GO

ALTER TABLE [dbo].[stock_balances] ADD  CONSTRAINT [DF_stock_balances_reserved]  DEFAULT ((0)) FOR [reserved]
GO

ALTER TABLE [dbo].[stock_balances] ADD  CONSTRAINT [DF_stock_balances_updated_at]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO

ALTER TABLE [dbo].[stock_balances]  WITH CHECK ADD  CONSTRAINT [FK_stock_balances_items] FOREIGN KEY([item_id])
REFERENCES [dbo].[items] ([id])
GO

ALTER TABLE [dbo].[stock_balances] CHECK CONSTRAINT [FK_stock_balances_items]
GO

ALTER TABLE [dbo].[stock_balances]  WITH CHECK ADD  CONSTRAINT [CK_stock_balances_on_hand_non_negative] CHECK  (([on_hand]>=(0)))
GO

ALTER TABLE [dbo].[stock_balances] CHECK CONSTRAINT [CK_stock_balances_on_hand_non_negative]
GO

ALTER TABLE [dbo].[stock_balances]  WITH CHECK ADD  CONSTRAINT [CK_stock_balances_reserved_non_negative] CHECK  (([reserved]>=(0)))
GO

ALTER TABLE [dbo].[stock_balances] CHECK CONSTRAINT [CK_stock_balances_reserved_non_negative]
GO

ALTER TABLE [dbo].[stock_balances]  WITH CHECK ADD  CONSTRAINT [CK_stock_balances_reserved_not_greater_than_on_hand] CHECK  (([reserved]<=[on_hand]))
GO

ALTER TABLE [dbo].[stock_balances] CHECK CONSTRAINT [CK_stock_balances_reserved_not_greater_than_on_hand]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[stock_reservation_items](
	[reservation_id] [uniqueidentifier] NOT NULL,
	[item_id] [uniqueidentifier] NOT NULL,
	[quantity] [int] NOT NULL,
 CONSTRAINT [PK_stock_reservation_items] PRIMARY KEY CLUSTERED 
(
	[reservation_id] ASC,
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

CREATE NONCLUSTERED INDEX [IX_stock_reservation_items_item_id] ON [dbo].[stock_reservation_items]
(
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[stock_reservation_items]  WITH CHECK ADD  CONSTRAINT [FK_stock_reservation_items_items] FOREIGN KEY([item_id])
REFERENCES [dbo].[items] ([id])
GO

ALTER TABLE [dbo].[stock_reservation_items] CHECK CONSTRAINT [FK_stock_reservation_items_items]
GO

ALTER TABLE [dbo].[stock_reservation_items]  WITH CHECK ADD  CONSTRAINT [CK_stock_reservation_items_quantity_positive] CHECK  (([quantity]>(0)))
GO

ALTER TABLE [dbo].[stock_reservation_items] CHECK CONSTRAINT [CK_stock_reservation_items_quantity_positive]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[stock_reservations](
	[id] [uniqueidentifier] NOT NULL,
	[shipment_id] [uniqueidentifier] NOT NULL,
	[status] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_stock_reservations] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
 CONSTRAINT [UQ_stock_reservations_shipment_id] UNIQUE NONCLUSTERED 
(
	[shipment_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

ALTER TABLE [dbo].[stock_reservations] ADD  CONSTRAINT [DF_stock_reservations_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[stock_reservations] ADD  CONSTRAINT [DF_stock_reservations_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO

ALTER TABLE [dbo].[stock_reservations] ADD  CONSTRAINT [DF_stock_reservations_updated_at]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO

ALTER TABLE [dbo].[stock_reservations]  WITH CHECK ADD  CONSTRAINT [CK_stock_reservations_status] CHECK  (([status]=N'Consumed' OR [status]=N'Released' OR [status]=N'Active'))
GO

ALTER TABLE [dbo].[stock_reservations] CHECK CONSTRAINT [CK_stock_reservations_status]
GO

ALTER TABLE [dbo].[stock_reservation_items]  WITH CHECK ADD  CONSTRAINT [FK_stock_reservation_items_stock_reservations] FOREIGN KEY([reservation_id])
REFERENCES [dbo].[stock_reservations] ([id])
GO

ALTER TABLE [dbo].[stock_reservation_items] CHECK CONSTRAINT [FK_stock_reservation_items_stock_reservations]
GO
