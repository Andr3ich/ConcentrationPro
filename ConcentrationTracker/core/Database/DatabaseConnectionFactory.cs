using System;
using System.Data.SQLite;
using System.IO;

namespace ConcentrationTracker.Core.Database
{
    public static class DatabaseConnectionFactory
    {
        private const string DatabaseFileName = "concentrationpro.db";

        public static string GetDatabaseDirectory()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(
                localAppData,
                "ConcentrationPro");
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(
                GetDatabaseDirectory(),
                DatabaseFileName);
        }

        public static string GetConnectionString()
        {
            SQLiteConnectionStringBuilder builder =
                new SQLiteConnectionStringBuilder
                {
                    DataSource = GetDatabasePath(),
                    Version = 3,
                    ForeignKeys = true,
                    JournalMode = SQLiteJournalModeEnum.Wal,
                    SyncMode = SynchronizationModes.Normal
                };

            return builder.ToString();
        }

        public static SQLiteConnection CreateConnection()
        {
            EnsureDatabaseDirectory();

            SQLiteConnection connection =
                new SQLiteConnection(
                    GetConnectionString());

            connection.Open();

            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    "PRAGMA foreign_keys = ON;";

                command.ExecuteNonQuery();
            }

            return connection;
        }

        public static void EnsureDatabaseDirectory()
        {
            string directory =
                GetDatabaseDirectory();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
