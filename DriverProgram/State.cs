﻿using System.Diagnostics;

namespace DriverProgram
{
    public class State
    {
        public readonly Stopwatch Watch = new Stopwatch();
        public readonly OverlaySample2RandomCircles OverlaySample2 = new OverlaySample2RandomCircles();
        public bool ShowClickableMenu = true;
        public bool ShowOverlaySample1 = true;
        public bool ShowImGuiDemo = false;
        public int[] resizeHelper = new int[4] {0, 0, 2560, 1440};
        public int Seconds = 5;
        public int CurrentDisplay = 0;
        public float Cooldown0X7Bkey = 0;
        public bool IsRunning = true;
        public bool Visible;


        public int LogicTickDelayInMilliseconds = 10;
        public float LogicalDelta = 0;
        public readonly Counter RenderFramesCounter = new Counter();
        public readonly Counter LogicTicksCounter = new Counter();
        public bool RequestLogicThreadSleep = false;
        public int SleepInSeconds = 5;


        public float ReappearTimeRemaining = 0;


        public State()
        {
            Watch.Start();
        }
    }
}