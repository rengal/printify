/**
 * Icon Management Module
 *
 * Handles loading, caching, and rendering SVG icons.
 */

// Icon cache to store loaded SVG content
const iconCache = {};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Load an SVG icon from the assets directory
 * @param {string} name - The icon name (without .svg extension)
 * @returns {Promise<string>} The SVG content
 */
export async function loadIcon(name) {
    if (iconCache[name]) {
        return iconCache[name];
    }
    try {
        const response = await fetch(`assets/icons/${name}.svg`);
        const svgText = await response.text();
        iconCache[name] = svgText;
        return svgText;
    } catch (err) {
        console.error(`Failed to load icon ${name}:`, err);
        return '';
    }
}

/**
 * Get an icon from cache with optional modifications
 * @param {string} name - The icon name
 * @param {object} options - Optional modifications
 * @param {number} options.width - Width to set
 * @param {number} options.height - Height to set
 * @param {string} options.stroke - Stroke color to set
 * @param {string} options.class - CSS class to add to the SVG
 * @returns {string} The modified SVG string, or empty string if not cached
 */
export function getIcon(name, options = {}) {
    const svg = iconCache[name] || '';
    if (!svg) return '';

    let result = svg;
    if (options.width) {
        result = result.replace(/width="[^"]*"/, `width="${options.width}"`);
    }
    if (options.height) {
        result = result.replace(/height="[^"]*"/, `height="${options.height}"`);
    }
    if (options.stroke) {
        result = result.replace(/stroke="[^"]*"/g, `stroke="${options.stroke}"`);
    }
    if (options.class) {
        result = result.replace(/<svg/, `<svg class="${options.class}"`);
    }
    return result;
}

/**
 * Preload multiple icons
 * @param {string[]} names - Array of icon names to preload
 * @returns {Promise<void>}
 */
export async function preloadIcons(names) {
    await Promise.all(names.map(name => loadIcon(name)));
}
