USE [DB_9B5365_barragem]
GO

/****** Object:  Table [dbo].[Patrocinio]    Script Date: 24/03/2022 19:30:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Patrocinio](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UrlImagem] [nvarchar](300) NOT NULL,
	[UrlPatrocinador] [nvarchar](500) NOT NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


