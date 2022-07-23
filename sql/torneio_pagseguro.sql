USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Torneio]
	ADD PagSeguroAtivo bit NOT NULL DEFAULT 0;
GO


update barragem set PagSeguroAtivo=1  where tokenPagSeguro is not null;