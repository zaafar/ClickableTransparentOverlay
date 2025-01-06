namespace SimpleExample
{
    using ClickableTransparentOverlay;
    using System.Threading.Tasks;
    using ImGuiNET;

    internal class SampleOverlay : Overlay
    {
        private bool wantKeepDemoWindow = true;
        private int FPSHelper;

        public SampleOverlay() : base(3840, 2160)
        {
            this.FPSHelper = this.FPSLimit;
        }

        protected override Task PostInitialized()
        {
            return Task.CompletedTask;
        }

        protected override void Render()
        {
            ImGui.ShowDemoWindow(ref wantKeepDemoWindow);

            if (ImGui.Begin("FPS Changer"))
            {
                if (ImGui.InputInt("Set FPS", ref FPSHelper))
                {
                    this.FPSLimit = this.FPSHelper;
                }
            }

            ImGui.End();

            if (!this.wantKeepDemoWindow)
            {
                this.Close();
            }
        }
    }
}
