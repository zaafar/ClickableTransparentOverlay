namespace MultiThreadedOverlay
{
    using System.Diagnostics;

    public class State
    {
        public readonly Stopwatch Watch = Stopwatch.StartNew();
        public readonly OverlaySample2RandomCircles OverlaySample2 = new OverlaySample2RandomCircles();
        public bool ShowClickableMenu = true;
        public bool ShowOverlaySample1 = true;
        public bool ShowImGuiDemo = false;
        public int[] resizeHelper = [0, 0, 2560, 1440];
        public int Seconds = 5;
        public int CurrentDisplay = 0;
        public bool IsRunning = true;
        public bool Visible;


        public int LogicTickDelayInMilliseconds = 10;
        public float LogicalDelta = 0;
        public readonly Counter RenderFramesCounter = new();
        public readonly Counter LogicTicksCounter = new();
        public bool RequestLogicThreadSleep = false;
        public int SleepInSeconds = 5;


        public float ReappearTimeRemaining = 0;

        public bool LogicThreadCloseOverlay = false;
    }
}