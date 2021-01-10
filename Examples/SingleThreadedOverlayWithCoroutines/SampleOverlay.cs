namespace SingleThreadedOverlayWithCoroutines
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ClickableTransparentOverlay;
    using Coroutine;
    using ImGuiNET;

    /// <summary>
    /// Render Loop and Logic Loop are synchronized.
    /// </summary>
    internal class SampleOverlay : Overlay
    {
        private int data;
        private string data2;
        private bool isRunning = true;
        private bool demoWindow = false;
        private Event myevent = new Event();
        private ActiveCoroutine myRoutine1;
        private ActiveCoroutine myRoutine2;

        public SampleOverlay()
        {
            myRoutine1 = CoroutineHandler.Start(TickServiceAsync(), name: "MyRoutine-1");
            myRoutine2 = CoroutineHandler.Start(EventServiceAsync(), name: "MyRoutine-2");
        }

        private IEnumerator<Wait> TickServiceAsync()
        {
            int counter = 0;
            while (true)
            {
                counter++;
                yield return new Wait(3);
                this.data = counter;
            }
        }

        private IEnumerator<Wait> EventServiceAsync()
        {
            int counter = 0;
            data2 = "Initializing Event Routine";
            while (true)
            {
                yield return new Wait(myevent);
                data2 = $"Event Raised x {++counter}";
            }
        }

        protected override Task Render()
        {
            CoroutineHandler.Tick(ImGui.GetIO().DeltaTime);
            if (data % 5 == 1)
            {
                CoroutineHandler.RaiseEvent(myevent);
            }

            ImGui.Begin("Sample Overlay", ref isRunning, ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text($"Total Time/Delta Time: {ImGui.GetTime():F3}/{ImGui.GetIO().DeltaTime:F3}");
            ImGui.NewLine();

            ImGui.Text($"Counter: {this.data}");
            ImGui.Text($"{this.data2}");
            ImGui.NewLine();

            ImGui.Text($"Event Coroutines: {CoroutineHandler.EventCount}");
            ImGui.Text($"Ticking Coroutines: {CoroutineHandler.TickingCount}");
            ImGui.NewLine();

            ImGui.Text($"Coroutine Name: {myRoutine1.Name}");
            ImGui.Text($"Avg Execution Time: {myRoutine1.AverageMoveNextTime.TotalMilliseconds}");
            ImGui.Text($"Total Executions: {myRoutine1.MoveNextCount}");
            ImGui.Text($"Total Execution Time: {myRoutine1.TotalMoveNextTime.TotalMilliseconds}");
            ImGui.NewLine();

            ImGui.Text($"Coroutine Name: {myRoutine2.Name}");
            ImGui.Text($"Avg Execution Time: {myRoutine2.AverageMoveNextTime.TotalMilliseconds}");
            ImGui.Text($"Total Executions: {myRoutine2.MoveNextCount}");
            ImGui.Text($"Total Execution Time: {myRoutine2.TotalMoveNextTime.TotalMilliseconds}");

            if (ImGui.Button("Show/Hide Demo Window"))
            {
                demoWindow = !demoWindow;
            }

            ImGui.End();
            if (!isRunning)
            {
                Close();
            }

            if (demoWindow)
            {
                ImGui.ShowDemoWindow(ref demoWindow);
            }

            return Task.CompletedTask;
        }
    }
}