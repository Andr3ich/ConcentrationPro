using ConcentrationTracker.MVVM.Model;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace ConcentrationTracker.Core.Services
{
    public class WindowTrackingService
    {
        public event Action<ActiveWindowInfo> OnWindowSwitched;

        private readonly DispatcherTimer _timer;

        private string _lastWindowKey;
        private bool _isRunning;

        private const int ProcessQueryLimitedInformation = 0x1000;
        private const int ErrorSuccess = 0;
        private const int ErrorInsufficientBuffer = 122;
        private const int AppModelErrorNoPackage = 15700;

        public WindowTrackingService()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += (s, e) => CheckActiveWindow();
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _lastWindowKey = string.Empty;

            CheckActiveWindow();
            _timer.Start();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _timer.Stop();
            _isRunning = false;
            _lastWindowKey = string.Empty;
        }

        public ActiveWindowInfo CaptureActiveWindow()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();

            if (foregroundWindowHandle == IntPtr.Zero)
                return null;

            int foregroundProcessId = GetWindowProcessId(foregroundWindowHandle);

            if (foregroundProcessId <= 0)
                return null;

            string windowTitle = GetWindowTitle(foregroundWindowHandle);

            int effectiveProcessId = ResolveEffectiveProcessId(
                foregroundWindowHandle,
                foregroundProcessId);

            string appName = "Unknown";
            string processPath = GetProcessPathByWinApi(effectiveProcessId);

            using (Process process = GetProcessByIdSafe(effectiveProcessId))
            {
                if (process != null)
                    appName = GetProcessNameSafe(process);

                if (string.IsNullOrWhiteSpace(processPath) && process != null)
                    processPath = GetProcessPathByMainModule(process);
            }

            string appUserModelId = GetApplicationUserModelIdSafe(effectiveProcessId);
            string packageFullName = GetPackageFullNameSafe(effectiveProcessId);
            string packageInstallPath = GetPackageInstallPathSafe(packageFullName);

            if (string.IsNullOrWhiteSpace(windowTitle))
                windowTitle = appName;

            return new ActiveWindowInfo
            {
                WindowHandle = foregroundWindowHandle,
                ProcessId = effectiveProcessId,
                AppName = appName,
                ProcessPath = processPath,
                AppUserModelId = appUserModelId,
                PackageFullName = packageFullName,
                PackageInstallPath = packageInstallPath,
                WindowTitle = windowTitle,
                DetectedAt = DateTime.Now
            };
        }

        private void CheckActiveWindow()
        {
            ActiveWindowInfo activeWindow = CaptureActiveWindow();

            if (activeWindow == null)
                return;

            string currentWindowKey = BuildWindowKey(activeWindow);

            if (currentWindowKey == _lastWindowKey)
                return;

            _lastWindowKey = currentWindowKey;
            OnWindowSwitched?.Invoke(activeWindow);
        }

        private string BuildWindowKey(ActiveWindowInfo activeWindow)
        {
            if (activeWindow == null)
                return string.Empty;

            string appName = activeWindow.AppName ?? string.Empty;
            string title = activeWindow.WindowTitle ?? string.Empty;
            string aumid = activeWindow.AppUserModelId ?? string.Empty;

            return activeWindow.WindowHandle + "|" +
                   activeWindow.ProcessId + "|" +
                   appName + "|" +
                   aumid + "|" +
                   title;
        }

        private int ResolveEffectiveProcessId(
            IntPtr windowHandle,
            int foregroundProcessId)
        {
            string foregroundProcessName;

            using (Process foregroundProcess = GetProcessByIdSafe(foregroundProcessId))
            {
                foregroundProcessName = GetProcessNameSafe(foregroundProcess);
            }

            if (!foregroundProcessName.Equals(
                    "ApplicationFrameHost",
                    StringComparison.OrdinalIgnoreCase))
            {
                return foregroundProcessId;
            }

            int childProcessId = FindFirstDifferentVisibleChildProcessId(
                windowHandle,
                foregroundProcessId);

            return childProcessId > 0
                ? childProcessId
                : foregroundProcessId;
        }

        private int FindFirstDifferentVisibleChildProcessId(
            IntPtr parentWindowHandle,
            int parentProcessId)
        {
            int foundProcessId = 0;

            EnumChildWindows(
                parentWindowHandle,
                (childHandle, lParam) =>
                {
                    if (!IsWindowVisible(childHandle))
                        return true;

                    int childProcessId = GetWindowProcessId(childHandle);

                    if (childProcessId > 0 &&
                        childProcessId != parentProcessId)
                    {
                        foundProcessId = childProcessId;
                        return false;
                    }

                    return true;
                },
                IntPtr.Zero);

            return foundProcessId;
        }

        private int GetWindowProcessId(IntPtr windowHandle)
        {
            uint processId;
            GetWindowThreadProcessId(windowHandle, out processId);
            return (int)processId;
        }

        private string GetWindowTitle(IntPtr windowHandle)
        {
            int length = GetWindowTextLength(windowHandle);

            if (length <= 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(windowHandle, builder, builder.Capacity);

            return builder.ToString();
        }

        private Process GetProcessByIdSafe(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch
            {
                return null;
            }
        }

        private string GetProcessNameSafe(Process process)
        {
            if (process == null)
                return "Unknown";

            try
            {
                if (!string.IsNullOrWhiteSpace(process.ProcessName))
                    return process.ProcessName;
            }
            catch
            {
            }

            return "Unknown";
        }

        private string GetProcessPathByMainModule(Process process)
        {
            if (process == null)
                return string.Empty;

            try
            {
                if (process.MainModule != null &&
                    !string.IsNullOrWhiteSpace(process.MainModule.FileName))
                {
                    return process.MainModule.FileName;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetProcessPathByWinApi(int processId)
        {
            return WithProcessHandle(
                processId,
                processHandle =>
                {
                    int capacity = 1024;
                    StringBuilder pathBuilder = new StringBuilder(capacity);

                    bool success = QueryFullProcessImageName(
                        processHandle,
                        0,
                        pathBuilder,
                        ref capacity);

                    if (!success)
                        return string.Empty;

                    return pathBuilder.ToString();
                });
        }

        private string GetApplicationUserModelIdSafe(int processId)
        {
            return WithProcessHandle(
                processId,
                processHandle =>
                {
                    int length = 0;

                    int result = GetApplicationUserModelId(
                        processHandle,
                        ref length,
                        null);

                    if (result != ErrorInsufficientBuffer || length <= 0)
                        return string.Empty;

                    StringBuilder builder = new StringBuilder(length);

                    result = GetApplicationUserModelId(
                        processHandle,
                        ref length,
                        builder);

                    if (result != ErrorSuccess)
                        return string.Empty;

                    return builder.ToString();
                });
        }

        private string GetPackageFullNameSafe(int processId)
        {
            return WithProcessHandle(
                processId,
                processHandle =>
                {
                    int length = 0;

                    int result = GetPackageFullName(
                        processHandle,
                        ref length,
                        null);

                    if (result == AppModelErrorNoPackage)
                        return string.Empty;

                    if (result != ErrorInsufficientBuffer || length <= 0)
                        return string.Empty;

                    StringBuilder builder = new StringBuilder(length);

                    result = GetPackageFullName(
                        processHandle,
                        ref length,
                        builder);

                    if (result != ErrorSuccess)
                        return string.Empty;

                    return builder.ToString();
                });
        }

        private string GetPackageInstallPathSafe(string packageFullName)
        {
            if (string.IsNullOrWhiteSpace(packageFullName))
                return string.Empty;

            try
            {
                int length = 0;

                int result = GetPackagePathByFullName(
                    packageFullName,
                    ref length,
                    null);

                if (result != ErrorInsufficientBuffer || length <= 0)
                    return string.Empty;

                StringBuilder builder = new StringBuilder(length);

                result = GetPackagePathByFullName(
                    packageFullName,
                    ref length,
                    builder);

                if (result != ErrorSuccess)
                    return string.Empty;

                return builder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string WithProcessHandle(
            int processId,
            Func<IntPtr, string> action)
        {
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(
                    ProcessQueryLimitedInformation,
                    false,
                    processId);

                if (processHandle == IntPtr.Zero)
                    return string.Empty;

                return action(processHandle);
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hWndParent,
            EnumWindowsProc lpEnumFunc,
            IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            int dwFlags,
            StringBuilder lpExeName,
            ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetApplicationUserModelId(
            IntPtr hProcess,
            ref int applicationUserModelIdLength,
            StringBuilder applicationUserModelId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetPackageFullName(
            IntPtr hProcess,
            ref int packageFullNameLength,
            StringBuilder packageFullName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetPackagePathByFullName(
            string packageFullName,
            ref int pathLength,
            StringBuilder path);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
