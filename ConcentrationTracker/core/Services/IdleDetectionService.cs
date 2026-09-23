using System;
using System.Runtime.InteropServices;

namespace ConcentrationTracker.Core.Services
{
    public class IdleDetectionService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(
            ref LastInputInfo plii);

        public TimeSpan GetIdleTime()
        {
            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(typeof(LastInputInfo));

            bool success = GetLastInputInfo(ref lastInputInfo);

            if (!success)
                return TimeSpan.Zero;

            uint currentTick =
                unchecked((uint)Environment.TickCount);

            uint idleTicks =
                unchecked(currentTick - lastInputInfo.dwTime);

            return TimeSpan.FromMilliseconds(idleTicks);
        }
    }
}
