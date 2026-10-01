using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConcentrationTracker.Core.Services
{
    public static class SessionMetricsSnapshot
    {
        private const string FormatVersion = "1";

        public static string Serialize(SessionRecordModel session)
        {
            if (session == null)
                return string.Empty;

            List<string> parts =
                new List<string>
                {
                    "v=" + FormatVersion,
                    Pair("score", session.ConcentrationScore),
                    Pair("quality", session.FocusQuality),
                    Pair("stability", session.FocusStability),
                    Pair("switches", session.TotalSwitches),
                    Pair("disruptive", session.DisruptiveSwitches),
                    Pair("interruptions", session.Interruptions),
                    Pair("idleBreaks", session.IdleBreaks),
                    Pair("session", session.SessionSeconds),
                    Pair("tracked", session.TrackedSeconds),
                    Pair("afk", session.AfkSeconds),
                    Pair("bestBlock", session.BestFocusBlockSeconds),
                    Pair("productive", session.ProductiveSeconds),
                    Pair("neutral", session.NeutralSeconds),
                    Pair("communication", session.CommunicationSeconds),
                    Pair("distraction", session.DistractionSeconds)
                };

            return string.Join(";", parts);
        }

        public static bool TryApply(
            string storedValue,
            SessionRecordModel session)
        {
            if (session == null || string.IsNullOrWhiteSpace(storedValue))
                return false;

            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string part in storedValue.Split(';'))
            {
                int separator = part.IndexOf('=');

                if (separator <= 0)
                    continue;

                values[part.Substring(0, separator).Trim()] = part.Substring(separator + 1).Trim();
            }

            string version;

            if (!values.TryGetValue("v", out version) || version != FormatVersion)
                return false;

            int score, quality, stability;

            if (!TryGetInt(values, "score", out score) ||
                !TryGetInt(values, "quality", out quality) ||
                !TryGetInt(values, "stability", out stability))
            {
                return false;
            }

            session.ConcentrationScore = score;
            session.FocusQuality = quality;
            session.FocusStability = stability;
            session.TotalSwitches = GetInt(values, "switches", session.TotalSwitches);
            session.DisruptiveSwitches = GetInt(values, "disruptive", session.DisruptiveSwitches);
            session.Interruptions = GetInt(values, "interruptions", session.Interruptions);
            session.IdleBreaks = GetInt(values, "idleBreaks", session.IdleBreaks);
            session.SessionSeconds = GetDouble(values, "session", session.SessionSeconds);
            session.TrackedSeconds = GetDouble(values, "tracked", session.TrackedSeconds);
            session.AfkSeconds = GetDouble(values, "afk", session.AfkSeconds);
            session.BestFocusBlockSeconds = GetDouble(values, "bestBlock", session.BestFocusBlockSeconds);
            session.ProductiveSeconds = GetDouble(values, "productive", session.ProductiveSeconds);
            session.NeutralSeconds = GetDouble(values, "neutral", session.NeutralSeconds);
            session.CommunicationSeconds = GetDouble(values, "communication", session.CommunicationSeconds);
            session.DistractionSeconds = GetDouble(values, "distraction", session.DistractionSeconds);

            return true;
        }

        private static string Pair(string key, int value)
        {
            return key + "=" + value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Pair(string key, double value)
        {
            return key + "=" + value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryGetInt(Dictionary<string, string> values, string key, out int result)
        {
            result = 0;
            string text;

            return values.TryGetValue(key, out text) &&
                   int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static int GetInt(Dictionary<string, string> values, string key, int fallback)
        {
            int result;
            return TryGetInt(values, key, out result) ? result : fallback;
        }

        private static double GetDouble(Dictionary<string, string> values, string key, double fallback)
        {
            string text;
            double result;

            if (values.TryGetValue(key, out text) &&
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return fallback;
        }
    }
}
