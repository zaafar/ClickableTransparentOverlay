
namespace ClickableTransparentOverlay
{
    using Veldrid.Sdl2;
    using Veldrid;

    internal static class SDL2Functions
    {
        // Rectangle and SDL_Rect are same.
        private delegate int SDL_GetDisplayBounds_int_SDL_Rect_t(int displayIndex, ref Rectangle rect);
        private static SDL_GetDisplayBounds_int_SDL_Rect_t s_SDL_GetDisplayBounds_int_SDL_Rect_t = Sdl2Native.LoadFunction<SDL_GetDisplayBounds_int_SDL_Rect_t>("SDL_GetDisplayBounds");

        /// <summary>
        /// Gets the bounding box of the monitor display.
        /// </summary>
        /// <param name="displayIndex">monitor display number, starting from zero.</param>
        /// <param name="rect">bounding box information, passed by reference</param>
        /// <returns>non-zero in case of any errors.</returns>
        public static int SDL_GetDisplayBounds(int displayIndex, ref Rectangle rect)
        {
            return s_SDL_GetDisplayBounds_int_SDL_Rect_t(displayIndex, ref rect);
        }
    }
}
