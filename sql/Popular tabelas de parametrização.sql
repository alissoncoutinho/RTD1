
USE [DB_9B5365_barragem]
GO

-- PaginaEspecial
INSERT INTO [dbo].[PaginaEspecial] ([Nome]) VALUES ('Circuito');
INSERT INTO [dbo].[PaginaEspecial] ([Nome]) VALUES ('Federação');
INSERT INTO [dbo].[PaginaEspecial] ([Nome]) VALUES ('Liga');
GO

-- ModalidadeTorneio
INSERT INTO [dbo].[ModalidadeTorneio] ([Nome]) VALUES ('Tênis');
INSERT INTO [dbo].[ModalidadeTorneio] ([Nome]) VALUES ('Beach Tennis');
INSERT INTO [dbo].[ModalidadeTorneio] ([Nome]) VALUES ('Kids');
GO

-- StatusInscricaoTorneio
INSERT INTO [dbo].[StatusInscricaoTorneio] ([Nome]) VALUES ('ABERTA');
INSERT INTO [dbo].[StatusInscricaoTorneio] ([Nome]) VALUES ('ENCERRADA');
INSERT INTO [dbo].[StatusInscricaoTorneio] ([Nome]) VALUES ('NÃO ABRIU');
GO