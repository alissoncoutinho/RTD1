
ALTER TABLE [dbo].[Torneio]
	ADD 
		[NomeBanco] varchar(50)  NULL,
		[Agencia] varchar(50)  NULL,
		[ContaCorrente] varchar(100)  NULL,
		[ChavePix] varchar(100)  NULL,
		[CpfConta] varchar(30)  NULL,
		[NomeOrganizador] varchar(100)  NULL,
		[ContatoOrganizador] varchar(50)  NULL
	
GO


ALTER TABLE [dbo].[Torneio] ALTER COLUMN [NomeBanco] varchar(50)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [Agencia] varchar(50)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [ContaCorrente] varchar(100)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [ChavePix] varchar(100)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [CpfConta] varchar(30)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [NomeOrganizador] varchar(100)  NULL;
ALTER TABLE [dbo].[Torneio] ALTER COLUMN [ContatoOrganizador] varchar(50)  NULL;

