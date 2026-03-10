/**
 * Workspace Settings Dialog Module
 *
 * Manages the workspace settings dialog:
 * - Shows dialog with tabbed interface (General, Retention Policy, Usage & Statistics, Danger Zone)
 * - Loads and displays workspace settings
 * - Handles settings updates
 * - Handles workspace deletion
 */

import { escapeHtml } from '../../../assets/js/utils/html-utils.js';
import { formatDateTime, formatDateTimeWithRelative, formatRelativeTime } from '../../../assets/js/utils/datetime-format.js';
import { V } from '../../../assets/js/utils/app-version.js';

// ============================================================================
// STATE
// ============================================================================

let template = null;
let currentOverlay = null;
let currentSettings = null;
let hasChanges = false;

// Callbacks for actions (set by main.js)
const callbacks = {
    apiRequest: null,
    closeModal: null,
    showToast: null,
    onWorkspaceUpdated: null,
    onWorkspaceDeleted: null,
    workspaceName: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the workspace settings dialog module with action callbacks
 */
function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

/**
 * Show the workspace settings dialog
 */
async function show() {
    // Close any existing dialog
    close();

    // Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    // Clone the template
    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-workspace-settings-overlay]');

    // Setup event listeners
    const closeBtn = modalOverlay.querySelector('[data-workspace-settings-close]');
    const cancelBtn = modalOverlay.querySelector('[data-workspace-settings-cancel]');
    const saveBtn = modalOverlay.querySelector('[data-workspace-settings-save]');
    const deleteBtn = modalOverlay.querySelector('[data-delete-workspace-btn]');

    closeBtn.addEventListener('click', close);
    cancelBtn.addEventListener('click', close);
    saveBtn.addEventListener('click', handleSave);
    deleteBtn.addEventListener('click', handleDelete);

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

    // Setup tab switching
    setupTabs(modalOverlay);

    // Setup input change detection
    setupChangeDetection(modalOverlay);

    // Append to DOM first
    document.getElementById('modalContainer').appendChild(modalOverlay);

    // Store references for later use
    currentOverlay = modalOverlay;
    currentOverlay.nameInput = modalOverlay.querySelector('[data-workspace-name-input]');
    currentOverlay.retentionDaysInput = modalOverlay.querySelector('[data-retention-days-input]');
    currentOverlay.retentionDaysError = modalOverlay.querySelector('[data-retention-days-error]');
    currentOverlay.createdAt = modalOverlay.querySelector('[data-workspace-created-at]');
    currentOverlay.saveBtn = saveBtn;
    currentOverlay.whitelistEnabled = modalOverlay.querySelector('[data-whitelist-enabled]');
    currentOverlay.whitelistEntries = modalOverlay.querySelector('[data-whitelist-entries]');
    currentOverlay.whitelistEntriesField = modalOverlay.querySelector('[data-whitelist-entries-field]');
    currentOverlay.whitelistConnections = modalOverlay.querySelector('[data-whitelist-connections]');
    currentOverlay.whitelistRefresh = modalOverlay.querySelector('[data-whitelist-refresh]');
    currentOverlay.connectionsMinutes = modalOverlay.querySelector('[data-connections-minutes]');

    // Whitelist event listeners
    currentOverlay.whitelistEnabled.addEventListener('change', () => {
        updateWhitelistEntriesVisibility();
        markChanged();
    });
    currentOverlay.whitelistEntries.addEventListener('input', markChanged);
    currentOverlay.whitelistRefresh.addEventListener('click', loadRecentConnections);
    currentOverlay.connectionsMinutes?.addEventListener('change', loadRecentConnections);

    // Auto-load connections when connections tab is opened
    modalOverlay.querySelectorAll('.workspace-settings-nav-item').forEach(item => {
        item.addEventListener('click', () => {
            if (item.dataset.tab === 'connections') {
                loadRecentConnections();
            }
        });
    });

    // Load settings
    await loadSettings();

    // Focus first input
    setTimeout(() => {
        currentOverlay.nameInput?.focus();
    }, 100);
}

/**
 * Close the workspace settings dialog
 */
function close() {
    if (currentOverlay) {
        if (currentOverlay.escapeHandler) {
            document.removeEventListener('keydown', currentOverlay.escapeHandler);
        }
        currentOverlay.remove();
        currentOverlay = null;
        currentSettings = null;
        hasChanges = false;
    }
}

