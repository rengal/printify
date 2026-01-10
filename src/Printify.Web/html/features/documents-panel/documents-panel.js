/**
 * Documents Panel Module
 *
 * Manages the documents panel for displaying printer documents:
 * - Renders different states (no workspace, no printer, no documents, documents list)
 * - Renders off-DOM to avoid flicker
 * - Handles document debug toggle and copy actions
 */

// ============================================================================
// STATE
// ============================================================================

let templateDocument = null;
let templates = {};
let currentContainer = null;

// Callbacks for actions (set by main.js)
const callbacks = {
    onCreateWorkspace: null,
    onAccessWorkspace: null,
    onToggleDocumentDebug: null,
    onCopyDocument: null,
    getWelcomeMessage: null,
    getDebugMode: null,
    getPrinterById: null,
    isDocumentRawDataActive: null,
    renderViewDocument: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the documents panel module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

// ============================================================================
// TEMPLATE LOADING
// ============================================================================

/**
 * Load the template document once and cache it
 */
async function loadTemplateDocument() {
    if (templateDocument) return templateDocument;

    const response = await fetch('features/documents-panel/documents-panel.html');
    const html = await response.text();
    const parser = new DOMParser();
    templateDocument = parser.parseFromString(html, 'text/html');

    // Cache all templates
    templates = {
        noWorkspace: templateDocument.querySelector('#docs-panel-no-workspace-template'),
        noPrinter: templateDocument.querySelector('#docs-panel-no-printer-template'),
        noDocuments: templateDocument.querySelector('#docs-panel-no-documents-template'),
        documentItem: templateDocument.querySelector('#docs-panel-document-item-template')
    };

    return templateDocument;
}

// ============================================================================
// RENDER FUNCTIONS
// ============================================================================

/**
 * Render the no-workspace state (landing page)
 */
export async function renderNoWorkspace(targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container) return null;

    await loadTemplateDocument();
    container.innerHTML = '';

    const fragment = templates.noWorkspace.content.cloneNode(true);
    container.appendChild(fragment);

    // Attach event handlers
    const createBtn = container.querySelector('[data-action="create-workspace"]');
    const accessBtn = container.querySelector('[data-action="access-workspace"]');

    if (createBtn) {
        createBtn.addEventListener('click', () => callbacks.onCreateWorkspace?.());
    }
    if (accessBtn) {
        accessBtn.addEventListener('click', () => callbacks.onAccessWorkspace?.());
    }

    currentContainer = container;
    return container;
}

/**
 * Render the no-printer-selected state
 */
export async function renderNoPrinter(options, targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container) return null;

    await loadTemplateDocument();
    container.innerHTML = '';

    const fragment = templates.noPrinter.content.cloneNode(true);
    container.appendChild(fragment);

    // Set greeting and message
    const greetingEl = container.querySelector('[data-docs-greeting]');
    const messageEl = container.querySelector('[data-docs-message]');

    if (greetingEl) {
        greetingEl.textContent = options?.greeting || 'Welcome!';
    }
    if (messageEl) {
        messageEl.textContent = options?.message || 'Select a printer to view documents';
    }

    currentContainer = container;
    return container;
}

/**
 * Render the no-documents state for a selected printer
 */
export async function renderNoDocuments(printer, targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container) return null;

    await loadTemplateDocument();
    container.innerHTML = '';

    const fragment = templates.noDocuments.content.cloneNode(true);
    container.appendChild(fragment);

    // Set printer connection info
    const hostEl = container.querySelector('[data-docs-host]');
    const portEl = container.querySelector('[data-docs-port]');
    const protocolEl = container.querySelector('[data-docs-protocol]');

    if (hostEl) hostEl.textContent = printer?.publicHost || 'localhost';
    if (portEl) portEl.textContent = printer?.port || 'not configured';
    if (protocolEl) protocolEl.textContent = (printer?.protocol || 'ESC/POS').toUpperCase();

    currentContainer = container;
    return container;
}

/**
 * Render the documents list state
 * @param {Array} documents - Array of document objects
 * @param {Object} printer - Printer object
 * @param {Element} targetContainer - Optional target container
 */
export async function renderDocumentsList(documents, printer, targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container) return null;

    await loadTemplateDocument();

    // Create a fragment off-DOM to avoid flicker
    const fragment = document.createDocumentFragment();

    for (const doc of documents) {
        const docElement = renderDocumentItem(doc);
        if (docElement) {
            fragment.appendChild(docElement);
        }
    }

    // Clear container and attach all at once
    container.innerHTML = '';
    container.appendChild(fragment);

    // Adjust Y positions in debug mode after DOM insertion
    const debugDocs = documents.filter(doc => callbacks.isDocumentRawDataActive?.(doc));
    if (debugDocs.length > 0) {
        requestAnimationFrame(() => {
            debugDocs.forEach(doc => {
                const contentId = `doc-content-${doc.id}`;
                adjustDebugYPositions(contentId, true);
            });
        });
    }

    currentContainer = container;
    return container;
}

