using ConcentrationTracker.Core.Database;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ConcentrationTracker.Core.Services
{
    public class AppCategoryRuleService
    {
        private readonly string _legacyXmlPath;
        private readonly Dictionary<string, Regex> _patternRegexCache =
            new Dictionary<string, Regex>();
        private List<AppCategoryRuleModel> _rules;

        public AppCategoryRuleService()
        {
            DatabaseInitializer.Initialize();
            _legacyXmlPath = GetLegacyXmlPath();
            ImportLegacyXmlIfNeeded();
            _rules = LoadRulesFromDatabase();
        }

        public bool TryClassify(string appName, string windowTitle, out AppCategory category)
        {
            category = AppCategory.Neutral;

            if (_rules == null || _rules.Count == 0)
                return false;

            string normalizedApp = Normalize(appName);
            string normalizedTitle = Normalize(windowTitle);

            foreach (AppCategoryRuleModel rule in _rules)
            {
                if (rule == null || !rule.IsEnabled)
                    continue;

                bool appMatches = MatchesPattern(normalizedApp, rule.AppPattern);
                bool titleMatches = MatchesPattern(normalizedTitle, rule.TitlePattern);

                if (appMatches && titleMatches)
                {
                    category = rule.Category;
                    return true;
                }
            }

            return false;
        }

        public List<AppCategoryRuleModel> GetRules()
        {
            if (_rules == null)
                _rules = LoadRulesFromDatabase();

            return _rules
                .Select(x => new AppCategoryRuleModel
                {
                    AppPattern = x.AppPattern,
                    TitlePattern = x.TitlePattern,
                    Category = x.Category,
                    IsEnabled = x.IsEnabled
                })
                .ToList();
        }

        public void SaveRules(List<AppCategoryRuleModel> rules)
        {
            if (rules == null)
                rules = new List<AppCategoryRuleModel>();

            SaveRulesToDatabase(rules);
            _rules = LoadRulesFromDatabase();
        }

        public void Reload()
        {
            _rules = LoadRulesFromDatabase();
        }

        private List<AppCategoryRuleModel> LoadRulesFromDatabase()
        {
            List<AppCategoryRuleModel> rules = new List<AppCategoryRuleModel>();

            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return rules;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"
                        SELECT
                            r.ApplicationPattern,
                            r.WindowTitlePattern,
                            c.ActivityCategoryName,
                            r.IsCategoryRuleEnabled
                        FROM CategoryRules r
                        INNER JOIN ActivityCategories c
                            ON c.ActivityCategoryId = r.ActivityCategoryId
                        WHERE r.ProfileId = @ProfileId
                        ORDER BY r.RulePriority ASC, r.CategoryRuleId ASC;";

                    command.Parameters.AddWithValue("@ProfileId", profileId);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rules.Add(new AppCategoryRuleModel
                            {
                                AppPattern = GetString(reader, "ApplicationPattern"),
                                TitlePattern = GetString(reader, "WindowTitlePattern"),
                                Category = ParseCategory(GetString(reader, "ActivityCategoryName")),
                                IsEnabled = GetBool(reader, "IsCategoryRuleEnabled")
                            });
                        }
                    }
                }
            }

            if (rules.Count == 0)
            {
                rules = CreateDefaultRules();
                SaveRulesToDatabase(rules);
            }

            return rules;
        }

        private void SaveRulesToDatabase(List<AppCategoryRuleModel> rules)
        {
            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return;

                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    using (SQLiteCommand deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.Transaction = transaction;
                        deleteCommand.CommandText = "DELETE FROM CategoryRules WHERE ProfileId = @ProfileId;";
                        deleteCommand.Parameters.AddWithValue("@ProfileId", profileId);
                        deleteCommand.ExecuteNonQuery();
                    }

                    int priority = 10;

                    foreach (AppCategoryRuleModel rule in rules)
                    {
                        if (rule == null)
                            continue;

                        long categoryId = GetOrCreateActivityCategoryId(connection, transaction, rule.Category.ToString());
                        if (categoryId <= 0)
                            continue;

                        using (SQLiteCommand insertCommand = connection.CreateCommand())
                        {
                            insertCommand.Transaction = transaction;
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
                                    @IsCategoryRuleEnabled,
                                    @RuleCreatedAt,
                                    @RuleUpdatedAt
                                );";

                            DateTime now = DateTime.Now;
                            insertCommand.Parameters.AddWithValue("@ProfileId", profileId);
                            insertCommand.Parameters.AddWithValue("@ActivityCategoryId", categoryId);
                            insertCommand.Parameters.AddWithValue("@ApplicationPattern", SafeText(rule.AppPattern));
                            insertCommand.Parameters.AddWithValue("@WindowTitlePattern", string.IsNullOrWhiteSpace(rule.TitlePattern) ? "*" : rule.TitlePattern.Trim());
                            insertCommand.Parameters.AddWithValue("@RulePriority", priority);
                            insertCommand.Parameters.AddWithValue("@IsCategoryRuleEnabled", rule.IsEnabled ? 1 : 0);
                            insertCommand.Parameters.AddWithValue("@RuleCreatedAt", now.ToString("o"));
                            insertCommand.Parameters.AddWithValue("@RuleUpdatedAt", now.ToString("o"));
                            insertCommand.ExecuteNonQuery();
                        }

                        priority += 10;
                    }

                    transaction.Commit();
                }
            }
        }

        private void ImportLegacyXmlIfNeeded()
        {
            try
            {
                if (!File.Exists(_legacyXmlPath))
                    return;
                if (HasAnyRulesInDatabase())
                    return;

                XmlSerializer serializer = new XmlSerializer(typeof(List<AppCategoryRuleModel>));
                List<AppCategoryRuleModel> legacyRules;

                using (FileStream stream = new FileStream(_legacyXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    legacyRules = serializer.Deserialize(stream) as List<AppCategoryRuleModel>;
                }

                if (legacyRules == null || legacyRules.Count == 0)
                    return;

                SaveRulesToDatabase(legacyRules);
            }
            catch
            {
            }
        }

        private bool HasAnyRulesInDatabase()
        {
            using (SQLiteConnection connection = DatabaseConnectionFactory.CreateConnection())
            {
                long profileId = GetDefaultProfileId(connection);
                if (profileId <= 0)
                    return false;

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM CategoryRules WHERE ProfileId = @ProfileId;";
                    command.Parameters.AddWithValue("@ProfileId", profileId);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private List<AppCategoryRuleModel> CreateDefaultRules()
        {
            return new List<AppCategoryRuleModel>
            {
                new AppCategoryRuleModel { AppPattern = "chrome", TitlePattern = "youtube", Category = AppCategory.Distraction, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "msedge", TitlePattern = "youtube", Category = AppCategory.Distraction, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "firefox", TitlePattern = "youtube", Category = AppCategory.Distraction, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "chrome", TitlePattern = "github", Category = AppCategory.Productive, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "msedge", TitlePattern = "github", Category = AppCategory.Productive, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "telegram", TitlePattern = "*", Category = AppCategory.Communication, IsEnabled = true },
                new AppCategoryRuleModel { AppPattern = "discord", TitlePattern = "*", Category = AppCategory.Communication, IsEnabled = true }
            };
        }

        private bool MatchesPattern(string source, string pattern)
        {
            string normalizedPattern = Normalize(pattern);
            if (string.IsNullOrWhiteSpace(normalizedPattern) || normalizedPattern == "*")
                return true;
            if (string.IsNullOrWhiteSpace(source))
                return false;

            Regex regex;

            if (!_patternRegexCache.TryGetValue(normalizedPattern, out regex))
            {
                string regexPattern =
                    @"(?<![\p{L}\p{N}])" +
                    Regex.Escape(normalizedPattern) +
                    @"(?![\p{L}\p{N}])";

                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                _patternRegexCache[normalizedPattern] = regex;
            }

            return regex.IsMatch(source);
        }

        private long GetDefaultProfileId(SQLiteConnection connection)
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
                    return Convert.ToInt64(result);
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

        private string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Trim().ToLowerInvariant();
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

        private bool GetBool(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return false;
            return Convert.ToInt32(reader.GetValue(ordinal)) != 0;
        }

        private AppCategory ParseCategory(string value)
        {
            AppCategory category;
            if (Enum.TryParse(value, true, out category))
                return category;
            return AppCategory.Neutral;
        }

        private string GetLegacyXmlPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ConcentrationPro", "category-rules.xml");
        }
    }
}