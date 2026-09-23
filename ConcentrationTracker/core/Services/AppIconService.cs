using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace ConcentrationTracker.Core.Services
{
    public static class AppIconService
    {
        private static readonly Dictionary<string, ImageSource> IconCache =
            new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public static ImageSource GetIconFromActivity(
            string processPath,
            string packageInstallPath,
            string appUserModelId,
            string packageFullName)
        {
            ImageSource packagedIcon =
                GetIconFromPackage(
                    packageInstallPath,
                    appUserModelId,
                    packageFullName);

            if (packagedIcon != null)
                return packagedIcon;

            return GetIconFromPath(processPath);
        }

        public static ImageSource GetIconFromPath(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
                return null;

            if (!File.Exists(processPath))
                return null;

            string cacheKey = "win32|" + processPath;

            if (IconCache.ContainsKey(cacheKey))
                return IconCache[cacheKey];

            try
            {
                using (System.Drawing.Icon icon =
                    System.Drawing.Icon.ExtractAssociatedIcon(processPath))
                {
                    if (icon == null)
                        return null;

                    ImageSource imageSource =
                        Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(32, 32));

                    imageSource.Freeze();

                    IconCache[cacheKey] = imageSource;

                    return imageSource;
                }
            }
            catch
            {
                return null;
            }
        }

        private static ImageSource GetIconFromPackage(
            string packageInstallPath,
            string appUserModelId,
            string packageFullName)
        {
            if (string.IsNullOrWhiteSpace(packageInstallPath))
                return null;

            if (!Directory.Exists(packageInstallPath))
                return null;

            string cacheKey =
                "package|" + packageInstallPath + "|" + appUserModelId + "|" + packageFullName;

            if (IconCache.ContainsKey(cacheKey))
                return IconCache[cacheKey];

            try
            {
                string manifestPath =
                    Path.Combine(packageInstallPath, "AppxManifest.xml");

                if (!File.Exists(manifestPath))
                    return null;

                string relativeLogoPath =
                    GetLogoPathFromManifest(
                        manifestPath,
                        appUserModelId);

                if (string.IsNullOrWhiteSpace(relativeLogoPath))
                    return null;

                string logoFile =
                    ResolveBestLogoFile(
                        packageInstallPath,
                        relativeLogoPath);

                if (string.IsNullOrWhiteSpace(logoFile) ||
                    !File.Exists(logoFile))
                {
                    return null;
                }

                ImageSource imageSource =
                    LoadImageFromFile(logoFile);

                if (imageSource == null)
                    return null;

                IconCache[cacheKey] = imageSource;

                return imageSource;
            }
            catch
            {
                return null;
            }
        }

        private static string GetLogoPathFromManifest(
            string manifestPath,
            string appUserModelId)
        {
            XDocument document =
                XDocument.Load(manifestPath);

            string appId =
                ExtractApplicationIdFromAumid(appUserModelId);

            List<XElement> applications =
                document
                    .Descendants()
                    .Where(x => x.Name.LocalName == "Application")
                    .ToList();

            XElement application = null;

            if (!string.IsNullOrWhiteSpace(appId))
            {
                application =
                    applications.FirstOrDefault(x =>
                        string.Equals(
                            GetAttributeValue(x, "Id"),
                            appId,
                            StringComparison.OrdinalIgnoreCase));
            }

            if (application == null)
                application = applications.FirstOrDefault();

            if (application == null)
                return string.Empty;

            XElement visualElements =
                application
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "VisualElements");

            if (visualElements == null)
                return string.Empty;

            string logo =
                GetAttributeValue(visualElements, "Square44x44Logo");

            if (string.IsNullOrWhiteSpace(logo))
                logo = GetAttributeValue(visualElements, "Square150x150Logo");

            if (string.IsNullOrWhiteSpace(logo))
                logo = GetAttributeValue(visualElements, "Logo");

            return logo;
        }

        private static string GetAttributeValue(
            XElement element,
            string attributeName)
        {
            if (element == null)
                return string.Empty;

            XAttribute attribute =
                element
                    .Attributes()
                    .FirstOrDefault(x =>
                        x.Name.LocalName.Equals(
                            attributeName,
                            StringComparison.OrdinalIgnoreCase));

            return attribute?.Value ?? string.Empty;
        }

        private static string ExtractApplicationIdFromAumid(
            string appUserModelId)
        {
            if (string.IsNullOrWhiteSpace(appUserModelId))
                return string.Empty;

            int separatorIndex =
                appUserModelId.LastIndexOf('!');

            if (separatorIndex < 0 ||
                separatorIndex >= appUserModelId.Length - 1)
            {
                return string.Empty;
            }

            return appUserModelId.Substring(separatorIndex + 1);
        }

        private static string ResolveBestLogoFile(
            string packageInstallPath,
            string relativeLogoPath)
        {
            string normalizedRelativePath =
                relativeLogoPath.Replace('/', Path.DirectorySeparatorChar);

            string exactPath =
                Path.Combine(packageInstallPath, normalizedRelativePath);

            if (File.Exists(exactPath))
                return exactPath;

            string directory =
                Path.GetDirectoryName(exactPath);

            if (string.IsNullOrWhiteSpace(directory) ||
                !Directory.Exists(directory))
            {
                return string.Empty;
            }

            string baseName =
                Path.GetFileNameWithoutExtension(exactPath);

            if (string.IsNullOrWhiteSpace(baseName))
                return string.Empty;

            List<string> candidates =
                Directory
                    .GetFiles(directory, baseName + "*.png", SearchOption.TopDirectoryOnly)
                    .ToList();

            if (candidates.Count == 0)
                return string.Empty;

            return candidates
                .OrderByDescending(GetLogoCandidateScore)
                .ThenByDescending(x => new FileInfo(x).Length)
                .FirstOrDefault();
        }

        private static int GetLogoCandidateScore(string path)
        {
            string fileName =
                Path.GetFileName(path).ToLowerInvariant();

            int score = 0;

            if (fileName.Contains("targetsize-48"))
                score += 1000;
            else if (fileName.Contains("targetsize-44"))
                score += 950;
            else if (fileName.Contains("targetsize-64"))
                score += 900;
            else if (fileName.Contains("targetsize-32"))
                score += 850;

            if (fileName.Contains("scale-200"))
                score += 700;
            else if (fileName.Contains("scale-150"))
                score += 650;
            else if (fileName.Contains("scale-100"))
                score += 600;

            if (fileName.Contains("altform-unplated"))
                score += 50;

            if (fileName.Contains("contrast-white") ||
                fileName.Contains("contrast-black"))
            {
                score -= 100;
            }

            return score;
        }

        private static ImageSource LoadImageFromFile(string filePath)
        {
            try
            {
                BitmapImage bitmap =
                    new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.DecodePixelWidth = 32;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
