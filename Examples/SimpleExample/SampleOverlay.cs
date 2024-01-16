using ClickableTransparentOverlay;
using ImGuiNET;

namespace SimpleExample;

internal class SampleOverlay : Overlay
{
    private bool wantKeepDemoWindow = true;

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
