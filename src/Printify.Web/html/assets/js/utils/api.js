/**
 * API Client Module
 *
 * Handles all HTTP requests to the backend API with authentication,
 * error handling, and session management.
 */

// API base URL (empty for same-origin requests)
const apiBase = '';

// Authentication state
let accessToken = null;
let workspaceToken = null;

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Set the access token for authenticated requests
 * @param {string} token - The JWT access token
 */
export function setAccessToken(token) {
    accessToken = token;
}

/**
 * Get the current access token
 * @returns {string|null} The current access token
 */
export function getAccessToken() {
    return accessToken;
}

/**
 * Set the workspace token
 * @param {string} token - The workspace token
 */
export function setWorkspaceToken(token) {
    workspaceToken = token;
}

/**
 * Get the workspace token
 * @returns {string|null} The workspace token
 */
export function getWorkspaceToken() {
    return workspaceToken;
}

/**
 * Generate authorization headers
 * @returns {object} Headers object with Authorization if token exists
 */
export function authHeaders() {
    return accessToken
        ? { 'Authorization': `Bearer ${accessToken}` }
        : {};
}

/**
 * Make an API request with error handling and authentication
 * @param {string} path - The API path
 * @param {object} options - Fetch options (method, body, etc.)
 * @param {object} options.isTokenLogin - Set to true when attempting login with token (prevents auto-logout on 401)
 * @returns {Promise<any>} The response data
 */
export async function apiRequest(path, options = {}) {
    const { isTokenLogin = false, ...fetchOptions } = options;

    const headers = {
        'Content-Type': 'application/json',
        ...authHeaders(),
        ...(fetchOptions.headers || {})
    };

    const response = await fetch(`${apiBase}${path}`, {
        ...fetchOptions,
        headers
    });

    if (!response.ok) {
        // Handle 401/403 - authentication/authorization failures
        if (response.status === 401 || response.status === 403) {
            console.error(`Auth failed (${response.status}) for ${path}, isTokenLogin: ${isTokenLogin}`);

            // Only auto-logout if we have a workspace token AND we're not trying to login with a new token
            if (workspaceToken && !isTokenLogin) {
                // Import dynamically to avoid circular dependency
                const { logOut } = await import('../main.js');
                logOut();
            }
        }

        const error = await response.json().catch(() => ({ error: response.statusText }));
        throw new Error(error.error || error.message || `API error: ${response.status}`);
    }

    return response.json();
}
