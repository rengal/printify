/**
 * Date/Time Formatting Utilities
 */

/**
 * Format a date as relative time (e.g., "5m ago", "2h ago", "yesterday")
 * @param {Date} date - The date to format
 * @returns {string} Formatted relative time string
 */
export function formatRelativeTime(date) {
    if (!date) return '';
    const now = new Date();
    const diff = now - date;
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days === 1) return 'yesterday';
    if (days < 7) return `${days}d ago`;
    return date.toLocaleDateString();
}

/**
 * Format a date as localized date/time string
 * @param {Date} date - The date to format
 * @returns {string} Formatted date/time string
 */
export function formatDateTime(date) {
    if (!date) return '—';
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}.${month}.${year} ${hours}:${minutes}`;
}

/**
 * Format a date as localized date/time string with seconds
 * @param {Date} date - The date to format
 * @returns {string} Formatted date/time string with seconds
 */
export function formatDateTimeWithSeconds(date) {
    if (!date) return 'Never';
    return date.toLocaleString(undefined, {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
    });
}

/**
 * Format a date with both absolute and relative time (for HTML display)
 * @param {Date} date - The date to format
 * @returns {string} HTML string with date/time and relative time on separate lines
 */
export function formatDateTimeWithRelative(date) {
    if (!date) return 'Never';

    const dateTimeStr = date.toLocaleString(undefined, {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', hour12: false
    });

    const relativeStr = formatRelativeTime(date);

    return `${dateTimeStr}<br><span style="font-size: 13px; opacity: 0.7;">${relativeStr}</span>`;
}
