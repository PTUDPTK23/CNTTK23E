IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset(0) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [InboxMessages] (
        [MessageId] uniqueidentifier NOT NULL,
        [ConsumerName] varchar(200) NOT NULL,
        [ProcessedAtUtc] datetimeoffset(0) NOT NULL,
        [Result] nvarchar(1000) NULL,
        CONSTRAINT [PK_InboxMessages] PRIMARY KEY ([MessageId], [ConsumerName])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Type] varchar(200) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredAtUtc] datetimeoffset(0) NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        [PublishedAtUtc] datetimeoffset(0) NULL,
        [NextAttemptAtUtc] datetimeoffset(0) NULL,
        [RetryCount] int NOT NULL,
        [LastError] nvarchar(2000) NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OutboxMessages_RetryCount] CHECK ([RetryCount] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Location] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Action] varchar(100) NOT NULL,
        [ResourceType] varchar(100) NOT NULL,
        [ResourceId] varchar(100) NOT NULL,
        [OccurredAtUtc] datetimeoffset(0) NOT NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [IdempotencyRequests] (
        [UserId] uniqueidentifier NOT NULL,
        [IdempotencyKey] uniqueidentifier NOT NULL,
        [RequestHash] varchar(128) NOT NULL,
        [ResponseStatusCode] int NOT NULL,
        [ResourceId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset(0) NOT NULL,
        CONSTRAINT [PK_IdempotencyRequests] PRIMARY KEY ([UserId], [IdempotencyKey]),
        CONSTRAINT [CK_IdempotencyRequests_ResponseStatusCode] CHECK ([ResponseStatusCode] >= 100 AND [ResponseStatusCode] <= 599),
        CONSTRAINT [FK_IdempotencyRequests_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [Patients] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [DateOfBirth] date NULL,
        [PhoneNumber] varchar(32) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [Doctors] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Doctors_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Doctors_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [DoctorSchedules] (
        [Id] uniqueidentifier NOT NULL,
        [DoctorId] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time(0) NOT NULL,
        [EndTime] time(0) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DoctorSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DoctorSchedules_DayOfWeek] CHECK ([DayOfWeek] >= 0 AND [DayOfWeek] <= 6),
        CONSTRAINT [CK_DoctorSchedules_TimeRange] CHECK ([EndTime] > [StartTime]),
        CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DoctorSchedules_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [TimeSlots] (
        [Id] uniqueidentifier NOT NULL,
        [DoctorScheduleId] uniqueidentifier NOT NULL,
        [DoctorId] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [StartAtUtc] datetimeoffset(0) NOT NULL,
        [EndAtUtc] datetimeoffset(0) NOT NULL,
        [Capacity] int NOT NULL,
        [BookedCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TimeSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TimeSlots_BookedCount] CHECK ([BookedCount] >= 0 AND [BookedCount] <= [Capacity]),
        CONSTRAINT [CK_TimeSlots_Capacity] CHECK ([Capacity] > 0),
        CONSTRAINT [CK_TimeSlots_TimeRange] CHECK ([EndAtUtc] > [StartAtUtc]),
        CONSTRAINT [FK_TimeSlots_DoctorSchedules_DoctorScheduleId] FOREIGN KEY ([DoctorScheduleId]) REFERENCES [DoctorSchedules] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TimeSlots_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TimeSlots_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] uniqueidentifier NOT NULL,
        [TimeSlotId] uniqueidentifier NOT NULL,
        [PatientId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [Status] varchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset(0) NOT NULL,
        [UpdatedAtUtc] datetimeoffset(0) NOT NULL,
        [CheckedInAtUtc] datetimeoffset(0) NULL,
        [CheckedInByUserId] uniqueidentifier NULL,
        [CancelledAtUtc] datetimeoffset(0) NULL,
        [NoShowAtUtc] datetimeoffset(0) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Appointments_Status] CHECK ([Status] IN (N'PendingNumber', N'Waiting', N'Serving', N'Completed', N'Cancelled', N'NoShow')),
        CONSTRAINT [FK_Appointments_AspNetUsers_CheckedInByUserId] FOREIGN KEY ([CheckedInByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_TimeSlots_TimeSlotId] FOREIGN KEY ([TimeSlotId]) REFERENCES [TimeSlots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [DailyQueues] (
        [Id] uniqueidentifier NOT NULL,
        [DoctorId] uniqueidentifier NOT NULL,
        [ServiceDate] date NOT NULL,
        [LastIssuedNumber] int NOT NULL,
        [CurrentTicketId] uniqueidentifier NULL,
        [Version] bigint NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DailyQueues] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_DailyQueues_Id_DoctorId_ServiceDate] UNIQUE ([Id], [DoctorId], [ServiceDate]),
        CONSTRAINT [CK_DailyQueues_LastIssuedNumber] CHECK ([LastIssuedNumber] >= 0),
        CONSTRAINT [CK_DailyQueues_Version] CHECK ([Version] >= 0),
        CONSTRAINT [FK_DailyQueues_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [QueueTickets] (
        [Id] uniqueidentifier NOT NULL,
        [DailyQueueId] uniqueidentifier NOT NULL,
        [DoctorId] uniqueidentifier NOT NULL,
        [ServiceDate] date NOT NULL,
        [AppointmentId] uniqueidentifier NOT NULL,
        [QueueNumber] int NOT NULL,
        [Status] varchar(32) NOT NULL,
        [IssuedAtUtc] datetimeoffset(0) NOT NULL,
        [ServingAtUtc] datetimeoffset(0) NULL,
        [CompletedAtUtc] datetimeoffset(0) NULL,
        [CancelledAtUtc] datetimeoffset(0) NULL,
        [NoShowAtUtc] datetimeoffset(0) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_QueueTickets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QueueTickets_QueueNumber] CHECK ([QueueNumber] > 0),
        CONSTRAINT [CK_QueueTickets_Status] CHECK ([Status] IN (N'Waiting', N'Serving', N'Completed', N'Cancelled', N'NoShow')),
        CONSTRAINT [FK_QueueTickets_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QueueTickets_DailyQueues_DailyQueueId_DoctorId_ServiceDate] FOREIGN KEY ([DailyQueueId], [DoctorId], [ServiceDate]) REFERENCES [DailyQueues] ([Id], [DoctorId], [ServiceDate]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QueueTickets_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE TABLE [QueueTransitions] (
        [Id] uniqueidentifier NOT NULL,
        [QueueTicketId] uniqueidentifier NOT NULL,
        [FromStatus] varchar(32) NOT NULL,
        [ToStatus] varchar(32) NOT NULL,
        [PerformedByUserId] uniqueidentifier NULL,
        [OccurredAtUtc] datetimeoffset(0) NOT NULL,
        [Reason] nvarchar(500) NULL,
        CONSTRAINT [PK_QueueTransitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QueueTransitions_AspNetUsers_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QueueTransitions_QueueTickets_QueueTicketId] FOREIGN KEY ([QueueTicketId]) REFERENCES [QueueTickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_CheckedInByUserId] ON [Appointments] ([CheckedInByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_CreatedByUserId] ON [Appointments] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_Patient_Status_TimeSlot] ON [Appointments] ([PatientId], [Status], [TimeSlotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_TimeSlot_Status] ON [Appointments] ([TimeSlotId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId] ON [AuditLogs] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Resource_OccurredAt] ON [AuditLogs] ([ResourceType], [ResourceId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DailyQueues_CurrentTicketId] ON [DailyQueues] ([CurrentTicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DailyQueues_Doctor_ServiceDate] ON [DailyQueues] ([DoctorId], [ServiceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Departments_Code] ON [Departments] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Doctors_DepartmentId_IsActive] ON [Doctors] ([DepartmentId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Doctors_UserId] ON [Doctors] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorSchedules_Doctor_Day_Time] ON [DoctorSchedules] ([DoctorId], [DayOfWeek], [StartTime], [EndTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorSchedules_Room_Day_Time] ON [DoctorSchedules] ([RoomId], [DayOfWeek], [StartTime], [EndTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IdempotencyRequests_CreatedAtUtc] ON [IdempotencyRequests] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InboxMessages_ProcessedAtUtc] ON [InboxMessages] ([ProcessedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_Pending] ON [OutboxMessages] ([PublishedAtUtc], [NextAttemptAtUtc], [OccurredAtUtc]) WHERE [PublishedAtUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Patients_UserId] ON [Patients] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QueueTickets_DailyQueueId_DoctorId_ServiceDate] ON [QueueTickets] ([DailyQueueId], [DoctorId], [ServiceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QueueTickets_Doctor_Date_Status_Number] ON [QueueTickets] ([DoctorId], [ServiceDate], [Status], [QueueNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_QueueTickets_AppointmentId] ON [QueueTickets] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_QueueTickets_Doctor_ServiceDate_Number] ON [QueueTickets] ([DoctorId], [ServiceDate], [QueueNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_QueueTickets_Doctor_Serving] ON [QueueTickets] ([DoctorId]) WHERE [Status] = ''Serving''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QueueTransitions_PerformedByUserId] ON [QueueTransitions] ([PerformedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QueueTransitions_Ticket_OccurredAt] ON [QueueTransitions] ([QueueTicketId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Rooms_Code] ON [Rooms] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeSlots_Doctor_Start_IsActive] ON [TimeSlots] ([DoctorId], [StartAtUtc], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeSlots_DoctorScheduleId] ON [TimeSlots] ([DoctorScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeSlots_Room_TimeRange] ON [TimeSlots] ([RoomId], [StartAtUtc], [EndAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    ALTER TABLE [DailyQueues] ADD CONSTRAINT [FK_DailyQueues_QueueTickets_CurrentTicketId] FOREIGN KEY ([CurrentTicketId]) REFERENCES [QueueTickets] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117142359_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117142359_InitialCreate', N'10.0.2');
END;

COMMIT;
GO

