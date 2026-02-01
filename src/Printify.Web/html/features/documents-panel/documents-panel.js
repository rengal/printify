/**
 * Documents Panel Module
 *
 * Manages the documents panel for displaying printer documents:
 * - Renders different states (no workspace, no printer, no documents, documents list)
 * - Renders off-DOM to avoid flicker
 * - Handles document debug toggle and copy actions
 * - Renders canvas document elements
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
    escapeHtml: null,
    resolveMediaUrl: null
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
                if (doc.canvases && doc.canvases.length > 0) {
                    // Adjust each canvas individually
                    doc.canvases.forEach((canvas, index) => {
                        const contentId = `doc-content-${doc.id}-canvas-${index}`;
                        adjustDebugYPositions(contentId, true);
                    });
                } else {
                    // Fallback for old single canvas format
                    const contentId = `doc-content-${doc.id}`;
                    adjustDebugYPositions(contentId, true);
                }
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

    // Set canvases container (multiple canvases)
    const canvasesContainer = item.querySelector('[data-docs-canvases-container]');
    if (canvasesContainer) {
        const canvases = doc.canvases || [];
        if (canvases.length === 0) {
            // Fallback for old single canvas format
            canvasesContainer.innerHTML = doc.previewHtml || '';
        } else {
            // Render each canvas as a separate block with its own copy button
            const totalPages = canvases.length;
            canvasesContainer.innerHTML = canvases.map((canvas, index) => `
                <div class="document-canvas-block">
                    ${canvas.previewHtml || ''}
                    <div class="document-canvas-footer">
                        <span class="document-meta-text document-footer-text">Page ${index + 1}/${totalPages}</span>
                        <button class="copy-icon-btn document-copy-btn" data-docs-copy data-docs-canvas-index="${index}" title="Copy canvas content">
                            <img src="assets/icons/copy.svg" width="14" height="14" alt="Copy">
                        </button>
                    </div>
                </div>
            `).join('');
        }
    }

    // Set bytes
    const bytesEl = item.querySelector('[data-docs-bytes]');
    if (bytesEl) {
        bytesEl.textContent = formatByteCount(doc.bytesReceived);
    }

    // Set copy buttons (one per canvas)
    const copyButtons = item.querySelectorAll('[data-docs-copy]');
    copyButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const canvasIndex = Number(btn.getAttribute('data-docs-canvas-index') || 0);
            const canvases = doc.canvases || [];
            const text = canvases[canvasIndex]?.plainText || doc.plainText || '';
            callbacks.onCopyDocument?.(text);
        });
    });

    return item;
}

// ============================================================================
// DOCUMENT RENDERING
// ============================================================================

/**
 * Render ViewDocument with absolute positioning
 * @param {Array} elements - Canvas elements to render
 * @param {number} documentWidth - Document width in dots
 * @param {number} documentHeight - Document height in dots (optional)
 * @param {string} docId - Document identifier
 * @param {Array} errorMessages - Array of error messages
 * @param {boolean} includeDebug - Whether to include debug information
 * @returns {string} HTML string
 */
export function renderViewDocument(elements, documentWidth, documentHeight, docId, errorMessages, includeDebug) {
    const width = Math.max(documentWidth || 384, 200);
    const hasErrors = errorMessages && errorMessages.length > 0;
    const errorClass = hasErrors ? ' has-errors' : '';
    const hasElements = Array.isArray(elements) && elements.length > 0;
    const hasVisualElements = hasElements && elements.some(el => {
        const type = (el?.type || '').toLowerCase();
        return type === 'text' || type === 'image';
    });
    const shouldShowEmptyMessage = !hasVisualElements && !includeDebug;

    if (!hasElements || shouldShowEmptyMessage) {
        const emptyClass = ' empty-document';
        const message = shouldShowEmptyMessage
            ? '<div class="document-empty-message">No visual elements detected.<br>Turn on Raw Data to see details</div>'
            : '';
        return `<div class="document-paper${errorClass}${emptyClass}">
            <div class="document-content empty-document" style="width:${width}px; height: auto;">
                ${message}
            </div>
        </div>`;
    }

    // Calculate height from elements if not provided
    let height = documentHeight;
    if (!height) {
        // Find max Y + Height to determine content height
        let maxBottom = 0;
        for (const el of elements) {
            if (el.type === 'text' || el.type === 'image') {
                const bottom = (Number(el.y) || 0) + (Number(el.height) || 0);
                if (bottom > maxBottom) {
                    maxBottom = bottom;
                }
            }
        }
        height = maxBottom || 100; // Minimum 100px
    }

    // Render elements in original order (don't sort - backend order is correct)
    let elementIndex = 0;
    const elementsHtml = elements.map(element => {
        const id = `el-${docId}-${elementIndex++}`;
        const desc = Array.isArray(element.commandDescription)
            ? element.commandDescription.join(' ')
            : (element.commandDescription || '');
        const visualText = element.text ? ` text="${element.text.substring(0, 30)}"` : '';
        const coords = element.type === 'text' || element.type === 'image'
            ? ` @(${element.x},${element.y})`
            : '';
        return renderViewElement(element, id, includeDebug);
    }).join('');

    const contentId = `doc-content-${docId}`;

    return `<div class="document-paper${errorClass}">
        <div class="document-content" id="${contentId}" style="width:${width}px; height:${height}px;" data-debug="${includeDebug}">
            ${elementsHtml}
        </div>
    </div>`;
}

