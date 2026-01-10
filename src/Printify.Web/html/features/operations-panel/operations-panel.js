/**
 * Operations Panel Module
 *
 * Manages the operations panel for printer controls:
 * - Renders panel off-DOM to avoid flicker
 * - Stores direct element references for fast updates
 * - Applies data from GET responses or SSE partial updates
 */

// ============================================================================
// STATE
// ============================================================================

let currentPanel = null;
let currentPrinterId = null;
let dangerZoneExpanded = false;
let template = null;
let emptyTemplate = null;
let templateDocument = null;
let elements = {};
let cachedBufferMaxCapacity = 0;

// Buffer animation state
let bufferAnimation = {
    lastKnownBytes: 0,
    bytesPerSecond: 0,
    lastUpdateTime: 0,
    animationFrame: null,
    isAnimating: false,
    lastAnimationTime: 0,
    lastDisplayedBytes: null  // Track the last interpolated value we showed
};

const ANIMATION_INTERVAL_MS = 100; // 10 fps

// Callbacks for actions (set by main.js)
const callbacks = {
    onClose: null,
    onTogglePin: null,
    onEdit: null,
    onStartStop: null,
    onToggleFlag: null,
    onToggleDebug: null,
    onToggleDrawer: null,
    onToggleDangerZone: null,
    onClearDocuments: null,
    onDeletePrinter: null,
    onCopyAddress: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the operations panel module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

/**
 * Get the current panel element (for DOM attachment)
 */
export function getPanelElement() {
    return currentPanel?.element || null;
}

// ============================================================================
// TEMPLATE LOADING
// ============================================================================

/**
 * Load template once and cache it
 */
async function loadTemplate() {
    if (template) return template;

    const doc = await loadTemplateDocument();
    template = doc.querySelector('#operations-panel-template');
    return template;
}

/**
 * Load empty-state template once and cache it
 */
async function loadEmptyTemplate() {
    if (emptyTemplate) return emptyTemplate;

    const doc = await loadTemplateDocument();
    emptyTemplate = doc.querySelector('#operations-panel-empty-template');
    return emptyTemplate;
}

/**
 * Load the template document once and cache it
 */
async function loadTemplateDocument() {
    if (templateDocument) return templateDocument;

    const response = await fetch('features/operations-panel/operations-panel.html');
    const html = await response.text();
    const parser = new DOMParser();
    templateDocument = parser.parseFromString(html, 'text/html');
    return templateDocument;
}

// ============================================================================
// ELEMENT CACHING
// ============================================================================

/**
 * Cache element references from template using data-* attributes
 * Returns object with direct references to all interactive elements
 */
function cacheElementReferences(panelElement) {
    return {
        // Header elements
        printerName: panelElement.querySelector('[data-ops-printer-name]'),
        protocol: panelElement.querySelector('[data-ops-protocol]'),
        closeBtn: panelElement.querySelector('[data-action="close"]'),

        // Info elements
        statusBadge: panelElement.querySelector('[data-ops-status-badge]'),
        address: panelElement.querySelector('[data-ops-address]'),
        copyBtn: panelElement.querySelector('[data-action="copy-address"]'),
        lastDoc: panelElement.querySelector('[data-ops-last-document]'),

        // Button elements
        startStopBtn: panelElement.querySelector('[data-action="start-stop"]'),
        startStopText: panelElement.querySelector('[data-ops-start-stop-text]'),
        editBtn: panelElement.querySelector('[data-action="edit"]'),
        pinBtn: panelElement.querySelector('[data-action="toggle-pin"]'),
        pinText: panelElement.querySelector('[data-ops-pin-text]'),

        // Flag checkboxes
        flagCoverOpen: panelElement.querySelector('[data-flag="isCoverOpen"]'),
        flagPaperOut: panelElement.querySelector('[data-flag="isPaperOut"]'),
        flagOffline: panelElement.querySelector('[data-flag="isOffline"]'),
        flagError: panelElement.querySelector('[data-flag="hasError"]'),

        // Debug mode
        debugCheckbox: panelElement.querySelector('[data-action="toggle-debug"]'),

        // Drawers
        drawer1State: panelElement.querySelector('[data-drawer-state="1"]'),
        drawer1Btn: panelElement.querySelector('[data-action="toggle-drawer"][data-drawer="1"]'),
        drawer2State: panelElement.querySelector('[data-drawer-state="2"]'),
        drawer2Btn: panelElement.querySelector('[data-action="toggle-drawer"][data-drawer="2"]'),

        // Buffer
        bufferSection: panelElement.querySelector('[data-section="buffer"]'),
        bufferValue: panelElement.querySelector('[data-ops-buffer-value]'),
        bufferBar: panelElement.querySelector('[data-ops-buffer-bar]'),
        bufferFill: panelElement.querySelector('[data-ops-buffer-fill]'),

        // Danger zone
        dangerZone: panelElement.querySelector('[data-danger-zone]'),
        dangerHeader: panelElement.querySelector('[data-action="toggle-danger"]'),
        dangerContent: panelElement.querySelector('[data-danger-content]'),
        dangerChevron: panelElement.querySelector('[data-action="toggle-danger"] .danger-zone-chevron')
    };
}

// ============================================================================
// EVENT HANDLERS
// ============================================================================

/**
 * Attach event handlers to cached elements
 */
function attachEventHandlers(elements) {
    // Header actions
    elements.closeBtn.addEventListener('click', () => callbacks.onClose?.());

    // Info actions
    elements.copyBtn.addEventListener('click', () =>
        callbacks.onCopyAddress?.(elements.address.textContent));

    // Button actions
    elements.startStopBtn.addEventListener('click', () => callbacks.onStartStop?.());
    elements.editBtn.addEventListener('click', () => callbacks.onEdit?.());
    elements.pinBtn.addEventListener('click', () => callbacks.onTogglePin?.());

    // Flag toggles
    elements.flagCoverOpen.addEventListener('change', (e) =>
        callbacks.onToggleFlag?.('isCoverOpen', e.target.checked));
    elements.flagPaperOut.addEventListener('change', (e) =>
        callbacks.onToggleFlag?.('isPaperOut', e.target.checked));
    elements.flagOffline.addEventListener('change', (e) =>
        callbacks.onToggleFlag?.('isOffline', e.target.checked));
    elements.flagError.addEventListener('change', (e) =>
        callbacks.onToggleFlag?.('hasError', e.target.checked));

    // Debug toggle
    elements.debugCheckbox.addEventListener('change', (e) =>
        callbacks.onToggleDebug?.(e.target.checked));

    // Drawer toggles
    elements.drawer1Btn.addEventListener('click', () => callbacks.onToggleDrawer?.(1));
    elements.drawer2Btn.addEventListener('click', () => callbacks.onToggleDrawer?.(2));

    // Danger zone
    elements.dangerHeader.addEventListener('click', toggleDangerZone);

    // Clear documents
    const clearDocsBtn = elements.dangerContent.querySelector('[data-action="clear-documents"]');
    clearDocsBtn.addEventListener('click', () => callbacks.onClearDocuments?.());

    // Delete printer
    const deletePrinterBtn = elements.dangerContent.querySelector('[data-action="delete-printer"]');
    deletePrinterBtn.addEventListener('click', () => callbacks.onDeletePrinter?.());
}

/**
 * Check if panel is ready for the given printer
 */
export function isPanelReady(printerId) {
    return currentPanel && currentPrinterId === printerId;
}

/**
 * Load and render operations panel for a printer
 * 1. Fetch full status via GET
 * 2. Create fresh panel structure (DocumentFragment is consumed on append)
 * 3. Apply full data
 * 4. Return panel element for atomic attachment
 *
 * @param {string} printerId - The printer ID
 * @param {string} accessToken - Access token for API calls
 * @returns {Promise<Element>} The panel element to attach to DOM
 */
export async function loadPanel(printerId, accessToken, targetContainer) {
    // 1. Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    // 2. Fetch full data
    const data = await fetchPrinterData(printerId, accessToken);

    // 3. Get target container and clear it
    const panelElement = targetContainer || document.getElementById('operationsPanel');
    panelElement.innerHTML = '';

    // 4. Clone template and append children to container
    const fragment = template.content.cloneNode(true);
    panelElement.appendChild(fragment);

    // 5. Cache element references from container
    const cachedElements = cacheElementReferences(panelElement);

    // 6. Attach event handlers
    attachEventHandlers(cachedElements);

    // 7. Store panel reference
    currentPanel = { element: panelElement, elements: cachedElements };
    currentPrinterId = printerId;

    // 8. Apply all data (full update)
    applyData(cachedElements, data, printerId);

    // 9. Return element (already in DOM)
    return panelElement;
}

/**
 * Apply partial update from SSE
 * Only updates fields that are present in the data
 *
 * @param {Object} data - Partial data from SSE
 * @param {string} printerId - The printer ID
 */
export function applyPartialUpdate(data, printerId) {
    if (!currentPanel || currentPrinterId !== printerId) {
        return; // Not our printer
    }

    applyData(currentPanel.elements, data, printerId);
}

/**
 * Toggle danger zone expanded state
 */
export function toggleDangerZone() {
    dangerZoneExpanded = !dangerZoneExpanded;

    if (currentPanel) {
        const { dangerContent, dangerZone, dangerChevron } = currentPanel.elements;

        if (dangerZoneExpanded) {
            dangerContent.classList.remove('collapsed');
            dangerContent.classList.add('expanded');
            dangerZone.classList.add('expanded');
            dangerChevron.classList.remove('collapsed');
            dangerChevron.classList.add('expanded');
        } else {
            dangerContent.classList.remove('expanded');
            dangerContent.classList.add('collapsed');
            dangerZone.classList.remove('expanded');
            dangerChevron.classList.remove('expanded');
            dangerChevron.classList.add('collapsed');
        }
    }

    // Persist to localStorage
    localStorage.setItem('dangerZoneExpanded', dangerZoneExpanded);
}

/**
 * Restore danger zone state from localStorage
 */
export function restoreDangerZoneState() {
    const saved = localStorage.getItem('dangerZoneExpanded');
    if (saved === 'true' && !dangerZoneExpanded) {
        toggleDangerZone();
    }
}

/**
 * Clear the current panel
 */
export function clearPanel() {
    currentPanel = null;
    currentPrinterId = null;
    dangerZoneExpanded = false;
    cachedBufferMaxCapacity = 0;
    cancelBufferAnimation();
}

/**
 * Render empty state panel (no printer selected, no printers, no workspace)
 */
export async function renderEmptyState(options, targetContainer) {
    const panelElement = targetContainer || document.getElementById('operationsPanel');
    if (!panelElement) return null;

    await loadEmptyTemplate();
    panelElement.innerHTML = '';

    const fragment = emptyTemplate.content.cloneNode(true);
    panelElement.appendChild(fragment);

    const closeBtn = panelElement.querySelector('[data-action="close"]');
    if (closeBtn) {
        closeBtn.addEventListener('click', () => callbacks.onClose?.());
    }

    const titleEl = panelElement.querySelector('[data-ops-empty-title]');
    const bodyEl = panelElement.querySelector('[data-ops-empty-body]');
    const titleText = options?.title || '';
    const bodyText = options?.body || '';

    if (titleEl) {
        titleEl.textContent = titleText;
    }

    if (bodyEl) {
        bodyEl.textContent = bodyText;
        bodyEl.classList.toggle('is-hidden', !bodyText);
    }

    currentPanel = { element: panelElement, elements: { closeBtn, emptyTitle: titleEl, emptyBody: bodyEl } };
    currentPrinterId = null;
    dangerZoneExpanded = false;
    cachedBufferMaxCapacity = 0;
    cancelBufferAnimation();

    return panelElement;
}

// ============================================================================
// PANEL CREATION
// ============================================================================

// ============================================================================
// DATA APPLICATION
// ============================================================================

/**
 * Apply data to panel elements
 * Handles both full updates (from GET) and partial updates (from SSE)
 */
function applyData(elements, data, printerId) {
    // Printer metadata
    if (data.printer) {
        if (data.printer.displayName) {
            elements.printerName.textContent = data.printer.displayName;
        }
        if (data.printer.protocol) {
            const protocol = data.printer.protocol.toLowerCase();
            elements.protocol.textContent = protocol === 'escpos' ? 'ESC/POS' : protocol.toUpperCase();
        }
        if (data.printer.isPinned !== undefined) {
            elements.pinText.textContent = data.printer.isPinned ? 'Unpin' : 'Pin';
        }
        if (data.printer.address) {
            elements.address.textContent = data.printer.address;
        }
        if (data.printer.lastDocumentAt !== undefined) {
            elements.lastDoc.textContent = data.printer.lastDocumentAt
                ? new Date(data.printer.lastDocumentAt).toLocaleString(undefined, {
                    year: 'numeric', month: '2-digit', day: '2-digit',
                    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
                  })
                : 'Never';
        }
    }

    // Runtime status
    if (data.runtimeStatus) {
        const rt = data.runtimeStatus;

        if (rt.state) {
            const statusClass = getStatusClass(rt.state);
            const statusText = formatStatus(rt.state);
            elements.statusBadge.className = `status-pill ${statusClass}`;
            elements.statusBadge.textContent = statusText;

            // Update start/stop button
            const isRunning = rt.state === 'started' || rt.state === 'starting';
            elements.startStopText.textContent = isRunning ? 'Stop' : 'Start';
        }

        if (rt.bufferedBytes !== undefined) {
            const bufferBytes = rt.bufferedBytes ?? 0;
            // Use bufferMaxCapacity from data if available, otherwise use cached value
            const bufferMax = data.settings?.bufferMaxCapacity ?? cachedBufferMaxCapacity ?? 0;
            const bytesPerSecond = rt.bufferedBytesDeltaBps ?? 0;

            // Start smooth animation with rate-of-change
            startBufferAnimation(bufferBytes, bytesPerSecond);

            // Only modify buffer section visibility during full updates (when settings are present)
            // For partial SSE updates, preserve current visibility state
            if (data.settings?.emulateBuffer !== undefined) {
                if (data.settings.emulateBuffer > 0) {
                    elements.bufferSection.style.display = '';
                } else {
                    elements.bufferSection.style.display = 'none';
                }
            }
        }

        if (rt.drawer1State !== undefined) {
            elements.drawer1State.textContent = rt.drawer1State === 'Closed' ? 'Closed' : 'Open';
            elements.drawer1Btn.textContent = rt.drawer1State === 'Closed' ? 'Open' : 'Close';
        }

        if (rt.drawer2State !== undefined) {
            elements.drawer2State.textContent = rt.drawer2State === 'Closed' ? 'Closed' : 'Open';
            elements.drawer2Btn.textContent = rt.drawer2State === 'Closed' ? 'Open' : 'Close';
        }
    }

    // Operational flags
    if (data.operationalFlags) {
        const flags = data.operationalFlags;

        if (flags.isCoverOpen !== undefined) {
            elements.flagCoverOpen.checked = flags.isCoverOpen;
        }
        if (flags.isPaperOut !== undefined) {
            elements.flagPaperOut.checked = flags.isPaperOut;
        }
        if (flags.isOffline !== undefined) {
            elements.flagOffline.checked = flags.isOffline;
        }
        if (flags.hasError !== undefined) {
            elements.flagError.checked = flags.hasError;
        }
        if (flags.isPaperNearEnd !== undefined) {
            // Handled separately if needed
        }
    }

    // Settings
    if (data.settings) {
        if (data.settings.debugMode !== undefined) {
            elements.debugCheckbox.checked = data.settings.debugMode;
        }
        // Cache bufferMaxCapacity for use in partial updates
        if (data.settings.bufferMaxCapacity !== undefined) {
            cachedBufferMaxCapacity = data.settings.bufferMaxCapacity || 0;
        }
    }
}

// ============================================================================
// HELPERS
// ============================================================================

/**
 * Fetch printer data from API
 */
async function fetchPrinterData(printerId, accessToken) {
    const response = await fetch(`/api/printers/${printerId}`, {
        headers: {
            'Authorization': `Bearer ${accessToken}`,
            'Content-Type': 'application/json'
        }
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch printer data: ${response.statusText}`);
    }

    const printer = await response.json();

    // Transform to internal format
    // The API returns PrinterResponseDto with this structure:
    // { printer: { displayName, isPinned, ... },
    //   settings: { protocol, tcpListenPort, ... },
    //   operationalFlags: { targetState, ... },
    //   runtimeStatus: { state, ... } }
    return {
        printer: {
            displayName: printer.printer?.displayName,
            protocol: printer.settings?.protocol,
            isPinned: printer.printer?.isPinned,
            address: formatPrinterAddress(printer),
            lastDocumentAt: printer.printer?.lastDocumentReceivedAt ? new Date(printer.printer.lastDocumentReceivedAt) : null
        },
        runtimeStatus: printer.runtimeStatus ? {
            state: printer.runtimeStatus.state?.toLowerCase(),
            bufferedBytes: printer.runtimeStatus.bufferedBytes,
            bufferedBytesDeltaBps: printer.runtimeStatus.bufferedBytesDeltaBps,
            drawer1State: printer.runtimeStatus.drawer1State,
            drawer2State: printer.runtimeStatus.drawer2State
        } : null,
        operationalFlags: {
            targetState: printer.operationalFlags?.targetState?.toLowerCase(),
            isCoverOpen: printer.operationalFlags?.isCoverOpen,
            isPaperOut: printer.operationalFlags?.isPaperOut,
            isOffline: printer.operationalFlags?.isOffline,
            hasError: printer.operationalFlags?.hasError,
            isPaperNearEnd: printer.operationalFlags?.isPaperNearEnd
        },
        settings: {
            bufferSize: printer.settings?.bufferMaxCapacity || 0,
            emulateBuffer: printer.settings?.emulateBufferCapacity,
            bufferMaxCapacity: printer.settings?.bufferMaxCapacity || 0,
            debugMode: false // Will be set from global state
        }
    };
}

function formatPrinterAddress(printer) {
    const host = printer.settings?.publicHost || 'localhost';
    const port = printer.settings?.tcpListenPort || 9100;
    return `${host}:${port}`;
}

function getStatusClass(state) {
    if (!state) return '';
    switch (state.toLowerCase()) {
        case 'started': return 'status-started';
        case 'starting': return 'status-starting';
        case 'stopped': return 'status-stopped';
        case 'stopping': return 'status-stopping';
        case 'error': return 'status-error';
        default: return '';
    }
}

function formatStatus(state) {
    if (!state) return 'Unknown';
    const s = state.toLowerCase();
    switch (s) {
        case 'started': return 'Listening';
        case 'starting': return 'Starting...';
        case 'stopped': return 'Stopped';
        case 'stopping': return 'Stopping...';
        case 'error': return 'Error';
        default: return s.charAt(0).toUpperCase() + s.slice(1);
    }
}

function updateBufferProgress(fillElement, bufferedBytes, maxSize) {
    if (!fillElement || !maxSize || maxSize <= 0) {
        if (fillElement) {
            fillElement.style.width = '0';
        }
        return;
    }

    const bytes = bufferedBytes ?? 0;
    const percentage = Math.min((bytes / maxSize) * 100, 100);

    // Determine fill color based on percentage
    let fillColor;
    if (percentage < 10) {
        fillColor = 'var(--accent)';
    } else if (percentage < 50) {
        fillColor = 'var(--warn)';
    } else {
        fillColor = 'var(--danger)';
    }

    // Update the fill element's style directly
    fillElement.style.width = percentage > 0 ? `${percentage}%` : '0';
    fillElement.style.backgroundColor = fillColor;
}

// ============================================================================
// BUFFER ANIMATION
// ============================================================================

const INTERPOLATION_DURATION_MS = 2000;  // Interpolate for 2 seconds (longer than SSE interval)
const TIMEOUT_DURATION_MS = 4000;        // Stop animation after 4 seconds without update

/**
 * Start buffer animation loop based on rate-of-change
 * @param {number} currentBytes - Current buffered bytes from SSE
 * @param {number} bytesPerSecond - Rate of change (can be negative for draining)
 */
function startBufferAnimation(currentBytes, bytesPerSecond) {
    const now = Date.now();

    // Store actual values from SSE
    bufferAnimation.lastKnownBytes = currentBytes;
    bufferAnimation.bytesPerSecond = bytesPerSecond || 0;
    bufferAnimation.lastUpdateTime = now;
    bufferAnimation.lastAnimationTime = now;
    bufferAnimation.lastDisplayedBytes = null;  // Reset, will be set during interpolation
    bufferAnimation.isAnimating = true;

    // Cancel any existing animation
    if (bufferAnimation.animationFrame) {
        cancelAnimationFrame(bufferAnimation.animationFrame);
    }

    // Start animation loop
    bufferAnimation.animationFrame = requestAnimationFrame(animateBuffer);
}

/**
 * Animation loop for smooth buffer interpolation
 * Runs at 5fps (200ms intervals) via requestAnimationFrame
 */
function animateBuffer() {
    const now = Date.now();
    const elapsedMs = now - bufferAnimation.lastUpdateTime;

    // Stop animation after timeout without update
    if (elapsedMs > TIMEOUT_DURATION_MS) {
        stopBufferAnimation();
        return;
    }

    // Only update UI at 5fps intervals (every 200ms)
    const timeSinceLastAnimation = now - bufferAnimation.lastAnimationTime;
    const shouldUpdateUI = timeSinceLastAnimation >= ANIMATION_INTERVAL_MS;

    // Only interpolate for the configured duration
    if (elapsedMs <= INTERPOLATION_DURATION_MS && bufferAnimation.bytesPerSecond !== 0) {
        const elapsedSeconds = elapsedMs / 1000;
        // Project current value based on rate
        const projectedBytes = bufferAnimation.lastKnownBytes + (bufferAnimation.bytesPerSecond * elapsedSeconds);

        // Clamp to valid bounds [0, maxCapacity]
        const clampedBytes = Math.max(0, Math.min(projectedBytes, cachedBufferMaxCapacity));

        // Stop animation if buffer is empty - nothing more to animate
        if (clampedBytes <= 0) {
            stopBufferAnimation();
            return;
        }

        // Update UI only at interval
        if (shouldUpdateUI) {
            bufferAnimation.lastAnimationTime = now;
            // Track the last value we displayed
            bufferAnimation.lastDisplayedBytes = clampedBytes;

            if (currentPanel?.elements) {
                currentPanel.elements.bufferValue.textContent = `${Math.round(clampedBytes)}/${cachedBufferMaxCapacity}`;
                updateBufferProgress(currentPanel.elements.bufferFill, clampedBytes, cachedBufferMaxCapacity);
            }
        }

        // Continue animation (still runs rAF loop for timing, but UI updates at 5fps)
        bufferAnimation.animationFrame = requestAnimationFrame(animateBuffer);
    } else {
        // After interpolation duration or rate is 0, show last displayed interpolated value (not lastKnownBytes!)
        // Use lastKnownBytes if we never interpolated (e.g., rate was 0 from start)
        const holdingValue = bufferAnimation.lastDisplayedBytes ?? bufferAnimation.lastKnownBytes;

        // Stop animation if buffer is empty - nothing more to animate
        if (holdingValue <= 0) {
            stopBufferAnimation();
            return;
        }

        if (shouldUpdateUI) {
            bufferAnimation.lastAnimationTime = now;
            if (currentPanel?.elements) {
                currentPanel.elements.bufferValue.textContent = `${Math.round(holdingValue)}/${cachedBufferMaxCapacity}`;
                updateBufferProgress(currentPanel.elements.bufferFill, holdingValue, cachedBufferMaxCapacity);
            }
        }
        // Keep checking for timeout
        bufferAnimation.animationFrame = requestAnimationFrame(animateBuffer);
    }
}

/**
 * Stop buffer animation and restore last known value
 */
function stopBufferAnimation() {
    if (bufferAnimation.animationFrame) {
        cancelAnimationFrame(bufferAnimation.animationFrame);
        bufferAnimation.animationFrame = null;
    }
    bufferAnimation.isAnimating = false;

    // Restore last known value
    if (currentPanel?.elements) {
        currentPanel.elements.bufferValue.textContent = `${bufferAnimation.lastKnownBytes}/${cachedBufferMaxCapacity}`;
        updateBufferProgress(currentPanel.elements.bufferFill, bufferAnimation.lastKnownBytes, cachedBufferMaxCapacity);
    }
}

/**
 * Cancel buffer animation (e.g., when switching printers)
 */
function cancelBufferAnimation() {
    if (bufferAnimation.animationFrame) {
        cancelAnimationFrame(bufferAnimation.animationFrame);
        bufferAnimation.animationFrame = null;
    }
    bufferAnimation.isAnimating = false;
    bufferAnimation.lastKnownBytes = 0;
    bufferAnimation.bytesPerSecond = 0;
    bufferAnimation.lastUpdateTime = 0;
    bufferAnimation.lastAnimationTime = 0;
    bufferAnimation.lastDisplayedBytes = null;
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.OperationsPanel = {
    init,
    getPanelElement,
    isPanelReady,
    loadPanel,
    applyPartialUpdate,
    toggleDangerZone,
    restoreDangerZoneState,
    clearPanel,
    renderEmptyState
};

