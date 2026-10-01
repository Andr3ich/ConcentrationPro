using System;
using System.Data.SQLite;

namespace ConcentrationTracker.Core.Database
{
    public class DatabaseSchemaService
    {
        public void CreateOrUpdateSchema()
        {
            using (SQLiteConnection connection =
                DatabaseConnectionFactory.CreateConnection())
            {
                CreateProfilesTable(connection);
                CreateSessionsTable(connection);
                CreateApplicationsTable(connection);
                CreateActivityCategoriesTable(connection);
                CreateActivityEventsTable(connection);
                CreateCategoryRulesTable(connection);

                CreateIndexes(connection);

                EnsureDefaultProfile(connection);
                EnsureDefaultActivityCategories(connection);
                EnsureDefaultCategoryRules(connection);
            }
        }

        private void CreateProfilesTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS Profiles
                (
                    ProfileId INTEGER PRIMARY KEY AUTOINCREMENT,

                    ProfileDisplayName TEXT NOT NULL,

                    LaunchOnWindowsStartup INTEGER NOT NULL DEFAULT 0,
                    LaunchMinimizedToTray INTEGER NOT NULL DEFAULT 0,
                    CloseToTray INTEGER NOT NULL DEFAULT 1,
                    ShowTrayNotification INTEGER NOT NULL DEFAULT 1,

                    IdleThresholdMinutes REAL NOT NULL DEFAULT 2,
                    IdleGraceSeconds INTEGER NOT NULL DEFAULT 15,

                    DefaultDashboardTab TEXT NOT NULL DEFAULT 'Charts',
                    ThemeMode TEXT NOT NULL DEFAULT 'Light',
                    InterfaceLanguage TEXT NOT NULL DEFAULT 'English',

                    ProfileCreatedAt TEXT NOT NULL,
                    ProfileUpdatedAt TEXT NOT NULL,

                    CHECK (LaunchOnWindowsStartup IN (0, 1)),
                    CHECK (LaunchMinimizedToTray IN (0, 1)),
                    CHECK (CloseToTray IN (0, 1)),
                    CHECK (ShowTrayNotification IN (0, 1)),
                    CHECK (IdleThresholdMinutes >= 0.25 AND IdleThresholdMinutes <= 60),
                    CHECK (IdleGraceSeconds >= 0 AND IdleGraceSeconds <= 300),
                    CHECK (DefaultDashboardTab IN ('Charts', 'Timeline', 'Summary', 'Categories', 'History')),
                    CHECK (ThemeMode IN ('Light', 'Dark')),
                    CHECK (InterfaceLanguage IN ('English', 'Ukrainian'))
                );");
        }

        private void CreateSessionsTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS Sessions
                (
                    SessionId TEXT PRIMARY KEY,

                    ProfileId INTEGER NOT NULL,

                    SessionStartedAt TEXT NOT NULL,
                    SessionEndedAt TEXT,

                    SessionState TEXT NOT NULL,
                    SessionRecordedAt TEXT NOT NULL,
                    SessionMetrics TEXT,

                    FOREIGN KEY(ProfileId) REFERENCES Profiles(ProfileId)
                        ON UPDATE CASCADE
                        ON DELETE CASCADE,

                    CHECK (SessionState IN ('Active', 'Paused', 'Saved', 'Reset'))
                );");

