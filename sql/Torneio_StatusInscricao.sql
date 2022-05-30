USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Torneio]
	ADD StatusInscricao int NOT NULL DEFAULT 3;
GO
