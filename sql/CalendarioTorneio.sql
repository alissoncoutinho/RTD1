
-- Creating table 'CalendarioTorneio'
CREATE TABLE [dbo].[CalendarioTorneio] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [DataInicial] datetime  NOT NULL,
    [DataFinal] datetime  NOT NULL,
    [Nome] nvarchar(150)  NOT NULL,
    [ModalidadeTorneioId] int  NOT NULL,
    [Local] nvarchar(300)  NOT NULL,
    [Pontuacao] int  NOT NULL,
    [StatusInscricaoTorneioId] int  NOT NULL,
    [LinkInscricao] nvarchar(200)  NULL
);
GO

-- Creating table 'ModalidadeTorneio'
CREATE TABLE [dbo].[ModalidadeTorneio] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Nome] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'StatusInscricaoTorneio'
CREATE TABLE [dbo].[StatusInscricaoTorneio] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Nome] nvarchar(50)  NOT NULL
);
GO

-- Creating primary key on [Id] in table 'CalendarioTorneio'
ALTER TABLE [dbo].[CalendarioTorneio]
ADD CONSTRAINT [PK_CalendarioTorneio]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ModalidadeTorneio'
ALTER TABLE [dbo].[ModalidadeTorneio]
ADD CONSTRAINT [PK_ModalidadeTorneio]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'StatusInscricaoTorneio'
ALTER TABLE [dbo].[StatusInscricaoTorneio]
ADD CONSTRAINT [PK_StatusInscricaoTorneio]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [ModalidadeTorneioId] in table 'CalendarioTorneio'
ALTER TABLE [dbo].[CalendarioTorneio]
ADD CONSTRAINT [FK_CalendarioTorneioModalidadeTorneio]
    FOREIGN KEY ([ModalidadeTorneioId])
    REFERENCES [dbo].[ModalidadeTorneio]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_CalendarioTorneioModalidadeTorneio'
CREATE INDEX [IX_FK_CalendarioTorneioModalidadeTorneio]
ON [dbo].[CalendarioTorneio]
    ([ModalidadeTorneioId]);
GO

-- Creating foreign key on [StatusInscricaoTorneioId] in table 'CalendarioTorneio'
ALTER TABLE [dbo].[CalendarioTorneio]
ADD CONSTRAINT [FK_StatusInscricaoTorneioCalendarioTorneio]
    FOREIGN KEY ([StatusInscricaoTorneioId])
    REFERENCES [dbo].[StatusInscricaoTorneio]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_StatusInscricaoTorneioCalendarioTorneio'
CREATE INDEX [IX_FK_StatusInscricaoTorneioCalendarioTorneio]
ON [dbo].[CalendarioTorneio]
    ([StatusInscricaoTorneioId]);
GO