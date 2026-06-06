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
    currentOverlay.retentionCleanupPanel = modalOverlay.querySelector('[data-retention-cleanup-panel]');
    currentOverlay.retentionCleanupSummary = modalOverlay.querySelector('[data-retention-cleanup-summary]');
    currentOverlay.retentionCleanupLimitInput = modalOverlay.querySelector('[data-retention-cleanup-limit-input]');
    currentOverlay.retentionCleanupLimitError = modalOverlay.querySelector('[data-retention-cleanup-limit-error]');
    currentOverlay.retentionCleanupRun = modalOverlay.querySelector('[data-retention-cleanup-run]');
    currentOverlay.createdAt = modalOverlay.querySelector('[data-workspace-created-at]');
    currentOverlay.saveBtn = saveBtn;
    currentOverlay.whitelistEnabled = modalOverlay.querySelector('[data-whitelist-enabled]');
    currentOverlay.whitelistEntries = modalOverlay.querySelector('[data-whitelist-entries]');
    currentOverlay.whitelistEntriesField = modalOverlay.querySelector('[data-whitelist-entries-field]');
    currentOverlay.whitelistConnections = modalOverlay.querySelector('[data-whitelist-connections]');
    currentOverlay.whitelistRefresh = modalOverlay.querySelector('[data-whitelist-refresh]');
    currentOverlay.connectionsMinutes = modalOverlay.querySelector('[data-connections-minutes]');
    currentOverlay.adminStatisticsTab = modalOverlay.querySelector('[data-admin-statistics-tab]');
    currentOverlay.adminStatisticsContent = modalOverlay.querySelector('[data-admin-statistics-content]');
    currentOverlay.adminWorkspaceRows = modalOverlay.querySelector('[data-admin-workspace-rows]');

    // Whitelist event listeners
    currentOverlay.whitelistEnabled.addEventListener('change', () => {
        updateWhitelistEntriesVisibility();
        markChanged();
    });
    currentOverlay.whitelistEntries.addEventListener('input', markChanged);
    currentOverlay.whitelistRefresh.addEventListener('click', loadRecentConnections);
    currentOverlay.connectionsMinutes?.addEventListener('change', loadRecentConnections);
    currentOverlay.retentionCleanupRun?.addEventListener('click', handleRunRetentionCleanup);
    currentOverlay.retentionCleanupLimitInput?.addEventListener('input', () => {
        currentOverlay.retentionCleanupLimitError?.classList.remove('show');
        currentOverlay.retentionCleanupLimitInput?.classList.remove('invalid');
    });

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

function setAdminStatisticsVisible(isVisible) {
    if (currentOverlay.adminStatisticsTab) {
        currentOverlay.adminStatisticsTab.hidden = !isVisible;
    }

    if (currentOverlay.adminStatisticsContent) {
        currentOverlay.adminStatisticsContent.hidden = !isVisible;
    }

    if (!isVisible && currentOverlay.adminStatisticsTab?.classList.contains('active')) {
        activateTab('general');
    }
}

function activateTab(tabName) {
    currentOverlay.querySelectorAll('.workspace-settings-nav-item').forEach(item => {
        item.classList.toggle('active', item.dataset.tab === tabName);
    });

    currentOverlay.querySelectorAll('.workspace-settings-tab-content').forEach(content => {
        content.classList.toggle('active', content.dataset.content === tabName);
    });
}

async function loadAdminStatistics() {
    try {
        const statistics = await callbacks.apiRequest('/api/workspaces/admin-statistics');
        renderAdminStatistics(statistics);
    } catch (err) {
        console.error('Failed to load admin statistics:', err);
        if (currentOverlay.adminWorkspaceRows) {
            currentOverlay.adminWorkspaceRows.innerHTML =
                '<tr><td colspan="9" class="admin-workspaces-empty">Failed to load admin statistics</td></tr>';
        }
    }
}

function renderAdminStatistics(statistics) {
    setText('[data-admin-total-workspaces]', formatNumber(statistics.totalWorkspaces));
    setText('[data-admin-active-workspaces-24h]', formatNumber(statistics.activeWorkspacesLast24h));
    setText('[data-admin-active-workspaces-7d]', formatNumber(statistics.activeWorkspacesLast7d));
    setText('[data-admin-total-printers]', formatNumber(statistics.totalPrinters));
    setText('[data-admin-total-documents]', formatNumber(statistics.totalDocuments));
    setText('[data-admin-total-media]', formatNumber(statistics.totalMedia));
    setText('[data-admin-total-media-bytes]', formatBytes(statistics.totalMediaBytes));
    setText(
        '[data-admin-documents-window]',
        `${formatNumber(statistics.documentsLast24h)} / ${formatNumber(statistics.documentsLast7d)}`);
    setText(
        '[data-admin-media-window]',
        `${formatNumber(statistics.mediaLast24h)} / ${formatNumber(statistics.mediaLast7d)}`);

    const lastDocumentEl = currentOverlay.querySelector('[data-admin-last-document]');
    if (lastDocumentEl) {
        lastDocumentEl.innerHTML = statistics.lastDocumentAt
            ? formatDateTimeWithRelative(new Date(statistics.lastDocumentAt))
            : 'Never';
    }

    renderAdminWorkspaceRows(statistics.workspaces ?? []);
}

