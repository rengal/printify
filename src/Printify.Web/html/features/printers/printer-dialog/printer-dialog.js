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
import { createUuid } from '../../../assets/js/utils/uuid.js';

// ============================================================================
// STATE
// ============================================================================

let template = null;
let currentMode = null; // 'create' or 'edit'
let currentPrinterId = null;
let currentOverlay = null;
let protocolDefaultsSet = false; // Track if defaults have been set for current dialog

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
        dimensionsGroup: modalOverlay.querySelector('[data-printer-dialogue-dimensions-group]'),
        widthInput: modalOverlay.querySelector('[data-printer-dialogue-width-input]'),
        heightField: modalOverlay.querySelector('[data-printer-dialogue-height-field]'),
        heightInput: modalOverlay.querySelector('[data-printer-dialogue-height-input]'),
        emulateBufferField: modalOverlay.querySelector('[data-printer-dialogue-emulate-buffer-field]'),
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
    elements.protocolInput.addEventListener('change', () => {
        elements.protocolInput.classList.remove('invalid');
        updateProtocolFields(elements);
    });
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

    // Reset protocol defaults tracking
    protocolDefaultsSet = false;

    // Set title
    elements.title.textContent = isEditMode ? 'Edit Printer' : 'New Printer';

    // Set submit button text and initial state
    elements.submitBtn.textContent = isEditMode ? 'Save' : 'Create';
    elements.submitBtn.disabled = !isEditMode; // Disabled in create mode until protocol selected

    // In create mode, hide all protocol-dependent fields initially
    if (!isEditMode) {
        elements.dimensionsGroup.style.display = 'none';
        elements.heightField.style.display = 'none';
        elements.emulateBufferField.style.display = 'none';
        elements.bufferFields.style.display = 'none';
        elements.securitySection.style.display = 'none';
    } else {
        // In edit mode, show security section is handled below
        // All fields will be shown by updateProtocolFields
    }

    // Hide security section in edit mode
    if (isEditMode) {
        elements.securitySection.style.display = 'none';
    }

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
        if (elements.heightInput) {
            elements.heightInput.value = printer.height || 300;
        }
        elements.emulateBufferInput.checked = printer.emulateBuffer || false;
        elements.bufferSizeInput.value = printer.bufferSize || 4096;
        elements.drainRateInput.value = printer.drainRate || 2048;

        // In edit mode, show all fields based on protocol
        protocolDefaultsSet = true; // Don't override existing values
    }

    // Set initial protocol-specific field visibility
    updateProtocolFields(elements);

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

// Protocol-specific defaults
const DEFAULT_WIDTH_ESCPOS = 512;
const DEFAULT_WIDTH_EPL = 412;
const DEFAULT_HEIGHT_EPL = 310;

/**
 * Update field visibility and defaults based on protocol selection
 */
function updateProtocolFields(elements) {
    const protocol = elements.protocolInput.value;
    const isEpl = protocol === 'epl';
    const isEscPos = protocol === 'escpos';
    const hasProtocol = protocol !== '';
    const isEditMode = currentMode === 'edit';

    // Show/hide protocol-dependent fields based on whether protocol is selected
    if (hasProtocol) {
        // Show dimensions group (contains width and height fields)
        if (elements.dimensionsGroup) {
            elements.dimensionsGroup.style.display = 'grid';
        }

        // Show height field only for EPL
        if (elements.heightField) {
            elements.heightField.style.display = isEpl ? 'block' : 'none';
        }

        // Show buffer emulation only for ESC/POS
        if (elements.emulateBufferField) {
            elements.emulateBufferField.style.display = isEscPos ? 'flex' : 'none';
        }

        // Show security section in create mode
        if (!isEditMode && elements.securitySection) {
            elements.securitySection.style.display = 'block';
        }

        // Enable submit button in create mode
        if (!isEditMode && elements.submitBtn) {
            elements.submitBtn.disabled = false;
        }
    } else {
        // Hide all protocol-dependent fields when no protocol selected
        if (elements.dimensionsGroup) {
            elements.dimensionsGroup.style.display = 'none';
        }
        if (elements.heightField) {
            elements.heightField.style.display = 'none';
        }
        if (elements.emulateBufferField) {
            elements.emulateBufferField.style.display = 'none';
        }
        if (elements.bufferFields) {
            elements.bufferFields.style.display = 'none';
        }
        if (!isEditMode && elements.securitySection) {
            elements.securitySection.style.display = 'none';
        }

        // Disable submit button in create mode
        if (!isEditMode && elements.submitBtn) {
            elements.submitBtn.disabled = true;
        }

        // Clear values when no protocol selected
        if (elements.widthInput) elements.widthInput.value = '';
        if (elements.heightInput) elements.heightInput.value = '';

        return;
    }

    // Set defaults only on first protocol selection in create mode
    if (!isEditMode && !protocolDefaultsSet) {
        if (isEscPos) {
            elements.widthInput.value = DEFAULT_WIDTH_ESCPOS;
        } else if (isEpl) {
            elements.widthInput.value = DEFAULT_WIDTH_EPL;
            if (elements.heightInput) {
                elements.heightInput.value = DEFAULT_HEIGHT_EPL;
            }
        }
        protocolDefaultsSet = true;
    }

    // Handle buffer fields visibility
    if (isEpl) {
        elements.bufferFields.style.display = 'none';
    } else if (isEscPos) {
        // For ESC/POS, respect the checkbox state
        toggleBufferFields(elements);
    }
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
    const height = elements.heightInput ? (parseInt(elements.heightInput.value) || 300) : 300;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value) || 2048;
    const securityAck = elements.securityAckInput.checked;

    // Clear validation errors
    clearNameError(elements);
    clearSecurityError(elements);
    elements.protocolInput.classList.remove('invalid');

    // Validate name
    if (!name) {
        elements.nameInput.classList.add('invalid');
        elements.nameError.classList.add('show');
        elements.nameInput.focus();
        return;
    }

    // Validate protocol
    if (!protocol) {
        elements.protocolInput.classList.add('invalid');
        elements.protocolInput.focus();
        if (callbacks.showToast) {
            callbacks.showToast('Please select a protocol', true);
        }
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
                id: createUuid(),
                displayName: name
            },
            settings: {
                protocol: callbacks.normalizeProtocol ? callbacks.normalizeProtocol(protocol) : normalizeProtocol(protocol),
                widthInDots: width,
                heightInDots: protocol === 'epl' ? height : null,
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
    const height = elements.heightInput ? (parseInt(elements.heightInput.value) || 300) : 300;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value) || 2048;

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
                heightInDots: protocol === 'epl' ? height : null,
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
    // Add cache busting parameter to force reload
    const cacheBuster = `?v=${Date.now()}`;
    const response = await fetch('features/printers/printer-dialog/printer-dialog.html' + cacheBuster, { cache: 'no-store' });
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
