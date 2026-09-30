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
                    DateTime finishTime = EndTime ?? DateTime.Now;
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
                            SessionState
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
                            SessionRecordedAt
                        )
                        VALUES
                        (
                            @SessionId,
                            @ProfileId,
                            @SessionStartedAt,
                            @SessionEndedAt,
                            @SessionState,
                            @SessionRecordedAt
                        )
                        ON CONFLICT(SessionId) DO UPDATE SET
                            ProfileId = excluded.ProfileId,
                            SessionStartedAt = excluded.SessionStartedAt,
                            SessionEndedAt = excluded.SessionEndedAt,
                            SessionState = excluded.SessionState,
                            SessionRecordedAt = excluded.SessionRecordedAt;";

                    command.Parameters.AddWithValue("@SessionId", SafeText(session.SessionId));
                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    command.Parameters.AddWithValue("@SessionStartedAt", session.StartedAt.ToString("o"));
                    command.Parameters.AddWithValue("@SessionEndedAt", session.EndedAt == DateTime.MinValue ? (object)DBNull.Value : session.EndedAt.ToString("o"));
                    command.Parameters.AddWithValue("@SessionState", NormalizeSessionState(session.Status));
                    command.Parameters.AddWithValue("@SessionRecordedAt", DateTime.Now.ToString("o"));
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

            List<ActivityAggregateRow> rows = LoadActivityRows(connection, sessionId);

            DateTime finishTime = endedAt == DateTime.MinValue ? DateTime.Now : endedAt;
            double sessionSeconds = Math.Max(0, (finishTime - startedAt).TotalSeconds);

            double productiveSeconds = SumCategory(rows, AppCategory.Productive.ToString());
            double neutralSeconds = SumCategory(rows, AppCategory.Neutral.ToString());
            double communicationSeconds = SumCategory(rows, AppCategory.Communication.ToString());
            double distractionSeconds = SumCategory(rows, AppCategory.Distraction.ToString());
            double afkSeconds = SumCategory(rows, "Away") + SumCategory(rows, "AFK");
            double trackedSeconds = Math.Max(0, rows.Sum(x => x.DurationSeconds) - afkSeconds);

            int totalSwitches = CountTransitions(rows);
            int disruptiveSwitches = CountDisruptiveSwitches(rows);
            int interruptions = disruptiveSwitches;
            int idleBreaks = rows.Count(x => IsCategory(x.CategoryName, "Away") || IsCategory(x.CategoryName, "AFK"));

            int focusQuality = CalculatePercentage(productiveSeconds + neutralSeconds, trackedSeconds);
            int focusStability = ClampScore(100 - disruptiveSwitches * 8);
            int concentrationScore = CalculateConcentrationScore(focusQuality, focusStability, distractionSeconds, trackedSeconds);

            string mostUsedApp = GetTopApp(rows, null);
            string mainWorkContext = GetTopApp(rows, new[] { AppCategory.Productive.ToString(), AppCategory.Neutral.ToString() });
            string topDistraction = GetTopApp(rows, new[] { AppCategory.Distraction.ToString() });

            return new SessionRecordModel
            {
                SessionId = sessionId,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Status = sessionState,

                ConcentrationScore = concentrationScore,
                FocusQuality = focusQuality,
                FocusStability = focusStability,

                TotalSwitches = totalSwitches,
                DisruptiveSwitches = disruptiveSwitches,
                Interruptions = interruptions,
                IdleBreaks = idleBreaks,

                SessionSeconds = sessionSeconds,
                TrackedSeconds = trackedSeconds,
                AfkSeconds = afkSeconds,
                BestFocusBlockSeconds = GetBestFocusBlockSeconds(rows),

                ProductiveSeconds = productiveSeconds,
                NeutralSeconds = neutralSeconds,
                CommunicationSeconds = communicationSeconds,
                DistractionSeconds = distractionSeconds,

                MostUsedApp = mostUsedApp,
                MainWorkContext = mainWorkContext,
                TopDistraction = topDistraction,
                TopInterrupter = topDistraction,
                Insight = BuildInsight(concentrationScore, disruptiveSwitches, distractionSeconds)
            };
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

        private double GetBestFocusBlockSeconds(List<ActivityAggregateRow> rows)
        {
            double best = 0;
            double current = 0;

            foreach (ActivityAggregateRow row in rows)
            {
                bool isFocus = IsCategory(row.CategoryName, AppCategory.Productive.ToString()) || IsCategory(row.CategoryName, AppCategory.Neutral.ToString());
                if (isFocus)
                {
                    current += row.DurationSeconds;
                    if (current > best)
                        best = current;
                }
                else
                {
                    current = 0;
                }
            }

            return best;
        }

        private int CalculatePercentage(double value, double total)
        {
            if (total <= 0)
                return 0;
            return ClampScore((int)Math.Round(value / total * 100));
        }

        private int CalculateConcentrationScore(int focusQuality, int focusStability, double distractionSeconds, double trackedSeconds)
        {
            int distractionPenalty = 0;
            if (trackedSeconds > 0)
                distractionPenalty = (int)Math.Round(distractionSeconds / trackedSeconds * 35);

            int score = (int)Math.Round(focusQuality * 0.65 + focusStability * 0.35) - distractionPenalty;
            return ClampScore(score);
        }

        private int ClampScore(int score)
        {
            if (score < 0)
                return 0;
            if (score > 100)
                return 100;
            return score;
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
