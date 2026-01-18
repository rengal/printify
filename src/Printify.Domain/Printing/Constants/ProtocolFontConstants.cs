namespace Printify.Domain.Printing.Constants;

/// <summary>
/// Defines font dimensions and spacing constants for ESC/POS and EPL protocols.
/// These values represent the current implementation based on existing tests and behavior.
///
/// Note: According to official ESC/POS specifications, Font B should be more condensed
/// (typically 9x24 vs Font A's 12x24), but the current implementation treats both as 12x24.
/// This is documented here for future correction.
///
/// Note: According to EPL2 specifications, fonts 2-5 have different base dimensions,
/// but the current implementation uses 24x24 for all scalable fonts.
/// </summary>
public static class ProtocolFontConstants
{
    /// <summary>
    /// ESC/POS protocol font constants.
    /// </summary>
    public static class EscPos
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
        /// Default character spacing (in dots) when no specific spacing is set.
        /// 0 means use the printer's default spacing.
        /// </summary>
        public const int DefaultCharSpacing = 0;

        /// <summary>
        /// Default line spacing (in dots) when no specific spacing is set.
        /// 0 means use the printer's default line spacing.
        /// </summary>
        public const int DefaultLineSpacing = 0;

        /// <summary>
        /// Gets the font width for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The width in dots.</returns>
        public static int GetFontWidth(int fontNumber) =>
            fontNumber == 1 ? FontB.WidthInDots : FontA.WidthInDots;

        /// <summary>
        /// Gets the font height for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The height in dots.</returns>
        public static int GetFontHeight(int fontNumber) =>
            fontNumber == 1 ? FontB.HeightInDots : FontA.HeightInDots;

        /// <summary>
        /// Gets the font name for a given font number.
        /// </summary>
        /// <param name="fontNumber">The font number (0 for Font A, 1 for Font B).</param>
        /// <returns>The font name used in CanvasTextElementDto.</returns>
        public static string GetFontName(int fontNumber) =>
            fontNumber == 1 ? FontB.FontName : FontA.FontName;
    }

    /// <summary>
    /// EPL (Eltron Programming Language) protocol font constants.
    ///
    /// EPL Font Numbering (from EPL2 specification):
    /// - Font 1: Non-scalable fixed font (not really supported in current implementation)
    /// - Font 2: Scalable, internally called "font 0" in EPL docs, command parameter = 2
    /// - Font 3: Scalable, internally called "font 1" in EPL docs, command parameter = 3
    /// - Font 4: Scalable, internally called "font 2" in EPL docs, command parameter = 4
    /// - Font 5: Scalable, internally called "font 3" in EPL docs, command parameter = 5
    ///
    /// The FontName values (e.g., "EplFont0", "EplFont1") are used in CanvasTextElementDto.
    /// </summary>
    public static class Epl
    {
        /// <summary>
        /// Font 1 (default font, non-scalable).
        ///
        /// Note: This is a fixed-size font that is NOT scalable.
        /// The current implementation doesn't properly handle Font 1 as non-scalable.
        ///
        /// Used in CanvasTextElementDto as FontName when Font 1 is selected.
        /// </summary>
        public static class Font1
        {
            public const int WidthInDots = 20;
            public const int HeightInDots = 24;
            public const string FontName = "EplFontDefault";
            public const int InternalFontIndex = 1;
        }

        /// <summary>
        /// Font 2 (scalable).
        /// In EPL command: A...2,...
        /// In EPL documentation: referred to as "font 0"
        /// Base dimensions before scaling: 24 dots wide x 24 dots high.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class Font2
        {
            public const int BaseWidthInDots = 24;
            public const int BaseHeightInDots = 24;
            public const string FontName = "EplFont0";
            public const int InternalFontIndex = 2;
        }

        /// <summary>
        /// Font 3 (scalable).
        /// In EPL command: A...3,...
        /// In EPL documentation: referred to as "font 1"
        /// Base dimensions before scaling: 24 dots wide x 24 dots high.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class Font3
        {
            public const int BaseWidthInDots = 24;
            public const int BaseHeightInDots = 24;
            public const string FontName = "EplFont1";
            public const int InternalFontIndex = 3;
        }

        /// <summary>
        /// Font 4 (scalable).
        /// In EPL command: A...4,...
        /// In EPL documentation: referred to as "font 2"
        /// Base dimensions before scaling: 24 dots wide x 24 dots high.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class Font4
        {
            public const int BaseWidthInDots = 24;
            public const int BaseHeightInDots = 24;
            public const string FontName = "EplFont2";
            public const int InternalFontIndex = 4;
        }

        /// <summary>
        /// Font 5 (scalable).
        /// In EPL command: A...5,...
        /// In EPL documentation: referred to as "font 3"
        /// Base dimensions before scaling: 24 dots wide x 24 dots high.
        ///
        /// Used in CanvasTextElementDto as FontName.
        /// </summary>
        public static class Font5
        {
            public const int BaseWidthInDots = 24;
            public const int BaseHeightInDots = 24;
            public const string FontName = "EplFont3";
            public const int InternalFontIndex = 5;
        }

        /// <summary>
        /// Gets the base font width for a given internal font index.
        /// </summary>
        /// <param name="internalFontIndex">The internal font index (1-5) as used in EPL commands.</param>
        /// <returns>The base width in dots (before horizontal multiplication).</returns>
        public static int GetFontBaseWidth(int internalFontIndex)
        {
            return internalFontIndex switch
            {
                1 => Font1.WidthInDots,
                2 => Font2.BaseWidthInDots,
                3 => Font3.BaseWidthInDots,
                4 => Font4.BaseWidthInDots,
                5 => Font5.BaseWidthInDots,
                _ => Font2.BaseWidthInDots // Default to Font 2
            };
        }

        /// <summary>
        /// Gets the base font height for a given internal font index.
        /// </summary>
        /// <param name="internalFontIndex">The internal font index (1-5) as used in EPL commands.</param>
        /// <returns>The base height in dots (before vertical multiplication).</returns>
        public static int GetFontBaseHeight(int internalFontIndex)
        {
            return internalFontIndex switch
            {
                1 => Font1.HeightInDots,
                2 => Font2.BaseHeightInDots,
                3 => Font3.BaseHeightInDots,
                4 => Font4.BaseHeightInDots,
                5 => Font5.BaseHeightInDots,
                _ => Font2.BaseHeightInDots // Default to Font 2
            };
        }

        /// <summary>
        /// Gets the font name for a given internal font index.
        /// </summary>
        /// <param name="internalFontIndex">The internal font index (1-5) as used in EPL commands.</param>
        /// <returns>The font name used in CanvasTextElementDto.</returns>
        public static string GetFontName(int internalFontIndex)
        {
            return internalFontIndex switch
            {
                1 => Font1.FontName,
                2 => Font2.FontName,
                3 => Font3.FontName,
                4 => Font4.FontName,
                5 => Font5.FontName,
                _ => Font2.FontName // Default to Font 2
            };
        }
    }
}
