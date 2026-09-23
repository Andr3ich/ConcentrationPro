using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConcentrationTracker.Core.Services
{
    public class AppClassifierService
    {
        private readonly AppCategoryRuleService _ruleService;
        private readonly Dictionary<string, Regex> _keywordRegexCache =
            new Dictionary<string, Regex>();

        public AppClassifierService()
            : this(new AppCategoryRuleService())
        {
        }

        public AppClassifierService(AppCategoryRuleService ruleService)
        {
            _ruleService = ruleService ?? new AppCategoryRuleService();
        }

        public AppCategory Classify(string appName, string windowTitle)
        {
            AppCategory customCategory;

            if (_ruleService.TryClassify(appName, windowTitle, out customCategory))
                return customCategory;

            string app = Normalize(appName);
            string title = Normalize(windowTitle);
            string combined = app + " " + title;

            if (IsSystemApp(app, title, combined))
                return AppCategory.System;

            if (IsDistraction(app, title, combined))
                return AppCategory.Distraction;

            if (IsCommunication(app, title, combined))
                return AppCategory.Communication;

            if (IsProductive(app, title, combined))
                return AppCategory.Productive;

            if (IsNeutral(app, title, combined))
                return AppCategory.Neutral;

            return AppCategory.Neutral;
        }

        private bool IsProductive(string app, string title, string combined)
        {
            if (ContainsAny(app,
                    "winword", "word", "excel", "powerpnt", "powerpoint", "onenote", "notepad", "notepad++",
                    "code", "devenv", "visual studio", "rider", "webstorm", "pycharm", "idea", "datagrip",
                    "ssms", "sql server management studio", "postman", "figma", "photoshop", "illustrator", "xd",
                    "obsidian", "notion", "sublime", "atom"))
            {
                return true;
            }

            if (ContainsAny(title,
                    "visual studio", "vs code", "documentation", "docs", "learn.microsoft.com", "stackoverflow",
                    "stack overflow", "github", "gitlab", "bitbucket", "jira", "trello", "confluence",
                    "google docs", "google sheets", "google slides", "overleaf", "figma", "документ", "документація",
                    "диплом", "курсова", "звіт", "лабораторна", "лекція", "навчання", "презентація"))
            {
                return true;
            }

            return false;
        }

        private bool IsCommunication(string app, string title, string combined)
        {
            if (ContainsAny(app,
                    "telegram", "discord", "slack", "teams", "zoom", "skype", "viber", "whatsapp",
                    "messenger", "outlook", "thunderbird", "mail"))
            {
                return true;
            }

            if (ContainsAny(title,
                    "telegram", "discord", "slack", "microsoft teams", "zoom", "skype", "viber", "whatsapp",
                    "messenger", "gmail", "outlook", "inbox", "mail", "пошта", "чат", "повідомлення"))
            {
                return true;
            }

            return false;
        }

        private bool IsDistraction(string app, string title, string combined)
        {
            if (ContainsAny(app,
                    "steam", "epicgameslauncher", "epic games", "riot client", "battle.net", "blizzard",
                    "spotify", "vlc", "mpc-hc", "media player", "tiktok"))
            {
                return true;
            }

            if (ContainsAny(title,
                    "youtube", "youtu.be", "tiktok", "instagram", "facebook", "twitter", "x.com", "reddit",
                    "netflix", "twitch", "spotify", "steam", "epic games", "game", "gaming", "shorts", "reels",
                    "music", "відео", "серіал", "фільм", "ігри", "гра"))
            {
                return true;
            }

            return false;
        }

        private bool IsSystemApp(string app, string title, string combined)
        {
            if (ContainsAny(app,
                    "applicationframehost", "shellexperiencehost", "searchhost", "startmenuexperiencehost",
                    "systemsettings", "taskmgr", "task manager", "snippingtool", "screenclippinghost", "lockapp",
                    "textinputhost", "runtimebroker", "securityhealthsystray"))
            {
                return true;
            }

            if (ContainsAny(title,
                    "settings", "параметри", "налаштування", "task manager", "диспетчер завдань",
                    "windows security", "безпека windows", "snipping tool", "засіб захоплення", "пошук", "search"))
            {
                return true;
            }

            return false;
        }

        private bool IsNeutral(string app, string title, string combined)
        {
            if (ContainsAny(app,
                    "explorer", "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "acrobat", "acrord32",
                    "photos", "calculator", "calc", "paint", "mspaint"))
            {
                return true;
            }

            if (ContainsAny(title,
                    "google chrome", "microsoft edge", "mozilla firefox", "file explorer", "файловий провідник",
                    "провідник", "downloads", "завантаження", "desktop", "робочий стіл", "pdf", "calculator",
                    "калькулятор", "photos", "фотографії"))
            {
                return true;
            }

            return false;
        }

        private string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToLowerInvariant();
        }

        private bool ContainsAny(string source, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            foreach (string keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                if (MatchesKeyword(source, keyword))
                    return true;
            }

            return false;
        }

        private bool MatchesKeyword(string source, string keyword)
        {
            Regex regex;

            if (!_keywordRegexCache.TryGetValue(keyword, out regex))
            {
                string pattern =
                    @"(?<![\p{L}\p{N}])" +
                    Regex.Escape(keyword) +
                    @"(?![\p{L}\p{N}])";

                regex = new Regex(pattern, RegexOptions.IgnoreCase);
                _keywordRegexCache[keyword] = regex;
            }

            return regex.IsMatch(source);
        }
    }
}