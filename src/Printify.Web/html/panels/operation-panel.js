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
export async function loadPanel(printerId, accessToken) {
    // 1. Fetch full data
    const data = await fetchPrinterData(printerId, accessToken);

    // 2. Create fresh panel structure (DocumentFragment is consumed when appended, so always recreate)
    const panel = createPanelStructure();
    currentPanel = panel;
    currentPrinterId = printerId;

    // 3. Apply all data (full update)
    applyData(panel.elements, data, printerId);

    // 4. Return element for DOM attachment
    return panel.element;
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
}

// ============================================================================
// PANEL CREATION
// ============================================================================

/**
 * Create the panel structure using DOM API
 * Returns object with element and direct references to all interactive elements
 */
function createPanelStructure() {
    const panel = document.createDocumentFragment();
    // Using DocumentFragment so appendChild(container) moves all children directly
    // without creating a wrapper div

    // Header
    const header = panel.appendChild(document.createElement('div'));
    header.className = 'operations-header';

    const titleRow = header.appendChild(document.createElement('div'));
    titleRow.className = 'operations-title-row';

    const printerName = titleRow.appendChild(document.createElement('span'));
    printerName.className = 'ops-printer-name';

    const separator = titleRow.appendChild(document.createElement('span'));
    separator.className = 'ops-separator';
    separator.textContent = '·';

    const protocol = titleRow.appendChild(document.createElement('span'));
    protocol.className = 'ops-protocol';

    const closeBtn = header.appendChild(document.createElement('button'));
    closeBtn.className = 'icon-btn';
    closeBtn.onclick = () => callbacks.onClose?.();
    closeBtn.innerHTML = '<img src="assets/icons/x.svg" width="18" height="18" alt="Close">';

    // Info section
    const info = panel.appendChild(document.createElement('div'));
    info.className = 'operations-info';

    // Info row 1: status + address
    const infoRow1 = info.appendChild(document.createElement('div'));
    infoRow1.className = 'info-row';

    const statusBadge = infoRow1.appendChild(document.createElement('span'));
    statusBadge.className = 'ops-status-badge';

    const row1Separator = infoRow1.appendChild(document.createElement('span'));
    row1Separator.className = 'info-separator';
    row1Separator.textContent = '·';

    const address = infoRow1.appendChild(document.createElement('span'));
    address.className = 'ops-address';

    const copyBtn = infoRow1.appendChild(document.createElement('button'));
    copyBtn.className = 'copy-icon-btn';
    copyBtn.onclick = () => callbacks.onCopyAddress?.(address.textContent);
    copyBtn.innerHTML = '<img src="assets/icons/copy.svg" width="14" height="14" alt="Copy">';

    // Info row 2: last document
    const infoRow2 = info.appendChild(document.createElement('div'));
    infoRow2.className = 'info-row';

    const lastDocLabel = infoRow2.appendChild(document.createElement('span'));
    lastDocLabel.className = 'info-label';
    lastDocLabel.textContent = 'LAST DOCUMENT:';

    const lastDoc = infoRow2.appendChild(document.createElement('span'));
    lastDoc.className = 'ops-last-document';

    // Actions section
    const actions = panel.appendChild(document.createElement('div'));
    actions.className = 'operations-actions';

    // Button row
    const buttonRow = actions.appendChild(document.createElement('div'));
    buttonRow.className = 'operations-button-row';

    const startStopBtn = buttonRow.appendChild(document.createElement('button'));
    startStopBtn.className = 'btn btn-primary btn-sm';
    startStopBtn.onclick = () => callbacks.onStartStop?.();

    const editBtn = buttonRow.appendChild(document.createElement('button'));
    editBtn.className = 'btn btn-secondary btn-sm';
    editBtn.onclick = () => callbacks.onEdit?.();
    editBtn.innerHTML = '<img class="themed-icon" src="assets/icons/edit-3.svg" width="14" height="14" alt="">Edit';

    const pinBtn = buttonRow.appendChild(document.createElement('button'));
    pinBtn.className = 'btn btn-secondary btn-sm';
    pinBtn.onclick = () => callbacks.onTogglePin?.();
    const pinText = pinBtn.appendChild(document.createElement('span'));
    pinText.className = 'ops-pin-text';
    const pinIcon = document.createElement('img');
    pinIcon.className = 'themed-icon';
    pinIcon.src = 'assets/icons/star.svg';
    pinIcon.width = 14;
    pinIcon.height = 14;
    pinBtn.prepend(pinIcon);

    // Flags grid
    const flagsGrid = actions.appendChild(document.createElement('div'));
    flagsGrid.className = 'flags-grid';

    const flagCoverOpen = createFlagSwitch(flagsGrid, 'isCoverOpen', 'Cover Open', 'flag-error');
    const flagPaperOut = createFlagSwitch(flagsGrid, 'isPaperOut', 'Paper Out', 'flag-error');
    const flagOffline = createFlagSwitch(flagsGrid, 'isOffline', 'Offline', 'flag-error');
    const flagError = createFlagSwitch(flagsGrid, 'hasError', 'Error', 'flag-error');

    // Debug mode switch
    const debugSwitch = actions.appendChild(document.createElement('label'));
    debugSwitch.className = 'flag-switch debug-switch';
    const debugCheckbox = debugSwitch.appendChild(document.createElement('input'));
    debugCheckbox.type = 'checkbox';
    debugCheckbox.className = 'ops-debug-mode';
    debugCheckbox.onchange = () => callbacks.onToggleDebug?.(debugCheckbox.checked);
    const debugLabel = debugSwitch.appendChild(document.createElement('span'));
    debugLabel.className = 'flag-label';
    debugLabel.textContent = 'Raw Data';

    // Section divider
    const divider1 = actions.appendChild(document.createElement('div'));
    divider1.className = 'section-divider';

    // Drawer 1
    const drawer1 = createDrawerControl(actions, 1);

    // Drawer 2
    const drawer2 = createDrawerControl(actions, 2);

    // Buffer status
    const bufferSection = actions.appendChild(document.createElement('div'));
    bufferSection.className = 'buffer-status-ascii';

    const bufferHeader = bufferSection.appendChild(document.createElement('div'));
    bufferHeader.className = 'buffer-header-ascii';

    const bufferLabel = bufferHeader.appendChild(document.createElement('span'));
    bufferLabel.textContent = 'Buffer Usage (bytes)';

    const bufferValue = bufferHeader.appendChild(document.createElement('span'));
    bufferValue.className = 'ops-buffer-value buffer-header-value';

    const bufferBar = bufferSection.appendChild(document.createElement('div'));
    bufferBar.className = 'ops-buffer-bar buffer-bar-ascii';

    // Danger zone
    const dangerZone = panel.appendChild(document.createElement('div'));
    dangerZone.className = 'danger-zone';

    const dangerHeader = dangerZone.appendChild(document.createElement('div'));
    dangerHeader.className = 'danger-zone-header';
    dangerHeader.addEventListener('click', toggleDangerZone);

    const dangerTitle = dangerHeader.appendChild(document.createElement('div'));
    dangerTitle.className = 'danger-zone-title';
    dangerTitle.innerHTML = '<img class="danger-zone-icon" src="assets/icons/alert-triangle.svg" width="16" height="16" alt="">Danger Zone';

    const dangerChevron = dangerHeader.appendChild(document.createElement('img'));
    dangerChevron.className = 'danger-zone-chevron collapsed';
    dangerChevron.src = 'assets/icons/chevron-down.svg';
    dangerChevron.width = 16;
    dangerChevron.height = 16;

    const dangerContent = dangerZone.appendChild(document.createElement('div'));
    dangerContent.className = 'danger-zone-content collapsed';

    const clearDocsBtn = dangerContent.appendChild(document.createElement('button'));
    clearDocsBtn.className = 'btn btn-sm';
    clearDocsBtn.onclick = () => callbacks.onClearDocuments?.();
    clearDocsBtn.innerHTML = '<img class="themed-icon" src="assets/icons/trash-2.svg" width="14" height="14" alt="">Delete all documents';

    const deletePrinterBtn = dangerContent.appendChild(document.createElement('button'));
    deletePrinterBtn.className = 'btn btn-sm';
    deletePrinterBtn.onclick = () => callbacks.onDeletePrinter?.();
    deletePrinterBtn.innerHTML = '<img class="themed-icon" src="assets/icons/trash.svg" width="14" height="14" alt="">Delete Printer';

    return {
        element: panel,
        elements: {
            // Header
            printerName,
            protocol,
            closeBtn,

            // Info
            statusBadge,
            address,
            copyBtn,
            lastDoc,

            // Buttons
            startStopBtn,
            editBtn,
            pinBtn,
            pinText,

            // Flags
            flagCoverOpen,
            flagPaperOut,
            flagOffline,
            flagError,
            debugCheckbox,

            // Drawers
            drawer1State: drawer1.state,
            drawer1Btn: drawer1.button,
            drawer2State: drawer2.state,
            drawer2Btn: drawer2.button,

            // Buffer
            bufferSection,
            bufferValue,
            bufferBar,

            // Danger zone
            dangerZone,
            dangerHeader,
            dangerContent,
            dangerChevron
        }
    };
}