// ============================================================================
// INTERNAL FUNCTIONS
// ============================================================================

function setupTabs(modalOverlay) {
    const navItems = modalOverlay.querySelectorAll('.workspace-settings-nav-item');
    const contents = modalOverlay.querySelectorAll('.workspace-settings-tab-content');

    navItems.forEach(item => {
        item.addEventListener('click', () => {
            const tabName = item.dataset.tab;

            // Update active nav item
            navItems.forEach(nav => nav.classList.remove('active'));
            item.classList.add('active');

            // Update active content
            contents.forEach(content => {
                content.classList.remove('active');
                if (content.dataset.content === tabName) {
                    content.classList.add('active');
                }
            });
        });
    });
}

function setupChangeDetection(modalOverlay) {
    const nameInput = modalOverlay.querySelector('[data-workspace-name-input]');
    const retentionInput = modalOverlay.querySelector('[data-retention-days-input]');

    const checkChanges = () => {
        if (!currentSettings) return;

        const nameChanged = nameInput.value !== currentSettings.name;
        const retentionChanged = parseInt(retentionInput.value) !== currentSettings.documentRetentionDays;
        const whitelistEnabledChanged = currentOverlay.whitelistEnabled
            ? currentOverlay.whitelistEnabled.checked !== currentSettings.tcpWhitelistEnabled
            : false;
        const whitelistEntriesChanged = currentOverlay.whitelistEntries
            ? currentOverlay.whitelistEntries.value !== currentSettings.tcpWhitelistEntries
            : false;

        hasChanges = nameChanged || retentionChanged || whitelistEnabledChanged || whitelistEntriesChanged;
        currentOverlay.saveBtn.disabled = !hasChanges;
    };

    nameInput.addEventListener('input', checkChanges);
    retentionInput.addEventListener('input', () => {
        // Clear error on input
        currentOverlay.retentionDaysError.classList.remove('show');
        currentOverlay.retentionDaysInput.classList.remove('invalid');
        checkChanges();
    });
}

function markChanged() {
    if (!currentSettings) return;
    const nameInput = currentOverlay.nameInput;
    const retentionInput = currentOverlay.retentionDaysInput;
    const nameChanged = nameInput.value !== currentSettings.name;
    const retentionChanged = parseInt(retentionInput.value) !== currentSettings.documentRetentionDays;
    const whitelistEnabledChanged = currentOverlay.whitelistEnabled.checked !== currentSettings.tcpWhitelistEnabled;
    const whitelistEntriesChanged = currentOverlay.whitelistEntries.value !== currentSettings.tcpWhitelistEntries;
    hasChanges = nameChanged || retentionChanged || whitelistEnabledChanged || whitelistEntriesChanged;
    currentOverlay.saveBtn.disabled = !hasChanges;
}

function updateWhitelistEntriesVisibility() {
    if (!currentOverlay.whitelistEntriesField) return;
    if (currentOverlay.whitelistEnabled.checked) {
        currentOverlay.whitelistEntriesField.classList.remove('hidden');
    } else {
        currentOverlay.whitelistEntriesField.classList.add('hidden');
    }
}

async function loadRecentConnections() {
    if (!currentOverlay.whitelistConnections) return;
    try {
        const minutes = currentOverlay.connectionsMinutes?.value ?? '60';
        const entries = await callbacks.apiRequest(`/api/workspaces/connections?minutes=${minutes}`);
        renderConnections(entries);
    } catch (err) {
        currentOverlay.whitelistConnections.innerHTML = '<div class="whitelist-connections-empty">Failed to load connections</div>';
    }
}

