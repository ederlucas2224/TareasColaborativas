CREATE TABLE tasks
(
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Titulo NVARCHAR(250) NOT NULL,
    Decripcion NVARCHAR(MAX) NULL,
    Estatus NVARCHAR(50) NOT NULL,
    AsignadoA NVARCHAR(200) NULL,
    Evidencia VARBINARY(MAX) NULL,
    Evidencia_fileName NVARCHAR(255) NULL,
    Evidencia_Content_Type NVARCHAR(100) NULL,
    Creado DATETIME2,
    Actualizado DATETIME2,
    Row_Version ROWVERSION
);

CREATE INDEX IX_Tasks_CreatedAt ON tasks(Creado);
CREATE INDEX IX_Tasks_UpdatedAt ON tasks(Actualizado);