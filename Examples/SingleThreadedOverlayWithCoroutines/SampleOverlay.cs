namespace SingleThreadedOverlayWithCoroutines
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ClickableTransparentOverlay;
    using Coroutine;
    using ImGuiNET;

    /// <summary>
    /// Render Loop and Logic Loop are synchronized.
    /// </summary>
    internal class SampleOverlay : Overlay
    {
        private CoroutineHandlerInstance renderCoroutines;
        private Stopwatch sw;
        private string data;
        private bool isRunning;

        public SampleOverlay()
        {
            renderCoroutines = new CoroutineHandlerInstance();
            data = string.Empty;
            isRunning = true;
            sw = new Stopwatch();
            sw.Start();
            renderCoroutines.Start(SlowServiceAsync());
        }

        private void RenderCoroutineTick()
        {
            var deltaSeconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            sw.Restart();
            renderCoroutines.Tick(deltaSeconds);
        }

        private IEnumerator<Wait> SlowServiceAsync()
        {
            int counter = 0;
            while (true)
            {
                counter++;
                yield return new Wait(3);
                this.data = $"{counter}";
            }
        }

        protected override Task Render()
        {
            RenderCoroutineTick();
            ImGui.Begin("Sample Overlay", ref isRunning);
            ImGui.Text($"Data: {this.data}");
            ImGui.End();
            if (!isRunning)
            {
                Close();
            }

            return Task.CompletedTask;
        }
    }
}