using System.Windows.Media;

namespace ConcentrationTracker.Core.Services
{
    public static class ActivityIconResolverService
    {
        public static ImageSource ResolveIcon(
            string appName,
            string windowTitle,
            string processPath,
            string packageInstallPath,
            string appUserModelId,
            string packageFullName)
        {

            ImageSource packagedOrWin32Icon =
                AppIconService.GetIconFromActivity(
                    processPath,
                    packageInstallPath,
                    appUserModelId,
                    packageFullName);

            return packagedOrWin32Icon;
        }
    }
}
