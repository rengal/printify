/**
 * Protocol Utility Module
 *
 * Defines protocol constants for API contract and provides
 * converters between API values and visual display names.
 */

// ============================================================================
// API PROTOCOL VALUES (backend contract)
// ============================================================================

/**
 * Protocol values that match the backend API contract.
 * These are the raw values sent/received from the server.
 * @readonly
 * @enum {string}
 */
export const Protocol = {
    EscPos: 'escpos',
    Epl: 'epl',
    Zpl: 'zpl',
    Tspl: 'tspl',
    Slcs: 'slcs'
};

// ============================================================================
// DISPLAY NAME MAPPING
// ============================================================================

/**
 * Mapping from protocol API values to user-friendly display names.
 * @readonly
 * @enum {string}
 */
export const ProtocolDisplayNames = {
    [Protocol.EscPos]: 'ESC/POS emulation',
    [Protocol.Epl]: 'EPL emulation',
    [Protocol.Zpl]: 'ZPL emulation',
    [Protocol.Tspl]: 'TSPL emulation',
    [Protocol.Slcs]: 'SLCS emulation'
};

// ============================================================================
// CONVERTER FUNCTIONS
// ============================================================================

/**
 * Get the display name for a protocol API value.
 * @param {string} protocol - The protocol API value
 * @returns {string} The user-friendly display name
 */
export function toProtocolDisplayName(protocol) {
    if (!protocol) return ProtocolDisplayNames[Protocol.EscPos];
    return ProtocolDisplayNames[protocol] || protocol;
}

/**
 * Normalize a protocol value to a valid API protocol value.
 * Handles various input formats (case-insensitive, with/without spaces).
 * @param {string} protocol - The protocol value to normalize
 * @returns {string} A valid protocol API value
 */
export function normalizeProtocol(protocol) {
    if (!protocol) return Protocol.EscPos;
    const normalized = protocol.toLowerCase().replace(/[^a-z0-9]/g, '');

    // Map to known protocols
    for (const [key, value] of Object.entries(Protocol)) {
        if (value === normalized || key.toLowerCase() === normalized) {
            return value;
        }
    }

    // Fallback: check for partial matches
    if (normalized.includes('esc')) return Protocol.EscPos;
    if (normalized.includes('epl')) return Protocol.Epl;
    if (normalized.includes('zpl')) return Protocol.Zpl;
    if (normalized.includes('tspl')) return Protocol.Tspl;
    if (normalized.includes('slcs')) return Protocol.Slcs;

    // Default to EscPos for unknown protocols
    return Protocol.EscPos;
}

/**
 * Check if a protocol value is valid.
 * @param {string} protocol - The protocol value to check
 * @returns {boolean} True if the protocol is valid
 */
export function isValidProtocol(protocol) {
    return Object.values(Protocol).includes(protocol);
}

/**
 * Get all available protocol options for use in select dropdowns.
 * @returns {Array<{value: string, label: string}>} Array of protocol options
 */
export function getProtocolOptions() {
    return Object.values(Protocol).map(value => ({
        value,
        label: ProtocolDisplayNames[value]
    }));
}
