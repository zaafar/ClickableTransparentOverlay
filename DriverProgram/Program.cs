namespace DriverProgram
{
    using ClickableTransparentOverlay;
    using ImGuiNET;
    using System.IO;
    using System.Numerics;
    using System.Threading;

    class Program
    {
        private static bool isRunning = true;
        private static bool showImGuiDemo = false;
        private static int Fps = 144;
        private static int[] resizeHelper = new int[4] { 0, 0, 2560, 1440 };
        private static int seconds = 5;
        private static Overlay overlay = new Overlay(0, 0, 2560, 1440, Fps, false);

        private static void MainApplicationLogic()
        {
            while (isRunning)
            {
                Thread.Sleep(10);
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
                if (File.Exists("image.png"))
                {
                    ImGui.Image(overlay.AddOrGetImagePointer("image.png"), new Vector2(600, 400));
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
        }
    }
}