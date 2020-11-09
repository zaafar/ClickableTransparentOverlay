namespace MultiThreadedOverlay
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using ClickableTransparentOverlay;
    using ImGuiNET;

    /// <summary>
    /// Render Loop and Logic Loop are independent from each other. 
    /// </summary>
    public class SampleOverlay : Overlay
    {
        private volatile State state;
        private readonly Thread logicThread;

        public SampleOverlay()
        {
            state = new State();
            logicThread = new Thread(() =>
            {
                var lastRunTickStamp = state.Watch.ElapsedTicks;
                
                while (state.IsRunning)
                {
                    var currentRunTickStamp = state.Watch.ElapsedTicks;
                    var delta = currentRunTickStamp - lastRunTickStamp;
                    LogicUpdate(delta);
                    lastRunTickStamp = currentRunTickStamp;
                }
            });
            
            logicThread.Start();
        }

        public override void Close()
        {
            base.Close();
            this.state.IsRunning = false;
        }

        private void LogicUpdate(float updateDeltaTicks)
        {
            state.LogicTicksCounter.Increment();
            state.LogicalDelta = updateDeltaTicks;

            if (state.RequestLogicThreadSleep)
            {
                Thread.Sleep(TimeSpan.FromSeconds(state.SleepInSeconds));
                state.RequestLogicThreadSleep = false;
                Close();
            }

            state.OverlaySample2.Update();
            Thread.Sleep(state.LogicTickDelayInMilliseconds); //Not accurate at all as a mechanism for limiting thread runs
        }

        protected override Task Render()
        {
            var deltaSeconds = ImGui.GetIO().DeltaTime;
            
            if (state.Cooldown0X7Bkey > 0)
            {
                state.Cooldown0X7Bkey -= deltaSeconds;
            }
            
            if (!state.Visible)
            {
                state.ReappearTimeRemaining -= deltaSeconds;
                if (state.ReappearTimeRemaining < 0)
                {
                    state.Visible = true;
                }
                return Task.CompletedTask;
            }
            
            state.RenderFramesCounter.Increment();
            
            if (NativeMethods.IsKeyPressed(0x7B) && !(state.Cooldown0X7Bkey > 0)) //F12.
            {
                state.ShowClickableMenu = !state.ShowClickableMenu;
                state.Cooldown0X7Bkey = 0.2f;
            }
            
            if (state.ShowImGuiDemo)
            {
                ImGui.ShowDemoWindow(ref state.ShowImGuiDemo);
            }

            if (state.ShowOverlaySample1)
            {
                RenderOverlaySample1();
            }

            if (state.OverlaySample2.Show)
            {
                state.OverlaySample2.Render();
            }

            if (state.ShowClickableMenu)
            {
                RenderMainMenu();
            }

            return Task.CompletedTask;
        }

        private void RenderMainMenu()
        {
            bool isCollapsed = !ImGui.Begin(
                "Overlay Main Menu",
                ref state.IsRunning,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize);

            if (!state.IsRunning || isCollapsed)
            {
                ImGui.End();
                if (!state.IsRunning)
                {
                    Close();
                }

                return;
            }

            ImGui.Text("Try pressing F12 button to show/hide this menu.");
            ImGui.Text("Click X on top right of this menu to close the overlay.");
            ImGui.Checkbox("Show non-clickable transparent overlay Sample 1.", ref state.ShowOverlaySample1);
            ImGui.Checkbox("Show full-screen non-clickable transparent overlay sample 2.", ref state.OverlaySample2.Show);
            ImGui.NewLine();
            if (ImGui.InputInt("Set To Display", ref state.CurrentDisplay))
            {
                var box = GetDisplayBounds(state.CurrentDisplay);
                state.resizeHelper[0] = box.X;
                state.resizeHelper[1] = box.Y;
                state.resizeHelper[2] = box.Width;
                state.resizeHelper[3] = box.Height;
            }

            ImGui.SliderInt2("Set Position", ref state.resizeHelper[0], 0, 3840);
            ImGui.SliderInt2("Set Size", ref state.resizeHelper[2], 0, 3840);
            if (ImGui.Button("Resize"))
            {
                Position = new Veldrid.Point(state.resizeHelper[0], state.resizeHelper[1]);
                Size = new Veldrid.Point(state.resizeHelper[2], state.resizeHelper[3]);
            }

            ImGui.NewLine();
            ImGui.SliderInt("###time(sec)", ref state.Seconds, 1, 30);
            if (ImGui.Button($"Hide for {state.Seconds} seconds"))
            {
                state.Visible = false;
                state.ReappearTimeRemaining = state.Seconds;
            }

            ImGui.SliderInt("###sleeptime(sec)", ref state.SleepInSeconds, 1, 30);
            if (ImGui.Button($"Sleep Render Thread for {state.SleepInSeconds}"))
            {
                Thread.Sleep(TimeSpan.FromSeconds(state.SleepInSeconds));
            }

            if (ImGui.Button($"Sleep Logic Thread for {state.SleepInSeconds} and then Close Overlay"))
            {
                state.RequestLogicThreadSleep = true;
            }

            ImGui.SliderInt("Logical Thread Delay(ms)", ref state.LogicTickDelayInMilliseconds, 1, 1000);
            ImGui.NewLine();
            if (ImGui.Button("Toggle ImGui Demo"))
            {
                state.ShowImGuiDemo = !state.ShowImGuiDemo;
            }

            if (ImGui.Button("Toggle Terminal"))
            {
                TerminalWindow = !TerminalWindow;
            }

            ImGui.NewLine();
            if (File.Exists("image.png"))
            {
                AddOrGetImagePointer(
                    "image.png",
                    out IntPtr imgPtr,
                    out uint w,
                    out uint h);
                ImGui.Image(imgPtr, new Vector2(w, h));
            }
            else
            {
                ImGui.Text("Put any image where the exe is, name is 'image.png'");
            }

            ImGui.End();
            return;
        }

        private void RenderOverlaySample1()
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
            ImGui.Text($"Number of displays {Overlay.NumberVideoDisplays}");
            ImGui.Text("You can not click me");
            ImGui.Text("I am here just to display stuff");
            ImGui.Text($"Current Date: {DateTime.Now.Date}");
            ImGui.Text($"Current Time: {DateTime.Now.TimeOfDay}");
            ImGui.Text($"Total Rendered Frames: {state.RenderFramesCounter.Count}");
            ImGui.Text($"Render Delta (seconds): {ImGui.GetIO().DeltaTime}");
            ImGui.Text($"Total Logic Frames: {state.LogicTicksCounter.Count}");
            ImGui.Text($"Logic Delta (seconds): {state.LogicalDelta/Stopwatch.Frequency}");
            ImGui.End();
        }
        
    }
}