            EnsureColumn(
                connection,
                "Sessions",
                "SessionMetrics",
                "TEXT");
        }

        private void EnsureColumn(
            SQLiteConnection connection,
            string tableName,
            string columnName,
            string columnDefinition)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(" + tableName + ");";

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(
                                Convert.ToString(reader["name"]),
                                columnName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }
            }

            ExecuteNonQuery(
                connection,
                "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + columnDefinition + ";");
        }

        private void CreateApplicationsTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS Applications
                (
                    ApplicationId INTEGER PRIMARY KEY AUTOINCREMENT,

                    ApplicationProcessName TEXT NOT NULL,
                    ApplicationDisplayName TEXT NOT NULL,
                    ExecutablePath TEXT NOT NULL DEFAULT '',
                    ApplicationDetectedAt TEXT NOT NULL
                );");
        }

        private void CreateActivityCategoriesTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS ActivityCategories
                (
                    ActivityCategoryId INTEGER PRIMARY KEY AUTOINCREMENT,

                    ActivityCategoryName TEXT NOT NULL,
                    ActivityCategoryDescription TEXT,
                    IsSystemCategory INTEGER NOT NULL DEFAULT 1,
                    ActivityCategorySortOrder INTEGER NOT NULL DEFAULT 0,

                    CHECK (IsSystemCategory IN (0, 1))
                );");
        }

        private void CreateActivityEventsTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS ActivityEvents
                (
                    ActivityEventId INTEGER PRIMARY KEY AUTOINCREMENT,

                    SessionId TEXT NOT NULL,
                    ApplicationId INTEGER NOT NULL,
                    ActivityCategoryId INTEGER NOT NULL,

                    WindowTitle TEXT,
                    ActivityStartedAt TEXT NOT NULL,
                    ActivityEndedAt TEXT,

                    FOREIGN KEY(SessionId) REFERENCES Sessions(SessionId)
                        ON UPDATE CASCADE
                        ON DELETE CASCADE,

                    FOREIGN KEY(ApplicationId) REFERENCES Applications(ApplicationId)
                        ON UPDATE CASCADE,

                    FOREIGN KEY(ActivityCategoryId) REFERENCES ActivityCategories(ActivityCategoryId)
                        ON UPDATE CASCADE
                );");
        }

        private void CreateCategoryRulesTable(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                @"
                CREATE TABLE IF NOT EXISTS CategoryRules
                (
                    CategoryRuleId INTEGER PRIMARY KEY AUTOINCREMENT,

                    ProfileId INTEGER NOT NULL,
                    ActivityCategoryId INTEGER NOT NULL,

                    ApplicationPattern TEXT NOT NULL,
                    WindowTitlePattern TEXT NOT NULL,

                    RulePriority INTEGER NOT NULL DEFAULT 100,
                    IsCategoryRuleEnabled INTEGER NOT NULL DEFAULT 1,

                    RuleCreatedAt TEXT NOT NULL,
                    RuleUpdatedAt TEXT NOT NULL,

                    FOREIGN KEY(ProfileId) REFERENCES Profiles(ProfileId)
                        ON UPDATE CASCADE
                        ON DELETE CASCADE,

                    FOREIGN KEY(ActivityCategoryId) REFERENCES ActivityCategories(ActivityCategoryId)
                        ON UPDATE CASCADE,

                    CHECK (IsCategoryRuleEnabled IN (0, 1))
                );");
        }

        private void CreateIndexes(
            SQLiteConnection connection)
        {
            ExecuteNonQuery(connection,
                "CREATE UNIQUE INDEX IF NOT EXISTS UX_Applications_ProcessName_Path ON Applications(ApplicationProcessName, ExecutablePath);");
            ExecuteNonQuery(connection,
                "CREATE UNIQUE INDEX IF NOT EXISTS UX_ActivityCategories_Name ON ActivityCategories(ActivityCategoryName);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_Sessions_ProfileId ON Sessions(ProfileId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_Sessions_StartedAt ON Sessions(SessionStartedAt);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_ActivityEvents_SessionId ON ActivityEvents(SessionId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_ActivityEvents_ApplicationId ON ActivityEvents(ApplicationId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_ActivityEvents_CategoryId ON ActivityEvents(ActivityCategoryId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_ActivityEvents_StartedAt ON ActivityEvents(ActivityStartedAt);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_CategoryRules_ProfileId ON CategoryRules(ProfileId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_CategoryRules_CategoryId ON CategoryRules(ActivityCategoryId);");
            ExecuteNonQuery(connection,
                "CREATE INDEX IF NOT EXISTS IX_CategoryRules_Priority ON CategoryRules(ProfileId, RulePriority);");
        }

        private void EnsureDefaultProfile(SQLiteConnection connection)
        {
            using (SQLiteCommand checkCommand = connection.CreateCommand())
            {
                checkCommand.CommandText = "SELECT COUNT(*) FROM Profiles;";
                long count = (long)checkCommand.ExecuteScalar();
                if (count > 0)
                    return;
            }

            string now = DateTime.Now.ToString("o");

            using (SQLiteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.CommandText =
                    @"
                    INSERT INTO Profiles
                    (
                        ProfileDisplayName,
                        LaunchOnWindowsStartup,
                        LaunchMinimizedToTray,
                        CloseToTray,
                        ShowTrayNotification,
                        IdleThresholdMinutes,
                        IdleGraceSeconds,
                        DefaultDashboardTab,
                        ThemeMode,
                        InterfaceLanguage,
                        ProfileCreatedAt,
                        ProfileUpdatedAt
                    )
                    VALUES
                    (
                        @ProfileDisplayName,
                        0,
                        0,
                        1,
                        1,
                        2,
                        15,
                        'Charts',
                        'Light',
                        'English',
                        @ProfileCreatedAt,
                        @ProfileUpdatedAt
                    );";

                insertCommand.Parameters.AddWithValue("@ProfileDisplayName", "Local profile");
                insertCommand.Parameters.AddWithValue("@ProfileCreatedAt", now);
                insertCommand.Parameters.AddWithValue("@ProfileUpdatedAt", now);
                insertCommand.ExecuteNonQuery();
            }
        }

        private void EnsureDefaultActivityCategories(SQLiteConnection connection)
        {
            EnsureActivityCategory(connection, "Productive", "Work, study and focused tasks", 1, 10);
            EnsureActivityCategory(connection, "Neutral", "Neutral activity that does not strongly affect focus", 1, 20);
            EnsureActivityCategory(connection, "Communication", "Messengers, calls and communication tools", 1, 30);
            EnsureActivityCategory(connection, "Distraction", "Entertainment and distracting activity", 1, 40);
            EnsureActivityCategory(connection, "Away", "Time when the user is inactive", 1, 50);
            EnsureActivityCategory(connection, "AFK", "Time when the user is away from keyboard", 1, 55);
            EnsureActivityCategory(connection, "System", "System windows and technical application activity", 1, 60);
        }

        private void EnsureActivityCategory(SQLiteConnection connection, string categoryName, string categoryDescription, int isSystemCategory, int sortOrder)
        {
            using (SQLiteCommand checkCommand = connection.CreateCommand())
            {
                checkCommand.CommandText =
                    @"
                    SELECT COUNT(*)
                    FROM ActivityCategories
                    WHERE ActivityCategoryName = @ActivityCategoryName;";
                checkCommand.Parameters.AddWithValue("@ActivityCategoryName", categoryName);
                long count = (long)checkCommand.ExecuteScalar();
                if (count > 0)
                    return;
            }

            using (SQLiteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.CommandText =
                    @"
                    INSERT INTO ActivityCategories
                    (
                        ActivityCategoryName,
                        ActivityCategoryDescription,
                        IsSystemCategory,
                        ActivityCategorySortOrder
                    )
                    VALUES
                    (
                        @ActivityCategoryName,
                        @ActivityCategoryDescription,
                        @IsSystemCategory,
                        @ActivityCategorySortOrder
                    );";
                insertCommand.Parameters.AddWithValue("@ActivityCategoryName", categoryName);
                insertCommand.Parameters.AddWithValue("@ActivityCategoryDescription", categoryDescription);
                insertCommand.Parameters.AddWithValue("@IsSystemCategory", isSystemCategory);
                insertCommand.Parameters.AddWithValue("@ActivityCategorySortOrder", sortOrder);
                insertCommand.ExecuteNonQuery();
            }
        }

        private void EnsureDefaultCategoryRules(SQLiteConnection connection)
        {
            long profileId = GetDefaultProfileId(connection);
            if (profileId <= 0)
                return;

            EnsureCategoryRule(connection, profileId, "chrome", "youtube", "Distraction", 10);
            EnsureCategoryRule(connection, profileId, "msedge", "youtube", "Distraction", 11);
            EnsureCategoryRule(connection, profileId, "firefox", "youtube", "Distraction", 12);
            EnsureCategoryRule(connection, profileId, "chrome", "github", "Productive", 20);
            EnsureCategoryRule(connection, profileId, "msedge", "github", "Productive", 21);
            EnsureCategoryRule(connection, profileId, "telegram", "*", "Communication", 30);
            EnsureCategoryRule(connection, profileId, "discord", "*", "Communication", 31);
        }

        private void EnsureCategoryRule(SQLiteConnection connection, long profileId, string applicationPattern, string windowTitlePattern, string categoryName, int rulePriority)
        {
            long categoryId = GetActivityCategoryId(connection, categoryName);
            if (categoryId <= 0)
                return;

            using (SQLiteCommand checkCommand = connection.CreateCommand())
            {
                checkCommand.CommandText =
                    @"
                    SELECT COUNT(*)
                    FROM CategoryRules
                    WHERE ProfileId = @ProfileId
                      AND ApplicationPattern = @ApplicationPattern
                      AND WindowTitlePattern = @WindowTitlePattern;";
                checkCommand.Parameters.AddWithValue("@ProfileId", profileId);
                checkCommand.Parameters.AddWithValue("@ApplicationPattern", applicationPattern);
                checkCommand.Parameters.AddWithValue("@WindowTitlePattern", windowTitlePattern);
                long count = (long)checkCommand.ExecuteScalar();
                if (count > 0)
                    return;
            }

            string now = DateTime.Now.ToString("o");

            using (SQLiteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.CommandText =
                    @"
                    INSERT INTO CategoryRules
                    (
                        ProfileId,
                        ActivityCategoryId,
                        ApplicationPattern,
                        WindowTitlePattern,
                        RulePriority,
                        IsCategoryRuleEnabled,
                        RuleCreatedAt,
                        RuleUpdatedAt
                    )
                    VALUES
                    (
                        @ProfileId,
                        @ActivityCategoryId,
                        @ApplicationPattern,
                        @WindowTitlePattern,
                        @RulePriority,
                        1,
                        @RuleCreatedAt,
                        @RuleUpdatedAt
                    );";
                insertCommand.Parameters.AddWithValue("@ProfileId", profileId);
                insertCommand.Parameters.AddWithValue("@ActivityCategoryId", categoryId);
                insertCommand.Parameters.AddWithValue("@ApplicationPattern", applicationPattern);
                insertCommand.Parameters.AddWithValue("@WindowTitlePattern", windowTitlePattern);
                insertCommand.Parameters.AddWithValue("@RulePriority", rulePriority);
                insertCommand.Parameters.AddWithValue("@RuleCreatedAt", now);
                insertCommand.Parameters.AddWithValue("@RuleUpdatedAt", now);
                insertCommand.ExecuteNonQuery();
            }
        }

        private long GetDefaultProfileId(SQLiteConnection connection)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    @"
                    SELECT ProfileId
                    FROM Profiles
                    ORDER BY ProfileId ASC
                    LIMIT 1;";
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt64(result);
            }
        }

        private long GetActivityCategoryId(SQLiteConnection connection, string categoryName)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    @"
                    SELECT ActivityCategoryId
                    FROM ActivityCategories
                    WHERE ActivityCategoryName = @ActivityCategoryName
                    LIMIT 1;";
                command.Parameters.AddWithValue("@ActivityCategoryName", categoryName);
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt64(result);
            }
        }

        private void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }
}
