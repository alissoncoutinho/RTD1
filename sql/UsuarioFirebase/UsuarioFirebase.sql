
-- Creating table 'UsuarioFirebase'
CREATE TABLE [dbo].[UsuarioFirebase] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId]  int  NOT NULL,
    [Token] nvarchar(500) NOT NULL,
	[DataAtualizacao] datetime  NOT NULL
);
GO

-- Creating primary key on [Id] in table 'UsuarioFirebase'
ALTER TABLE [dbo].[UsuarioFirebase]
ADD CONSTRAINT [PK_UsuarioFirebase]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO


-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UserId] in table 'UsuarioFirebase'
ALTER TABLE [dbo].[UsuarioFirebase]
ADD CONSTRAINT [FK_UserProfileUsuarioFirebase]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[UserProfile]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserProfileUsuarioFirebase'
CREATE INDEX [IX_FK_UserProfileUsuarioFirebase]
ON [dbo].[UsuarioFirebase]
    ([UserId]);
GO