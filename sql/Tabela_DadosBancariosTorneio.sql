
-- Creating table 'DadosBancariosTorneio'
CREATE TABLE [dbo].[DadosBancariosTorneio](
	[Id] [int]			 IDENTITY(1,1) NOT NULL,
	[IdTorneio]			 [int] NOT NULL,
	[NomeBanco]			 [varchar](50) NULL,
	[Agencia]			 [varchar](10) NULL,
	[ContaCorrente]		 [varchar](20) NULL,
	[ChavePix]			 [varchar](100) NULL,
	[CPF]				 [varchar](20) NULL,
	[NomeOrganizador]	 [varchar](50) NULL,
	[ContatoOrganizador] [varchar](20) NULL,
	[Ativo]				 [bit] NOT NULL
);
GO


-- Creating primary key on [Id] in table 'DadosBancariosTorneio'
ALTER TABLE [dbo].[DadosBancariosTorneio]
ADD CONSTRAINT [PK_DadosBancariosTorneio]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO


-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [IdTorneio] in table 'DadosBancariosTorneio'
ALTER TABLE [dbo].[DadosBancariosTorneio]
ADD CONSTRAINT [FK_DadosBancariosTorneio_Torneio]
    FOREIGN KEY ([IdTorneio])
    REFERENCES [dbo].[Torneio]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_DadosBancariosTorneio_Torneio'
CREATE INDEX [IX_FK_DadosBancariosTorneio_Torneio]
ON [dbo].[DadosBancariosTorneio]
    (IdTorneio);
GO