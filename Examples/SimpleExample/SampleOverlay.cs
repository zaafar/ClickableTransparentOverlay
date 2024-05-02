namespace SimpleExample
{
    using ClickableTransparentOverlay;
    using System.Threading.Tasks;
    using ImGuiNET;

    internal class SampleOverlay : Overlay
    {
        private bool wantKeepDemoWindow = true;

        public SampleOverlay() : base(3840, 2160)
        {
        }

        protected override Task PostInitialized()
        {
            this.VSync = false;
            return Task.CompletedTask;
        }

        protected override void Render()
        {
            ImGui.ShowDemoWindow(ref wantKeepDemoWindow);
            if (!this.wantKeepDemoWindow)
            {
                this.Close();
            }
        }
    }
}
