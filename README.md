# Clickable Transparent Overlay
CTO is a library for creating transparent overlay using Win32 API and ImGui.

# How to Use

Please see the sample projects ([here](https://github.com/zaafar/ClickableTransparentOverlay/tree/master/Examples)) which demonstrate how to use this library.

# Nuget

https://www.nuget.org/packages/ClickableTransparentOverlay

# Dependencies

* [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
* [ImGui.NET](https://github.com/mellinoe/ImGui.NET/)
* [ImageSharp](https://github.com/SixLabors/ImageSharp)

# Reference code to watch

Lots of classes in this library (e.g. ImGuiRenderer, ImGuiInputHandler and etc) closely
follow (if not identical to) the official example in c++ ImGui library
([here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_dx11.cpp "Last changelog looked was 2022-10-11")
and [here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp "Last changelog looked was 2023-10-05")).
This allows us to easily update this library if something changes over there.

# Feedback

Please open issue in github repo for feedback.

# To Trigger a release push a tag as shown below

git tag -a 8.0.0 -m "version 8.0.0"

git push origin 8.0.0
