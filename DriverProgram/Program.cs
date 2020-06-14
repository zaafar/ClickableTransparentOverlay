using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ClickableTransparentOverlay;
using Coroutine;
using ImGuiNET;

namespace DriverProgram
{
    class Program
    {
        public static bool showClickableMenu = true;
        public static bool showOverlaySample1 = true;
        public static bool showOverlaySample2 = false;
        public static bool showImGuiDemo = false;

        private static int[] resizeHelper = new int[4] { 0, 0, 2560, 1440 };

        private static int seconds = 5;

        private static Random randomGen = new Random();
        private static Vector2[] circleCenters = new Vector2[200];

        static void Main()
        {
            CoroutineHandler.Start(UpdateOverlaySample2());
            CoroutineHandler.Start(SubmitRenderLogic());
            Overlay.RunInfiniteLoop();
        }

        private static IEnumerator<IWait> SubmitRenderLogic()
        {
            while (true)
            {
                yield return new WaitEvent(Overlay.OnRender);

                if (NativeMethods.IsKeyPressed(0x7B)) //F12.
                {
                    showClickableMenu = !showClickableMenu;
                }

                if (showImGuiDemo)
                {
                    ImGui.ShowDemoWindow(ref showImGuiDemo);
                }

                if (showOverlaySample1)
                {
                    ImGui.SetNextWindowPos(new Vector2(0f, 0f));
                    ImGui.SetNextWindowBgAlpha(0.9f);
                    ImGui.Begin(
                        "Sample Overlay",
                        ImGuiWindowFlags.NoInputs |
                        ImGuiWindowFlags.NoCollapse |
                        ImGuiWindowFlags.NoTitleBar |
                        ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoResize);

                    ImGui.Text("I am sample Overlay");
                    ImGui.Text("You can not click me");
                    ImGui.Text("I am here just to display stuff");
                    ImGui.Text($"Current Date: {DateTime.Now.Date}");
                    ImGui.Text($"Current Time: {DateTime.Now.TimeOfDay}");
                    ImGui.End();
                }

                if (showOverlaySample2)
                {
                    ImGui.SetNextWindowContentSize(ImGui.GetIO().DisplaySize);
                    ImGui.SetNextWindowPos(new Vector2(0, 0));
                    ImGui.Begin(
                        "Background Screen",
                        ref showOverlaySample2,
                        ImGuiWindowFlags.NoInputs |
                        ImGuiWindowFlags.NoBackground |
                        ImGuiWindowFlags.NoBringToFrontOnFocus |
                        ImGuiWindowFlags.NoCollapse |
                        ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.NoScrollbar |
                        ImGuiWindowFlags.NoSavedSettings |
                        ImGuiWindowFlags.NoResize |
                        ImGuiWindowFlags.NoTitleBar);
                    var windowPtr = ImGui.GetWindowDrawList();
                    for (int i = 0; i < circleCenters.Length; i++)
                    {
                        windowPtr.AddCircleFilled(circleCenters[i], 10.0f, (uint)(((255 << 24) | (00 << 16) | (00 << 8) | 255) & 0xffffffffL));
                    }
                    ImGui.End();
                }

                if (showClickableMenu)
                {
                    bool isRunning = true;
                    if (!ImGui.Begin("Overlay Main Menu", ref isRunning, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
                    {
                        Overlay.Close = !isRunning;
                        ImGui.End();
                        continue;
                    }

                    Overlay.Close = !isRunning;
                    ImGui.Text("Try pressing F12 button to show/hide this menu.");
                    ImGui.Text("Click X on top right of this menu to close the overlay.");
                    ImGui.Checkbox("Show non-clickable transparent overlay Sample 1.", ref showOverlaySample1);
                    ImGui.Checkbox("Show full-screen non-clickable transparent overlay sample 2.", ref showOverlaySample2);
                    ImGui.NewLine();

                    ImGui.SliderInt2("Set Position", ref resizeHelper[0], 0, 3840);
                    ImGui.SliderInt2("Set Size", ref resizeHelper[2], 0, 3840);
                    if (ImGui.Button("Resize"))
                    {
                        Overlay.Position = new Veldrid.Point(resizeHelper[0], resizeHelper[1]);
                        Overlay.Size = new Veldrid.Point(resizeHelper[2], resizeHelper[3]);
                    }

                    ImGui.NewLine();
                    ImGui.SliderInt("###time(sec)", ref seconds, 1, 30);
                    if (ImGui.Button($"Hide for {seconds} seconds"))
                    {
                        Overlay.Visible = false;
                        // Time Based Coroutines are executed even when the Overlay is invisible.
                        // So in case there is a reason you want to hide the overlay, u can use timebased
                        // coroutines to bring it back.
                        CoroutineHandler.InvokeLater(new WaitSeconds(seconds), () => { Overlay.Visible = true; });
                    }

                    ImGui.NewLine();
                    if (ImGui.Button("Toggle ImGui Demo"))
                    {
                        showImGuiDemo = !showImGuiDemo;
                    }

                    if (ImGui.Button("Toggle Terminal"))
                    {
                        Overlay.TerminalWindow = !Overlay.TerminalWindow;
                    }

                    ImGui.NewLine();
                    if (File.Exists("image.png"))
                    {
                        ImGui.Image(Overlay.AddOrGetImagePointer("image.png"), new Vector2(256, 256));
                    }
                    else
                    {
                        ImGui.Text("Put any image where the exe is, name is 'image.png'");
                    }

                    ImGui.End();
                }

                ImGui.End();
            }
        }

        private static IEnumerator<IWait> UpdateOverlaySample2()
        {
            while (true)
            {
                yield return new WaitSeconds(1);
                for (int i = 0; i < circleCenters.Length; i++)
                {
                    circleCenters[i].X = randomGen.Next(0, 2560);
                    circleCenters[i].Y = randomGen.Next(0, 1440);
                }
            }
        }
    }
}
