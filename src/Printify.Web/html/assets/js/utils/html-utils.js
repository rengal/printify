/**
 * HTML/Utility Helper Functions
 */

/**
 * Escape HTML to prevent XSS
 * @param {string} value - The string to escape
 * @returns {string} Escaped HTML string
 */
export function escapeHtml(value) {
    if (value === null || value === undefined) {
        return '';
    }

    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

/**
 * Check if a token is valid
 * @param {string} token - The token to validate
 * @returns {boolean} True if token is valid
 */
export function isValidToken(token) {
    return /^[a-z]+-[a-z]+-\d{4}$/.test(token);
}
