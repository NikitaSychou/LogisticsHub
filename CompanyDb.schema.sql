-- LogisticsHub manual schema baseline
-- Database: CompanyDb
-- Schema only. No table data is included.

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[companies](
	[id] [uniqueidentifier] NOT NULL,
	[name] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[external_code] [nvarchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[status] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[created_at_utc] [datetime2](7) NOT NULL,
	[updated_at_utc] [datetime2](7) NULL,
 CONSTRAINT [PK_companies] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

SET ANSI_PADDING ON

GO

CREATE NONCLUSTERED INDEX [IX_Companies_Name] ON [dbo].[companies]
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Companies_ExternalCode] ON [dbo].[companies]
(
	[external_code] ASC
)
WHERE ([external_code] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[companies] ADD  CONSTRAINT [DF_companies_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[companies] ADD  CONSTRAINT [DF_companies_status]  DEFAULT (N'Active') FOR [status]
GO

ALTER TABLE [dbo].[companies] ADD  CONSTRAINT [DF_companies_created_at_utc]  DEFAULT (sysutcdatetime()) FOR [created_at_utc]
GO

ALTER TABLE [dbo].[companies]  WITH CHECK ADD  CONSTRAINT [CK_companies_status] CHECK  (([status]=N'Inactive' OR [status]=N'Active'))
GO

ALTER TABLE [dbo].[companies] CHECK CONSTRAINT [CK_companies_status]
GO

ALTER TABLE [dbo].[companies]  WITH CHECK ADD  CONSTRAINT [CK_companies_name_not_empty] CHECK  ((len(ltrim(rtrim([name])))>(0)))
GO

ALTER TABLE [dbo].[companies] CHECK CONSTRAINT [CK_companies_name_not_empty]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[company_addresses](
	[id] [uniqueidentifier] NOT NULL,
	[company_id] [uniqueidentifier] NOT NULL,
	[address_type] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[country_code] [nvarchar](2) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[city] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[postal_code] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[line1] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[line2] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[created_at_utc] [datetime2](7) NOT NULL,
	[updated_at_utc] [datetime2](7) NULL,
 CONSTRAINT [PK_company_addresses] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
)

GO

SET ANSI_PADDING ON

GO

CREATE NONCLUSTERED INDEX [IX_CompanyAddresses_CompanyId] ON [dbo].[company_addresses]
(
	[company_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

CREATE NONCLUSTERED INDEX [IX_CompanyAddresses_AddressType] ON [dbo].[company_addresses]
(
	[address_type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
GO

ALTER TABLE [dbo].[company_addresses] ADD  CONSTRAINT [DF_company_addresses_id]  DEFAULT (newsequentialid()) FOR [id]
GO

ALTER TABLE [dbo].[company_addresses] ADD  CONSTRAINT [DF_company_addresses_created_at_utc]  DEFAULT (sysutcdatetime()) FOR [created_at_utc]
GO

ALTER TABLE [dbo].[company_addresses]  WITH CHECK ADD  CONSTRAINT [FK_company_addresses_companies] FOREIGN KEY([company_id])
REFERENCES [dbo].[companies] ([id])
GO

ALTER TABLE [dbo].[company_addresses] CHECK CONSTRAINT [FK_company_addresses_companies]
GO

ALTER TABLE [dbo].[company_addresses]  WITH CHECK ADD  CONSTRAINT [CK_company_addresses_address_type] CHECK  (([address_type]=N'Warehouse' OR [address_type]=N'Shipping' OR [address_type]=N'Billing' OR [address_type]=N'Legal'))
GO

ALTER TABLE [dbo].[company_addresses] CHECK CONSTRAINT [CK_company_addresses_address_type]
GO

ALTER TABLE [dbo].[company_addresses]  WITH CHECK ADD  CONSTRAINT [CK_company_addresses_country_code_length] CHECK  ((len([country_code])=(2)))
GO

ALTER TABLE [dbo].[company_addresses] CHECK CONSTRAINT [CK_company_addresses_country_code_length]
GO

ALTER TABLE [dbo].[company_addresses]  WITH CHECK ADD  CONSTRAINT [CK_company_addresses_city_not_empty] CHECK  ((len(ltrim(rtrim([city])))>(0)))
GO

ALTER TABLE [dbo].[company_addresses] CHECK CONSTRAINT [CK_company_addresses_city_not_empty]
GO

ALTER TABLE [dbo].[company_addresses]  WITH CHECK ADD  CONSTRAINT [CK_company_addresses_line1_not_empty] CHECK  ((len(ltrim(rtrim([line1])))>(0)))
GO

ALTER TABLE [dbo].[company_addresses] CHECK CONSTRAINT [CK_company_addresses_line1_not_empty]
GO