function createFlagSwitch(container, flagName, labelText, extraClass) {
    const label = container.appendChild(document.createElement('label'));
    label.className = `flag-switch ${extraClass || ''}`;

    const checkbox = label.appendChild(document.createElement('input'));
    checkbox.type = 'checkbox';
    checkbox.dataset.flag = flagName;
    checkbox.onchange = () => callbacks.onToggleFlag?.(flagName, checkbox.checked);

    const span = label.appendChild(document.createElement('span'));
    span.className = 'flag-label';
    span.textContent = labelText;

    return checkbox;
}

function createDrawerControl(container, drawerNumber) {
    const div = container.appendChild(document.createElement('div'));
    div.className = 'drawer-control';
    div.dataset.drawer = drawerNumber;

    const info = div.appendChild(document.createElement('div'));
    info.className = 'drawer-info';

    const label = info.appendChild(document.createElement('span'));
    label.className = 'drawer-label';
    label.textContent = `Drawer ${drawerNumber}:`;

    const state = info.appendChild(document.createElement('span'));
    state.className = 'drawer-state';

    const button = div.appendChild(document.createElement('button'));
    button.className = 'btn btn-secondary btn-sm';
    button.dataset.drawer = drawerNumber;
    button.onclick = () => callbacks.onToggleDrawer?.(drawerNumber);

    return { state, button };
}

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
            elements.startStopBtn.innerHTML = isRunning
                ? '<img class="themed-icon" src="assets/icons/square.svg" width="14" height="14" alt="">Stop'
                : '<img class="themed-icon" src="assets/icons/play.svg" width="14" height="14" alt="">Start';
        }

        if (rt.bufferedBytes !== undefined) {
            const bufferBytes = rt.bufferedBytes ?? 0;
            const bufferMax = data.settings?.bufferMaxCapacity || 0;
            elements.bufferValue.textContent = `${bufferBytes}/${bufferMax}`;
            elements.bufferBar.innerHTML = renderBufferProgress(bufferBytes, bufferMax);

            // Show/hide buffer section - check if emulateBufferCapacity is greater than 0
            if (data.settings?.emulateBuffer && data.settings.emulateBuffer > 0) {
                elements.bufferSection.style.display = '';
            } else {
                elements.bufferSection.style.display = 'none';
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
    const host = 'localhost';
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

function renderBufferProgress(bufferedBytes, maxSize) {
    if (!maxSize || maxSize <= 0) {
        return '';
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

    // Build graphical progress bar
    const fillStyle = percentage > 0 ? `width: ${percentage}%; background-color: ${fillColor};` : 'width: 0;';

    return `<div class="buffer-progress-bar">
        <div class="buffer-progress-fill" style="${fillStyle}"></div>
    </div>`;
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
    clearPanel
};

