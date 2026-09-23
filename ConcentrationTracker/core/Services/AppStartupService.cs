using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace ConcentrationTracker.Core.Services
{
    public static class AppStartupService
    {
        private const string RunKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string AppRegistryName =
            "ConcentrationPro";

        public static bool IsStartWithWindowsEnabled()
        {
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null)
                        return false;

                    object value =
                        key.GetValue(AppRegistryName);

                    return value != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool SetStartWithWindowsEnabled(
            bool isEnabled)
        {
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null)
                        return false;

                    if (isEnabled)
                    {
                        string executablePath =
                            GetCurrentExecutablePath();

                        if (string.IsNullOrWhiteSpace(executablePath))
                            return false;

                        string startupCommand =
                            "\"" + executablePath + "\" --minimized";

                        key.SetValue(
                            AppRegistryName,
                            startupCommand,
                            RegistryValueKind.String);
                    }
                    else
                    {
                        if (key.GetValue(AppRegistryName) != null)
                        {
                            key.DeleteValue(
                                AppRegistryName,
                                false);
                        }
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetCurrentExecutablePath()
        {
            try
            {
                Process currentProcess =
                    Process.GetCurrentProcess();

                if (currentProcess.MainModule != null &&
                    !string.IsNullOrWhiteSpace(currentProcess.MainModule.FileName))
                {
                    return currentProcess.MainModule.FileName;
                }
            }
            catch
            {
            }

            return string.Empty;
        }
    }
}
