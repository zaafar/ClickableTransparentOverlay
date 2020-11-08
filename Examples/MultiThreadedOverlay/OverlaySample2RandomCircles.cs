namespace MultiThreadedOverlay
{
    using System;
    using System.Numerics;
    using ImGuiNET;

    public class OverlaySample2RandomCircles
    {
        public bool Show = false;
        private static Random randomGen = new Random();
        private Vector2[] circleCenters = new Vector2[200];

        public void Update()
        {
            if (!Show)
            {
                return;
            }
            
            for (int i = 0; i < circleCenters.Length; i++)
            {
                circleCenters[i].X = randomGen.Next(0, 2560);
                circleCenters[i].Y = randomGen.Next(0, 1440);
            }
        }

        public void Render()
        {
            ImGui.SetNextWindowContentSize(ImGui.GetIO().DisplaySize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin(
                "Background Screen",
                ref Show,
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
                windowPtr.AddCircleFilled(circleCenters[i], 10.0f, (uint) (((255 << 24) | (00 << 16) | (00 << 8) | 255) & 0xffffffffL));
            }
            
            ImGui.End();
        }
    }
}