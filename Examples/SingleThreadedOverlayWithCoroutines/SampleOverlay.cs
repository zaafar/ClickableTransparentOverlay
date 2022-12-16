namespace SingleThreadedOverlayWithCoroutines
{
    using System.Collections.Generic;
    using ClickableTransparentOverlay;
    using Coroutine;
    using ImGuiNET;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp;
    using System.Threading.Tasks;
    using System.Numerics;
    using System;

    /// <summary>
    /// Render Loop and Logic Loop are synchronized.
    /// </summary>
    internal class SampleOverlay : Overlay
    {
        private readonly ushort[] custom = new ushort[3] { 0x0020, 0xFFFF, 0x00 };
        private int fontSize = 13;
        private int data;
        private string data2;
        private bool isRunning = true;
        private bool demoWindow = false;
        private readonly Event myevent = new();
        private readonly ActiveCoroutine myRoutine1;
        private readonly ActiveCoroutine myRoutine2;
        private Image<Rgba32> image = new(100, 100);

        public SampleOverlay()
            : base(true)
        {
            myRoutine1 = CoroutineHandler.Start(TickServiceAsync(), name: "MyRoutine-1");
            myRoutine2 = CoroutineHandler.Start(EventServiceAsync(), name: "MyRoutine-2");
            this.CreateNewImageAtRuntime();

        }

        protected override void Dispose(bool disposing)
        {
            image.Dispose();
            base.Dispose(disposing);
        }

        private void CreateNewImageAtRuntime()
        {
            Parallel.For(0, this.image.Height, y =>
            {
                for (int x = 0; x < this.image.Width; x++)
                {
                    image[x, y] = new Rgba32(Vector3.One * new Random().Next(0, 255));
                }
            });

            image.Save("foo.jpeg");
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

        protected override void Render()
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
            ImGui.Text($"Total Executions: {myRoutine1.MoveNextCount}");
            ImGui.Text($"Total Execution Time: {myRoutine1.TotalMoveNextTime.TotalMilliseconds}");
            ImGui.Text($"Avg Execution Time: {myRoutine1.TotalMoveNextTime.TotalMilliseconds / myRoutine1.MoveNextCount}");
            ImGui.NewLine();

            ImGui.Text($"Coroutine Name: {myRoutine2.Name}");
            ImGui.Text($"Total Executions: {myRoutine2.MoveNextCount}");
            ImGui.Text($"Total Execution Time: {myRoutine2.TotalMoveNextTime.TotalMilliseconds}");
            ImGui.Text($"Avg Execution Time: {myRoutine2.TotalMoveNextTime.TotalMilliseconds/ myRoutine2.MoveNextCount}");
            ImGui.DragInt("Font Size", ref fontSize, 0.1f, 13, 40);

            if (ImGui.Button("Change Font (更改字体)"))
            {
                ReplaceFont(@"C:\Windows\Fonts\msyh.ttc", fontSize, FontGlyphRangeType.ChineseSimplifiedCommon);
            }

            if (ImGui.Button("Change Font (更改字体) Custom Range"))
            {
                ReplaceFont(@"C:\Windows\Fonts\msyh.ttc", fontSize, custom);
            }

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

            this.AddOrGetImagePointer("image", image, true, out var handle);
            ImGui.GetBackgroundDrawList().AddImage(handle, new Vector2(200f), new Vector2(300f));
        }
    }
}