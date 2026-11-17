SET NOCOUNT ON;

IF DB_NAME() <> N'MediQueue'
BEGIN
    SELECT
        N'ABORTED' AS ValidationStatus,
        DB_NAME() AS ActualDatabase,
        N'MediQueue' AS ExpectedDatabase,
        N'Chuyển query window sang database MediQueue rồi chạy lại script.' AS ValidationMessage;
    RETURN;
END;

SELECT
    N'OK' AS ValidationStatus,
    CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) AS ServerName,
    DB_NAME() AS DatabaseName,
    database_state.state_desc AS DatabaseState,
    database_state.compatibility_level AS CompatibilityLevel
FROM sys.databases AS database_state
WHERE database_state.name = DB_NAME();

SELECT
    schema_name.name AS SchemaName,
    table_name.name AS TableName
FROM sys.tables AS table_name
INNER JOIN sys.schemas AS schema_name
    ON schema_name.schema_id = table_name.schema_id
WHERE table_name.is_ms_shipped = 0
ORDER BY schema_name.name, table_name.name;

SELECT
    OBJECT_SCHEMA_NAME(column_name.object_id) AS SchemaName,
    OBJECT_NAME(column_name.object_id) AS TableName,
    column_name.column_id AS ColumnOrder,
    column_name.name AS ColumnName,
    type_name.name AS DataType,
    column_name.max_length AS MaxLength,
    column_name.precision AS [Precision],
    column_name.scale AS Scale,
    column_name.is_nullable AS IsNullable,
    column_name.is_identity AS IsIdentity,
    column_name.is_computed AS IsComputed
FROM sys.columns AS column_name
INNER JOIN sys.types AS type_name
    ON type_name.user_type_id = column_name.user_type_id
WHERE column_name.object_id IN
(
    SELECT table_name.object_id
    FROM sys.tables AS table_name
    WHERE table_name.is_ms_shipped = 0
)
ORDER BY SchemaName, TableName, ColumnOrder;

SELECT
    OBJECT_SCHEMA_NAME(primary_key.parent_object_id) AS SchemaName,
    OBJECT_NAME(primary_key.parent_object_id) AS TableName,
    primary_key.name AS PrimaryKeyName,
    STRING_AGG(column_name.name, N', ')
        WITHIN GROUP (ORDER BY index_column.key_ordinal) AS KeyColumns
FROM sys.key_constraints AS primary_key
INNER JOIN sys.index_columns AS index_column
    ON index_column.object_id = primary_key.parent_object_id
    AND index_column.index_id = primary_key.unique_index_id
INNER JOIN sys.columns AS column_name
    ON column_name.object_id = index_column.object_id
    AND column_name.column_id = index_column.column_id
WHERE primary_key.type = 'PK'
GROUP BY primary_key.parent_object_id, primary_key.name
ORDER BY TableName, PrimaryKeyName;

SELECT
    OBJECT_SCHEMA_NAME(foreign_key.parent_object_id) AS SchemaName,
    OBJECT_NAME(foreign_key.parent_object_id) AS ChildTable,
    foreign_key.name AS ForeignKeyName,
    OBJECT_NAME(foreign_key.referenced_object_id) AS ParentTable,
    foreign_key.delete_referential_action_desc AS DeleteAction,
    foreign_key.update_referential_action_desc AS UpdateAction,
    foreign_key.is_disabled AS IsDisabled,
    foreign_key.is_not_trusted AS IsNotTrusted
FROM sys.foreign_keys AS foreign_key
WHERE foreign_key.parent_object_id IN
(
    SELECT table_name.object_id
    FROM sys.tables AS table_name
    WHERE table_name.is_ms_shipped = 0
)
ORDER BY ChildTable, ForeignKeyName;

SELECT
    OBJECT_SCHEMA_NAME(index_name.object_id) AS SchemaName,
    OBJECT_NAME(index_name.object_id) AS TableName,
    index_name.name AS IndexName,
    index_name.type_desc AS IndexType,
    index_name.is_unique AS IsUnique,
    index_name.is_primary_key AS IsPrimaryKey,
    index_name.is_disabled AS IsDisabled,
    index_name.has_filter AS HasFilter,
    index_name.filter_definition AS FilterDefinition,
    STRING_AGG(column_name.name, N', ')
        WITHIN GROUP (ORDER BY index_column.key_ordinal) AS KeyColumns
