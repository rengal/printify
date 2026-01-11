/**
 * Printer Dialogue Module
 *
 * Manages the printer add/edit dialog:
 * - Shows dialog for creating new printers
 * - Shows dialog for editing existing printers
 * - Handles form validation and submission
 * - Uses template-based rendering with data-* attributes
 */

import { normalizeProtocol } from '../../../assets/js/api/protocol.js';

// ============================================================================
// STATE
// ============================================================================

let template = null;
let currentMode = null; // 'create' or 'edit'
let currentPrinterId = null;
let currentOverlay = null;

// Callbacks for actions (set by main.js)
const callbacks = {
    apiRequest: null,
    normalizeProtocol: null,
    loadPrinters: null,
    closeModal: null,
    showToast: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the printer dialogue module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

/**
 * Show the printer dialog for creating a new printer
 */
export async function showCreate() {
    await show('create', null);
}

/**
 * Show the printer dialog for editing an existing printer
 * @param {Object} printer - Printer object with id, name, protocol, width, emulateBuffer, bufferSize, drainRate
 */
export async function showEdit(printer) {
    if (!printer) {
        console.error('Printer data is required for edit mode');
        return;
    }
    await show('edit', printer);
}

/**
 * Close the current printer dialog
 */
export function close() {
    if (currentOverlay) {
        if (currentOverlay.escapeHandler) {
            document.removeEventListener('keydown', currentOverlay.escapeHandler);
        }
        currentOverlay.remove();
        currentOverlay = null;
        currentMode = null;
        currentPrinterId = null;
    }
}

// ============================================================================
// INTERNAL FUNCTIONS
// ============================================================================

/**
 * Show the printer dialog
 * @param {string} mode - 'create' or 'edit'
 * @param {Object|null} printer - Printer data for edit mode
 */
async function show(mode, printer) {
    // Close any existing dialog
    close();

    // Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    currentMode = mode;
    currentPrinterId = printer?.id || null;

    // Clone the template
    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-printer-dialogue-overlay]');

    // Store element references
    const elements = {
        overlay: modalOverlay,
        title: modalOverlay.querySelector('[data-printer-dialogue-title]'),
        nameInput: modalOverlay.querySelector('[data-printer-dialogue-name-input]'),
        nameError: modalOverlay.querySelector('[data-printer-dialogue-name-error]'),
        protocolInput: modalOverlay.querySelector('[data-printer-dialogue-protocol-input]'),
        protocolHint: modalOverlay.querySelector('[data-printer-dialogue-protocol-hint]'),
        widthInput: modalOverlay.querySelector('[data-printer-dialogue-width-input]'),
        emulateBufferInput: modalOverlay.querySelector('[data-printer-dialogue-emulate-buffer-input]'),
        bufferFields: modalOverlay.querySelector('[data-printer-dialogue-buffer-fields]'),
        bufferSizeInput: modalOverlay.querySelector('[data-printer-dialogue-buffer-size-input]'),
        drainRateInput: modalOverlay.querySelector('[data-printer-dialogue-drain-rate-input]'),
        securitySection: modalOverlay.querySelector('[data-printer-dialogue-security-section]'),
        securityAckInput: modalOverlay.querySelector('[data-printer-dialogue-security-ack-input]'),
        securityAckError: modalOverlay.querySelector('[data-printer-dialogue-security-ack-error]'),
        cancelBtn: modalOverlay.querySelector('[data-printer-dialogue-cancel]'),
        submitBtn: modalOverlay.querySelector('[data-printer-dialogue-submit]')
    };

    // Setup event listeners
    elements.cancelBtn.addEventListener('click', close);
    elements.submitBtn.addEventListener('click', () => handleSubmit(elements));
    elements.emulateBufferInput.addEventListener('change', () => toggleBufferFields(elements));
    elements.nameInput.addEventListener('input', () => clearNameError(elements));
    elements.securityAckInput?.addEventListener('change', () => clearSecurityError(elements));

    // ESC key closes dialog
    const handleEscape = (e) => {
        if (e.key === 'Escape') {
            close();
        }
    };
    document.addEventListener('keydown', handleEscape);
    elements.overlay.escapeHandler = handleEscape;

    // Click outside closes dialog
    elements.overlay.addEventListener('click', (e) => {
        if (e.target === elements.overlay) {
            close();
        }
    });

    // Configure dialog based on mode
    configureDialog(elements, mode, printer);

    // Append to DOM (fully prepared before showing)
    document.getElementById('modalContainer').appendChild(modalOverlay);

    // Store overlay reference
    currentOverlay = elements.overlay;

    // Focus name input after a short delay to ensure dialog is visible
    setTimeout(() => {
        elements.nameInput.focus();
    }, 50);
}

/**
 * Configure the dialog based on mode (create/edit) and printer data
 */
