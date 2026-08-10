# Clickable Transparent Overlay (Hexa)

> Fork of [ClickableTransparentOverlay](https://github.com/zaafar/ClickableTransparentOverlay) upgraded to use Hexa.NET.ImGui instead of ImGui.NET.

CTO is a library for creating clickable transparent overlays on Windows using Win32 API and Hexa.NET.ImGui.

# How to Use
Please see the sample projects ([here](https://github.com/xfi0/ClickableTransparentOverlay.Hexa/tree/master/Examples)) which demonstrate how to use this library.

# NuGet
https://www.nuget.org/packages/ClickableTransparentOverlay.Hexa

# Dependencies
* [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
* [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui)
* [ImageSharp](https://github.com/SixLabors/ImageSharp)

# Reference Code
Lots of classes in this library (e.g. ImGuiRenderer, ImGuiInputHandler, etc.) closely
follow (if not identical to) the official example in the C++ ImGui library
([here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_dx11.cpp "Last changelog looked was 2022-10-11")
and [here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp "Last changelog looked was 2023-10-05")).
This allows the library to be easily updated if something changes upstream.

# Feedback
Please open an issue in the GitHub repo for feedback.
