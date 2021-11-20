namespace ClickableTransparentOverlay
{
    /// <summary>
    /// ImGui provides some default Glyph Ranges for different languages, this enum helps select one of those glyph ranges
    /// or a custom one in case user wants to provide it's own range.
    /// 
    /// NOTE: When loading a font, specifying the glyph ranges helps reduces the font memory footprint.
    /// </summary>
    public enum FontGlyphRangeType
    {
        /// <summary>
        /// Glyph range enough for english language
        /// </summary>
        English,

        /// <summary>
        /// Glyph range enough for english and chinese simplified common language
        /// </summary>
        ChineseSimplifiedCommon,

        /// <summary>
        /// Glyph range enough for english and full chinese language
        /// </summary>
        ChineseFull,

        /// <summary>
        /// Glyph range enough for english and Japanese language
        /// </summary>
        Japanese,

        /// <summary>
        /// Glyph range enough for english and korean language
        /// </summary>
        Korean,

        /// <summary>
        /// Glyph range enough for english and Thai language
        /// </summary>
        Thai,

        /// <summary>
        /// Glyph range enough for english and Vietnamese language
        /// </summary>
        Vietnamese,

        /// <summary>
        /// Glyph range enough for english and few special chars.
        /// </summary>
        Cyrillic,
    }
}
