namespace ClickableTransparentOverlay.Win32
{
    using System;
    using System.Runtime.InteropServices;

    internal static class Winmm
    {
        public const string LibraryName = "winmm.dll";

        [DllImport(LibraryName, EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);

        [DllImport(LibraryName, EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);
    }
}
