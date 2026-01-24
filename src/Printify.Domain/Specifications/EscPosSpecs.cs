namespace Printify.Domain.Specifications;

/// <summary>
/// ESC/POS protocol specifications.
/// </summary>
public static class EscPosSpecs
{
    /// <summary>
    /// Default canvas width for ESC/POS documents.
    /// </summary>
    public const int DefaultCanvasWidth = 512;

    /// <summary>
    /// Font specifications for ESC/POS protocol.
    ///
    /// Note: According to official ESC/POS specifications, Font B should be more condensed
    /// (typically 9x24 vs Font A's 12x24), but the current implementation treats both as 12x24.
    /// This is documented here for future correction.
    /// </summary>
    public static class Fonts
    {
        /// <summary>
        /// Font A (default, typically used for normal text).
        /// Current implementation: 12 dots wide x 24 dots high.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class FontA
        {
            public const int WidthInDots = 12;
            public const int HeightInDots = 24;
            public const string FontName = "EscPosA";
        }

        /// <summary>
        /// Font B (intended for more condensed text).
        /// Current implementation: 12 dots wide x 24 dots high (same as Font A).
        ///
        /// TODO: Per ESC/POS spec, Font B should be 9x24, but changing this would
        /// require updating all existing tests that expect 12x24.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class FontB
        {
            public const int WidthInDots = 12;
            public const int HeightInDots = 24;
            public const string FontName = "EscPosB";
        }

        /// <summary>
        /// Gets the font width for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The width in dots.</returns>
        public static int GetWidth(int fontNumber) =>
            fontNumber == 1 ? FontB.WidthInDots : FontA.WidthInDots;

        /// <summary>
        /// Gets the font height for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The height in dots.</returns>
        public static int GetHeight(int fontNumber) =>
            fontNumber == 1 ? FontB.HeightInDots : FontA.HeightInDots;

        /// <summary>
        /// Gets the font name for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The font name used in CanvasTextElementDto.</returns>
        public static string GetName(int fontNumber) =>
            fontNumber == 1 ? FontB.FontName : FontA.FontName;
    }

    /// <summary>
    /// Rendering parameters for ESC/POS protocol.
    /// </summary>
    public static class Rendering
    {
        /// <summary>
        /// Default character spacing (in dots) when no specific spacing is set.
        /// 0 means use the printer's default spacing.
        /// </summary>
        public const int DefaultCharSpacing = 0;

        /// <summary>
        /// Default line spacing (in dots) when no specific spacing is set.
        /// 0 means use the printer's default line spacing.
        /// </summary>
        public const int DefaultLineSpacing = 0;
    }
}