function renderConnections(entries) {
    const container = currentOverlay.whitelistConnections;
    if (!entries || entries.length === 0) {
        container.innerHTML = '<div class="whitelist-connections-empty">No recent connections</div>';
        return;
    }

    const whitelistEnabled = currentOverlay.whitelistEnabled?.checked;
    const currentEntries = currentOverlay.whitelistEntries?.value ?? '';

    container.innerHTML = '';
    for (const e of entries) {
        const isTcp = (e.connectionType ?? 'Tcp').toLowerCase() === 'tcp';
        const isWeb = !isTcp;

        const time = formatRelativeTime(new Date(e.connectedAt));
        const typeClass = isWeb ? 'web' : 'tcp';
        const typeLabel = isWeb ? 'Web' : 'TCP';

        // Status: web connections are always "allowed" (JWT-guarded, can't be blocked by whitelist)
        const statusClass = isWeb ? 'allowed' : (e.allowed ? 'allowed' : 'blocked');
        const statusLabel = isWeb ? 'Allowed' : (e.allowed ? 'Allowed' : 'Blocked');

        // Show "Add to whitelist" for TCP connections whose IP isn't already listed
        const canAdd = isTcp && whitelistEnabled && !isIpInWhitelist(e.clientIp, currentEntries);
        const addBtn = canAdd
            ? `<button class="whitelist-add-btn" data-add-ip="${escapeHtml(e.clientIp)}">+ Add</button>`
            : '';

        const row = document.createElement('div');
        row.className = 'whitelist-connection-row';
        row.innerHTML = `<span class="whitelist-connection-ip">${escapeHtml(e.clientIp)}</span>
            <span class="whitelist-connection-time">${time}</span>
            <span class="whitelist-connection-type ${typeClass}">${typeLabel}</span>
            <span class="whitelist-connection-status ${statusClass}">${statusLabel}</span>
            ${addBtn}`;

        if (canAdd) {
            row.querySelector('[data-add-ip]').addEventListener('click', () => addIpToWhitelist(e.clientIp));
        }

        container.appendChild(row);
    }
}

function isIpInWhitelist(ip, whitelistText) {
    // Strip port from "1.2.3.4:port" or "[::1]:port"
    let bare = ip;
    const ipv6PortMatch = bare.match(/^\[(.+)\]:\d+$/);
    if (ipv6PortMatch) {
        bare = ipv6PortMatch[1];
    } else {
        const parts = bare.split(':');
        if (parts.length === 2) bare = parts[0];
    }
    return whitelistText.split(/[\n,]/).map(s => s.trim()).some(entry => entry === bare || entry === ip);
}

function addIpToWhitelist(ip) {
    // Strip port — same logic as isIpInWhitelist
    let bare = ip;
    const ipv6PortMatch = bare.match(/^\[(.+)\]:\d+$/);
    if (ipv6PortMatch) {
        bare = ipv6PortMatch[1];
    } else {
        const parts = bare.split(':');
        if (parts.length === 2) bare = parts[0];
    }

    const textarea = currentOverlay.whitelistEntries;
    const existing = textarea.value.trimEnd();
    textarea.value = existing ? `${existing}\n${bare}` : bare;
    markChanged();
    // Re-render to update "Add" buttons
    loadRecentConnections();
}

async function loadSettings() {
    try {
        // Fetch workspace settings and summary in parallel
        const [workspace, summary] = await Promise.all([
            callbacks.apiRequest('/api/workspaces'),
            callbacks.apiRequest('/api/workspaces/summary')
        ]);

        currentSettings = {
            name: workspace.name,
            createdAt: workspace.createdAt,
            documentRetentionDays: workspace.documentRetentionDays,
            tcpWhitelistEnabled: workspace.tcpWhitelistEnabled,
            tcpWhitelistEntries: workspace.tcpWhitelistEntries ?? ''
        };

        // Populate form
        currentOverlay.nameInput.value = currentSettings.name;
        currentOverlay.retentionDaysInput.value = currentSettings.documentRetentionDays;
        currentOverlay.whitelistEnabled.checked = currentSettings.tcpWhitelistEnabled;
        currentOverlay.whitelistEntries.value = currentSettings.tcpWhitelistEntries;
        updateWhitelistEntriesVisibility();

        // Format created at date
        const createdAt = new Date(currentSettings.createdAt);
        currentOverlay.createdAt.textContent = formatDateTime(createdAt);

        // Populate usage stats
        const totalPrintersEl = currentOverlay.querySelector('[data-stat-total-printers]');
        const totalDocumentsEl = currentOverlay.querySelector('[data-stat-total-documents]');
        const documents24hEl = currentOverlay.querySelector('[data-stat-documents-24h]');
        const lastDocumentEl = currentOverlay.querySelector('[data-stat-last-document]');

        if (totalPrintersEl) totalPrintersEl.textContent = summary.totalPrinters || 0;
        if (totalDocumentsEl) totalDocumentsEl.textContent = summary.totalDocuments || 0;
        if (documents24hEl) documents24hEl.textContent = summary.documentsLast24h || 0;
        if (lastDocumentEl) {
            lastDocumentEl.innerHTML = summary.lastDocumentAt
                ? formatDateTimeWithRelative(new Date(summary.lastDocumentAt))
                : 'Never';
        }

        // Reset save button
        hasChanges = false;
        currentOverlay.saveBtn.disabled = true;
    } catch (err) {
        console.error('Failed to load workspace settings:', err);
        if (callbacks.showToast) {
            callbacks.showToast('Failed to load workspace settings', true);
        }
        close();
    }
}

