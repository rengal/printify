/**
 * Confirm Dialog Module
 *
 * Shows modal confirmation dialogs with customizable title, message, and actions.
 * Supports danger mode for destructive actions like delete.
 */

// ============================================================================
// STATE
// ============================================================================

let currentModal = null;
let template = null;

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Show a confirmation dialog
 * @param {string} title - Dialog title
 * @param {string} message - Dialog message (supports HTML)
 * @param {string} confirmText - Text for the confirm button
 * @param {Function} onConfirm - Callback when confirm is clicked
 * @param {boolean} isDanger - Whether this is a dangerous action (shows red icon)
 */
export async function show(title, message, confirmText, onConfirm, isDanger = false) {
    // Close any existing modal
    close();

    // Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    // Clone the template
    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-confirm-dialog-overlay]');
    const modal = modalOverlay.querySelector('.modal');
    const confirmBtn = modalOverlay.querySelector('[data-confirm-dialog-confirm]');
    const cancelBtn = modalOverlay.querySelector('[data-confirm-dialog-cancel]');
    const titleEl = modalOverlay.querySelector('[data-confirm-dialog-title]');
    const messageEl = modalOverlay.querySelector('[data-confirm-dialog-message]');
    const iconEl = modalOverlay.querySelector('[data-confirm-dialog-icon]');

    // Set content
    titleEl.textContent = title;
    messageEl.innerHTML = message;
    confirmBtn.textContent = confirmText;
    confirmBtn.className = `btn ${isDanger ? 'btn-danger' : 'btn-primary'}`;

    // Set icon
    const iconColor = isDanger ? '#ef4444' : '#3b82f6';
    const iconName = isDanger ? 'alert-triangle' : 'alert-circle';
    iconEl.innerHTML = createIcon(iconName, iconColor);

    // Setup event listeners
    cancelBtn.addEventListener('click', close);
    confirmBtn.addEventListener('click', () => {
        close();
        onConfirm();
    });

    modalOverlay.addEventListener('click', (e) => {
        if (e.target === modalOverlay) {
            close();
        }
    });

    // ESC key closes modal
    const handleEscape = (e) => {
        if (e.key === 'Escape') {
            close();
        }
    };
    document.addEventListener('keydown', handleEscape);

    currentModal = modalOverlay;
    currentModal.escapeHandler = handleEscape;

    document.body.appendChild(modalOverlay);
}

/**
 * Close the current confirmation dialog
 */
export function close() {
    if (currentModal) {
        if (currentModal.escapeHandler) {
            document.removeEventListener('keydown', currentModal.escapeHandler);
        }
        currentModal.remove();
        currentModal = null;
    }
}

// ============================================================================
// HELPERS
// ============================================================================

/**
 * Load the HTML template
 */
async function loadTemplate() {
    const response = await fetch('components/dialogs/confirm-dialog/confirm-dialog.html');
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

/**
 * Create an icon SVG element
 */
function createIcon(name, color) {
    const icons = {
        'alert-triangle': `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0;">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
            <line x1="12" y1="9" x2="12" y2="13"></line>
            <line x1="12" y1="17" x2="12.01" y2="17"></line>
        </svg>`,
        'alert-circle': `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0;">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
        </svg>`
    };

    return icons[name] || icons['alert-circle'];
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.ConfirmDialog = {
    show,
    close
};
