namespace ClickableTransparentOverlay.Win32
{
    using System;
    using System.Runtime.InteropServices;

    internal static class Kernel32
    {
        public const string LibraryName = "kernel32.dll";

        [DllImport(LibraryName)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