/**
 * Render individual ViewElement
 * @param {Object} element - Element to render
 * @param {string} id - Element identifier
 * @param {boolean} includeDebug - Whether to include debug information
 * @returns {string} HTML string
 */
function renderViewElement(element, id, includeDebug) {
    const elementType = (element?.type || '').toLowerCase();

    switch (elementType) {
        case 'text':
            return renderViewTextElement(element, id);
        case 'image':
            return renderViewImageElement(element, id);
        case 'debug':
        case 'none':
            // Debug-only element - only render debug table if debug mode enabled
            return includeDebug ? `<div id="${id}" data-element-type="debug" data-original-y="0">${renderDebugTable(element)}</div>` : '';
        default:
            return '';
    }
}

/**
 * Render text element with absolute positioning
 * @param {Object} element - Text element
 * @param {string} id - Element identifier
 * @returns {string} HTML string
 */
function renderViewTextElement(element, id) {
    const x = Number(element.x) || 0;
    const y = Number(element.y) || 0;
    const width = Number(element.width) || 0;
    const height = Number(element.height) || 0;
    const zIndex = Number(element.zIndex) || 0;
    const text = element.text || '';
    const font = element.font || 'ESCPOS_A';
    const charSpacing = Number(element.charSpacing) || 0;
    const charScaleX = Number(element.charScaleX) || 1;
    const charScaleY = Number(element.charScaleY) || 1;

    // Map font to CSS class
    const fontClass = font === 'ESCPOS_B' ? 'escpos-font-b' : 'escpos-font-a';

    // Build inline styles
    const styles = [];
    styles.push(`left: ${x}px`);
    styles.push(`top: ${y}px`);
    styles.push(`width: ${width}px`);
    styles.push(`height: ${height}px`);
    styles.push(`z-index: ${zIndex}`);

    if (charSpacing !== 0) {
        styles.push(`letter-spacing: ${charSpacing}px`);
    }

    // Apply character scaling (for double-width, double-height text)
    if (charScaleX !== 1 || charScaleY !== 1) {
        styles.push(`transform: scale(${charScaleX}, ${charScaleY})`);
        styles.push('transform-origin: left top');
    }

    // Apply text styling modifiers inline
    if (element.isBold) {
        styles.push('font-weight: 700');
    }
    if (element.isUnderline) {
        styles.push('text-decoration: underline');
    }
    if (element.isReverse) {
        styles.push('background: #000');
        styles.push('color: #fff');
        styles.push('padding: 2px 4px');
        styles.push('border-radius: 3px');
    }

    const textContent = callbacks.escapeHtml?.(text) || text;

    return `<div id="${id}" data-element-type="text" data-original-y="${y}"><div class="view-text ${fontClass}" style="${styles.join('; ')};">${textContent}</div></div>`;
}

/**
 * Render image element with absolute positioning
 * @param {Object} element - Image element
 * @param {string} id - Element identifier
 * @returns {string} HTML string
 */
function renderViewImageElement(element, id) {
    const x = Number(element.x) || 0;
    const y = Number(element.y) || 0;
    const width = Number(element.width) || 0;
    const height = Number(element.height) || 0;
    const zIndex = Number(element.zIndex) || 0;

    const mediaUrl = callbacks.resolveMediaUrl?.(element?.media?.url || '') || element?.media?.url || '';
    if (!mediaUrl) {
        return '';
    }

    const styles = [];
    styles.push(`left: ${x}px`);
    styles.push(`top: ${y}px`);
    styles.push(`width: ${width}px`);
    styles.push(`height: ${height}px`);
    styles.push(`z-index: ${zIndex}`);

    const altText = `Image ${width}x${height}`;
    const escapedUrl = callbacks.escapeHtml?.(mediaUrl) || mediaUrl;

    return `<div id="${id}" data-element-type="image" data-original-y="${y}"><img class="view-image" src="${escapedUrl}" alt="${altText}" style="${styles.join('; ')};" loading="lazy"></div>`;
}

/**
 * Render debug table element
 * @param {Object} element - Debug element
 * @returns {string} HTML string
 */
function renderDebugTable(element) {
    const commandRaw = element.commandRaw || '';
    const commandDescription = Array.isArray(element.commandDescription)
        ? element.commandDescription.join('\n')
        : (element.commandDescription || '');
    const debugType = element.debugType || '';

    // Determine CSS class based on debug type
    const isError = debugType === 'error' || debugType === 'printerError';
    const isStatusResponse = debugType === 'statusResponse';
    const typeClass = isError ? ' debug-error' : (isStatusResponse ? ' debug-statusResponse' : '');

    // Format hex command with spaces
    const hexFormatted = formatHexCommand(commandRaw);

    // Truncate long text in descriptions
    const descFormatted = truncateTextInDescription(commandDescription);

    return `
        <table class="debug-table${typeClass}">
            <tr>
                <td class="debug-hex">${hexFormatted}</td>
                  <td class="debug-desc">${(callbacks.escapeHtml?.(descFormatted) || descFormatted).replace(/\n/g, '<br>') || '<span class="debug-missing">??</span>'}</td>
            </tr>
        </table>
    `;
}

