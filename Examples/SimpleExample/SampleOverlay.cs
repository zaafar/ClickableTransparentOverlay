namespace SimpleExample
{
    using ClickableTransparentOverlay;
    using System.Threading.Tasks;
    using ImGuiNET;
    using System.Numerics;

    internal class SampleOverlay : Overlay
    {
        private bool wantKeepDemoWindow = true;
        protected override Task PostInitialized()
        {
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
