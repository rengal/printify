/**
 * Returns the app version string from the <meta name="app-version"> tag.
 * Used as a cache-busting query parameter on all template fetches.
 */
export const APP_VERSION = document.querySelector('meta[name="app-version"]')?.content ?? '1';
export const V = `?v=${APP_VERSION}`;
