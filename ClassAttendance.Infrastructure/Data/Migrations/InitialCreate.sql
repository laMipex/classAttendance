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
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [StudyPrograms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Year] int NOT NULL,
        CONSTRAINT [PK_StudyPrograms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Subjects] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ETCS] int NOT NULL,
        [Semester] int NOT NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [id] int NOT NULL IDENTITY,
        [Email] nvarchar(450) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Professors] (
        [UserId] int NOT NULL,
        [EmployeeCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Professors] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Professors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [UserId] int NOT NULL,
        [Index] nvarchar(max) NOT NULL,
        [StudyProgramId] int NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Students_StudyPrograms_StudyProgramId] FOREIGN KEY ([StudyProgramId]) REFERENCES [StudyPrograms] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Lectures] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] int NOT NULL,
        [ProfessorId] int NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [Rooms] nvarchar(max) NULL,
        CONSTRAINT [PK_Lectures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Lectures_Professors_ProfessorId] FOREIGN KEY ([ProfessorId]) REFERENCES [Professors] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Lectures_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [SubjectProfessors] (
        [SubjectId] int NOT NULL,
        [ProfessorId] int NOT NULL,
        CONSTRAINT [PK_SubjectProfessors] PRIMARY KEY ([SubjectId], [ProfessorId]),
        CONSTRAINT [FK_SubjectProfessors_Professors_ProfessorId] FOREIGN KEY ([ProfessorId]) REFERENCES [Professors] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_SubjectProfessors_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [StudentId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [AcademicYear] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([StudentId], [SubjectId]),
        CONSTRAINT [FK_Enrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Enrollments_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentDevices] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [DeviceFingerprint] nvarchar(max) NOT NULL,
        [isPrimary] bit NOT NULL,
        [RegisteredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentDevices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StudentDevices_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendanceSessions] (
        [Id] int NOT NULL IDENTITY,
        [LectureId] int NOT NULL,
        [OpenFrom] datetime2 NOT NULL,
        [OpenUntil] datetime2 NOT NULL,
        [QrTokenHash] nvarchar(max) NULL,
        [WifiRequired] bit NOT NULL,
        CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceSessions_Lectures_LectureId] FOREIGN KEY ([LectureId]) REFERENCES [Lectures] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizSessions] (
        [Id] int NOT NULL IDENTITY,
        [LectureId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [isActive] bit NOT NULL,
        CONSTRAINT [PK_QuizSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizSessions_Lectures_LectureId] FOREIGN KEY ([LectureId]) REFERENCES [Lectures] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [Attendances] (
        [Id] int NOT NULL IDENTITY,
        [AttendanceSessionId] int NOT NULL,
        [StudentId] int NOT NULL,
        [CheckInAt] datetime2 NOT NULL,
        [Method] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Attendances_AttendanceSessions_AttendanceSessionId] FOREIGN KEY ([AttendanceSessionId]) REFERENCES [AttendanceSessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Attendances_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizQuestions] (
        [Id] int NOT NULL IDENTITY,
        [QuizSessionId] int NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [OrderNo] int NOT NULL,
        CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizQuestions_QuizSessions_QuizSessionId] FOREIGN KEY ([QuizSessionId]) REFERENCES [QuizSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [IsCorrect] bit NOT NULL,
        CONSTRAINT [PK_QuizOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizOptions_QuizQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizResponses] (
        [Id] int NOT NULL IDENTITY,
        [QuizSessionId] int NOT NULL,
        [QuestionId] int NOT NULL,
        [StudentId] int NOT NULL,
        [AnswerText] nvarchar(max) NULL,
        [SelectedOptionId] int NULL,
        [SubmittedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_QuizResponses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizResponses_QuizOptions_SelectedOptionId] FOREIGN KEY ([SelectedOptionId]) REFERENCES [QuizOptions] ([Id]),
        CONSTRAINT [FK_QuizResponses_QuizQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuizResponses_QuizSessions_QuizSessionId] FOREIGN KEY ([QuizSessionId]) REFERENCES [QuizSessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuizResponses_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attendances_AttendanceSessionId_StudentId] ON [Attendances] ([AttendanceSessionId], [StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Attendances_StudentId] ON [Attendances] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AttendanceSessions_LectureId] ON [AttendanceSessions] ([LectureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_SubjectId] ON [Enrollments] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Lectures_ProfessorId] ON [Lectures] ([ProfessorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Lectures_SubjectId_StartsAt] ON [Lectures] ([SubjectId], [StartsAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizOptions_QuestionId] ON [QuizOptions] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizQuestions_QuizSessionId] ON [QuizQuestions] ([QuizSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizResponses_QuestionId] ON [QuizResponses] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuizResponses_QuizSessionId_QuestionId_StudentId] ON [QuizResponses] ([QuizSessionId], [QuestionId], [StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizResponses_SelectedOptionId] ON [QuizResponses] ([SelectedOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizResponses_StudentId] ON [QuizResponses] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizSessions_LectureId] ON [QuizSessions] ([LectureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentDevices_StudentId] ON [StudentDevices] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_StudyProgramId] ON [Students] ([StudyProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SubjectProfessors_ProfessorId] ON [SubjectProfessors] ([ProfessorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Subjects_Code] ON [Subjects] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728115736_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728115736_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