FROM sys.indexes AS index_name
INNER JOIN sys.index_columns AS index_column
    ON index_column.object_id = index_name.object_id
    AND index_column.index_id = index_name.index_id
    AND index_column.key_ordinal > 0
INNER JOIN sys.columns AS column_name
    ON column_name.object_id = index_column.object_id
    AND column_name.column_id = index_column.column_id
WHERE index_name.object_id IN
(
    SELECT table_name.object_id
    FROM sys.tables AS table_name
    WHERE table_name.is_ms_shipped = 0
)
GROUP BY
    index_name.object_id,
    index_name.name,
    index_name.type_desc,
    index_name.is_unique,
    index_name.is_primary_key,
    index_name.is_disabled,
    index_name.has_filter,
    index_name.filter_definition
ORDER BY TableName, IndexName;

SELECT
    OBJECT_SCHEMA_NAME(check_constraint.parent_object_id) AS SchemaName,
    OBJECT_NAME(check_constraint.parent_object_id) AS TableName,
    check_constraint.name AS CheckConstraintName,
    check_constraint.definition AS CheckDefinition,
    check_constraint.is_disabled AS IsDisabled,
    check_constraint.is_not_trusted AS IsNotTrusted
FROM sys.check_constraints AS check_constraint
ORDER BY TableName, CheckConstraintName;

SELECT
    OBJECT_SCHEMA_NAME(default_constraint.parent_object_id) AS SchemaName,
    OBJECT_NAME(default_constraint.parent_object_id) AS TableName,
    COL_NAME(default_constraint.parent_object_id, default_constraint.parent_column_id) AS ColumnName,
    default_constraint.name AS DefaultConstraintName,
    default_constraint.definition AS DefaultDefinition
FROM sys.default_constraints AS default_constraint
ORDER BY TableName, ColumnName;

SELECT
    OBJECT_SCHEMA_NAME(column_name.object_id) AS SchemaName,
    OBJECT_NAME(column_name.object_id) AS TableName,
    column_name.name AS RowVersionColumn,
    type_name.name AS DataType,
    column_name.max_length AS MaxLength,
    column_name.is_nullable AS IsNullable
FROM sys.columns AS column_name
INNER JOIN sys.types AS type_name
    ON type_name.user_type_id = column_name.user_type_id
WHERE type_name.name IN (N'timestamp', N'rowversion')
ORDER BY TableName, RowVersionColumn;

SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS TotalTableCount,
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> N'__EFMigrationsHistory') AS ModelTableCount,
    (SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK' AND parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)) AS PrimaryKeyCount,
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)) AS ForeignKeyCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0) AND index_id > 0 AND is_primary_key = 0) AS NonPrimaryIndexCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0) AND index_id > 0 AND is_unique = 1 AND is_primary_key = 0) AS UniqueIndexCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0) AND index_id > 0 AND has_filter = 1) AS FilteredIndexCount,
    (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)) AS CheckConstraintCount,
    (SELECT COUNT(*) FROM sys.default_constraints WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)) AS DefaultConstraintCount,
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0) AND (is_disabled = 1 OR is_not_trusted = 1)) AS InvalidForeignKeyCount,
    (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0) AND (is_disabled = 1 OR is_not_trusted = 1)) AS InvalidCheckConstraintCount,
    (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) AS AppliedMigrationCount;

SELECT
    index_name.name AS RequiredIndex,
    index_name.is_unique AS IsUnique,
    index_name.is_disabled AS IsDisabled,
    index_name.has_filter AS HasFilter,
    index_name.filter_definition AS FilterDefinition,
    type_name.name AS StatusDataType,
    status_column.max_length AS StatusMaxLength
FROM sys.indexes AS index_name
INNER JOIN sys.columns AS status_column
    ON status_column.object_id = index_name.object_id
    AND status_column.name = N'Status'
INNER JOIN sys.types AS type_name
    ON type_name.user_type_id = status_column.user_type_id
WHERE index_name.object_id = OBJECT_ID(N'dbo.QueueTickets')
    AND index_name.name = N'UX_QueueTickets_Doctor_Serving';
