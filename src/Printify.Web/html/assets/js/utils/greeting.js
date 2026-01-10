/**
 * Greeting Module
 *
 * Handles fetching and selecting time-appropriate greetings from the server.
 * The server selects content strategy, client handles time interpretation.
 */

import { apiRequest } from './api.js';

// Strong daytime intervals (server and client must agree)
const TIME_INTERVALS = {
    morning: { start: 6, end: 11 },      // 06:00 - 11:00
    afternoon: { start: 12.5, end: 15.5 }, // 12:30 - 15:30
    evening: { start: 18.5, end: 22 }    // 18:30 - 22:00
};

// Cache buster - increments on workspace changes to bypass browser HTTP cache
let greetingCacheBuster = 0;

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Increment the cache buster to force refresh of greeting
 */
export function invalidateGreetingCache() {
    greetingCacheBuster++;
}

/**
 * Select the appropriate time-based greeting from the server response
 * @param {object} data - The greeting data from the server
 * @param {string} data.morning - Morning greeting
 * @param {string} data.afternoon - Afternoon greeting
 * @param {string} data.evening - Evening greeting
 * @param {string} data.general - General/fallback greeting
 * @returns {string} The selected greeting
 */
export function selectTimeBasedGreeting(data) {
    const now = new Date();
    const hour = now.getHours() + now.getMinutes() / 60;

    // Check if within morning interval
    if (data.morning && hour >= TIME_INTERVALS.morning.start && hour < TIME_INTERVALS.morning.end) {
        return data.morning;
    }

    // Check if within afternoon interval
    if (data.afternoon && hour >= TIME_INTERVALS.afternoon.start && hour < TIME_INTERVALS.afternoon.end) {
        return data.afternoon;
    }

    // Check if within evening interval
    if (data.evening && hour >= TIME_INTERVALS.evening.start && hour < TIME_INTERVALS.evening.end) {
        return data.evening;
    }

    // Fallback to general greeting
    return data.general;
}

/**
 * Fetch the greeting from the server
 * Uses cache-busting to ensure fresh greeting when workspace changes
 * @returns {Promise<string>} The selected greeting
 */
export async function getWelcomeMessage() {
    try {
        // Add cache-busting parameter to bypass browser HTTP cache when workspace changes
        const cacheParam = greetingCacheBuster > 0 ? `?_cb=${greetingCacheBuster}` : '';
        const data = await apiRequest(`/api/workspaces/greeting${cacheParam}`);
        // Select appropriate greeting based on client time
        return selectTimeBasedGreeting(data);
    } catch (err) {
        console.error('[Greeting] Failed to fetch greeting:', err);
        return 'Welcome to Printify!';
    }
}
