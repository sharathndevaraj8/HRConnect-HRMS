BEGIN TRANSACTION;
ALTER TABLE [UserAccounts] ADD [EmployeeId] int NULL;

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LeaveRequests]') AND [c].[name] = N'Reason');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [LeaveRequests] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [LeaveRequests] ALTER COLUMN [Reason] nvarchar(1000) NOT NULL;

ALTER TABLE [LeaveRequests] ADD [CancelledAtUtc] datetime2 NULL;

ALTER TABLE [LeaveRequests] ADD [ContactDuringLeave] nvarchar(100) NULL;

ALTER TABLE [LeaveRequests] ADD [IsHalfDay] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [LeaveRequests] ADD [LeaveTypeId] int NOT NULL DEFAULT 0;

ALTER TABLE [LeaveRequests] ADD [NumberOfDays] decimal(6,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [LeaveRequests] ADD [ReviewComment] nvarchar(1000) NULL;

ALTER TABLE [LeaveRequests] ADD [ReviewedAtUtc] datetime2 NULL;

ALTER TABLE [LeaveRequests] ADD [ReviewedByUserAccountId] int NULL;

ALTER TABLE [Employees] ADD [AddressLine1] nvarchar(255) NULL;

ALTER TABLE [Employees] ADD [AddressLine2] nvarchar(255) NULL;

ALTER TABLE [Employees] ADD [AlternatePhoneNumber] nvarchar(20) NULL;

ALTER TABLE [Employees] ADD [BloodGroup] nvarchar(10) NULL;

ALTER TABLE [Employees] ADD [City] nvarchar(100) NULL;

ALTER TABLE [Employees] ADD [Country] nvarchar(100) NULL;

ALTER TABLE [Employees] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Employees] ADD [DateOfBirth] datetime2 NULL;

ALTER TABLE [Employees] ADD [DateOfLeaving] datetime2 NULL;

ALTER TABLE [Employees] ADD [DepartmentId] int NULL;

ALTER TABLE [Employees] ADD [EmergencyContactName] nvarchar(100) NULL;

ALTER TABLE [Employees] ADD [EmergencyContactPhone] nvarchar(20) NULL;

ALTER TABLE [Employees] ADD [EmergencyContactRelationship] nvarchar(50) NULL;

ALTER TABLE [Employees] ADD [EmployeeCode] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [Employees] ADD [EmploymentStatus] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [Employees] ADD [EmploymentType] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [Employees] ADD [Gender] nvarchar(30) NULL;

ALTER TABLE [Employees] ADD [ManagerId] int NULL;

ALTER TABLE [Employees] ADD [MaritalStatus] nvarchar(30) NULL;

ALTER TABLE [Employees] ADD [PersonalEmail] nvarchar(255) NULL;

ALTER TABLE [Employees] ADD [PhoneNumber] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [Employees] ADD [PostalCode] nvarchar(12) NULL;

ALTER TABLE [Employees] ADD [State] nvarchar(100) NULL;

ALTER TABLE [Employees] ADD [UpdatedAtUtc] datetime2 NULL;

ALTER TABLE [Employees] ADD [WorkLocation] nvarchar(100) NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Departments]') AND [c].[name] = N'Name');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Departments] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Departments] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

ALTER TABLE [Departments] ADD [Code] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [Departments] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Departments] ADD [Description] nvarchar(500) NULL;

ALTER TABLE [Departments] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE TABLE [EmployeeDocuments] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [DocumentType] nvarchar(50) NOT NULL,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StoredFileName] nvarchar(255) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [FileSize] bigint NOT NULL,
    [Notes] nvarchar(500) NULL,
    [UploadedByUserAccountId] int NOT NULL,
    [UploadedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeDocuments_UserAccounts_UploadedByUserAccountId] FOREIGN KEY ([UploadedByUserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LeaveTypes] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [AnnualEntitlement] decimal(6,2) NOT NULL,
    [CarryForwardLimit] decimal(6,2) NOT NULL,
    [MaxConsecutiveDays] decimal(6,2) NULL,
    [DocumentRequiredAfterDays] decimal(6,2) NULL,
    [IsPaid] bit NOT NULL,
    [AllowsHalfDay] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [ApplicableGender] nvarchar(30) NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [LeaveBalances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [Year] int NOT NULL,
    [OpeningBalance] decimal(7,2) NOT NULL,
    [Accrued] decimal(7,2) NOT NULL,
    [Used] decimal(7,2) NOT NULL,
    [Adjustment] decimal(7,2) NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
    SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] ([Id], [Code], [CreatedAtUtc], [Description], [IsActive], [Name])
VALUES (1, N'ENG', '2026-01-01T00:00:00.0000000Z', N'Product engineering and quality', CAST(1 AS bit), N'Engineering'),
(2, N'HR', '2026-01-01T00:00:00.0000000Z', N'People operations and culture', CAST(1 AS bit), N'Human Resources'),
(3, N'FIN', '2026-01-01T00:00:00.0000000Z', N'Finance and accounting', CAST(1 AS bit), N'Finance'),
(4, N'OPS', '2026-01-01T00:00:00.0000000Z', N'Business operations', CAST(1 AS bit), N'Operations');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
    SET IDENTITY_INSERT [Departments] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowsHalfDay', N'AnnualEntitlement', N'ApplicableGender', N'CarryForwardLimit', N'Code', N'Description', N'DocumentRequiredAfterDays', N'IsActive', N'IsPaid', N'MaxConsecutiveDays', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[LeaveTypes]'))
    SET IDENTITY_INSERT [LeaveTypes] ON;
