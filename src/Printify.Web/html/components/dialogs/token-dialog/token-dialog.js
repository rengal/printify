/**
 * Token Dialog Module
 *
 * Shows a dialog displaying the workspace token with copy functionality
 */

let template = null;
let currentOverlay = null;

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Show the token dialog with the given token
 * @param {string} token - The workspace token to display
 */
export async function show(token) {
    // Close any existing dialog
    close();

    // Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    // Clone the template
    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-token-dialog-overlay]');

    // Set the token value
    const tokenValueEl = modalOverlay.querySelector('[data-token-value]');
    if (tokenValueEl) {
        tokenValueEl.textContent = token;
    }

    // Setup event listeners
    const closeBtn = modalOverlay.querySelector('[data-token-dialog-close]');
    const copyBtn = modalOverlay.querySelector('[data-copy-token-btn]');

    closeBtn.addEventListener('click', close);
    copyBtn.addEventListener('click', () => copyToClipboard(token));

    // ESC key closes dialog
    const handleEscape = (e) => {
        if (e.key === 'Escape') {
            close();
        }
    };
    document.addEventListener('keydown', handleEscape);
    modalOverlay.escapeHandler = handleEscape;

    // Click outside closes dialog
    modalOverlay.addEventListener('click', (e) => {
        if (e.target === modalOverlay) {
            close();
        }
    });

    // Append to DOM
    document.getElementById('modalContainer').appendChild(modalOverlay);
    currentOverlay = modalOverlay;
}

/**
 * Close the token dialog
 */
export function close() {
    if (currentOverlay) {
        if (currentOverlay.escapeHandler) {
            document.removeEventListener('keydown', currentOverlay.escapeHandler);
        }
        currentOverlay.remove();
        currentOverlay = null;
    }
}

// ============================================================================
// INTERNAL FUNCTIONS
// ============================================================================

async function loadTemplate() {
    const response = await fetch('components/dialogs/token-dialog/token-dialog.html');
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        // Show success feedback
        const copyBtn = currentOverlay?.querySelector('[data-copy-token-btn]');
        if (copyBtn) {
            const originalText = copyBtn.innerHTML;
            copyBtn.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
                Copied!
            `;
            copyBtn.classList.add('btn-success');
            setTimeout(() => {
                copyBtn.innerHTML = originalText;
                copyBtn.classList.remove('btn-success');
            }, 2000);
        }
    }).catch(err => {
        console.error('Failed to copy:', err);
    });
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.TokenDialog = {
    show,
    close
};
