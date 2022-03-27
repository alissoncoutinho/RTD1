USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Barragem]
	ADD PaginaEspecialId int NULL;
GO

-- Creating table 'PaginaEspecial'
CREATE TABLE [dbo].[PaginaEspecial] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Nome] nvarchar(max)  NOT NULL
);
GO

-- Creating primary key on [Id] in table 'PaginaEspecial'
ALTER TABLE [dbo].[PaginaEspecial]
ADD CONSTRAINT [PK_PaginaEspecial]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating foreign key on [PaginaEspecialId] in table 'Barragem'
ALTER TABLE [dbo].[Barragem]
ADD CONSTRAINT [FK_PaginaEspecialBarragem]
    FOREIGN KEY ([PaginaEspecialId])
    REFERENCES [dbo].[PaginaEspecial]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_PaginaEspecialBarragem'
CREATE INDEX [IX_FK_PaginaEspecialBarragem]
ON [dbo].[Barragem]
    ([PaginaEspecialId]);
GO


ALTER VIEW [dbo].[BarragemView]
AS
SELECT        Id, nome, isBeachTenis, isModeloTodosContraTodos, suspensaoPorWO, suspensaoPorAtraso, valorPorUsuario, cpfResponsavel, nomeResponsavel, isAtiva, isTeste, linkPagSeguro, isClasseUnica, dominio, email, soTorneio, emailPagSeguro, tokenPagSeguro, cidade, contato, PaginaEspecialId
FROM            dbo.Barragem;

GO
