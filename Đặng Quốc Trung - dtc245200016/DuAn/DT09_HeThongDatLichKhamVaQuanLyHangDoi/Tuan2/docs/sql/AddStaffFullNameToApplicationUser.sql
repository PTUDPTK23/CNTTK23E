BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119135325_AddStaffFullNameToApplicationUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [StaffFullName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119135325_AddStaffFullNameToApplicationUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261119135325_AddStaffFullNameToApplicationUser', N'10.0.2');
END;

COMMIT;
GO

