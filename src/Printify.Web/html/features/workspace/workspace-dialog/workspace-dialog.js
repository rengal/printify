/**
 * Workspace Dialog Module
 *
 * Manages the workspace create/access dialog:
 * - Shows dialog with mode toggle (create/access)
 * - Handles workspace creation
 * - Handles workspace access with token
 */

// ============================================================================
// STATE
// ============================================================================

let template = null;
let currentMode = 'create'; // 'create' or 'access'
let currentOverlay = null;

// Callbacks for actions (set by main.js)
const callbacks = {
    apiRequest: null,
    loginWithToken: null,
    closeModal: null,
    showTokenDialog: null,
    showToast: null,
    onWorkspaceCreated: null,
    onWorkspaceAccessed: null
};

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the workspace dialog module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
}

/**
 * Show the workspace dialog
 * @param {string} mode - 'create' or 'access'
 */
export async function show(mode = 'create') {
    // Ensure mode is either 'create' or 'access'
    if (mode !== 'create' && mode !== 'access') {
        mode = 'create';
    }
    // Close any existing dialog
    close();

    // Load template if not already loaded
    if (!template) {
        await loadTemplate();
    }

    currentMode = mode;

    // Clone the template
    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-workspace-dialog-overlay]');

    // Get elements
    const createModeBtn = modalOverlay.querySelector('[data-workspace-mode-create]');
    const accessModeBtn = modalOverlay.querySelector('[data-workspace-mode-access]');
    const createModeDiv = modalOverlay.querySelector('[data-workspace-create-mode]');
    const accessModeDiv = modalOverlay.querySelector('[data-workspace-access-mode]');
    const cancelBtn = modalOverlay.querySelector('[data-workspace-cancel]');
    const submitBtn = modalOverlay.querySelector('[data-workspace-submit]');

    // Setup event listeners
    createModeBtn.addEventListener('click', () => switchMode('create'));
    accessModeBtn.addEventListener('click', () => switchMode('access'));
    cancelBtn.addEventListener('click', close);
    submitBtn.addEventListener('click', handleSubmit);

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

    // Append to DOM first
    document.getElementById('modalContainer').appendChild(modalOverlay);

    // Store references for later use
    currentOverlay = modalOverlay;
    currentOverlay.createModeBtn = createModeBtn;
    currentOverlay.accessModeBtn = accessModeBtn;
    currentOverlay.createModeDiv = createModeDiv;
    currentOverlay.accessModeDiv = accessModeDiv;
    currentOverlay.submitBtn = submitBtn;
    currentOverlay.nameInput = modalOverlay.querySelector('[data-workspace-name-input]');
    currentOverlay.nameError = modalOverlay.querySelector('[data-workspace-name-error]');
    currentOverlay.tokenInput = modalOverlay.querySelector('[data-workspace-token-input]');
    currentOverlay.tokenError = modalOverlay.querySelector('[data-workspace-token-error]');

    // Clear validation errors on input
    currentOverlay.nameInput?.addEventListener('input', function() {
        this.classList.remove('invalid');
        currentOverlay.nameError?.classList.remove('show');
    });

    currentOverlay.tokenInput?.addEventListener('input', function() {
        this.classList.remove('invalid');
        currentOverlay.tokenError?.classList.remove('show');
    });

    // Set initial mode (must be after append to DOM)
    setMode(mode);

    // Focus appropriate input
    setTimeout(() => {
        const isAccessMode = mode === 'access';
        if (!isAccessMode) {
            currentOverlay.nameInput?.focus();
        } else {
            currentOverlay.tokenInput?.focus();
        }
    }, 100);
}

/**
 * Close the workspace dialog
 */
export function close() {
    if (currentOverlay) {
        if (currentOverlay.escapeHandler) {
            document.removeEventListener('keydown', currentOverlay.escapeHandler);
        }
        currentOverlay.remove();
        currentOverlay = null;
    }
}

// ============================================================================
// INTERNAL FUNCTIONS
// ============================================================================

function switchMode(mode) {
    if (!currentOverlay) return;
    setMode(mode);
}

