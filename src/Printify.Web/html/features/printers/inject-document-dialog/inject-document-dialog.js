/**
 * Import Document Dialog Module
 *
 * Shows a dialog that lets the user import raw document bytes into a printer
 * either by pasting base64 text or by dropping / selecting a binary file.
 */

// ============================================================================
// STATE
// ============================================================================

let template = null;
let currentOverlay = null;
let selectedFile = null;   // File object when loaded from disk

const callbacks = {
    apiRequest: null,
    showToast: null,
    loadPrinters: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

export async function show(printerId) {
    close();

    if (!template) {
        await loadTemplate();
    }

    selectedFile = null;

    const fragment = template.content.cloneNode(true);
    const overlay = fragment.querySelector('[data-import-dialog-overlay]');

    const elements = {
        overlay,
        closeBtn:      overlay.querySelector('[data-import-dialog-close]'),
        cancelBtn:     overlay.querySelector('[data-import-dialog-cancel]'),
        submitBtn:     overlay.querySelector('[data-import-dialog-submit]'),
        dropZone:      overlay.querySelector('[data-import-drop-zone]'),
        dropIdle:      overlay.querySelector('[data-import-drop-idle]'),
        dropActive:    overlay.querySelector('[data-import-drop-active]'),
        fileInfo:      overlay.querySelector('[data-import-file-info]'),
        fileInput:     overlay.querySelector('[data-import-file-input]'),
        fileName:      overlay.querySelector('[data-import-file-name]'),
        fileClearBtn:  overlay.querySelector('[data-import-file-clear]'),
        base64Input:   overlay.querySelector('[data-import-base64-input]'),
        base64Error:   overlay.querySelector('[data-import-base64-error]')
    };

    bindEvents(elements, printerId);

    document.getElementById('modalContainer').appendChild(overlay);
    currentOverlay = overlay;

    setTimeout(() => elements.base64Input.focus(), 50);
}

export function close() {
    if (!currentOverlay) return;

    if (currentOverlay.escapeHandler) {
        document.removeEventListener('keydown', currentOverlay.escapeHandler);
    }

    currentOverlay.remove();
    currentOverlay = null;
    selectedFile = null;
}

// ============================================================================
// EVENTS
// ============================================================================

function bindEvents(elements, printerId) {
    elements.closeBtn.addEventListener('click', close);
    elements.cancelBtn.addEventListener('click', close);

    elements.submitBtn.addEventListener('click', () => handleSubmit(elements, printerId));

    // File input via browse
    elements.fileInput.addEventListener('change', () => {
        const file = elements.fileInput.files[0];
        if (file) setFile(elements, file);
    });

    // Clear selected file
    elements.fileClearBtn.addEventListener('click', () => clearFile(elements));

    // Drag-and-drop
    elements.dropZone.addEventListener('dragenter', (e) => {
        e.preventDefault();
        elements.dropZone.classList.add('drag-over');
        elements.dropIdle.style.display = 'none';
        elements.dropActive.style.display = 'flex';
        elements.fileInfo.style.display = 'none';
    });

    elements.dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
    });

    elements.dropZone.addEventListener('dragleave', (e) => {
        if (!elements.dropZone.contains(e.relatedTarget)) {
            elements.dropZone.classList.remove('drag-over');
            restoreDropZoneIdle(elements);
        }
    });

    elements.dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        elements.dropZone.classList.remove('drag-over');
        const file = e.dataTransfer.files[0];
        if (file) {
            setFile(elements, file);
        } else {
            restoreDropZoneIdle(elements);
        }
    });

    // Close on overlay click
    elements.overlay.addEventListener('click', (e) => {
        if (e.target === elements.overlay) close();
    });

    // Close on ESC
    const handleEscape = (e) => {
        if (e.key === 'Escape') close();
    };
    document.addEventListener('keydown', handleEscape);
    elements.overlay.escapeHandler = handleEscape;

    // Clear base64 error on edit
    elements.base64Input.addEventListener('input', () => {
        clearError(elements);
    });
}

// ============================================================================
// FILE HANDLING
// ============================================================================

function setFile(elements, file) {
    selectedFile = file;
    elements.fileName.textContent = file.name;
    elements.dropIdle.style.display = 'none';
    elements.dropActive.style.display = 'none';
    elements.fileInfo.style.display = 'flex';
    // Clear the textarea when a file is selected
    elements.base64Input.value = '';
    clearError(elements);
}

function clearFile(elements) {
    selectedFile = null;
    elements.fileInput.value = '';
    restoreDropZoneIdle(elements);
}

function restoreDropZoneIdle(elements) {
    elements.dropIdle.style.display = 'flex';
    elements.dropActive.style.display = 'none';
    if (!selectedFile) {
        elements.fileInfo.style.display = 'none';
    } else {
        elements.fileInfo.style.display = 'flex';
        elements.dropIdle.style.display = 'none';
    }
}

// ============================================================================
// SUBMIT
// ============================================================================

async function handleSubmit(elements, printerId) {
    clearError(elements);

    let base64;

    if (selectedFile) {
        try {
            base64 = await readFileAsBase64(selectedFile);
        } catch {
            showError(elements, 'Failed to read file.');
            return;
        }
    } else {
        const raw = elements.base64Input.value.trim();
        if (!raw) {
            showError(elements, 'Paste base64 data or select a file.');
            return;
        }
        base64 = raw.replace(/\s/g, '');
        if (!isValidBase64(base64)) {
            showError(elements, 'Invalid base64 string.');
            elements.base64Input.classList.add('invalid');
            return;
        }
    }

    elements.submitBtn.disabled = true;
    elements.submitBtn.textContent = 'Importing…';

    try {
        await callbacks.apiRequest(`/api/printers/${printerId}/documents/import`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ data: base64 })
        });
        callbacks.showToast?.('Document imported');
        await callbacks.loadPrinters?.(printerId);
        close();
    } catch (err) {
        console.error(err);
        showError(elements, err.message || 'Failed to import document.');
        elements.submitBtn.disabled = false;
        elements.submitBtn.textContent = 'Import';
    }
}

// ============================================================================
// HELPERS
// ============================================================================

function readFileAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            // If the file is a text file containing base64, use it directly.
            // If it's a binary file, encode it as base64.
            const result = reader.result;
            const stripped = result.replace(/\s/g, '');

            if (isValidBase64(stripped)) {
                resolve(stripped);
            } else {
                // Binary file — re-read as ArrayBuffer and encode
                const binaryReader = new FileReader();
                binaryReader.onload = () => {
                    const bytes = new Uint8Array(binaryReader.result);
                    let binary = '';
                    for (const b of bytes) binary += String.fromCharCode(b);
                    resolve(btoa(binary));
                };
                binaryReader.onerror = () => reject(binaryReader.error);
                binaryReader.readAsArrayBuffer(file);
            }
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsText(file);
    });
}

function isValidBase64(str) {
    // Remove whitespace, then check characters and padding
    const s = str.replace(/\s/g, '');
    if (s.length % 4 !== 0) return false;
    return /^[A-Za-z0-9+/]*={0,2}$/.test(s);
}

function showError(elements, message) {
    elements.base64Error.textContent = message;
    elements.base64Error.classList.add('show');
}

function clearError(elements) {
    elements.base64Error.textContent = '';
    elements.base64Error.classList.remove('show');
    elements.base64Input.classList.remove('invalid');
}

async function loadTemplate() {
    const response = await fetch('features/printers/inject-document-dialog/import-document-dialog.html');
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

// ============================================================================
// WINDOW EXPORTS
// ============================================================================

window.ImportDocumentDialog = {
    init,
    show,
    close
};
