/**
 * Workspace Menu Module
 *
 * Manages the workspace menu and dropdown:
 * - Updates workspace display (avatar, name, status)
 * - Shows dropdown menu with options
 * - Handles create/access/exit workspace
 */

import { escapeHtml } from '../../assets/js/utils/html-utils.js';

// ============================================================================
// STATE
// ============================================================================

// Callbacks for actions (set by main.js)
const callbacks = {
    onLogOut: null,
    onShowWorkspaceDialog: null,
    onShowWorkspaceSettings: null,
    onOpenDocs: null,
    apiRequest: null,
    closeModal: null,
    copyToClipboard: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the workspace menu module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

/**
 * Update the workspace display (avatar, name, status)
 */
export function updateDisplay(workspaceToken, workspaceName) {
    console.log('[WorkspaceMenu] updateDisplay - workspaceToken:', workspaceToken, 'workspaceName:', workspaceName);
    const avatar = document.getElementById('workspaceAvatar');
    const nameEl = document.getElementById('workspaceName');
    const statusEl = document.getElementById('workspaceStatus');

    if (!avatar || !nameEl || !statusEl) return;

    if (workspaceToken) {
        if (workspaceName) {
            avatar.textContent = getInitials(workspaceName);
            nameEl.textContent = workspaceName;
        } else {
            avatar.textContent = workspaceToken.substring(0, 2).toUpperCase();
            nameEl.textContent = workspaceToken;
        }
        statusEl.textContent = 'Workspace active';
    } else {
        avatar.textContent = '?';
        nameEl.textContent = 'No workspace';
        statusEl.textContent = '';
    }
    console.log('[WorkspaceMenu] updateDisplay - nameEl.textContent:', nameEl.textContent);
}

/**
 * Show the workspace dropdown menu
 */
export function showMenu(event) {
    event.stopPropagation();

    // If menu is already open, close it and return
    const existingMenu = document.querySelector('.menu');
    if (existingMenu) {
        closeMenu(existingMenu);
        return;
    }

    const menu = document.createElement('div');
    menu.className = 'menu';
    menu.style.position = 'fixed';
    menu.style.left = event.currentTarget.getBoundingClientRect().left + 'px';
    menu.style.bottom = (window.innerHeight - event.currentTarget.getBoundingClientRect().top) + 'px';
    menu.style.minWidth = '200px';

    const hasToken = !!callbacks.workspaceToken?.();

    menu.innerHTML = buildMenuHtml(hasToken);
    document.body.appendChild(menu);

    // Auto-close on click outside menu
    const closeMenu = () => menu.remove();
    setTimeout(() => {
        document.addEventListener('click', closeMenu);
    }, 0);
    menu.closeMenuHandler = closeMenu;

    // Setup event listeners for menu items
    setupMenuListeners(menu);
}

// ============================================================================
// MENU HTML GENERATION
// ============================================================================

function buildMenuHtml(hasToken) {
    if (hasToken) {
        return `
      <div class="menu-item" data-action="open-docs" data-doc="about">
        <img class="themed-icon menu-item-icon" src="assets/icons/info.svg" alt="">
        About Virtual Printer
      </div>
      <div class="menu-help">
        <div class="menu-item menu-item-submenu-toggle" data-action="toggle-help">
          <span class="menu-item-text">
            <img class="themed-icon menu-item-icon" src="assets/icons/book-open.svg" alt="">
            Help
          </span>
          <img class="themed-icon menu-item-chevron" src="assets/icons/chevron-right.svg" width="14" height="14" alt="">
        </div>
        <div class="menu-submenu">
          <div class="menu-item" data-action="open-docs" data-doc="guide">
            <img class="themed-icon menu-item-icon" src="assets/icons/book.svg" alt="">
            Getting Started
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="faq">
            <img class="themed-icon menu-item-icon" src="assets/icons/help-circle.svg" alt="">
            FAQ & Troubleshooting
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="security">
            <img class="themed-icon menu-item-icon" src="assets/icons/shield.svg" alt="">
            Security Guidelines
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="terms">
            <img class="themed-icon menu-item-icon" src="assets/icons/file-text.svg" alt="">
            Terms of Service
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="privacy">
            <img class="themed-icon menu-item-icon" src="assets/icons/lock.svg" alt="">
            Privacy Policy
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="licenses">
            <img class="themed-icon menu-item-icon" src="assets/icons/file-minus.svg" alt="">
            Third-Party Licenses
          </div>
        </div>
      </div>
      <div class="menu-divider"></div>
      <div class="menu-item" data-action="workspace-settings">
        <img class="themed-icon menu-item-icon" src="assets/icons/settings.svg" alt="">
        Workspace Settings
      </div>
      <div class="menu-item" data-action="show-workspace-dialog" data-mode="create">
        <img class="themed-icon menu-item-icon" src="assets/icons/plus-circle.svg" alt="">
        New Workspace
      </div>
      <div class="menu-item" data-action="show-workspace-dialog" data-mode="access">
        <img class="themed-icon menu-item-icon" src="assets/icons/refresh.svg" alt="">
        Switch Workspace
      </div>
      <div class="menu-item" data-action="logout">
        <img class="themed-icon menu-item-icon" src="assets/icons/log-out.svg" alt="">
        Exit Workspace
      </div>
    `;
    } else {
        return `
      <div class="menu-item" data-action="open-docs" data-doc="about">
        <img class="themed-icon menu-item-icon" src="assets/icons/info.svg" alt="">
        About Virtual Printer
      </div>
      <div class="menu-help">
        <div class="menu-item menu-item-submenu-toggle" data-action="toggle-help">
          <span class="menu-item-text">
            <img class="themed-icon menu-item-icon" src="assets/icons/book-open.svg" alt="">
            Help
          </span>
          <img class="themed-icon menu-item-chevron" src="assets/icons/chevron-right.svg" width="14" height="14" alt="">
        </div>
        <div class="menu-submenu">
          <div class="menu-item" data-action="open-docs" data-doc="guide">
            <img class="themed-icon menu-item-icon" src="assets/icons/book.svg" alt="">
            Getting Started
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="faq">
            <img class="themed-icon menu-item-icon" src="assets/icons/help-circle.svg" alt="">
            FAQ & Troubleshooting
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="security">
            <img class="themed-icon menu-item-icon" src="assets/icons/shield.svg" alt="">
            Security Guidelines
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="terms">
            <img class="themed-icon menu-item-icon" src="assets/icons/file-text.svg" alt="">
            Terms of Service
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="privacy">
            <img class="themed-icon menu-item-icon" src="assets/icons/lock.svg" alt="">
            Privacy Policy
          </div>
          <div class="menu-item" data-action="open-docs" data-doc="licenses">
            <img class="themed-icon menu-item-icon" src="assets/icons/file-minus.svg" alt="">
            Third-Party Licenses
          </div>
        </div>
      </div>
      <div class="menu-divider"></div>
      <div class="menu-item" data-action="show-workspace-dialog" data-mode="create">
        <img class="themed-icon menu-item-icon" src="assets/icons/plus-circle.svg" alt="">
        Create Workspace
      </div>
      <div class="menu-item" data-action="show-workspace-dialog" data-mode="access">
        <img class="themed-icon menu-item-icon" src="assets/icons/log-in.svg" alt="">
        Access Workspace
      </div>
    `;
    }
}

// ============================================================================
// EVENT HANDLERS
// ============================================================================

function setupMenuListeners(menu) {
    menu.addEventListener('click', (e) => {
        const item = e.target.closest('.menu-item');
        if (!item) return;

        const action = item.dataset.action;
        if (!action) return;

        e.stopPropagation();

        switch (action) {
            case 'open-docs':
                const doc = item.dataset.doc;
                if (doc && callbacks.onOpenDocs) {
                    window.open(`/docs/${doc}`, '_blank');
                }
                break;

            case 'toggle-help':
                toggleHelpMenu(item);
                break;

            case 'show-workspace-dialog':
                // Close menu first
                closeMenu(menu);
                const mode = item.dataset.mode || 'create';
                if (callbacks.onShowWorkspaceDialog) {
                    callbacks.onShowWorkspaceDialog(mode);
                }
                break;

            case 'workspace-settings':
                // Close menu first
                closeMenu(menu);
                if (callbacks.onShowWorkspaceSettings) {
                    callbacks.onShowWorkspaceSettings();
                }
                break;

            case 'logout':
                // Close menu first
                closeMenu(menu);
                const workspaceName = callbacks.workspaceName?.();
                const message = workspaceName
                    ? `Are you sure you want to exit "<strong>${escapeHtml(workspaceName)}</strong>"?<br><br>To re-enter, you will need to enter your workspace token.`
                    : 'Are you sure you want to exit this workspace?<br><br>To re-enter, you will need to enter your workspace token.';

                if (window.ConfirmDialog) {
                    window.ConfirmDialog.show(
                        'Exit Workspace',
                        message,
                        'Exit Workspace',
                        () => {
                            if (callbacks.onLogOut) {
                                callbacks.onLogOut();
                            }
                        },
                        true
                    );
                } else {
                    // Fallback if ConfirmDialog not loaded
                    if (callbacks.onLogOut) {
                        callbacks.onLogOut();
                    }
                }
                break;
        }
    });
}

function toggleHelpMenu(menuItem) {
    const helpMenu = menuItem.closest('.menu-help');
    if (!helpMenu) return;

    const submenu = helpMenu.querySelector('.menu-submenu');
    const chevron = helpMenu.querySelector('.menu-item-chevron');
    if (!submenu || !chevron) return;

    const isOpen = submenu.classList.toggle('open');
    chevron.src = isOpen ? 'assets/icons/chevron-down.svg' : 'assets/icons/chevron-right.svg';
}

// ============================================================================
// HELPERS
// ============================================================================

/**
 * Close a menu and clean up its event listener
 */
function closeMenu(menu) {
    if (menu.closeMenuHandler) {
        document.removeEventListener('click', menu.closeMenuHandler);
    }
    menu.remove();
}

/**
 * Get initials from a name
 */
function getInitials(name) {
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return parts[0].substring(0, 2).toUpperCase();
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.WorkspaceMenu = {
    init,
    updateDisplay,
    showMenu
};
