namespace DriverProgram
{
    using ClickableTransparentOverlay;
    using ImGuiNET;
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
            if (showImGuiDemo)
            {
                ImGui.ShowDemoWindow(ref showImGuiDemo);
            }

            if(ImGui.Begin("Overlay Config", ref isRunning, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Current FPS: {Fps}");
                if (ImGui.SliderInt("Set FPS", ref Fps, 30, 144))
                {
                    overlay.SetFps(Fps);
                }

                ImGui.NewLine();
                ImGui.Text($"Current Position: {resizeHelper[0]}, {resizeHelper[1]}");
                ImGui.Text($"Current Size:  {resizeHelper[2]}, {resizeHelper[3]}");
                ImGui.SliderInt4("Set Position & Size", ref resizeHelper[0], 0, 3840);
                if (ImGui.Button("Resize"))
                {
                    overlay.Resize(resizeHelper[0], resizeHelper[1], resizeHelper[2], resizeHelper[3]);
                }

                ImGui.NewLine();
                ImGui.DragInt("Set Hidden Time", ref seconds);
                if (ImGui.Button("Hide for X Seconds"))
                {
                    new Thread(() => { Thread.Sleep(seconds * 1000); overlay.Show(); }).Start();
                    overlay.Hide();
                }

                ImGui.NewLine();
                if(ImGui.Button("Show ImGui Demo"))
                {
                    showImGuiDemo = true;
                }

                ImGui.End();
            }
        }
    }
}