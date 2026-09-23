using System;
using System.Security.Cryptography;
using System.Text;

namespace ConcentrationTracker.Core.Services
{
    public static class DataProtectionService
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("ConcentrationPro.WindowTitle.v1");

        private const string ProtectedPrefix = "dp1:";

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText ?? string.Empty;

            try
            {
                byte[] plainBytes =
                    Encoding.UTF8.GetBytes(plainText);

                byte[] protectedBytes =
                    ProtectedData.Protect(
                        plainBytes,
                        Entropy,
                        DataProtectionScope.CurrentUser);

                return ProtectedPrefix + Convert.ToBase64String(protectedBytes);
            }
            catch
            {
                return plainText;
            }
        }

        public static string Unprotect(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue))
                return storedValue ?? string.Empty;

            if (!storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
            {
                return storedValue;
            }

            try
            {
                byte[] protectedBytes =
                    Convert.FromBase64String(
                        storedValue.Substring(ProtectedPrefix.Length));

                byte[] plainBytes =
                    ProtectedData.Unprotect(
                        protectedBytes,
                        Entropy,
                        DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return "[protected]";
            }
        }
    }
}