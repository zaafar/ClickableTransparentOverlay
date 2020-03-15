namespace DriverProgram
{
    using ClickableTransparentOverlay;
    using ImGuiNET;
    using System;
    using System.IO;
    using System.Numerics;
    using System.Threading;

    class Program
    {
        private static bool isRunning = true;
        private static bool showImGuiDemo = false;

        private static bool drawOnScreen = false;
        private static Random randomGen = new Random();
        private static int totalCircles = 10;
        private static Vector2[] circleCenters = new Vector2[totalCircles];

        private static int Fps = 144;
        private static int[] resizeHelper = new int[4] { 0, 0, 2560, 1440 };
        private static int seconds = 5;
        private static Overlay overlay = new Overlay(0, 0, 2560, 1440, Fps, true);

        private static void MainApplicationLogic()
        {
            while (isRunning)
            {
                for (int i = 0; i < circleCenters.Length; i++)
                {
                    circleCenters[i].X = randomGen.Next(0, 2560);
                    circleCenters[i].Y = randomGen.Next(0, 1440);
                }
                Thread.Sleep(600);
            }

            overlay.Dispose();
        }

        static void Main(string[] args)
        {
            overlay.SubmitUI += RenderUi;
            Thread p = new Thread(MainApplicationLogic);
            p.Start();
            overlay.Run();
            p.Join();
        }

        private static void RenderUi(object sender, System.EventArgs e)
        {
            if(isRunning)
            {
                ImGui.Begin("Overlay Config", ref isRunning, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize);

                if (ImGui.SliderInt("Set FPS", ref Fps, 30, 144))
                {
                    overlay.SetFps(Fps);
                }

                ImGui.NewLine();
                ImGui.SliderInt2("Set Position", ref resizeHelper[0], 0, 3840);
                ImGui.SliderInt2("Set Size", ref resizeHelper[2], 0, 3840);
                if (ImGui.Button("Resize"))
                {
                    overlay.Resize(resizeHelper[0], resizeHelper[1], resizeHelper[2], resizeHelper[3]);
                }

                ImGui.NewLine();
                ImGui.SliderInt("###time(sec)", ref seconds, 1, 30);
                if (ImGui.Button($"Hide for {seconds} seconds"))
                {
                    new Thread(() => { Thread.Sleep(seconds * 1000); overlay.Show(); }).Start();
                    overlay.Hide();
                }

                ImGui.NewLine();
                if(ImGui.Button("Show ImGui Demo"))
                {
                    showImGuiDemo = true;
                }

                ImGui.NewLine();
                if(ImGui.Button("Draw on Screen"))
                {
                    drawOnScreen = true;
                }

                ImGui.NewLine();
                if (File.Exists("image.png"))
                {
                    ImGui.Image(overlay.AddOrGetImagePointer("image.png"), new Vector2(256, 256));
                }
                else
                {
                    ImGui.Text("Put any image where the exe is, name is 'image.png'");
                }

                ImGui.End();
            }

            if (showImGuiDemo)
            {
                ImGui.ShowDemoWindow(ref showImGuiDemo);
            }

            if(drawOnScreen)
            {
                ImGui.SetNextWindowContentSize(ImGui.GetIO().DisplaySize);
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                ImGui.Begin("Background Screen", ref drawOnScreen, ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
                var windowPtr = ImGui.GetWindowDrawList();
                for (int i = 0; i < circleCenters.Length; i++)
                {
                    windowPtr.AddCircleFilled(circleCenters[i], 10.0f, (uint)(((255 << 24) | (00 << 16) | (00 << 8) | 255) & 0xffffffffL));
                }
                ImGui.End();
            }
        }
    }
}