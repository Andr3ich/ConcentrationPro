using ConcentrationTracker.Core.Database;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace ConcentrationTracker.Core.Services
{
    public class SessionDetailsReadService
    {
        private readonly FocusBlockReconstructionService _focusBlockReconstructionService = new FocusBlockReconstructionService();

        private class ActivityRow
        {
            public string AppName { get; set; }
            public string WindowTitle { get; set; }
            public string Category { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public double DurationSeconds { get; set; }
        }

        public SessionDetailsModel LoadDetails(string sessionId)
        {
            SessionDetailsModel details = new SessionDetailsModel();

            if (string.IsNullOrWhiteSpace(sessionId))
                return details;

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                List<ActivityRow> rows = LoadActivityRows(connection, sessionId);
                FillActivityEvents(details, rows);
                FillSwitchEvents(details, rows);
                FillFocusBlocks(details, rows);
            }

            return details;
        }

        private List<ActivityRow> LoadActivityRows(SQLiteConnection connection, string sessionId)
        {
            List<ActivityRow> rows = new List<ActivityRow>();

            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    @"
                    SELECT
                        COALESCE(a.ApplicationDisplayName, a.ApplicationProcessName, '') AS AppName,
                        e.WindowTitle,
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
                        DateTime startTime = ParseDateTime(GetString(reader, "ActivityStartedAt"), DateTime.Now);
                        DateTime? endTime = ParseNullableDateTime(GetString(reader, "ActivityEndedAt"));
                        DateTime finishTime = endTime ?? startTime;
                        double durationSeconds = Math.Max(0, (finishTime - startTime).TotalSeconds);

                        rows.Add(new ActivityRow
                        {
                            AppName = GetString(reader, "AppName"),

                            WindowTitle = DataProtectionService.Unprotect(GetString(reader, "WindowTitle")),

                            Category = GetString(reader, "ActivityCategoryName"),
                            StartTime = startTime,
                            EndTime = endTime,
                            DurationSeconds = durationSeconds
                        });
                    }
                }
            }

            return rows;
        }

        private void FillActivityEvents(SessionDetailsModel details, List<ActivityRow> rows)
        {
            foreach (ActivityRow row in rows)
            {
                details.ActivityEvents.Add(new SessionDetailActivityModel
                {
                    AppName = row.AppName,
                    WindowTitle = row.WindowTitle,
                    Category = row.Category,
                    StartText = FormatDateTime(row.StartTime),
                    EndText = row.EndTime.HasValue ? FormatDateTime(row.EndTime.Value) : "-",
                    DurationText = FormatDuration(row.DurationSeconds)
                });
            }
        }

        private void FillSwitchEvents(SessionDetailsModel details, List<ActivityRow> rows)
        {
            for (int index = 1; index < rows.Count; index++)
            {
                ActivityRow previous = rows[index - 1];
                ActivityRow current = rows[index];

                bool changedApp = !string.Equals(previous.AppName, current.AppName, StringComparison.OrdinalIgnoreCase);
                bool changedCategory = !string.Equals(previous.Category, current.Category, StringComparison.OrdinalIgnoreCase);
                bool changedWindow = !string.Equals(previous.WindowTitle, current.WindowTitle, StringComparison.OrdinalIgnoreCase);

                if (!changedApp && !changedCategory && !changedWindow)
                    continue;

                bool isDisruptive = string.Equals(current.Category, AppCategory.Distraction.ToString(), StringComparison.OrdinalIgnoreCase);

                details.SwitchEvents.Add(new SessionDetailSwitchModel
                {
                    TimeText = FormatDateTime(current.StartTime),
                    FromCategory = previous.Category,
                    ToCategory = current.Category,
                    ToAppName = current.AppName,
                    ToWindowTitle = current.WindowTitle,
                    IsDisruptiveText = isDisruptive ? "Yes" : "No"
                });
            }
        }

        private void FillFocusBlocks(SessionDetailsModel details, List<ActivityRow> rows)
        {
            List<ActivityEventModel> events = new List<ActivityEventModel>();

            foreach (ActivityRow row in rows)
            {
                AppCategory category;

                if (!Enum.TryParse(row.Category, true, out category))
                    continue;

                events.Add(new ActivityEventModel
                {
                    AppName = row.AppName,
                    WindowTitle = row.WindowTitle,
                    Category = category,
                    StartTime = row.StartTime,
                    EndTime = row.EndTime
                });
            }

            if (events.Count == 0)
                return;

            DateTime sessionEnd = events.Max(x => x.EndTime ?? x.StartTime);

            FocusBlockReconstructionResult result =
                _focusBlockReconstructionService.Reconstruct(events, sessionEnd);

            foreach (FocusBlockModel block in result.FocusBlocks)
            {
                details.FocusBlocks.Add(new SessionDetailFocusBlockModel
                {
                    MainApp = block.MainApp,
                    LastWindowTitle = block.LastWindowTitle,
                    Category = block.Category.ToString(),
                    StartText = FormatDateTime(block.StartTime),
                    EndText = block.EndTime.HasValue ? FormatDateTime(block.EndTime.Value) : "-",
                    DurationText = FormatDuration(block.AccumulatedDuration.TotalSeconds),
                    BreakReason = GetBreakReasonText(block.BreakReason)
                });
            }
        }

        private string GetBreakReasonText(FocusBreakReason reason)
        {
            switch (reason)
            {
                case FocusBreakReason.ContextChanged:
                    return "Context changed";
                case FocusBreakReason.Idle:
                    return "Idle";
                case FocusBreakReason.SessionPaused:
                case FocusBreakReason.SessionReset:
                    return "Saved session end";
                case FocusBreakReason.Distraction:
                    return "Distraction";
                case FocusBreakReason.Communication:
                    return "Communication";
                default:
                    return string.Empty;
            }
        }

        private string GetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return string.Empty;
            return reader.GetString(ordinal);
        }

        private DateTime ParseDateTime(string value, DateTime fallback)
        {
            DateTime parsed;
            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
                return parsed;
            return fallback;
        }

        private DateTime? ParseNullableDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            DateTime parsed;
            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
                return parsed;
            return null;
        }

        private string FormatDateTime(DateTime value)
        {
            return value.ToString("HH:mm:ss");
        }

        private string FormatDuration(double seconds)
        {
            if (seconds < 0)
                seconds = 0;

            TimeSpan duration = TimeSpan.FromSeconds(seconds);

            if (duration.TotalHours >= 1)
                return string.Format("{0}h {1}m {2}s", (int)duration.TotalHours, duration.Minutes, duration.Seconds);

            if (duration.TotalMinutes >= 1)
                return string.Format("{0}m {1}s", duration.Minutes, duration.Seconds);

            return string.Format("{0}s", Math.Round(duration.TotalSeconds));
        }
    }
}