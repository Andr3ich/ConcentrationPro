using ConcentrationTracker.Core.Database;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ConcentrationTracker.Core.Services
{
    public class SessionHistoryStorageService
    {
        private readonly string _legacyXmlPath;
        private readonly ConcentrationMetricsService _metricsService = new ConcentrationMetricsService();
        private readonly FocusBlockReconstructionService _focusBlockReconstructionService = new FocusBlockReconstructionService();

        private class ActivityAggregateRow
        {
            public string AppName { get; set; }
            public string CategoryName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }

            public double DurationSeconds
            {
                get
                {
                    DateTime finishTime = EndTime ?? StartTime;
                    return Math.Max(0, (finishTime - StartTime).TotalSeconds);
                }
            }
        }

        public SessionHistoryStorageService()
        {
            DatabaseInitializer.Initialize();
            _legacyXmlPath = GetLegacyXmlPath();
            ImportLegacyXmlIfNeeded();
        }

        public List<SessionRecordModel> LoadSessions()
        {
            List<SessionRecordModel> sessions = new List<SessionRecordModel>();

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return sessions;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"
                        SELECT
                            SessionId,
                            SessionStartedAt,
                            SessionEndedAt,
                            SessionState,
                            SessionMetrics
                        FROM Sessions
                        WHERE ProfileId = @ProfileId
                        ORDER BY SessionStartedAt DESC;";
                    command.Parameters.AddWithValue("@ProfileId", profileId);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            sessions.Add(ReadSessionRecord(connection, reader));
                    }
                }
            }

            return sessions;
        }

        public void SaveOrUpdate(SessionRecordModel session)
        {
            if (session == null)
                return;

            if (string.IsNullOrWhiteSpace(session.SessionId))
                session.SessionId = Guid.NewGuid().ToString();

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"
                        INSERT INTO Sessions
                        (
                            SessionId,
                            ProfileId,
                            SessionStartedAt,
                            SessionEndedAt,
                            SessionState,
                            SessionRecordedAt,
                            SessionMetrics
                        )
                        VALUES
                        (
                            @SessionId,
                            @ProfileId,
                            @SessionStartedAt,
                            @SessionEndedAt,
                            @SessionState,
                            @SessionRecordedAt,
                            @SessionMetrics
                        )
                        ON CONFLICT(SessionId) DO UPDATE SET
                            ProfileId = excluded.ProfileId,
                            SessionStartedAt = excluded.SessionStartedAt,
                            SessionEndedAt = excluded.SessionEndedAt,
                            SessionState = excluded.SessionState,
                            SessionRecordedAt = excluded.SessionRecordedAt,
                            SessionMetrics = excluded.SessionMetrics;";

                    command.Parameters.AddWithValue("@SessionId", SafeText(session.SessionId));
                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    command.Parameters.AddWithValue("@SessionStartedAt", session.StartedAt.ToString("o"));
                    command.Parameters.AddWithValue("@SessionEndedAt", session.EndedAt == DateTime.MinValue ? (object)DBNull.Value : session.EndedAt.ToString("o"));
                    command.Parameters.AddWithValue("@SessionState", NormalizeSessionState(session.Status));
                    command.Parameters.AddWithValue("@SessionRecordedAt", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("@SessionMetrics", SessionMetricsSnapshot.Serialize(session));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Sessions WHERE SessionId = @SessionId;";
                    command.Parameters.AddWithValue("@SessionId", sessionId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ClearSessions()
        {
            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Sessions WHERE ProfileId = @ProfileId;";
                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private SessionRecordModel ReadSessionRecord(SQLiteConnection connection, SQLiteDataReader reader)
        {
            string sessionId = GetString(reader, "SessionId");
            DateTime startedAt = GetDateTime(reader, "SessionStartedAt");
            DateTime endedAt = GetDateTimeOrDefault(reader, "SessionEndedAt");
            string sessionState = GetString(reader, "SessionState");
            string storedMetrics = GetString(reader, "SessionMetrics");

            List<ActivityAggregateRow> rows = LoadActivityRows(connection, sessionId);

            DateTime finishTime = endedAt == DateTime.MinValue
                ? rows.Select(x => x.EndTime ?? x.StartTime).DefaultIfEmpty(startedAt).Max()
                : endedAt;

            foreach (ActivityAggregateRow row in rows)
            {
                if (!row.EndTime.HasValue)
                    row.EndTime = finishTime > row.StartTime ? finishTime : row.StartTime;
            }

            double afkSeconds = SumCategory(rows, "Away") + SumCategory(rows, "AFK");

            SessionRecordModel session = new SessionRecordModel
            {
                SessionId = sessionId,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Status = sessionState,

                SessionSeconds = Math.Max(0, (finishTime - startedAt).TotalSeconds),
                AfkSeconds = afkSeconds,
                IdleBreaks = rows.Count(x => IsCategory(x.CategoryName, "Away") || IsCategory(x.CategoryName, "AFK")),

                ProductiveSeconds = SumCategory(rows, AppCategory.Productive.ToString()),
                NeutralSeconds = SumCategory(rows, AppCategory.Neutral.ToString()),
                CommunicationSeconds = SumCategory(rows, AppCategory.Communication.ToString()),
                DistractionSeconds = SumCategory(rows, AppCategory.Distraction.ToString()),

                MostUsedApp = GetTopApp(rows, null),
                MainWorkContext = GetTopApp(rows, new[] { AppCategory.Productive.ToString(), AppCategory.Neutral.ToString() }),
                TopDistraction = GetTopApp(rows, new[] { AppCategory.Distraction.ToString() })
            };

            session.TopInterrupter = session.TopDistraction;

            if (!SessionMetricsSnapshot.TryApply(storedMetrics, session))
                ApplyCalculatedMetrics(session, rows, finishTime);

            session.Insight = BuildInsight(session.ConcentrationScore, session.DisruptiveSwitches, session.DistractionSeconds);

            return session;
        }

        private void ApplyCalculatedMetrics(
            SessionRecordModel session,
            List<ActivityAggregateRow> rows,
            DateTime finishTime)
        {
            List<ActivityEventModel> events = new List<ActivityEventModel>();

            foreach (ActivityAggregateRow row in rows)
            {
                AppCategory category;

                if (!Enum.TryParse(row.CategoryName, true, out category))
                    continue;

                events.Add(new ActivityEventModel
                {
                    AppName = row.AppName,
                    Category = category,
                    StartTime = row.StartTime,
                    EndTime = row.EndTime
                });
            }

            DateTime metricsNow = events.Count == 0
                ? finishTime
                : events.Max(x => x.EndTime ?? x.StartTime);

            TimeSpan trackedElapsed = TimeSpan.FromSeconds(
                events.Sum(x => Math.Max(0, ((x.EndTime ?? x.StartTime) - x.StartTime).TotalSeconds)));

            FocusBlockReconstructionResult focus =
                _focusBlockReconstructionService.Reconstruct(events, metricsNow);

            session.FocusQuality = _metricsService.CalculateFocusQualityPercent(
                events,
                focus.FocusBlocks,
                trackedElapsed,
                focus.InterruptionCount,
                metricsNow);

            session.FocusStability = _metricsService.CalculateFocusStabilityPercent(
                events,
                trackedElapsed,
                metricsNow);

            session.ConcentrationScore = _metricsService.CalculateConcentrationScore(
                events,
                focus.FocusBlocks,
                trackedElapsed,
                focus.InterruptionCount,
                metricsNow);

            session.TrackedSeconds = trackedElapsed.TotalSeconds;
            session.Interruptions = focus.InterruptionCount;
            session.BestFocusBlockSeconds = focus.BestFocusBlockDuration.TotalSeconds;
            session.TotalSwitches = CountTransitions(rows);
            session.DisruptiveSwitches = CountDisruptiveSwitches(rows);
        }

        private List<ActivityAggregateRow> LoadActivityRows(SQLiteConnection connection, string sessionId)
        {
            List<ActivityAggregateRow> rows = new List<ActivityAggregateRow>();

            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    @"
                    SELECT
                        COALESCE(a.ApplicationDisplayName, a.ApplicationProcessName, '') AS AppName,
                        c.ActivityCategoryName,
                        e.ActivityStartedAt,
                        e.ActivityEndedAt
                    FROM ActivityEvents e
                    INNER JOIN Applications a
                        ON a.ApplicationId = e.ApplicationId
                    INNER JOIN ActivityCategories c
                        ON c.ActivityCategoryId = e.ActivityCategoryId
                    WHERE e.SessionId = @SessionId
                    ORDER BY e.ActivityStartedAt ASC;";
                command.Parameters.AddWithValue("@SessionId", sessionId);

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ActivityAggregateRow
                        {
                            AppName = GetString(reader, "AppName"),
                            CategoryName = GetString(reader, "ActivityCategoryName"),
                            StartTime = ParseDateTime(GetString(reader, "ActivityStartedAt"), DateTime.Now),
                            EndTime = ParseNullableDateTime(GetString(reader, "ActivityEndedAt"))
                        });
                    }
                }
            }

            return rows;
        }

        private double SumCategory(IEnumerable<ActivityAggregateRow> rows, string categoryName)
        {
            return rows.Where(x => IsCategory(x.CategoryName, categoryName)).Sum(x => x.DurationSeconds);
        }

        private bool IsCategory(string value, string categoryName)
        {
            return string.Equals(value, categoryName, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSameActivity(ActivityAggregateRow previous, ActivityAggregateRow current)
        {
            return IsCategory(previous.CategoryName, current.CategoryName) &&
                   string.Equals(previous.AppName, current.AppName, StringComparison.OrdinalIgnoreCase);
        }

        private int CountTransitions(List<ActivityAggregateRow> rows)
        {
            int count = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                if (!IsSameActivity(rows[i - 1], rows[i]))
                    count++;
            }
            return count;
        }

        private int CountDisruptiveSwitches(List<ActivityAggregateRow> rows)
        {
            int count = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                if (IsSameActivity(rows[i - 1], rows[i]))
                    continue;

                if (IsCategory(rows[i].CategoryName, AppCategory.Distraction.ToString()))
                    count++;
            }
            return count;
        }

        private string GetTopApp(IEnumerable<ActivityAggregateRow> rows, string[] categoryFilter)
        {
            IEnumerable<ActivityAggregateRow> filtered = rows;
            if (categoryFilter != null && categoryFilter.Length > 0)
                filtered = filtered.Where(x => categoryFilter.Any(category => IsCategory(x.CategoryName, category)));

            var topApp = filtered
                .Where(x => !string.IsNullOrWhiteSpace(x.AppName))
                .GroupBy(x => x.AppName)
                .Select(group => new { AppName = group.Key, Seconds = group.Sum(x => x.DurationSeconds) })
                .OrderByDescending(x => x.Seconds)
                .FirstOrDefault();

            if (topApp == null || topApp.Seconds <= 0)
                return LocalizationService.GetString("Common_NoData");

            return topApp.AppName;
        }

        private string BuildInsight(int score, int disruptiveSwitches, double distractionSeconds)
        {
            if (score >= 80)
                return LocalizationService.GetString("Vm_StrongFocusSession");
            if (disruptiveSwitches > 5)
                return LocalizationService.GetString("Vm_SwitchDrop");
            if (distractionSeconds > 0)
                return LocalizationService.GetString("History_DistractionAffectedScore");
            return LocalizationService.GetString("History_SessionSavedSuccessfully");
        }

        private void ImportLegacyXmlIfNeeded()
        {
            try
            {
                if (!File.Exists(_legacyXmlPath))
                    return;
                if (HasAnySessions())
                    return;

                XmlSerializer serializer = new XmlSerializer(typeof(List<SessionRecordModel>));
                List<SessionRecordModel> legacySessions;
                using (FileStream stream = new FileStream(_legacyXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    legacySessions = serializer.Deserialize(stream) as List<SessionRecordModel>;
                }

                if (legacySessions == null || legacySessions.Count == 0)
                    return;

                foreach (SessionRecordModel session in legacySessions)
                    SaveOrUpdate(session);
            }
            catch
            {
            }
        }

        private bool HasAnySessions()
        {
            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return false;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Sessions WHERE ProfileId = @ProfileId;";
                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private long GetDefaultProfileId(SQLiteConnection connection)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ProfileId FROM Profiles ORDER BY ProfileId ASC LIMIT 1;";
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
        }

        private string NormalizeSessionState(string status)
        {
            string value = SafeText(status);
            if (string.IsNullOrWhiteSpace(value))
                return "Saved";

            string normalized = value.ToLowerInvariant();
            if (normalized.Contains("reset"))
                return "Reset";
            if (normalized.Contains("active"))
                return "Active";
            if (normalized.Contains("pause") || normalized.Contains("paused") || normalized.Contains("пауз"))
                return "Paused";
            return "Saved";
        }

        private string GetLegacyXmlPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ConcentrationPro", "session-history.xml");
        }

        private string SafeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Trim();
        }

        private string GetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return string.Empty;
            return reader.GetString(ordinal);
        }

        private DateTime GetDateTime(SQLiteDataReader reader, string columnName)
        {
            return ParseDateTime(GetString(reader, columnName), DateTime.Now);
        }

        private DateTime GetDateTimeOrDefault(SQLiteDataReader reader, string columnName)
        {
            DateTime? parsed = ParseNullableDateTime(GetString(reader, columnName));
            return parsed ?? DateTime.MinValue;
        }

        private DateTime ParseDateTime(string value, DateTime fallback)
        {
            DateTime result;
            if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out result))
                return result;
            return fallback;
        }

        private DateTime? ParseNullableDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            DateTime result;
            if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out result))
                return result;
            return null;
        }
    }
}
