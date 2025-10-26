CREATE TABLE [dbo].[Logs] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Message] [nvarchar](max) NULL,
        [MessageTemplate] [nvarchar](max) NULL,
        [Level] [nvarchar](128) NULL,
        [TimeStamp] [datetimeoffset](7) NOT NULL,
        [Exception] [nvarchar](max) NULL,
        [Properties] [nvarchar](max) NULL,
        [UserId] [nvarchar](50) NULL,
        [Operation] [nvarchar](100) NULL,
        [EntityId] [uniqueidentifier] NULL,
        CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    -- Crear índices para mejorar el rendimiento
    CREATE NONCLUSTERED INDEX [IX_Logs_Level] ON [dbo].[Logs] ([Level]);
    CREATE NONCLUSTERED INDEX [IX_Logs_TimeStamp] ON [dbo].[Logs] ([TimeStamp]);
    CREATE NONCLUSTERED INDEX [IX_Logs_UserId] ON [dbo].[Logs] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_Logs_Operation] ON [dbo].[Logs] ([Operation]);
    CREATE NONCLUSTERED INDEX [IX_Logs_EntityId] ON [dbo].[Logs] ([EntityId]);