async function handleSave() {
    if (!hasChanges || !currentSettings) return;

    const name = currentOverlay.nameInput.value.trim();
    const retentionDays = parseInt(currentOverlay.retentionDaysInput.value);

    // Validate
    if (!name) {
        currentOverlay.nameInput.classList.add('invalid');
        currentOverlay.nameInput.focus();
        if (callbacks.showToast) {
            callbacks.showToast('Workspace name is required', true);
        }
        return;
    }

    if (isNaN(retentionDays) || retentionDays < 1 || retentionDays > 365) {
        currentOverlay.retentionDaysInput.classList.add('invalid');
        currentOverlay.retentionDaysError.classList.add('show');
        currentOverlay.retentionDaysInput.focus();
        if (callbacks.showToast) {
            callbacks.showToast('Document retention days must be between 1 and 365', true);
        }
        return;
    }

    try {
        const request = {};
        if (name !== currentSettings.name) {
            request.name = name;
        }
        if (retentionDays !== currentSettings.documentRetentionDays) {
            request.documentRetentionDays = retentionDays;
        }
        const whitelistEnabled = currentOverlay.whitelistEnabled.checked;
        const whitelistEntries = currentOverlay.whitelistEntries.value;
        if (whitelistEnabled !== currentSettings.tcpWhitelistEnabled) {
            request.tcpWhitelistEnabled = whitelistEnabled;
        }
        if (whitelistEntries !== currentSettings.tcpWhitelistEntries) {
            request.tcpWhitelistEntries = whitelistEntries;
        }

        const updated = await callbacks.apiRequest('/api/workspaces', {
            method: 'PATCH',
            body: JSON.stringify(request)
        });

        // Update local state
        currentSettings = {
            name: updated.name,
            createdAt: updated.createdAt,
            documentRetentionDays: updated.documentRetentionDays,
            tcpWhitelistEnabled: updated.tcpWhitelistEnabled,
            tcpWhitelistEntries: updated.tcpWhitelistEntries ?? ''
        };

        // Notify callback
        if (callbacks.onWorkspaceUpdated) {
            callbacks.onWorkspaceUpdated(currentSettings);
        }

        if (callbacks.showToast) {
            callbacks.showToast('Workspace settings saved');
        }

        close();
    } catch (err) {
        console.error('Failed to save workspace settings:', err);
        if (callbacks.showToast) {
            callbacks.showToast(err.message || 'Failed to save workspace settings', true);
        }
    }
}

async function handleDelete() {
    const workspaceName = currentSettings?.name || 'this workspace';
    const message = `Are you sure you want to delete "<strong>${escapeHtml(workspaceName)}</strong>"?<br><br>` +
        `This will permanently delete all printers and documents in this workspace.<br><br>` +
        `This action cannot be undone.`;

    if (window.ConfirmDialog) {
        ConfirmDialog.show(
            'Delete Workspace',
            message,
            'Delete Workspace',
            async () => {
                try {
                    await callbacks.apiRequest('/api/workspaces', {
                        method: 'DELETE'
                    });

                    if (callbacks.onWorkspaceDeleted) {
                        callbacks.onWorkspaceDeleted();
                    }

                    if (callbacks.showToast) {
                        callbacks.showToast('Workspace deleted');
                    }

                    close();
                } catch (err) {
                    console.error('Failed to delete workspace:', err);
                    if (callbacks.showToast) {
                        callbacks.showToast(err.message || 'Failed to delete workspace', true);
                    }
                }
            },
            true
        );
    }
}

async function loadTemplate() {
    const response = await fetch('features/workspace/workspace-settings-dialog/workspace-settings-dialog.html' + V);
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.WorkspaceSettingsDialog = {
    init,
    show,
    close
};
