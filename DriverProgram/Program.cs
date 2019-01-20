namespace DriverProgram
{
    using ClickableTransparentOverlay;
    using ImGuiNET;
    using System;
    using System.Threading;

    class Program
    {
        private static Overlay demo;
        private static int Width = 2560;
        private static int Height = 1440;
        private static int Fps = 144;
        private static int RunFor = 10;
        static void Main(string[] args)
        {
            Console.Write("Enter Screen Width:");
            Width = Convert.ToInt32(Console.ReadLine());

            Console.Write("Enter Screen Height:");
            Height = Convert.ToInt32(Console.ReadLine());

            Console.Write("Enter Monitor Max FPS:");
            Fps = Convert.ToInt32(Console.ReadLine());

            Console.Write("You want to run this demo for X (seconds):");
            RunFor = Convert.ToInt32(Console.ReadLine());

            var EndDemo = new Thread(DistroyDemo);
            EndDemo.Start();
            StartDemo();
        }

        public static void StartDemo()
        {
            demo = new Overlay(0, 0, Width, Height, Fps);
            demo.SubmitUI += (object sender, EventArgs e) => ImGui.ShowDemoWindow();
            demo.Run();
        }

        public static void DistroyDemo()
        {
            Thread.Sleep(RunFor * 1000);
            //demo.ResizeWindow(0, 0, 2560, 1440);
            //Thread.Sleep(10000);
            //demo.HideWindow();
            //Thread.Sleep(10000);
            //demo.ShowWindow();
            //Thread.Sleep(10000);
            demo.Dispose();
        }
    }
}