USE [DB_9B5365_barragem]
GO

ALTER TABLE [dbo].[Liga]
	ADD ModalidadeTorneioId int NULL;
GO

-- Creating foreign key on [ModalidadeTorneioId] in table 'Liga'
ALTER TABLE [dbo].[Liga]
ADD CONSTRAINT [FK_ModalidadeTorneioLiga]
    FOREIGN KEY ([ModalidadeTorneioId])
    REFERENCES [dbo].[ModalidadeTorneio]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_ModalidadeTorneioLiga'
CREATE INDEX [IX_FK_ModalidadeTorneioLiga]
ON [dbo].[Liga]
    ([ModalidadeTorneioId]);
GO