function configureDialog(elements, mode, printer) {
    const isEditMode = mode === 'edit';

    // Set title
    elements.title.textContent = isEditMode ? 'Edit Printer' : 'New Printer';

    // Set submit button text
    elements.submitBtn.textContent = isEditMode ? 'Save' : 'Create';

    // Hide security section in edit mode
    elements.securitySection.style.display = isEditMode ? 'none' : 'block';

    // Configure protocol field
    if (isEditMode) {
        elements.protocolInput.disabled = true;
        elements.protocolInput.classList.add('no-dropdown');
        elements.protocolHint.textContent = 'Protocol cannot be changed after creation';
        elements.protocolHint.style.display = 'block';
    } else {
        elements.protocolInput.disabled = false;
        elements.protocolInput.classList.remove('no-dropdown');
        elements.protocolHint.style.display = 'none';
    }

    // Populate fields for edit mode
    if (isEditMode && printer) {
        elements.nameInput.value = printer.name || '';
        elements.protocolInput.value = normalizeProtocol(printer.protocol);
        elements.widthInput.value = printer.width || 512;
        elements.emulateBufferInput.checked = printer.emulateBuffer || false;
        elements.bufferSizeInput.value = printer.bufferSize || 4096;
        elements.drainRateInput.value = printer.drainRate || 4096;
    }

    // Set initial buffer fields visibility
    toggleBufferFields(elements);
}

/**
 * Toggle visibility of buffer fields based on emulate buffer checkbox
 */
function toggleBufferFields(elements) {
    const shouldShow = elements.emulateBufferInput.checked;
    elements.bufferFields.style.display = shouldShow ? 'block' : 'none';
}

/**
 * Clear name validation error
 */
function clearNameError(elements) {
    elements.nameInput.classList.remove('invalid');
    elements.nameError.classList.remove('show');
}

/**
 * Clear security acknowledgment error
 */
function clearSecurityError(elements) {
    elements.securityAckInput.classList.remove('invalid');
    elements.securityAckError.classList.remove('show');
}

/**
 * Handle form submission
 */
async function handleSubmit(elements) {
    if (currentMode === 'create') {
        await handleCreate(elements);
    } else if (currentMode === 'edit') {
        await handleEdit(elements);
    }
}

/**
 * Handle create printer submission
 */
async function handleCreate(elements) {
    const name = elements.nameInput.value.trim();
    const protocol = elements.protocolInput.value;
    const width = parseInt(elements.widthInput.value) || 512;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value) || 4096;
    const securityAck = elements.securityAckInput.checked;

    // Clear validation errors
    clearNameError(elements);
    clearSecurityError(elements);

    // Validate name
    if (!name) {
        elements.nameInput.classList.add('invalid');
        elements.nameError.classList.add('show');
        elements.nameInput.focus();
        return;
    }

    // Validate security acknowledgment
    if (!securityAck) {
        elements.securityAckError.classList.add('show');
        elements.securityAckInput.focus();
        return;
    }

    try {
        const request = {
            printer: {
                id: crypto.randomUUID(),
                displayName: name
            },
            settings: {
                protocol: callbacks.normalizeProtocol ? callbacks.normalizeProtocol(protocol) : normalizeProtocol(protocol),
                widthInDots: width,
                heightInDots: null,
                emulateBufferCapacity: emulateBuffer,
                bufferDrainRate: drainRate,
                bufferMaxCapacity: bufferSize
            }
        };

        const created = await callbacks.apiRequest('/api/printers', {
            method: 'POST',
            body: JSON.stringify(request)
        });

        close();

        if (callbacks.loadPrinters) {
            await callbacks.loadPrinters(created.printer.id);
        }

        if (callbacks.showToast) {
            callbacks.showToast('Printer created successfully');
        }
    } catch (err) {
        console.error(err);
        if (callbacks.showToast) {
            callbacks.showToast(err.message || 'Failed to create printer', true);
        }
    }
}

/**
 * Handle edit printer submission
 */
async function handleEdit(elements) {
    if (!currentPrinterId) {
        console.error('Printer ID is required for edit mode');
        return;
    }

    const name = elements.nameInput.value.trim();
    const protocol = elements.protocolInput.value;
    const width = parseInt(elements.widthInput.value) || 512;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value) || 4096;

    // Clear validation errors
    clearNameError(elements);

    // Validate name
    if (!name) {
        elements.nameInput.classList.add('invalid');
        elements.nameError.classList.add('show');
        elements.nameInput.focus();
        return;
    }

    try {
        const request = {
            printer: {
                id: currentPrinterId,
                displayName: name
            },
            settings: {
                protocol: callbacks.normalizeProtocol ? callbacks.normalizeProtocol(protocol) : normalizeProtocol(protocol),
                widthInDots: width,
                heightInDots: null,
                emulateBufferCapacity: emulateBuffer,
                bufferDrainRate: drainRate,
                bufferMaxCapacity: bufferSize
            }
        };

        await callbacks.apiRequest(`/api/printers/${currentPrinterId}`, {
            method: 'PUT',
            body: JSON.stringify(request)
        });

        close();

        if (callbacks.loadPrinters) {
            await callbacks.loadPrinters(currentPrinterId);
        }

        if (callbacks.showToast) {
            callbacks.showToast('Printer updated successfully');
        }
    } catch (err) {
        console.error(err);
        if (callbacks.showToast) {
            callbacks.showToast(err.message || 'Failed to update printer', true);
        }
    }
}

/**
 * Load the HTML template
 */
async function loadTemplate() {
    const response = await fetch('features/printers/printer-dialog/printer-dialog.html');
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.PrinterDialogue = {
    init,
    showCreate,
    showEdit,
    close
};
