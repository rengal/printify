
        // API + Workspace State
        const apiBase = '';
        let workspaceToken = null;
        let workspaceName = null;
        let accessToken = null;

        // Data
        let printers = [];
        let documents = {};
        let selectedPrinterId = null;
        let statusStreamController = null;

        function authHeaders() {
            return accessToken
                ? { 'Authorization': `Bearer ${accessToken}` }
                : {};
        }

        async function apiRequest(path, options = {}) {
            const headers = {
                'Content-Type': 'application/json',
                ...authHeaders(),
                ...(options.headers || {})
            };

            const response = await fetch(`${apiBase}${path}`, {
                ...options,
                headers
            });

            if (!response.ok) {
                const text = await response.text().catch(() => '');
                throw new Error(text || `Request failed: ${response.status}`);
            }

            if (response.status === 204) {
                return null;
            }

            return await response.json();
        }

        function normalizeProtocol(protocol) {
            if (!protocol) return 'escpos';
            const normalized = protocol.toLowerCase().replace(/[^a-z0-9]/g, '');
            if (normalized.includes('esc')) return 'escpos';
            return normalized;
        }

        function mapPrinterDto(dto, index) {
            const targetStatus = (dto.targetStatus || 'started').toLowerCase();
            const runtimeStatus = (dto.runtimeStatus || 'unknown').toLowerCase();
            return {
                id: dto.id,
                name: dto.displayName,
                protocol: dto.protocol,
                width: dto.widthInDots,
                height: dto.heightInDots,
                port: dto.tcpListenPort,
                emulateBuffer: dto.emulateBufferCapacity,
                bufferSize: dto.bufferMaxCapacity || 0,
                drainRate: dto.bufferDrainRate || 0,
                pinned: dto.isPinned,
                targetStatus,
                runtimeStatus,
                runtimeStatusAt: dto.runtimeStatusUpdatedAt ? new Date(dto.runtimeStatusUpdatedAt) : null,
                runtimeStatusError: dto.runtimeStatusError || null,
                lastDocumentAt: dto.lastDocumentReceivedAt ? new Date(dto.lastDocumentReceivedAt) : null,
                newDocs: 0,
                pinOrder: index
            };
        }

        async function loadPrinters(selectId = null) {
            try {
                const list = await apiRequest('/api/printers');
                printers = list.map((p, idx) => mapPrinterDto(p, idx));
                if (selectId && printers.some(p => p.id === selectId)) {
                    selectedPrinterId = selectId;
                } else if (!selectedPrinterId && printers.length > 0) {
                    selectedPrinterId = printers[0].id;
                }
                renderSidebar();
                renderDocuments();
            } catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to load printers', true);
            }
        }

        // Token Generation
        const ADJECTIVES = ['happy', 'bright', 'swift', 'calm', 'bold', 'wise', 'kind', 'brave', 'quick', 'cool', 'warm', 'fresh', 'smart', 'clear', 'neat'];
        const NOUNS = ['tiger', 'eagle', 'wolf', 'bear', 'lion', 'hawk', 'fox', 'deer', 'owl', 'seal', 'crow', 'swan', 'lynx', 'orca', 'puma'];

        function generateToken() {
            const adj = ADJECTIVES[Math.floor(Math.random() * ADJECTIVES.length)];
            const noun = NOUNS[Math.floor(Math.random() * NOUNS.length)];
            const num = Math.floor(1000 + Math.random() * 9000);
            return `${adj}-${noun}-${num}`;
        }

        function isValidToken(token) {
            return /^[a-z]+-[a-z]+-\d{4}$/.test(token);
        }

        // Utility Functions
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

        function formatDateTime(date) {
            if (!date) return '—';
            const day = String(date.getDate()).padStart(2, '0');
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const year = date.getFullYear();
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            return `${day}.${month}.${year} ${hours}:${minutes}`;
        }

        function formatPrinterAddress(printer) {
            if (printer.port) {
                return `localhost:${printer.port}`;
            }

            return 'Listener not configured';
        }

        function formatRuntimeStatus(status) {
            if (!status) return 'unknown';
            const normalized = status.toLowerCase();
            switch (normalized) {
                case 'starting':
                    return 'Starting…';
                case 'started':
                    return 'Listening';
                case 'stopped':
                    return 'Stopped';
                case 'error':
                    return 'Error';
                default:
                    return 'Unknown';
            }
        }

        function runtimeStatusClass(status) {
            if (!status) return 'status-pill status-unknown';
            const normalized = status.toLowerCase();
            if (normalized === 'started') return 'status-pill status-started';
            if (normalized === 'starting') return 'status-pill status-starting';
            if (normalized === 'stopped') return 'status-pill status-stopped';
            if (normalized === 'error') return 'status-pill status-error';
            return 'status-pill status-unknown';
        }

        function showToast(message, type = 'ok') {
            const toast = document.createElement('div');
            toast.className = `toast ${type}`;
            toast.textContent = message;
            document.getElementById('toastHost').appendChild(toast);
            setTimeout(() => toast.remove(), 3000);
        }

        function copyToClipboard(text) {
            navigator.clipboard.writeText(text).then(() => {
                showToast('Copied to clipboard!');
            }).catch(() => {
                showToast('Failed to copy', 'danger');
            });
        }

        // Render Functions
        function renderSidebar() {
            const pinnedList = document.getElementById('pinnedList');
            const otherList = document.getElementById('otherList');
            const pinnedSection = document.getElementById('pinnedSection');
            const otherSection = document.getElementById('otherSection');

            const pinnedPrinters = printers.filter(p => p.pinned).sort((a, b) => a.pinOrder - b.pinOrder);
            const otherPrinters = printers.filter(p => !p.pinned).sort((a, b) => a.name.localeCompare(b.name));

            pinnedSection.style.display = pinnedPrinters.length > 0 ? 'block' : 'none';
            otherSection.style.display = otherPrinters.length > 0 ? 'block' : 'none';

            pinnedList.innerHTML = pinnedPrinters.map(p => {
                const docInfo = p.lastDocumentAt
                    ? `Last: ${formatDateTime(p.lastDocumentAt)}`
                    : 'No documents';
                const isStarted = p.targetStatus === 'started';

                return `
                <div class="list-item ${selectedPrinterId === p.id ? 'active' : ''}" onclick="selectPrinter('${p.id}')">
                  <div class="list-item-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <line x1="12" y1="17" x2="12" y2="22"></line>
                      <path d="M5 17h14v-1.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V6h1a2 2 0 0 0 0-4H8a2 2 0 0 0 0 4h1v4.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24Z"></path>
                    </svg>
                  </div>
                  <div class="list-item-content">
                    <div class="list-item-line1">
                      <span class="list-item-name">${p.name}</span>
                      ${p.newDocs > 0 ? `<span class="list-item-badge">● ${p.newDocs}</span>` : ''}
                    </div>
                    <div class="list-item-line2">${formatPrinterAddress(p)}</div>
                    <div class="list-item-line2">
                      <span class="${runtimeStatusClass(p.runtimeStatus)}">${formatRuntimeStatus(p.runtimeStatus)}</span>
                      <span class="list-item-dot">•</span>
                      ${docInfo}
                    </div>
                  </div>
                  <div class="list-item-actions">
                    ${isStarted
                    ? `<button class="btn btn-secondary btn-xs" onclick="stopPrinter(event, '${p.id}')">Stop</button>`
                    : `<button class="btn btn-primary btn-xs" onclick="startPrinter(event, '${p.id}')">Start</button>`}
                  </div>
                  <button class="btn btn-ghost btn-sm list-item-menu" onclick="event.stopPropagation(); showMenu(event, '${p.id}', true)">⋯</button>
                </div>
              `;
            }).join('');

            otherList.innerHTML = otherPrinters.map(p => {
                const isStarted = p.targetStatus === 'started';
                return `
                <div class="list-item ${selectedPrinterId === p.id ? 'active' : ''}" onclick="selectPrinter('${p.id}')">
                  <div class="list-item-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="6 9 6 2 18 2 18 9"></polyline>
                      <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                      <rect x="6" y="14" width="12" height="8"></rect>
                    </svg>
                  </div>
                  <div class="list-item-content">
                    <div class="list-item-line1">
                      <span class="list-item-name">${p.name}</span>
                    </div>
                    <div class="list-item-line2">${formatPrinterAddress(p)}</div>
                    <div class="list-item-line2">
                      <span class="${runtimeStatusClass(p.runtimeStatus)}">${formatRuntimeStatus(p.runtimeStatus)}</span>
                    </div>
                  </div>
                  <div class="list-item-actions">
                    ${isStarted
                    ? `<button class="btn btn-secondary btn-xs" onclick="stopPrinter(event, '${p.id}')">Stop</button>`
                    : `<button class="btn btn-primary btn-xs" onclick="startPrinter(event, '${p.id}')">Start</button>`}
                  </div>
                  <button class="btn btn-ghost btn-sm list-item-menu" onclick="event.stopPropagation(); showMenu(event, '${p.id}', false)">⋯</button>
                </div>
              `;
            }).join('');
        }

        function selectPrinter(id) {
            selectedPrinterId = id;
            const printer = printers.find(p => p.id === id);
            if (printer) {
                printer.newDocs = 0;
                document.getElementById('topbarTitle').textContent = `${printer.name} · ${printer.protocol.toUpperCase()} · ${printer.width} dots`;
                renderDocuments();
                renderSidebar();
            }
        }

        function renderDocuments() {
            const mainContent = document.getElementById('mainContent');

            if (!workspaceToken) {
                mainContent.innerHTML = `
              <div style="max-width: 600px; margin: 60px auto; text-align: center;">
                <h1>Printer Management System</h1>
                <p style="color: var(--muted); font-size: 18px; margin: 24px 0 40px; line-height: 1.6;">
                  Manage receipt and label printers with real-time document streaming
                </p>

                <div style="text-align: left; background: var(--bg-elev); border: 1px solid var(--border); border-radius: 12px; padding: 32px; margin-bottom: 32px;">
                  <h3 style="margin-top: 0;">Features</h3>
                  <ul style="color: var(--muted); line-height: 1.8; padding-left: 24px;">
                    <li>Configure multiple printers with ESC/POS, ZPL, and other protocols</li>
                    <li>Monitor print jobs in real-time with document preview</li>
                    <li>Replay and download previously printed documents</li>
                    <li>Pin frequently used printers for quick access</li>
                    <li>Emulate buffer capacity for testing and optimization</li>
                    <li>Share printers across devices with your workspace token</li>
                  </ul>
                </div>

                <div style="background: rgba(16,185,129,0.1); border: 1px solid var(--accent); border-radius: 12px; padding: 24px; margin-bottom: 32px;">
                  <h3 style="margin-top: 0; color: var(--accent);">Get Started</h3>
                  <p style="color: var(--muted); margin-bottom: 16px;">
                    Create a new workspace or access an existing one with your token
                  </p>
                  <button class="btn btn-primary" onclick="showWorkspaceDialog()">Create or Access Workspace</button>
                </div>
              </div>
            `;
                return;
            }

            if (!selectedPrinterId) {
                mainContent.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h2>Welcome back${workspaceName ? ', ' + workspaceName : ''}!</h2>
                <p>Select a printer from the sidebar to view documents</p>
              </div>
            `;
                return;
            }

            const docs = documents[selectedPrinterId] || [];

            if (docs.length === 0) {
                mainContent.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h3>No documents yet</h3>
                <p>Documents will appear here when they are printed</p>
              </div>
            `;
                return;
            }

            mainContent.innerHTML = docs.map(doc => `
            <div class="document-item">
              <div class="document-preview">${doc.content}</div>
              <div class="document-meta">
                <span>${formatRelativeTime(doc.timestamp)}</span>
                <div class="document-actions">
                  <button class="btn btn-secondary btn-sm" onclick="downloadDocument(${doc.id})">Download</button>
                  <button class="btn btn-secondary btn-sm" onclick="replayDocument(${doc.id})">Replay</button>
                </div>
              </div>
            </div>
          `).join('');
        }

        function showMenu(event, printerId, isPinned) {
            event.stopPropagation();

            const existingMenu = document.querySelector('.menu');
            if (existingMenu) existingMenu.remove();

            const menu = document.createElement('div');
            menu.className = 'menu';
            menu.style.position = 'fixed';
            menu.style.left = event.clientX + 'px';
            menu.style.top = event.clientY + 'px';

            menu.innerHTML = `
            <div class="menu-item" onclick="editPrinter('${printerId}')">Edit</div>
            <div class="menu-item" onclick="togglePin('${printerId}')">${isPinned ? 'Unpin' : 'Pin'}</div>
          `;

            document.body.appendChild(menu);

            setTimeout(() => {
                document.addEventListener('click', function closeMenu() {
                    menu.remove();
                    document.removeEventListener('click', closeMenu);
                });
            }, 0);
        }

        async function togglePin(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            try {
                await apiRequest(`/api/printers/${printerId}/pin`, {
                    method: 'POST',
                    body: JSON.stringify({ isPinned: !printer.pinned })
                });
                await loadPrinters(printerId);
                showToast(!printer.pinned ? 'Printer pinned' : 'Printer unpinned');
            } catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to toggle pin', true);
            }
        }

        async function setPrinterStatus(printerId, targetStatus) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            try {
                await apiRequest(`/api/printers/${printerId}/status`, {
                    method: 'POST',
                    body: JSON.stringify({ targetStatus })
                });
                await loadPrinters(printerId);
                showToast(targetStatus.toLowerCase() === 'started' ? 'Printer started' : 'Printer stopped');
            } catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to change status', true);
            }
        }

        function startPrinter(event, printerId) {
            event?.stopPropagation();
            setPrinterStatus(printerId, 'Started');
        }

        function stopPrinter(event, printerId) {
            event?.stopPropagation();
            setPrinterStatus(printerId, 'Stopped');
        }

        function openNewPrinterDialog() {
            if (!workspaceToken) {
                showWorkspaceDialog();
                return;
            }

            const modal = document.createElement('div');
            modal.className = 'modal-overlay';
            modal.innerHTML = `
            <div class="modal">
              <div class="modal-header">New Printer</div>
              <div class="modal-body">
                <div class="field">
                  <label class="label required">Name</label>
                  <input class="input" id="printerName" placeholder="e.g., Kitchen Printer" />
                  <div class="field-error" id="nameError">Name is required</div>
                </div>

                <div class="field-group">
                    <div class="field">
                      <label class="label required">Protocol</label>
                      <select class="input" id="printerProtocol">
                        <option value="escpos">ESC/POS</option>
                      </select>
                    </div>

                  <div class="field">
                    <label class="label">Width (dots)</label>
                    <input class="input" id="printerWidth" type="number" value="576" />
                    <div class="field-hint">Paper width in dots</div>
                  </div>
                </div>

                <div class="checkbox-field">
                  <input type="checkbox" id="emulateBuffer" onchange="toggleBufferFields()" />
                  <label for="emulateBuffer">Emulate buffer capacity</label>
                </div>

                <div id="bufferFields" class="indent" style="display: none;">
                  <div class="field-group">
                    <div class="field">
                      <label class="label">Buffer size (bytes)</label>
                      <input class="input" id="bufferSize" type="number" value="4096" />
                      <div class="field-hint">Internal buffer size</div>
                    </div>

                    <div class="field">
                      <label class="label">Drain rate (bytes/s)</label>
                      <input class="input" id="drainRate" type="number" value="4096" />
                      <div class="field-hint">Processing speed</div>
                    </div>
                  </div>
                </div>

                <div class="form-actions">
                  <button class="btn btn-secondary" onclick="closeModal()">Cancel</button>
                  <button class="btn btn-primary" onclick="createPrinter()">Create</button>
                </div>
              </div>
            </div>
          `;
            document.getElementById('modalContainer').appendChild(modal);
        }

        function toggleBufferFields() {
            const checked = document.getElementById('emulateBuffer').checked;
            document.getElementById('bufferFields').style.display = checked ? 'block' : 'none';
        }

        function editPrinter(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            const modal = document.createElement('div');
            modal.className = 'modal-overlay';
            modal.innerHTML = `
            <div class="modal">
              <div class="modal-header">Edit Printer</div>
              <div class="modal-body">
                <div class="field">
                  <label class="label required">Name</label>
                  <input class="input" id="printerName" value="${printer.name}" />
                  <div class="field-error" id="nameError">Name is required</div>
                </div>

                <div class="field-group">
                    <div class="field">
                      <label class="label required">Protocol</label>
                      <select class="input" id="printerProtocol">
                      <option value="escpos" ${printer.protocol.toLowerCase() === 'escpos' ? 'selected' : ''}>ESC/POS</option>
                      </select>
                    </div>

                  <div class="field">
                    <label class="label">Width (dots)</label>
                    <input class="input" id="printerWidth" type="number" value="${printer.width}" />
                    <div class="field-hint">Paper width in dots</div>
                  </div>
                </div>

                <div class="checkbox-field">
                  <input type="checkbox" id="emulateBuffer" ${printer.emulateBuffer ? 'checked' : ''} onchange="toggleBufferFields()" />
                  <label for="emulateBuffer">Emulate buffer capacity</label>
                </div>

                <div id="bufferFields" class="indent" style="display: ${printer.emulateBuffer ? 'block' : 'none'};">
                  <div class="field-group">
                    <div class="field">
                      <label class="label">Buffer size (bytes)</label>
                      <input class="input" id="bufferSize" type="number" value="${printer.bufferSize}" />
                      <div class="field-hint">Internal buffer size</div>
                    </div>

                    <div class="field">
                      <label class="label">Drain rate (bytes/s)</label>
                      <input class="input" id="drainRate" type="number" value="${printer.drainRate}" />
                      <div class="field-hint">Processing speed</div>
                    </div>
                  </div>
                </div>

                <div class="form-actions">
                  <button class="btn btn-secondary" onclick="closeModal()">Cancel</button>
                  <button class="btn btn-primary" onclick="updatePrinter('${printerId}')">Save</button>
                </div>
              </div>
            </div>
          `;
            document.getElementById('modalContainer').appendChild(modal);
        }

        async function createPrinter() {
            const name = document.getElementById('printerName').value.trim();
            const protocol = document.getElementById('printerProtocol').value;
            const width = parseInt(document.getElementById('printerWidth').value) || 576;
            const emulateBuffer = document.getElementById('emulateBuffer').checked;
            const bufferSize = parseInt(document.getElementById('bufferSize').value) || 4096;
            const drainRate = parseInt(document.getElementById('drainRate').value) || 4096;

            const nameInput = document.getElementById('printerName');
            const nameError = document.getElementById('nameError');

            nameInput.classList.remove('invalid');
            nameError.classList.remove('show');

            if (!name) {
                nameInput.classList.add('invalid');
                nameError.classList.add('show');
                nameInput.focus();
                return;
            }

            try
            {
                const request = {
                    id: crypto.randomUUID(),
                    displayName: name,
                    protocol: normalizeProtocol(protocol),
                    widthInDots: width,
                    heightInDots: null,
                    tcpListenPort: 9106,
                    emulateBufferCapacity: emulateBuffer,
                    bufferDrainRate: drainRate,
                    bufferMaxCapacity: bufferSize
                };

                const created = await apiRequest('/api/printers', {
                    method: 'POST',
                    body: JSON.stringify(request)
                });

                await loadPrinters(created.id);
                closeModal();
                showToast('Printer created successfully');
            }
            catch (err)
            {
                console.error(err);
                showToast(err.message || 'Failed to create printer', true);
            }
        }

        async function updatePrinter(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            const name = document.getElementById('printerName').value.trim();
            const protocol = document.getElementById('printerProtocol').value;
            const width = parseInt(document.getElementById('printerWidth').value) || 576;
            const emulateBuffer = document.getElementById('emulateBuffer').checked;
            const bufferSize = parseInt(document.getElementById('bufferSize').value) || 4096;
            const drainRate = parseInt(document.getElementById('drainRate').value) || 4096;

            const nameInput = document.getElementById('printerName');
            const nameError = document.getElementById('nameError');

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
                    displayName: name,
                    protocol: normalizeProtocol(protocol),
                    widthInDots: width,
                    heightInDots: null,
                    tcpListenPort: printer.port || 9106,
                    emulateBufferCapacity: emulateBuffer,
                    bufferDrainRate: drainRate,
                    bufferMaxCapacity: bufferSize
                };

                await apiRequest(`/api/printers/${printerId}`, {
                    method: 'PUT',
                    body: JSON.stringify(request)
                });

                await loadPrinters(printerId);
                closeModal();
                showToast('Printer updated successfully');
            }
            catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to update printer', true);
            }
        }

        function closeModal() {
            document.getElementById('modalContainer').innerHTML = '';
        }

        function downloadDocument(docId) {
            showToast('Download started');
        }

        function replayDocument(docId) {
            showToast('Document replayed');
        }

        // Workspace Management
        function showWorkspaceDialog(mode = 'create') {
            const modal = document.createElement('div');
            modal.className = 'modal-overlay';
            modal.innerHTML = `
            <div class="modal">
              <div class="modal-header">Workspace Access</div>
              <div class="modal-body">
                <div class="mode-toggle">
                  <button class="mode-toggle-btn ${mode === 'create' ? 'active' : ''}" onclick="switchWorkspaceMode('create')">Create New</button>
                  <button class="mode-toggle-btn ${mode === 'join' ? 'active' : ''}" onclick="switchWorkspaceMode('join')">Access Existing</button>
                </div>

                <div id="createMode" style="display: ${mode === 'create' ? 'block' : 'none'};">
                  <div class="field">
                    <label class="label">Your name (optional)</label>
                    <input class="input" id="workspaceNameInput" placeholder="e.g., John Smith" />
                    <div class="field-hint">Used only for display purposes</div>
                  </div>

                  <p style="color: var(--muted); font-size: 14px; margin: 16px 0;">
                    A unique workspace token will be generated for you. Save it to access your printers from other devices.
                  </p>
                </div>

                <div id="joinMode" style="display: ${mode === 'join' ? 'block' : 'none'};">
                  <div class="field">
                    <label class="label required">Workspace Token</label>
                    <input class="input" id="workspaceTokenInput" placeholder="e.g., brave-tiger-1234" style="font-family: 'Courier New', monospace;" />
                    <div class="field-error" id="tokenError">Please enter a valid token</div>
                    <div class="field-hint">Format: word-word-1234</div>
                  </div>
                </div>

                <div class="form-actions">
                  <button class="btn btn-secondary" onclick="closeModal()">Cancel</button>
                  <button class="btn btn-primary" onclick="${mode === 'create' ? 'createWorkspace()' : 'accessWorkspace()'}">
                    ${mode === 'create' ? 'Create Workspace' : 'Access Workspace'}
                  </button>
                </div>
              </div>
            </div>
          `;
            document.getElementById('modalContainer').appendChild(modal);

            if (mode === 'create') {
                setTimeout(() => document.getElementById('workspaceNameInput')?.focus(), 100);
            } else {
                setTimeout(() => document.getElementById('workspaceTokenInput')?.focus(), 100);
            }
        }

        function switchWorkspaceMode(mode) {
            const createMode = document.getElementById('createMode');
            const joinMode = document.getElementById('joinMode');
            const buttons = document.querySelectorAll('.mode-toggle-btn');
            const primaryBtn = document.querySelector('.form-actions .btn-primary');

            buttons[0].classList.toggle('active', mode === 'create');
            buttons[1].classList.toggle('active', mode === 'join');

            createMode.style.display = mode === 'create' ? 'block' : 'none';
            joinMode.style.display = mode === 'join' ? 'block' : 'none';

            primaryBtn.textContent = mode === 'create' ? 'Create Workspace' : 'Access Workspace';
            primaryBtn.onclick = mode === 'create' ? createWorkspace : accessWorkspace;
        }

        async function createWorkspace() {
            const name = document.getElementById('workspaceNameInput').value.trim();
            if (!name) {
                showToast('Please enter a workspace owner name', true);
                return;
            }

            try {
                const request = {
                    id: crypto.randomUUID(),
                    ownerName: name
                };

                const workspace = await apiRequest('/api/workspaces', {
                    method: 'POST',
                    body: JSON.stringify(request)
                });

                if (!workspace?.token) {
                    throw new Error('Workspace token missing from response');
                }

                workspaceToken = workspace.token;
                workspaceName = workspace.ownerName;
                localStorage.setItem('workspaceToken', workspaceToken);
                localStorage.setItem('workspaceName', workspaceName);

                await loginWithToken(workspaceToken);

                closeModal();
                showTokenDialog(workspaceToken);
                updateUserDisplay();
                renderSidebar();
                renderDocuments();
            }
            catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to create workspace', true);
            }
        }

        async function accessWorkspace() {
            const token = document.getElementById('workspaceTokenInput').value.trim();
            const tokenInput = document.getElementById('workspaceTokenInput');
            const tokenError = document.getElementById('tokenError');

            tokenInput.classList.remove('invalid');
            tokenError.classList.remove('show');

            if (!token) {
                tokenInput.classList.add('invalid');
                tokenError.classList.add('show');
                tokenInput.focus();
                return;
            }

            try {
                await loginWithToken(token);
                workspaceToken = token;
                localStorage.setItem('workspaceToken', token);

                closeModal();
                updateUserDisplay();
                renderSidebar();
                renderDocuments();
                showToast('Workspace accessed successfully');
            }
            catch (err) {
                console.error(err);
                tokenInput.classList.add('invalid');
                tokenError.classList.add('show');
                tokenError.textContent = 'Workspace not found or token is invalid';
            }
        }

        async function loginWithToken(token) {
            const loginResponse = await apiRequest('/api/auth/login', {
                method: 'POST',
                body: JSON.stringify({ token })
            });

            accessToken = loginResponse.accessToken;
            const workspace = loginResponse.workspace;
            workspaceName = workspace?.ownerName || null;

            localStorage.setItem('accessToken', accessToken);
            if (workspaceName) {
                localStorage.setItem('workspaceName', workspaceName);
            }
            else {
                localStorage.removeItem('workspaceName');
            }

            // Fetch current workspace to confirm auth
            await apiRequest('/api/auth/me');
            startStatusStream();
            await loadPrinters();
        }

        function showTokenDialog(token) {
            const modal = document.createElement('div');
            modal.className = 'modal-overlay';
            modal.innerHTML = `
            <div class="modal">
              <div class="modal-header">Your Workspace Token</div>
              <div class="modal-body">
                <div class="token-display">
                  <p style="color: var(--muted); margin: 0 0 8px;">Save this token to access your workspace from other devices</p>
                  <div class="token-value">${token}</div>
                  <div class="token-actions">
                    <button class="btn btn-primary" onclick="copyToClipboard('${token}')">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                      </svg>
                      Copy Token
                    </button>
                  </div>
                </div>

                <div style="background: rgba(239,68,68,0.1); border: 1px solid var(--danger); border-radius: 10px; padding: 16px; margin-top: 16px;">
                  <p style="margin: 0; font-size: 14px; color: var(--muted);">
                    <strong style="color: var(--danger);">⚠️ Important:</strong> This token will only be shown once. Anyone with this token can access your printers.
                  </p>
                </div>

                <div class="form-actions">
                  <button class="btn btn-primary w100" onclick="closeModal()">I've Saved My Token</button>
                </div>
              </div>
            </div>
          `;
            document.getElementById('modalContainer').appendChild(modal);
        }

        async function logOut() {
            try {
                await apiRequest('/api/auth/logout', { method: 'POST' });
            } catch {
                // ignore logout errors
            }
            workspaceToken = null;
            workspaceName = null;
            accessToken = null;
            printers = [];
            documents = {};
            selectedPrinterId = null;
            stopStatusStream();

            localStorage.removeItem('workspaceToken');
            localStorage.removeItem('workspaceName');
            localStorage.removeItem('accessToken');

            updateUserDisplay();
            renderSidebar();
            renderDocuments();
            showToast('Logged out successfully');
        }

        // Theme Functions
        function toggleTheme() {
            const html = document.documentElement;
            const current = html.getAttribute('data-theme');
            const next = current === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-theme', next);
            localStorage.setItem('theme', next);
            updateThemeIcon(next);
        }

        function updateThemeIcon(theme) {
            const darkIcon = document.getElementById('themeIconDark');
            const lightIcon = document.getElementById('themeIconLight');
            if (theme === 'dark') {
                darkIcon.style.display = 'block';
                lightIcon.style.display = 'none';
            } else {
                darkIcon.style.display = 'none';
                lightIcon.style.display = 'block';
            }
        }

        function initTheme() {
            const html = document.documentElement;
            const saved = localStorage.getItem('theme') || 'dark';
            html.setAttribute('data-theme', saved);
            updateThemeIcon(saved);
        }

        function toggleSidebar() {
            const container = document.querySelector('.container');
            container.classList.toggle('sidebar-hidden');

            const isHidden = container.classList.contains('sidebar-hidden');
            localStorage.setItem('sidebarHidden', isHidden);
        }

        function getInitials(name) {
            if (!name) return '?';
            const parts = name.trim().split(' ');
            if (parts.length >= 2) {
                return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
            }
            return parts[0].substring(0, 2).toUpperCase();
        }

        function updateUserDisplay() {
            const avatar = document.getElementById('userAvatar');
            const userName = document.getElementById('userName');
            const userStatus = document.getElementById('userStatus');

            if (workspaceToken) {
                if (workspaceName) {
                    avatar.textContent = getInitials(workspaceName);
                    userName.textContent = workspaceName;
                } else {
                    avatar.textContent = workspaceToken.substring(0, 2).toUpperCase();
                    userName.textContent = workspaceToken;
                }
                userStatus.textContent = 'Workspace active';
            } else {
                avatar.textContent = '?';
                userName.textContent = 'No workspace';
                userStatus.textContent = '';
            }
        }

        function showUserMenu(event) {
            event.stopPropagation();

            const existingMenu = document.querySelector('.menu');
            if (existingMenu) existingMenu.remove();

            const menu = document.createElement('div');
            menu.className = 'menu';
            menu.style.position = 'fixed';
            menu.style.left = event.currentTarget.getBoundingClientRect().left + 'px';
            menu.style.bottom = (window.innerHeight - event.currentTarget.getBoundingClientRect().top) + 'px';

            if (workspaceToken) {
                menu.innerHTML = `
              <div class="menu-item" onclick="showWorkspaceDialog('create')">New Workspace</div>
              <div class="menu-item" onclick="showWorkspaceDialog('join')">Switch Workspace</div>
              <div style="border-top: 1px solid var(--border); margin: 4px 0;"></div>
              <div class="menu-item" onclick="logOut()">End Workspace</div>
            `;
            } else {
                menu.innerHTML = `
              <div class="menu-item" onclick="showWorkspaceDialog('create')">Create Workspace</div>
              <div class="menu-item" onclick="showWorkspaceDialog('join')">Access Workspace</div>
            `;
            }

            document.body.appendChild(menu);

            setTimeout(() => {
                document.addEventListener('click', function closeMenu() {
                    menu.remove();
                    document.removeEventListener('click', closeMenu);
                });
            }, 0);
        }

        async function startStatusStream() {
            if (!accessToken || !workspaceToken) {
                stopStatusStream();
                return;
            }

            stopStatusStream();

            const controller = new AbortController();
            statusStreamController = controller;

            try {
                const response = await fetch('/api/printers/status/stream', {
                    method: 'GET',
                    headers: { ...authHeaders(), 'Accept': 'text/event-stream' },
                    signal: controller.signal
                });

                if (!response.ok || !response.body) {
                    throw new Error('Failed to start status stream');
                }

                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                while (true) {
                    const { value, done } = await reader.read();
                    if (done) break;
                    buffer += decoder.decode(value, { stream: true });

                    const parts = buffer.split('\n\n');
                    buffer = parts.pop() || '';

                    for (const part of parts) {
                        if (!part.trim()) continue;

                        const lines = part.split('\n');
                        let eventName = 'message';
                        let data = '';

                        for (const line of lines) {
                            if (line.startsWith('event:')) {
                                eventName = line.substring(6).trim();
                            } else if (line.startsWith('data:')) {
                                data += line.substring(5).trim();
                            }
                        }

                        if (eventName === 'status' && data) {
                            try {
                                const payload = JSON.parse(data);
                                handleStatusEvent(payload);
                            } catch (e) {
                                console.error('Failed to parse status event', e);
                            }
                        }
                    }
                }
            } catch (err) {
                if (!(err instanceof DOMException && err.name === 'AbortError')) {
                    console.error('Status stream error', err);
                }
            }
        }

        function stopStatusStream() {
            if (statusStreamController) {
                statusStreamController.abort();
                statusStreamController = null;
            }
        }

        function handleStatusEvent(payload) {
            if (!payload?.printerId) return;
            const idx = printers.findIndex(p => p.id === payload.printerId);
            if (idx === -1) return;

            const updated = { ...printers[idx] };
            updated.targetStatus = (payload.targetState || payload.targetStatus || updated.targetStatus || '').toLowerCase();
            updated.runtimeStatus = (payload.runtimeStatus || updated.runtimeStatus || '').toLowerCase();
            updated.runtimeStatusAt = payload.updatedAt ? new Date(payload.updatedAt) : updated.runtimeStatusAt;
            updated.runtimeStatusError = payload.error || null;
            printers[idx] = updated;
            renderSidebar();
        }

        // Initialize
        initTheme();

        // Restore workspace
        const savedToken = localStorage.getItem('workspaceToken');
        const savedName = localStorage.getItem('workspaceName');
        const savedAccessToken = localStorage.getItem('accessToken');
        if (savedToken && savedAccessToken) {
            workspaceToken = savedToken;
            workspaceName = savedName;
            accessToken = savedAccessToken;
            startStatusStream();
            loadPrinters();
        }

        updateUserDisplay();
        renderSidebar();
        renderDocuments();

        // Restore sidebar state
        const sidebarHidden = localStorage.getItem('sidebarHidden') === 'true';
        if (sidebarHidden) {
            document.querySelector('.container').classList.add('sidebar-hidden');
        }
    
