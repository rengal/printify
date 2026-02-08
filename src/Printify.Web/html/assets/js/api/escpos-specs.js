/**
 * ESC/POS client-side contract helpers.
 *
 * Keeps UI mappings aligned with backend font identifiers used in view payloads.
 */

export const EscPosFont = {
    A: 'ESCPOS_A',
    B: 'ESCPOS_B'
};

/**
 * Normalize ESC/POS font identifiers from API payloads.
 * Supports variants like "ESCPOS_B", "EscPosB", "escpos-b".
 * @param {string} font - Raw font value from API.
 * @returns {string} Normalized font value from EscPosFont.
 */
export function normalizeEscPosFont(font) {
    const normalizedFont = String(font ?? '')
        .replace(/[^a-zA-Z0-9]/g, '')
        .toUpperCase();

    return normalizedFont === 'ESCPOSB'
        ? EscPosFont.B
        : EscPosFont.A;
}

/**
 * Convert API font identifier to preview CSS class.
 * @param {string} font - Raw font value from API.
 * @returns {string} CSS class name for preview rendering.
 */
export function toEscPosFontCssClass(font) {
    return normalizeEscPosFont(font) === EscPosFont.B
        ? 'escpos-font-b'
        : 'escpos-font-a';
}
