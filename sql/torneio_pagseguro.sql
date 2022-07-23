USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Torneio]
	ADD PagSeguroAtivo bit NOT NULL DEFAULT 0;
GO


update torneio set PagSeguroAtivo=1  where barragemId in (
		select b.Id
		from torneio t
		inner join Barragem b
		 on b.id =t.barragemId
		 where  b.tokenPagSeguro is not null)