INSERT INTO [LeaveTypes] ([Id], [AllowsHalfDay], [AnnualEntitlement], [ApplicableGender], [CarryForwardLimit], [Code], [Description], [DocumentRequiredAfterDays], [IsActive], [IsPaid], [MaxConsecutiveDays], [Name], [SortOrder])
VALUES (1, CAST(1 AS bit), 18.0, NULL, 10.0, N'PTO', N'Planned personal or vacation leave; entitlement is configurable.', NULL, CAST(1 AS bit), CAST(1 AS bit), 10.0, N'Paid Time Off', 1),
(2, CAST(1 AS bit), 12.0, NULL, 0.0, N'SICK', N'Leave for illness or medical care.', 2.0, CAST(1 AS bit), CAST(1 AS bit), NULL, N'Sick Leave', 2),
(3, CAST(0 AS bit), 0.0, NULL, 0.0, N'PARENTAL', N'Parental leave governed by company policy and applicable law.', NULL, CAST(1 AS bit), CAST(1 AS bit), NULL, N'Parental Leave', 3),
(4, CAST(0 AS bit), 5.0, NULL, 0.0, N'BEREAVEMENT', N'Time away following the loss of a family member.', NULL, CAST(1 AS bit), CAST(1 AS bit), 5.0, N'Bereavement Leave', 4),
(5, CAST(1 AS bit), 2.0, NULL, 0.0, N'VOLUNTEER', N'Paid time to support approved charitable activities.', NULL, CAST(1 AS bit), CAST(1 AS bit), 2.0, N'Volunteer Time Off', 5),
(6, CAST(1 AS bit), 0.0, NULL, 5.0, N'COMP_OFF', N'Time off granted for approved work on a holiday or weekend.', NULL, CAST(1 AS bit), CAST(1 AS bit), NULL, N'Compensatory Off', 6),
(7, CAST(1 AS bit), 0.0, NULL, 0.0, N'UNPAID', N'Approved leave without pay after paid balances are exhausted.', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Unpaid Leave', 7);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowsHalfDay', N'AnnualEntitlement', N'ApplicableGender', N'CarryForwardLimit', N'Code', N'Description', N'DocumentRequiredAfterDays', N'IsActive', N'IsPaid', N'MaxConsecutiveDays', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[LeaveTypes]'))
    SET IDENTITY_INSERT [LeaveTypes] OFF;

UPDATE [Employees]
SET [EmployeeCode] = 'EMP' + RIGHT('000000' + CAST([Id] AS varchar(6)), 6),
    [PhoneNumber] = 'Not provided',
    [EmploymentType] = 'Permanent',
    [EmploymentStatus] = 'Active',
    [CreatedAtUtc] = SYSUTCDATETIME();

UPDATE [LeaveRequests]
SET [LeaveTypeId] = 1,
    [NumberOfDays] = CASE
        WHEN DATEDIFF(day, [StartDate], [EndDate]) + 1 > 0
        THEN DATEDIFF(day, [StartDate], [EndDate]) + 1
        ELSE 1 END;

UPDATE [UserAccounts]
SET [Role] = 'Admin'
WHERE [Id] = (SELECT MIN([Id]) FROM [UserAccounts]);

UPDATE u
SET u.[EmployeeId] = e.[Id]
FROM [UserAccounts] u
INNER JOIN [Employees] e ON LOWER(u.[Email]) = LOWER(e.[Email])
WHERE u.[EmployeeId] IS NULL;

CREATE UNIQUE INDEX [IX_UserAccounts_EmployeeId] ON [UserAccounts] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;

CREATE INDEX [IX_LeaveRequests_EmployeeId_StartDate_EndDate] ON [LeaveRequests] ([EmployeeId], [StartDate], [EndDate]);

CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [LeaveRequests] ([LeaveTypeId]);

CREATE INDEX [IX_LeaveRequests_ReviewedByUserAccountId] ON [LeaveRequests] ([ReviewedByUserAccountId]);

CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);

CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);

CREATE INDEX [IX_Employees_ManagerId] ON [Employees] ([ManagerId]);

CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);

CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);

CREATE INDEX [IX_EmployeeDocuments_EmployeeId_DocumentType] ON [EmployeeDocuments] ([EmployeeId], [DocumentType]);

CREATE INDEX [IX_EmployeeDocuments_UploadedByUserAccountId] ON [EmployeeDocuments] ([UploadedByUserAccountId]);

CREATE UNIQUE INDEX [IX_LeaveBalances_EmployeeId_LeaveTypeId_Year] ON [LeaveBalances] ([EmployeeId], [LeaveTypeId], [Year]);

CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [LeaveBalances] ([LeaveTypeId]);

CREATE UNIQUE INDEX [IX_LeaveTypes_Code] ON [LeaveTypes] ([Code]);

CREATE UNIQUE INDEX [IX_LeaveTypes_Name] ON [LeaveTypes] ([Name]);

ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL;

ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [LeaveRequests] ADD CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE;

ALTER TABLE [LeaveRequests] ADD CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [LeaveRequests] ADD CONSTRAINT [FK_LeaveRequests_UserAccounts_ReviewedByUserAccountId] FOREIGN KEY ([ReviewedByUserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [UserAccounts] ADD CONSTRAINT [FK_UserAccounts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260813133206_CompleteHRMSModules', N'10.0.8');

COMMIT;
GO