function setMode(mode) {
    currentMode = mode;

    const { createModeBtn, accessModeBtn, createModeDiv, accessModeDiv, submitBtn } = currentOverlay;

    const isCreateMode = mode === 'create';
    const isAccessMode = mode === 'access';

    // Clear any validation errors when switching modes
    if (currentOverlay.nameInput) {
        currentOverlay.nameInput.classList.remove('invalid');
        currentOverlay.nameError?.classList.remove('show');
    }
    if (currentOverlay.tokenInput) {
        currentOverlay.tokenInput.classList.remove('invalid');
        currentOverlay.tokenError?.classList.remove('show');
    }

    // Update button states
    createModeBtn.classList.toggle('active', isCreateMode);
    accessModeBtn.classList.toggle('active', isAccessMode);

    // Show/hide mode sections
    createModeDiv.style.display = isCreateMode ? 'block' : 'none';
    accessModeDiv.style.display = isAccessMode ? 'block' : 'none';

    // Update submit button
    submitBtn.textContent = isCreateMode ? 'Create Workspace' : 'Access Workspace';

    // Focus appropriate input
    setTimeout(() => {
        if (isCreateMode) {
            currentOverlay.nameInput?.focus();
        } else {
            currentOverlay.tokenInput?.focus();
        }
    }, 50);
}

async function handleSubmit() {
    if (currentMode === 'create') {
        await handleCreate();
    } else if (currentMode === 'access') {
        await handleAccess();
    }
}

async function handleCreate() {
    const name = currentOverlay.nameInput.value.trim();
    const nameInput = currentOverlay.nameInput;
    const nameError = currentOverlay.nameError;

    // Clear previous errors
    nameInput.classList.remove('invalid');
    nameError.classList.remove('show');

    if (!name) {
        nameInput.classList.add('invalid');
        nameError.classList.add('show');
        nameInput.focus();
        return;
    }

    try {
        const request = {
            id: crypto.randomUUID(),
            ownerName: name
        };

        const workspace = await callbacks.apiRequest('/api/workspaces', {
            method: 'POST',
            body: JSON.stringify(request)
        });

        if (!workspace?.token) {
            throw new Error('Workspace token missing from response');
        }

        const workspaceToken = workspace.token;
        const workspaceName = workspace.ownerName;

        // Store in localStorage
        localStorage.setItem('workspaceToken', workspaceToken);
        localStorage.setItem('workspaceName', workspaceName);

        // Login with the token
        await callbacks.loginWithToken(workspaceToken);

        // Close dialog
        close();

        // Show token dialog
        if (callbacks.showTokenDialog) {
            callbacks.showTokenDialog(workspaceToken);
        }

        // Notify callback
        if (callbacks.onWorkspaceCreated) {
            callbacks.onWorkspaceCreated(workspaceToken, workspaceName);
        }
    } catch (err) {
        console.error(err);
        if (callbacks.showToast) {
            callbacks.showToast(err.message || 'Failed to create workspace', true);
        }
    }
}

async function handleAccess() {
    const token = currentOverlay.tokenInput.value.trim();
    const tokenInput = currentOverlay.tokenInput;
    const tokenError = currentOverlay.tokenError;

    tokenInput.classList.remove('invalid');
    tokenError.classList.remove('show');

    if (!token) {
        tokenInput.classList.add('invalid');
        tokenError.classList.add('show');
        tokenError.textContent = 'Please enter a workspace token';
        tokenInput.focus();
        return;
    }

    try {
        await callbacks.loginWithToken(token);

        localStorage.setItem('workspaceToken', token);

        // Close dialog
        close();

        // Notify callback
        if (callbacks.onWorkspaceAccessed) {
            callbacks.onWorkspaceAccessed(token);
        }

        if (callbacks.showToast) {
            callbacks.showToast('Workspace accessed successfully');
        }
    } catch (err) {
        console.error(err);
        tokenInput.classList.add('invalid');
        tokenError.classList.add('show');
        tokenError.textContent = 'Workspace not found or token is invalid';
    }
}

async function loadTemplate() {
    const response = await fetch('features/workspace/workspace-dialog/workspace-dialog.html');
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.WorkspaceDialog = {
    init,
    show,
    close
};