/**
 * Format hex command for display
 * @param {string} commandRaw - Raw hex command
 * @returns {string} Formatted hex string
 */
function formatHexCommand(commandRaw) {
    if (!commandRaw || commandRaw.trim() === '') {
        return ''; // Leave blank if commandRaw is empty
    }

    // Remove any existing spaces
    const hex = commandRaw.replace(/\s+/g, '');

    // Add space between each pair of hex characters
    let formatted = '';
    for (let i = 0; i < hex.length; i += 2) {
        if (i > 0) formatted += ' ';
        formatted += hex.substr(i, 2);
    }

    // Split into lines of max 16 hex chars (8 bytes = 8*2 + 7 spaces = 23 chars)
    const maxCharsPerLine = 23; // "XX XX XX XX XX XX XX XX"
    const lines = [];
    let currentLine = '';

    const pairs = formatted.split(' ');
    for (let i = 0; i < pairs.length; i++) {
        if (lines.length >= 8) {
            // Truncate after 8 lines
            break;
        }

        const pair = pairs[i];
        const testLine = currentLine ? currentLine + ' ' + pair : pair;

        if (testLine.length <= maxCharsPerLine) {
            currentLine = testLine;
        } else {
            if (currentLine) lines.push(currentLine);
            currentLine = pair;
        }
    }

    if (currentLine && lines.length < 8) {
        lines.push(currentLine);
    }

    let result = lines.join('<br>');

    // Add truncation indicator if we cut off content
    if (pairs.length > lines.join(' ').split(' ').length) {
        result += '<br><span class="debug-truncated">... (truncated)</span>';
    }

    return result;
}

/**
 * Truncate text in description to max 40 characters
 * @param {string} desc - Description text
 * @returns {string} Truncated description
 */
function truncateTextInDescription(desc) {
    if (!desc) return '';

    // Match text parameters like: Text="very long text here"
    // Truncate text content to max 40 characters
    return desc.replace(/Text="([^"]{40})[^"]*"/g, (match, captured) => {
        const fullText = match.substring(6, match.length - 1); // Remove Text=" and "
        if (fullText.length > 40) {
            return `Text="${captured}..."`;
        }
        return match;
    });
}

/**
 * Extract plain text from ViewDocument elements
 * @param {Array} elements - Array of elements
 * @returns {string} Extracted plain text
 */
export function extractViewDocumentText(elements) {
    return (elements || [])
        .filter(el => el.type === 'text')
        .map(el => el.text || '')
        .join('\n');
}

/**
 * Map RenderedDocumentDto to internal document object
 * @param {Object} dto - RenderedDocumentDto from API
 * @param {Object} printer - Printer object (for default width)
 * @returns {Object} Internal document object
 */
export function mapViewDocumentDto(dto, printer) {
    const canvases = dto.canvases || [];
    const protocol = (dto.protocol || 'escpos').toLowerCase();
    const errorMessages = dto.errorMessages || null;
    const debugMode = callbacks.getDebugMode?.() || false;
    const docId = dto.id || `doc-${Date.now()}`;

    // Build canvas previews - one entry per canvas
    const canvasPreviews = canvases.map((canvas, index) => {
        const width = Number(canvas.widthInDots) || printer?.width || 384;
        const height = canvas.heightInDots ?? null;
        const elements = canvas.items || [];
        const canvasId = `${docId}-canvas-${index}`;

        const previewHtml = renderViewDocument(elements, width, height, canvasId, errorMessages, debugMode);
        const plainText = extractViewDocumentText(elements);

        return {
            index,
            width,
            heightInDots: height,
            elements,
            previewHtml,
            plainText
        };
    });

    // Use first canvas for backward compatibility (width, elements)
    const firstCanvas = canvasPreviews[0] || {};
    const width = firstCanvas.width || printer?.width || 384;

    return {
        id: dto.id,
        printerId: dto.printerId,
        timestamp: dto.timestamp ? new Date(dto.timestamp) : new Date(),
        errorMessages: errorMessages,
        protocol,
        width,
        widthInDots: width,
        heightInDots: firstCanvas.heightInDots || null,
        bytesReceived: dto.bytesReceived ?? 0,
        bytesSent: dto.bytesSent ?? 0,
        elements: firstCanvas.elements || [], // Store raw elements for re-rendering
        debugEnabled: false,
        canvases: canvasPreviews // Multiple canvases
    };
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
    renderDocumentsList,
    renderViewDocument,
    extractViewDocumentText,
    mapViewDocumentDto
};
