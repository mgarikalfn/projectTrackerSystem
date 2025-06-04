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
CREATE TABLE [Projects] (
    [Id] nvarchar(450) NOT NULL,
    [Key] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Lead] nvarchar(max) NULL,
    [HealthLevel] nvarchar(max) NOT NULL,
    [HealthReason] nvarchar(max) NOT NULL,
    [TotalTasks] int NOT NULL,
    [CompletedTasks] int NOT NULL,
    [StoryPointsCompleted] decimal(18,2) NOT NULL,
    [StoryPointsTotal] decimal(18,2) NOT NULL,
    [ProgressPercentage] decimal(18,2) NOT NULL,
    [Progress_VelocityTrend] decimal(18,2) NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);

CREATE TABLE [SyncHistory] (
    [Id] nvarchar(450) NOT NULL,
    [SyncTime] datetime2 NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [ProjectId] nvarchar(450) NULL,
    [TasksProcessed] int NOT NULL,
    [TasksCreated] int NOT NULL,
    [TasksUpdated] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [Duration] time NOT NULL,
    [SyncTrigger] nvarchar(max) NOT NULL,
    [JiraRequestId] nvarchar(max) NULL,
    [JiraDataCutoff] datetime2 NULL,
    [ProjectId1] nvarchar(450) NULL,
    CONSTRAINT [PK_SyncHistory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyncHistory_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]),
    CONSTRAINT [FK_SyncHistory_Projects_ProjectId1] FOREIGN KEY ([ProjectId1]) REFERENCES [Projects] ([Id])
);

CREATE TABLE [Tasks] (
    [Id] nvarchar(450) NOT NULL,
    [Key] nvarchar(450) NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(450) NOT NULL,
    [StatusChangedDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    [AssigneeId] nvarchar(450) NOT NULL,
    [AssigneeName] nvarchar(max) NOT NULL,
    [Updated] datetime2 NOT NULL,
    [ProjectId] nvarchar(450) NOT NULL,
    [StoryPoints] decimal(18,2) NULL,
    [TimeEstimateMinutes] int NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tasks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Projects_Key] ON [Projects] ([Key]);

CREATE INDEX [IX_SyncHistory_ProjectId] ON [SyncHistory] ([ProjectId]);

CREATE INDEX [IX_SyncHistory_ProjectId_SyncTime] ON [SyncHistory] ([ProjectId], [SyncTime]);

CREATE INDEX [IX_SyncHistory_ProjectId1] ON [SyncHistory] ([ProjectId1]);

CREATE INDEX [IX_SyncHistory_Status] ON [SyncHistory] ([Status]);

CREATE INDEX [IX_SyncHistory_SyncTime] ON [SyncHistory] ([SyncTime]);

CREATE INDEX [IX_Tasks_AssigneeId] ON [Tasks] ([AssigneeId]);

CREATE UNIQUE INDEX [IX_Tasks_Key] ON [Tasks] ([Key]);

CREATE INDEX [IX_Tasks_ProjectId] ON [Tasks] ([ProjectId]);

CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);

CREATE INDEX [IX_Tasks_Updated] ON [Tasks] ([Updated]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250519131803_InitialCreate', N'9.0.5');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'ProgressPercentage');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Projects] DROP COLUMN [ProgressPercentage];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'Progress_VelocityTrend');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Projects] DROP COLUMN [Progress_VelocityTrend];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'Name');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Projects] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'Lead');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Projects] ALTER COLUMN [Lead] nvarchar(100) NULL;

DROP INDEX [IX_Projects_Key] ON [Projects];
DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'Key');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Projects] ALTER COLUMN [Key] nvarchar(50) NOT NULL;
CREATE UNIQUE INDEX [IX_Projects_Key] ON [Projects] ([Key]);

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'Description');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Projects] ALTER COLUMN [Description] nvarchar(500) NULL;

ALTER TABLE [Projects] ADD [ActiveBlockers] int NOT NULL DEFAULT 0;

ALTER TABLE [Projects] ADD [HealthConfidence] nvarchar(max) NULL;

