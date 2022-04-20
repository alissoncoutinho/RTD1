USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[CalendarioTorneio]
	ADD BarragemId int NOT NULL DEFAULT 1;
GO

-- Creating foreign key on [BarragemId] in table 'CalendarioTorneio'
ALTER TABLE [dbo].[CalendarioTorneio]
ADD CONSTRAINT [FK_BarragemCalendarioTorneio]
    FOREIGN KEY ([BarragemId])
    REFERENCES [dbo].[Barragem]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_BarragemCalendarioTorneio'
CREATE INDEX [IX_FK_BarragemCalendarioTorneio]
ON [dbo].[CalendarioTorneio]
    ([BarragemId]);
GO


ALTER TABLE [dbo].[Patrocinio]
	ADD BarragemId int NOT NULL DEFAULT 1;
GO

-- Creating foreign key on [BarragemId] in table 'Patrocinio'
ALTER TABLE [dbo].[Patrocinio]
ADD CONSTRAINT [FK_BarragemPatrocinio]
    FOREIGN KEY ([BarragemId])
    REFERENCES [dbo].[Barragem]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_BarragemPatrocinio'
CREATE INDEX [IX_FK_BarragemPatrocinio]
ON [dbo].[Patrocinio]
    ([BarragemId]);
GO