function renderAdminWorkspaceRows(rows) {
    if (!currentOverlay.adminWorkspaceRows) {
        return;
    }

    if (rows.length === 0) {
        currentOverlay.adminWorkspaceRows.innerHTML =
            '<tr><td colspan="9" class="admin-workspaces-empty">No workspace statistics</td></tr>';
        return;
    }

    currentOverlay.adminWorkspaceRows.innerHTML = rows
        .map(row => `<tr>
            <td><span class="admin-workspace-name" title="${escapeHtml(row.workspaceName)}">${escapeHtml(row.workspaceName)}</span></td>
            <td>${escapeHtml(row.role)}</td>
            <td class="numeric">${formatNumber(row.printerCount)}</td>
            <td class="numeric">${formatNumber(row.documentCount)}</td>
            <td class="numeric">${formatNumber(row.mediaCount)}</td>
            <td class="numeric">${formatBytes(row.mediaBytes)}</td>
            <td class="numeric">${formatNumber(row.documentsLast24h)}</td>
            <td>${formatRetention(row.documentRetentionDays)}</td>
            <td>${row.lastDocumentAt ? formatRelativeTime(new Date(row.lastDocumentAt)) : 'Never'}</td>
        </tr>`)
        .join('');
}

function setText(selector, value) {
    const element = currentOverlay.querySelector(selector);
    if (element) {
        element.textContent = value;
    }
}

function formatNumber(value) {
    return Number(value ?? 0).toLocaleString();
}

function formatBytes(value) {
    const bytes = Number(value ?? 0);
    if (bytes < 1024) {
        return `${bytes} B`;
    }

    const units = ['KB', 'MB', 'GB', 'TB'];
    let size = bytes / 1024;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
        size /= 1024;
        unitIndex++;
    }

    return `${size.toFixed(size >= 10 ? 1 : 2)} ${units[unitIndex]}`;
}

function formatRetention(days) {
    return Number(days) === 0 ? 'Forever' : `${formatNumber(days)}d`;
}

async function loadRetentionCleanupSummary() {
    if (!currentOverlay.retentionCleanupSummary) {
        return;
    }

    try {
        const summary = await callbacks.apiRequest('/api/workspaces/retention/cleanup-summary');
        renderRetentionCleanupSummary(summary);
    } catch (err) {
        currentOverlay.retentionCleanupSummary.textContent = 'Failed to load retention summary';
    }
}

function renderRetentionCleanupSummary(summary) {
    const documents = formatNumber(summary?.expiredDocuments ?? 0);
    const mediaFiles = formatNumber(summary?.retentionMediaFiles ?? 0);
    currentOverlay.retentionCleanupSummary.textContent = `${documents} documents retention ${mediaFiles} media files`;
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
            role: workspace.role,
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

        const isAdmin = currentSettings.role === 'Admin';
        setAdminStatisticsVisible(isAdmin);

        if (currentOverlay.retentionCleanupPanel) {
            currentOverlay.retentionCleanupPanel.hidden = !isAdmin;
        }

        if (isAdmin) {
            await loadRetentionCleanupSummary();
            await loadAdminStatistics();
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

async function handleRunRetentionCleanup() {
    const maxDocuments = parseInt(currentOverlay.retentionCleanupLimitInput?.value, 10);
    if (isNaN(maxDocuments) || maxDocuments <= 0) {
        currentOverlay.retentionCleanupLimitInput?.classList.add('invalid');
        currentOverlay.retentionCleanupLimitError?.classList.add('show');
        currentOverlay.retentionCleanupLimitInput?.focus();
        callbacks.showToast?.('Documents to delete must be greater than 0', true);
        return;
    }

    try {
        currentOverlay.retentionCleanupRun.disabled = true;
        const result = await callbacks.apiRequest('/api/workspaces/retention/cleanup', {
            method: 'POST',
            body: JSON.stringify({ maxDocuments })
        });

        callbacks.showToast?.(
            `Retention cleanup deleted ${formatNumber(result.deletedDocuments)} documents and ` +
            `${formatNumber(result.deletedMedia)} media files`);

        await loadRetentionCleanupSummary();
        await loadAdminStatistics();
    } catch (err) {
        console.error('Failed to run retention cleanup:', err);
        callbacks.showToast?.(err.message || 'Failed to run retention cleanup', true);
    } finally {
        if (currentOverlay?.retentionCleanupRun) {
            currentOverlay.retentionCleanupRun.disabled = false;
        }
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

    if (isNaN(retentionDays) || retentionDays < 0 || retentionDays > 365) {
        currentOverlay.retentionDaysInput.classList.add('invalid');
        currentOverlay.retentionDaysError.classList.add('show');
        currentOverlay.retentionDaysInput.focus();
        if (callbacks.showToast) {
            callbacks.showToast('Document retention days must be between 0 and 365', true);
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
            role: updated.role,
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
