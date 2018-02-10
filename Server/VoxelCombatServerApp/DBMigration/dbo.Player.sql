CREATE TABLE [dbo].[Player]
(
	[PlayerId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(512) NOT NULL, 
    [Password] BINARY(24) NOT NULL, 
    [Salt] BINARY(24) NOT NULL, 
    [Iterations] INT NOT NULL
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Player_Column] ON [dbo].[Player] ([Name])
