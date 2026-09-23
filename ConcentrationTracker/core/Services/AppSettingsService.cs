using ConcentrationTracker.Core.Database;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace ConcentrationTracker.Core.Services
{
    public static class AppSettingsService
    {
        public static event Action<AppSettingsModel> SettingsChanged;

        private static readonly string LegacyXmlPath;

        static AppSettingsService()
        {
            DatabaseInitializer.Initialize();

            LegacyXmlPath =
                GetLegacyXmlPath();

            ImportLegacyXmlIfNeeded();
        }

        public static AppSettingsModel LoadSettings()
        {
            AppSettingsModel settings =
                new AppSettingsModel();

            using (SQLiteConnection connection =
                DatabaseConnectionFactory.CreateConnection())
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"
                        SELECT
                            LaunchOnWindowsStartup,
                            LaunchMinimizedToTray,
                            CloseToTray,
                            ShowTrayNotification,
                            IdleThresholdMinutes,
                            IdleGraceSeconds,
                            DefaultDashboardTab,
                            ThemeMode,
                            InterfaceLanguage
                        FROM Profiles
                        ORDER BY ProfileId ASC
                        LIMIT 1;";

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            SetProperty(settings, "StartWithWindows", GetBool(reader, "LaunchOnWindowsStartup"));
                            SetProperty(settings, "StartMinimizedToTray", GetBool(reader, "LaunchMinimizedToTray"));
                            SetProperty(settings, "MinimizeToTray", GetBool(reader, "CloseToTray"));
                            SetProperty(settings, "ShowTrayNotification", GetBool(reader, "ShowTrayNotification"));
                            SetProperty(settings, "IdleThresholdMinutes", GetDouble(reader, "IdleThresholdMinutes"));
                            SetProperty(settings, "PendingIdleGraceSeconds", GetInt(reader, "IdleGraceSeconds"));
                            SetProperty(settings, "DefaultDashboardTab", GetString(reader, "DefaultDashboardTab"));
                            SetProperty(settings, "ThemeMode", GetString(reader, "ThemeMode"));
                            SetProperty(settings, "InterfaceLanguage", GetString(reader, "InterfaceLanguage"));
                        }
                    }
                }
            }

            return settings;
        }

        public static AppSettingsModel GetSettings()
        {
            return LoadSettings();
        }

        public static void SaveSettings(AppSettingsModel settings)
        {
            if (settings == null)
                settings = new AppSettingsModel();

            SaveSettingsToDatabase(settings);
            RaiseSettingsChanged(settings);
        }

        public static void Save(AppSettingsModel settings)
        {
            SaveSettings(settings);
        }

        public static void Reload()
        {
            RaiseSettingsChanged(LoadSettings());
        }

        public static AppSettingsModel ResetToDefaults()
        {
            AppSettingsModel settings = new AppSettingsModel();
            SaveSettings(settings);
            return settings;
        }

        private static void SaveSettingsToDatabase(AppSettingsModel settings)
        {
            using (SQLiteConnection connection =
                DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"
                        UPDATE Profiles
                        SET
                            LaunchOnWindowsStartup = @LaunchOnWindowsStartup,
                            LaunchMinimizedToTray = @LaunchMinimizedToTray,
                            CloseToTray = @CloseToTray,
                            ShowTrayNotification = @ShowTrayNotification,
                            IdleThresholdMinutes = @IdleThresholdMinutes,
                            IdleGraceSeconds = @IdleGraceSeconds,
                            DefaultDashboardTab = @DefaultDashboardTab,
                            ThemeMode = @ThemeMode,
                            InterfaceLanguage = @InterfaceLanguage,
                            ProfileUpdatedAt = @ProfileUpdatedAt
                        WHERE ProfileId = @ProfileId;";

                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    command.Parameters.AddWithValue("@LaunchOnWindowsStartup", GetProperty(settings, "StartWithWindows", false) ? 1 : 0);
                    command.Parameters.AddWithValue("@LaunchMinimizedToTray", GetProperty(settings, "StartMinimizedToTray", false) ? 1 : 0);
                    command.Parameters.AddWithValue("@CloseToTray", GetProperty(settings, "MinimizeToTray", true) ? 1 : 0);
                    command.Parameters.AddWithValue("@ShowTrayNotification", GetProperty(settings, "ShowTrayNotification", true) ? 1 : 0);
                    command.Parameters.AddWithValue("@IdleThresholdMinutes", Clamp(GetProperty(settings, "IdleThresholdMinutes", 2.0), 0.25, 60));
                    command.Parameters.AddWithValue("@IdleGraceSeconds", Clamp(GetProperty(settings, "PendingIdleGraceSeconds", 15), 0, 300));
                    command.Parameters.AddWithValue("@DefaultDashboardTab", NormalizeDashboardTab(GetPropertyAsString(settings, "DefaultDashboardTab", "Charts")));
                    command.Parameters.AddWithValue("@ThemeMode", NormalizeThemeMode(GetPropertyAsString(settings, "ThemeMode", "Light")));
                    command.Parameters.AddWithValue("@InterfaceLanguage", NormalizeLanguage(GetPropertyAsString(settings, "InterfaceLanguage", "English")));
                    command.Parameters.AddWithValue("@ProfileUpdatedAt", DateTime.Now.ToString("o"));

                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ImportLegacyXmlIfNeeded()
        {
            try
            {
                if (!File.Exists(LegacyXmlPath))
                    return;

                XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsModel));
                AppSettingsModel legacySettings;

                using (FileStream stream = new FileStream(LegacyXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    legacySettings = serializer.Deserialize(stream) as AppSettingsModel;
                }

                if (legacySettings == null)
                    return;

                SaveSettingsToDatabase(legacySettings);

                File.Delete(LegacyXmlPath);
            }
            catch
            {

            }
        }

        private static void RaiseSettingsChanged(AppSettingsModel settings)
        {
            Action<AppSettingsModel> handler = SettingsChanged;
            if (handler != null)
                handler(settings);
        }

        private static long GetDefaultProfileId(SQLiteConnection connection)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ProfileId FROM Profiles ORDER BY ProfileId ASC LIMIT 1;";
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt64(result);
            }
        }

        private static string GetLegacyXmlPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ConcentrationPro", "settings.xml");
        }

        private static string NormalizeDashboardTab(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized == "Timeline" || normalized == "Summary" || normalized == "Categories" || normalized == "History")
                return normalized;
            return "Charts";
        }

        private static string NormalizeThemeMode(string value)
        {
            return string.Equals(value, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
        }

        private static string NormalizeLanguage(string value)
        {
            return string.Equals(value, "Ukrainian", StringComparison.OrdinalIgnoreCase) ? "Ukrainian" : "English";
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }

        private static string GetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return string.Empty;
            return reader.GetString(ordinal);
        }

        private static int GetInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return 0;
            return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static double GetDouble(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return 0;
            return Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool GetBool(SQLiteDataReader reader, string columnName)
        {
            return GetInt(reader, columnName) != 0;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            if (target == null)
                return;

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
                return;

            try
            {
                object converted = ConvertValue(value, property.PropertyType);
                property.SetValue(target, converted, null);
            }
            catch
            {
            }
        }

        private static T GetProperty<T>(object source, string propertyName, T fallback)
        {
            if (source == null)
                return fallback;

            PropertyInfo property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanRead)
                return fallback;

            try
            {
                object value = property.GetValue(source, null);
                if (value == null)
                    return fallback;
                if (value is T)
                    return (T)value;
                return (T)ConvertValue(value, typeof(T));
            }
            catch
            {
                return fallback;
            }
        }

        private static string GetPropertyAsString(object source, string propertyName, string fallback)
        {
            object value = GetProperty<object>(source, propertyName, null);
            return value == null ? fallback : value.ToString();
        }

        private static object ConvertValue(object value, Type targetType)
        {
            Type realTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (realTargetType.IsEnum)
            {
                if (value is string)
                    return Enum.Parse(realTargetType, value.ToString(), true);
                return Enum.ToObject(realTargetType, value);
            }

            if (realTargetType == typeof(bool))
            {
                if (value is bool)
                    return value;
                if (value is int)
                    return (int)value != 0;

                bool boolValue;
                if (bool.TryParse(value.ToString(), out boolValue))
                    return boolValue;

                int intValue;
                if (int.TryParse(value.ToString(), out intValue))
                    return intValue != 0;

                return false;
            }

            return Convert.ChangeType(value, realTargetType, CultureInfo.InvariantCulture);
        }
    }
}
