# Clickable Transparent Overlay
A library for creating transparent overlay using Win32 API and ImGui.

# Nuget

https://www.nuget.org/packages/ClickableTransparentOverlay

# Dependencies

* [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
* [ImGui.NET](https://github.com/mellinoe/ImGui.NET/)

# Reference code to watch

Lots of classes in this library (e.g. ImGuiRenderer, ImGuiInputHandler and etc) closely
follow (if not identical to) the official example in c++ ImGui library
([here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_dx11.cpp "Last changelog looked was 2022-10-11")
and [here](https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp "Last changelog looked was 2022-01-26")).
This allows us to easily update this library if something changes over there.


# To Trigger a release push a tag as shown below

git tag -a 6.0.5 -m "version 6.0.5"

git push origin 6.0.5
