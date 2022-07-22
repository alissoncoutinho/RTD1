USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Barragem]
	ADD PagSeguroAtivo bit NOT NULL DEFAULT 0;
GO


ALTER VIEW [dbo].[BarragemView]
AS
SELECT barragem.Id,
       barragem.nome,
       barragem.isBeachTenis,
       barragem.isModeloTodosContraTodos,
       barragem.suspensaoPorWO,
       barragem.suspensaoPorAtraso,
       barragem.valorPorUsuario,
       barragem.cpfResponsavel,
       barragem.nomeResponsavel,
       barragem.isAtiva,
       barragem.isTeste,
       barragem.linkPagSeguro,
       barragem.isClasseUnica,
       barragem.dominio,
       barragem.email,
       barragem.soTorneio,
       barragem.emailPagSeguro,
       barragem.tokenPagSeguro,
	   barragem.PagSeguroAtivo,
       barragem.cidade,
       barragem.contato,
       barragem.PaginaEspecialId,
	   paginaEspecial.Nome as PaginaEspecialNome
FROM dbo.Barragem barragem
LEFT JOIN [dbo].[PaginaEspecial] paginaEspecial ON barragem.PaginaEspecialId = paginaEspecial.Id;
GO

update barragem set PagSeguroAtivo=1  where tokenPagSeguro is not null;