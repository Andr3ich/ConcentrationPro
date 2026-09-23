using System;
using System.Collections.Generic;
using System.IO;

namespace ConcentrationTracker.Core.Services
{
    public static class AppDisplayNameService
    {
        private static readonly Dictionary<string, string> KnownAppNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "winword", "Microsoft Word" },
                { "excel", "Microsoft Excel" },
                { "powerpnt", "Microsoft PowerPoint" },
                { "onenote", "Microsoft OneNote" },
                { "outlook", "Microsoft Outlook" },

                { "chrome", "Google Chrome" },
                { "msedge", "Microsoft Edge" },
                { "firefox", "Mozilla Firefox" },
                { "opera", "Opera" },
                { "brave", "Brave" },
                { "vivaldi", "Vivaldi" },

                { "devenv", "Visual Studio" },
                { "code", "Visual Studio Code" },
                { "rider", "JetBrains Rider" },
                { "webstorm", "WebStorm" },
                { "pycharm", "PyCharm" },
                { "idea", "IntelliJ IDEA" },
                { "datagrip", "DataGrip" },

                { "telegram", "Telegram" },
                { "discord", "Discord" },
                { "slack", "Slack" },
                { "teams", "Microsoft Teams" },
                { "zoom", "Zoom" },
                { "skype", "Skype" },
                { "viber", "Viber" },
                { "whatsapp", "WhatsApp" },

                { "explorer", "File Explorer" },
                { "taskmgr", "Task Manager" },
                { "systemsettings", "Settings" },
                { "applicationframehost", "Windows App" },
                { "searchhost", "Windows Search" },
                { "startmenuexperiencehost", "Start Menu" },
                { "shellexperiencehost", "Windows Shell" },
                { "snippingtool", "Snipping Tool" },
                { "screenclippinghost", "Snipping Tool" },
                { "calculator", "Calculator" },
                { "calc", "Calculator" },
                { "photos", "Photos" },
                { "mspaint", "Paint" },
                { "notepad", "Notepad" },

                { "steam", "Steam" },
                { "spotify", "Spotify" },
                { "vlc", "VLC Media Player" },
                { "obsidian", "Obsidian" },
                { "notion", "Notion" },
                { "figma", "Figma" },
                { "postman", "Postman" },
                { "ssms", "SQL Server Management Studio" }
            };

        public static string GetDisplayName(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return "Unknown";

            string normalized =
                NormalizeAppName(appName);

            if (KnownAppNames.ContainsKey(normalized))
                return KnownAppNames[normalized];

            return HumanizeProcessName(normalized);
        }

        private static string NormalizeAppName(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return string.Empty;

            string result =
                appName.Trim();

            try
            {
                result =
                    Path.GetFileNameWithoutExtension(result);
            }
            catch
            {
            }

            return result.Trim();
        }

        private static string HumanizeProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return "Unknown";

            string result =
                processName.Trim();

            if (result.Length == 1)
                return result.ToUpperInvariant();

            return char.ToUpperInvariant(result[0]) + result.Substring(1);
        }
    }
}
