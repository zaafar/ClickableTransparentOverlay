namespace DriverProgram
{
    using ClickableTransparentOverlay;
    using ImGuiNET;
    using System.Threading;

    class Program
    {
        private static Overlay demo;
        private static int Width;
        private static int Height;
        private static int Fps;
        static void Main(string[] args)
        {
            Width = int.Parse(System.IO.File.ReadAllText("config/width.txt"));
            Height = int.Parse(System.IO.File.ReadAllText("config/height.txt"));
            Fps = int.Parse(System.IO.File.ReadAllText("config/fps.txt"));
            var EndDemo = new Thread(DistroyDemo);
            EndDemo.Start();
            StartDemo();
        }

        public static void StartDemo()
        {
            demo = new Overlay(0, 0, Width, Height, Fps);
            demo.SubmitUI += (object sender, System.EventArgs e) => ImGui.ShowDemoWindow();
            demo.Run();
        }

        public static void DistroyDemo()
        {
            Thread.Sleep(10000);
            demo.ResizeWindow(100, 100, 1024, 1024);
            Thread.Sleep(10000);
            demo.HideWindow();
            Thread.Sleep(10000);
            demo.ShowWindow();
            Thread.Sleep(10000);
            demo.Dispose();
        }
    }
}