/**
 * Render a single document item
 */
function renderDocumentItem(doc) {
    if (!templates.documentItem) return null;

    const fragment = templates.documentItem.content.cloneNode(true);
    const item = fragment.querySelector('.document-item');

    // Format datetime
    const dateTime = doc.timestamp?.toLocaleString(undefined, {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
    }) || '';
    const relativeTime = formatRelativeTime(doc.timestamp) || '';

    // Set datetime
    const datetimeEl = item.querySelector('[data-docs-datetime]');
    if (datetimeEl) {
        datetimeEl.textContent = `${dateTime} \u00B7 ${relativeTime}`;
    }

    // Set debug toggle
    const debugToggle = item.querySelector('[data-docs-debug-toggle]');
    const debugMode = callbacks.getDebugMode?.() || false;
    if (debugToggle) {
        debugToggle.checked = doc.debugEnabled || false;
        debugToggle.disabled = debugMode;
        debugToggle.addEventListener('change', (e) => {
            callbacks.onToggleDocumentDebug?.(doc.id, e.target.checked);
        });
    }

    // Set error icon if present
    const hasErrors = doc.errorMessages && doc.errorMessages.length > 0;
    const errorIcon = item.querySelector('[data-docs-error-icon]');
    if (errorIcon) {
        if (hasErrors) {
            errorIcon.style.display = '';
            errorIcon.title = doc.errorMessages.join('\n');
        } else {
            errorIcon.style.display = 'none';
        }
    }

    // Set preview HTML (rendered by main.js)
    const previewEl = item.querySelector('[data-docs-preview]');
    if (previewEl) {
        previewEl.innerHTML = doc.previewHtml || '';
    }

    // Set bytes
    const bytesEl = item.querySelector('[data-docs-bytes]');
    if (bytesEl) {
        bytesEl.textContent = formatByteCount(doc.bytesReceived);
    }

    // Set copy button
    const copyBtn = item.querySelector('[data-docs-copy]');
    if (copyBtn) {
        copyBtn.addEventListener('click', () => {
            callbacks.onCopyDocument?.(doc.plainText || '');
        });
    }

    return item;
}

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Format relative time (e.g., "2h ago")
 */
function formatRelativeTime(date) {
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
 * Format byte count with narrow no-break spaces
 */
function formatByteCount(bytes) {
    if (bytes == null) return '0';
    const normalized = Math.trunc(Number(bytes)) || 0;
    return normalized.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '\u202F');
}

/**
 * Adjust Y positions in debug mode to account for debug table heights
 * Copied from main.js - this handles the debug layout adjustment
 */
function adjustDebugYPositions(contentId, includeDebug) {
    if (!includeDebug) return;

    const container = document.getElementById(contentId);
    if (!container) return;

    const elements = Array.from(container.querySelectorAll('[data-original-y]'));
    let currentY = 0;

    elements.forEach((wrapper, index) => {
        const elementType = wrapper.getAttribute('data-element-type') || 'unknown';

        if (elementType === 'debug') {
            const debugTable = wrapper.querySelector('.debug-table');
            if (debugTable) {
                debugTable.style.top = `${currentY}px`;
                const debugHeight = debugTable.offsetHeight || 20;
                const debugDesc = debugTable.querySelector('.debug-desc')?.textContent?.trim() || '';
                console.log(`[${index}] debug | Y=${currentY}px H=${debugHeight}px | ${debugDesc.substring(0, 50)}`);
                currentY += debugHeight;
            }
        } else if (elementType === 'text' || elementType === 'image') {
            const visualElement = wrapper.querySelector('.view-text, .view-image');
            if (visualElement) {
                const elementHeight = parseInt(visualElement.style.height) || 0;
                const elementText = visualElement.textContent?.trim() || visualElement.alt || '';
                visualElement.style.top = `${currentY}px`;
                console.log(`[${index}] ${elementType} | Y=${currentY}px H=${elementHeight}px | ${elementText.substring(0, 50)}`);
                currentY += elementHeight;
            }
        }
    });

    const originalHeight = parseInt(container.style.height) || 0;
    if (currentY > originalHeight) {
        container.style.height = `${currentY}px`;
    }
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.DocumentsPanel = {
    init,
    renderNoWorkspace,
    renderNoPrinter,
    renderNoDocuments,
    renderDocumentsList
};
