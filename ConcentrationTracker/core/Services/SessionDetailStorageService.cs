using ConcentrationTracker.Core.Database;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;

namespace ConcentrationTracker.Core.Services
{
    public class SessionDetailStorageService
    {
        public void SaveDetails(
            string sessionId,
            IEnumerable<ActivityEventModel> activityEvents,
            IEnumerable<object> switchEvents,
            IEnumerable<FocusBlockModel> focusBlocks,
            DateTime now)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    DeleteExistingActivityEvents(connection, transaction, sessionId);
                    SaveActivityEvents(connection, transaction, sessionId, activityEvents, now);
                    transaction.Commit();
                }
            }
        }

        private void DeleteExistingActivityEvents(SQLiteConnection connection, SQLiteTransaction transaction, string sessionId)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM ActivityEvents WHERE SessionId = @SessionId;";
                command.Parameters.AddWithValue("@SessionId", sessionId);
                command.ExecuteNonQuery();
            }
        }

        private void SaveActivityEvents(SQLiteConnection connection, SQLiteTransaction transaction, string sessionId, IEnumerable<ActivityEventModel> activityEvents, DateTime now)
        {
            if (activityEvents == null)
                return;

            foreach (ActivityEventModel activityEvent in activityEvents)
            {
                if (activityEvent == null)
                    continue;

                int applicationId = GetOrCreateApplicationId(connection, transaction, activityEvent.AppName, activityEvent.AppName, activityEvent.ProcessPath, now);
                long categoryId = GetOrCreateActivityCategoryId(connection, transaction, activityEvent.Category.ToString());

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        @"
                        INSERT INTO ActivityEvents
                        (
                            SessionId,
                            ApplicationId,
                            ActivityCategoryId,
                            WindowTitle,
                            ActivityStartedAt,
                            ActivityEndedAt
                        )
                        VALUES
                        (
                            @SessionId,
                            @ApplicationId,
                            @ActivityCategoryId,
                            @WindowTitle,
                            @ActivityStartedAt,
                            @ActivityEndedAt
                        );";

                    command.Parameters.AddWithValue("@SessionId", sessionId);
                    command.Parameters.AddWithValue("@ApplicationId", applicationId);
                    command.Parameters.AddWithValue("@ActivityCategoryId", categoryId);

                    command.Parameters.AddWithValue(
                        "@WindowTitle",
                        DataProtectionService.Protect(SafeText(activityEvent.WindowTitle)));

                    command.Parameters.AddWithValue("@ActivityStartedAt", activityEvent.StartTime.ToString("o"));
                    command.Parameters.AddWithValue("@ActivityEndedAt", activityEvent.EndTime.HasValue ? (object)activityEvent.EndTime.Value.ToString("o") : DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        private int GetOrCreateApplicationId(SQLiteConnection connection, SQLiteTransaction transaction, string processName, string displayName, string executablePath, DateTime now)
        {
            string safeProcessName = SafeText(processName);
            string safeDisplayName = SafeText(displayName);
            string safeExecutablePath = SafeText(executablePath);

            if (string.IsNullOrWhiteSpace(safeProcessName))
                safeProcessName = safeDisplayName;
            if (string.IsNullOrWhiteSpace(safeDisplayName))
                safeDisplayName = safeProcessName;
            if (string.IsNullOrWhiteSpace(safeProcessName))
                safeProcessName = "Unknown";
            if (string.IsNullOrWhiteSpace(safeDisplayName))
                safeDisplayName = "Unknown";

            using (SQLiteCommand selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText =
                    @"
                    SELECT ApplicationId
                    FROM Applications
                    WHERE ApplicationProcessName = @ApplicationProcessName
                      AND ExecutablePath = @ExecutablePath
                    LIMIT 1;";
                selectCommand.Parameters.AddWithValue("@ApplicationProcessName", safeProcessName);
                selectCommand.Parameters.AddWithValue("@ExecutablePath", safeExecutablePath);
                object result = selectCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }

            using (SQLiteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    @"
                    INSERT INTO Applications
                    (
                        ApplicationProcessName,
                        ApplicationDisplayName,
                        ExecutablePath,
                        ApplicationDetectedAt
                    )
                    VALUES
                    (
                        @ApplicationProcessName,
                        @ApplicationDisplayName,
                        @ExecutablePath,
                        @ApplicationDetectedAt
                    );";
                insertCommand.Parameters.AddWithValue("@ApplicationProcessName", safeProcessName);
                insertCommand.Parameters.AddWithValue("@ApplicationDisplayName", safeDisplayName);
                insertCommand.Parameters.AddWithValue("@ExecutablePath", safeExecutablePath);
                insertCommand.Parameters.AddWithValue("@ApplicationDetectedAt", now.ToString("o"));
                insertCommand.ExecuteNonQuery();
                return Convert.ToInt32(connection.LastInsertRowId, CultureInfo.InvariantCulture);
            }
        }

        private long GetOrCreateActivityCategoryId(SQLiteConnection connection, SQLiteTransaction transaction, string categoryName)
        {
            string safeCategoryName = SafeText(categoryName);
            if (string.IsNullOrWhiteSpace(safeCategoryName))
                safeCategoryName = AppCategory.Neutral.ToString();

            using (SQLiteCommand selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = "SELECT ActivityCategoryId FROM ActivityCategories WHERE ActivityCategoryName = @ActivityCategoryName LIMIT 1;";
                selectCommand.Parameters.AddWithValue("@ActivityCategoryName", safeCategoryName);
                object result = selectCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }

            using (SQLiteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
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
                        1,
                        100
                    );";
                insertCommand.Parameters.AddWithValue("@ActivityCategoryName", safeCategoryName);
                insertCommand.Parameters.AddWithValue("@ActivityCategoryDescription", safeCategoryName + " activity");
                insertCommand.ExecuteNonQuery();
                return connection.LastInsertRowId;
            }
        }

        private string SafeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Trim();
        }
    }
}