ALTER TABLE [Projects] ADD [HealthScore] float NULL;

ALTER TABLE [Projects] ADD [RecentUpdates] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521065020_UpdatedProjectProperty', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521130609_RemovedAtRisk', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521131141_ChangedAtRiskToCritical', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521131438_mig12', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521131712_mig14', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250522050424_mig15', N'9.0.5');

CREATE TABLE [Privileges] (
    [Id] int NOT NULL IDENTITY,
    [PrivilageName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Privileges] PRIMARY KEY ([Id])
);

CREATE TABLE [UserRoles] (
    [Id] nvarchar(450) NOT NULL,
    [RoleName] nvarchar(256) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Name] nvarchar(max) NULL,
    [NormalizedName] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [UserName] nvarchar(max) NULL,
    [NormalizedUserName] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [NormalizedEmail] nvarchar(max) NULL,
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
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RolePrivileges] (
    [RoleId] nvarchar(450) NOT NULL,
    [PrivilageId] int NOT NULL,
    CONSTRAINT [PK_RolePrivileges] PRIMARY KEY ([RoleId], [PrivilageId]),
    CONSTRAINT [FK_RolePrivileges_Privileges_PrivilageId] FOREIGN KEY ([PrivilageId]) REFERENCES [Privileges] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePrivileges_UserRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [UserRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoleMappings] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [AssignedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_UserRoleMappings] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoleMappings_UserRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [UserRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoleMappings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RolePrivileges_PrivilageId] ON [RolePrivileges] ([PrivilageId]);

CREATE INDEX [IX_UserRoleMappings_RoleId] ON [UserRoleMappings] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250528131748_AddedIdentityForUserManagment', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250529082622_fix-user', N'9.0.5');

ALTER TABLE [RolePrivileges] DROP CONSTRAINT [FK_RolePrivileges_UserRoles_RoleId];

ALTER TABLE [UserRoleMappings] DROP CONSTRAINT [FK_UserRoleMappings_UserRoles_RoleId];

ALTER TABLE [UserRoleMappings] DROP CONSTRAINT [FK_UserRoleMappings_Users_UserId];

ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];

ALTER TABLE [UserRoles] DROP CONSTRAINT [PK_UserRoles];

EXEC sp_rename N'[Users]', N'AspNetUsers', 'OBJECT';

EXEC sp_rename N'[UserRoles]', N'AspNetRoles', 'OBJECT';

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserName');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [AspNetUsers] ALTER COLUMN [UserName] nvarchar(256) NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'NormalizedUserName');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [AspNetUsers] ALTER COLUMN [NormalizedUserName] nvarchar(256) NULL;

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'NormalizedEmail');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [AspNetUsers] ALTER COLUMN [NormalizedEmail] nvarchar(256) NULL;

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'Email');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [AspNetUsers] ALTER COLUMN [Email] nvarchar(256) NULL;

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoles]') AND [c].[name] = N'NormalizedName');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoles] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [AspNetRoles] ALTER COLUMN [NormalizedName] nvarchar(256) NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoles]') AND [c].[name] = N'Name');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoles] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [AspNetRoles] ALTER COLUMN [Name] nvarchar(256) NULL;

ALTER TABLE [AspNetUsers] ADD CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]);

ALTER TABLE [AspNetRoles] ADD CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id]);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

ALTER TABLE [RolePrivileges] ADD CONSTRAINT [FK_RolePrivileges_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE;

ALTER TABLE [UserRoleMappings] ADD CONSTRAINT [FK_UserRoleMappings_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE;

ALTER TABLE [UserRoleMappings] ADD CONSTRAINT [FK_UserRoleMappings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250529083809_fixed-dbcontext', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250602003008_Added-MenuITem-Model', N'9.0.5');

CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Url] nvarchar(max) NULL,
    [Icon] nvarchar(max) NULL,
    [RequiredPrivilege] nvarchar(max) NULL,
    [ParentId] int NULL,
    [Order] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id])
);

CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250602003301_CreatedMenuITem', N'9.0.5');

COMMIT;
GO

