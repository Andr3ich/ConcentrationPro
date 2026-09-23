using System;
using System.Diagnostics;

namespace ConcentrationTracker.Core.Database
{
    public static class DatabaseInitializer
    {
        private static bool _isInitialized;

        public static void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                DatabaseConnectionFactory.EnsureDatabaseDirectory();

                DatabaseSchemaService schemaService =
                    new DatabaseSchemaService();

                schemaService.CreateOrUpdateSchema();

                _isInitialized =
                    true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Database initialization failed: " +
                    ex.Message);

                throw;
            }
        }
    }
}
