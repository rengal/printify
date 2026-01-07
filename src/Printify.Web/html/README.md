# Printify Web UI Structure

This folder hosts the static UI served by the backend.

## Files
- `index.html` — root HTML; links external CSS/JS.
- `assets/css/style.css` — theme tokens, layout, and component styles (extracted from inline styles).
- `assets/js/main.js` — all UI logic (workspace handling, printers/documents, theming, toasts).

## Serving
- Ensure the app serves `index.html` as the default static file (e.g., `UseDefaultFiles` + `UseStaticFiles` pointing to `html/`).
- All paths are relative (e.g., `assets/...`) so the page works when hosted at `/`.

## Styling
- Design tokens are defined at the top of `style.css` (`:root`), with dark/light variants.
- Base typography/layout utilities are shared across sections to keep a consistent look.

## Scripts
- `main.js` contains state, API interactions, render functions, and event wiring. Keep new logic modular inside this file or split into additional modules under `assets/js/` as needed.

## Licenses
- Third-party license inventory: `docs/licenses.md` (local links to license texts).
