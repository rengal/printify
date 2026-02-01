
        console.info('main.js loaded - revision 2', '2025-02-20T12:00:00Z');

        // API + Workspace State
        const apiBase = '';
        let workspaceToken = null;
        let workspaceName = null;
        let workspaceCreatedAt = null;
        let accessToken = null;

        // Data
        let printers = [];
        let documents = {};
        let selectedPrinterId = null;
        let statusStreamController = null;
        let documentStreamController = null;
        let documentStreamPrinterId = null;
        let runtimeStreamController = null;
        let runtimeStreamPrinterId = null;
        let debugMode = false;

        // Strong daytime intervals (server and client must agree)
        const TIME_INTERVALS = {
            morning: { start: 6, end: 11 },   // 06:00 - 11:00
            afternoon: { start: 12.5, end: 15.5 }, // 12:30 - 15:30
            evening: { start: 18.5, end: 22 }  // 18:30 - 22:00
        };

        function selectTimeBasedGreeting(data) {
            const now = new Date();
            const hour = now.getHours() + now.getMinutes() / 60;

            // Check if within morning interval
            if (data.morning && hour >= TIME_INTERVALS.morning.start && hour < TIME_INTERVALS.morning.end) {
                return data.morning;
            }

            // Check if within afternoon interval
            if (data.afternoon && hour >= TIME_INTERVALS.afternoon.start && hour < TIME_INTERVALS.afternoon.end) {
                return data.afternoon;
            }

            // Check if within evening interval
            if (data.evening && hour >= TIME_INTERVALS.evening.start && hour < TIME_INTERVALS.evening.end) {
                return data.evening;
            }

            // Fallback to general greeting
            return data.general;
        }

        // Cache buster - increments on workspace changes to bypass browser HTTP cache
        let greetingCacheBuster = 0;

        async function getWelcomeMessage() {
            try {
                // Add cache-busting parameter to bypass browser HTTP cache when workspace changes
                const cacheParam = greetingCacheBuster > 0 ? `?_cb=${greetingCacheBuster}` : '';
                const data = await apiRequest(`/api/workspaces/greeting${cacheParam}`);
                // Select appropriate greeting based on client time
                return selectTimeBasedGreeting(data);
            } catch (err) {
                console.error('[Greeting] Failed to fetch greeting:', err);
                return 'Welcome to Printify!';
            }
        }

        function invalidateGreetingCache() {
            greetingCacheBuster++;
        }

        function updateWorkspaceToken(newToken) {
            if (newToken !== workspaceToken) {
                workspaceToken = newToken;
                invalidateGreetingCache();
            } else {
                workspaceToken = newToken;
            }
        }

        function authHeaders() {
            return accessToken
                ? { 'Authorization': `Bearer ${accessToken}` }
                : {};
        }

        async function apiRequest(path, options = {}) {
            const { isTokenLogin = false, ...fetchOptions } = options;

            const headers = {
                'Content-Type': 'application/json',
                ...authHeaders(),
                ...(fetchOptions.headers || {})
            };

            const response = await fetch(`${apiBase}${path}`, {
                ...fetchOptions,
                headers
            });

            if (!response.ok) {
                // Handle 401/403 - authentication/authorization failures
                if (response.status === 401 || response.status === 403) {
                    console.error(`Auth failed (${response.status}) for ${path}, isTokenLogin: ${isTokenLogin}`);

                    // Only auto-logout if we have a workspace token AND we're not trying to login with a new token
                    if (workspaceToken && !isTokenLogin) {
                        console.log('[apiRequest] Session expired - logging out');
                        logOut();
                    }
                }
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
            const targetStatus = (dto.operationalFlags?.targetState || 'started').toLowerCase();
            const runtimeStatus = (dto.runtimeStatus?.state || 'unknown').toLowerCase();
            return {
                id: dto.printer.id,
                name: dto.printer.displayName,
                protocol: dto.settings.protocol,
                width: dto.settings.widthInDots,
                height: dto.settings.heightInDots,
                port: dto.settings.tcpListenPort,
                emulateBuffer: dto.settings.emulateBufferCapacity,
                bufferSize: dto.settings.bufferMaxCapacity || 0,
                drainRate: dto.settings.bufferDrainRate || 0,
                publicHost: dto.settings.publicHost,
                pinned: dto.printer.isPinned,
                targetStatus,
                runtimeStatus,
                runtimeStatusAt: dto.runtimeStatus?.updatedAt ? new Date(dto.runtimeStatus.updatedAt) : null,
                lastDocumentAt: dto.printer.lastDocumentReceivedAt ? new Date(dto.printer.lastDocumentReceivedAt) : null,
                newDocs: 0,
                pinOrder: index,
                // Operational flags
                isCoverOpen: dto.operationalFlags?.isCoverOpen ?? false,
                isPaperOut: dto.operationalFlags?.isPaperOut ?? false,
                isOffline: dto.operationalFlags?.isOffline ?? false,
                hasError: dto.operationalFlags?.hasError ?? false,
                isPaperNearEnd: dto.operationalFlags?.isPaperNearEnd ?? false,
                // Runtime status
                bufferedBytes: dto.runtimeStatus?.bufferedBytes ?? null,
                bufferedBytesDeltaBps: dto.runtimeStatus?.bufferedBytesDeltaBps ?? null,
                drawer1State: dto.runtimeStatus?.drawer1State ?? null,
                drawer2State: dto.runtimeStatus?.drawer2State ?? null
            };
        }

        function getPrinterById(id) {
            return printers.find(p => p.id === id) || null;
        }

        async function loadPrinters(selectId = null) {
            try {
                const list = await apiRequest('/api/printers');
                console.debug('loadPrinters fetched', list);
                printers = list.map((p, idx) => mapPrinterDto(p, idx));
                if (selectId && printers.some(p => p.id === selectId)) {
                    selectedPrinterId = selectId;
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

        function formatPrinterAddress(printer) {
            if (printer.port) {
                const host = printer.publicHost || 'localhost';
                return `${host}:${printer.port}`;
            }

            return 'Listener not configured';
        }

        function escapeHtml(value) {
            if (value === null || value === undefined) {
                return '';
            }

            return String(value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        }

        function resolveMediaUrl(url) {
            if (!url) {
                return '';
            }

            if (url.startsWith('http://') || url.startsWith('https://')) {
                return url;
            }

            if (!apiBase) {
                return url;
            }

            if (apiBase.endsWith('/') && url.startsWith('/')) {
                return `${apiBase.slice(0, -1)}${url}`;
            }

            return `${apiBase}${url}`;
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

            const pinnedPrinters = printers.filter(p => p.pinned).sort((a, b) => a.pinOrder - b.pinOrder);
            const otherPrinters = printers.filter(p => !p.pinned).sort((a, b) => a.name.localeCompare(b.name));

            console.debug('renderSidebar', { total: printers.length, pinned: pinnedPrinters.length, other: otherPrinters.length });

            pinnedList.innerHTML = pinnedPrinters.map(p => renderPrinterItem(p, true)).join('');
            otherList.innerHTML = otherPrinters.map(p => renderPrinterItem(p, false)).join('');
        }

        function renderPrinterItem(p, isPinned) {
            const isStopped = p.runtimeStatus === 'stopped';

            // Pin icon (if pinned) + name + red alert icon (if stopped)
            let pinIcon = '';
            if (isPinned) {
                pinIcon = '<svg class="pin-icon pin-icon-filled" width="12" height="12" viewBox="0 0 24 24" fill="#10b981" stroke="#10b981" stroke-width="2"><path d="M12 2l2.4 7.4h7.6l-6 4.6 2.3 7-6.3-4.6-6.3 4.6 2.3-7-6-4.6h7.6z"/></svg> ';
            }

            let statusIcon = '';
            if (isStopped) {
                statusIcon = '<img class="stopped-icon" src="assets/icons/alert-triangle.svg" width="18" height="18" alt="Printer is stopped" title="Printer is stopped">';
            }

            return `
            <div class="list-item ${selectedPrinterId === p.id ? 'active' : ''} ${isStopped ? 'has-status-icon' : ''}" onclick="selectPrinter('${p.id}')">
              <span class="list-item-name">${pinIcon}${escapeHtml(p.name)}</span>${statusIcon}
              <button class="list-item-gear" onclick="event.stopPropagation(); toggleOperationsForPrinter('${p.id}')" title="Toggle operations">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="1"></circle>
                  <circle cx="12" cy="5" r="1"></circle>
                  <circle cx="12" cy="19" r="1"></circle>
                </svg>
              </button>
            </div>
          `;
        }

        async function selectPrinter(id) {
            selectedPrinterId = id;
            const printer = getPrinterById(id);
            if (printer) {
                printer.newDocs = 0;
                renderSidebar();

                // Load operations panel using the module
                if (window.OperationsPanel && accessToken) {
                    try {
                        const container = document.getElementById('operationsPanel');
                        await OperationsPanel.loadPanel(id, accessToken, container);

                        // Restore danger zone state
                        OperationsPanel.restoreDangerZoneState();
                    } catch (err) {
                        console.error('Failed to load operations panel', err);
                    }
                }

                try {
                    await ensureDocumentsLoaded(id);
                    startDocumentStream(id);
                    startRuntimeStream(id);
                } catch (err) {
                    console.error('Failed to load documents', err);
                    showToast('Unable to load documents', true);
                }
                renderDocuments();
            }
        }

        async function ensureDocumentsLoaded(printerId) {
            if (documents[printerId]) {
                return;
            }

            console.debug('Loading documents for printer', printerId);
            const response = await apiRequest(`/api/printers/${printerId}/documents/canvas?limit=50`);
            const items = response?.result?.items || [];
            const printer = getPrinterById(printerId);
            documents[printerId] = items.map(dto => DocumentsPanel.mapViewDocumentDto(dto, printer));
        }

        async function startDocumentStream(printerId) {
            if (!accessToken || !workspaceToken || !printerId) {
                stopDocumentStream();
                return;
            }

            if (documentStreamPrinterId === printerId && documentStreamController) {
                return;
            }

            stopDocumentStream();

            const controller = new AbortController();
            documentStreamController = controller;
            documentStreamPrinterId = printerId;

            try {
                const response = await fetch(`/api/printers/${printerId}/documents/canvas/stream`, {
                    method: 'GET',
                    headers: { ...authHeaders(), 'Accept': 'text/event-stream' },
                    signal: controller.signal
                });

                if (!response.ok || !response.body) {
                    throw new Error('Failed to start document stream');
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

                        if (eventName === 'documentCanvasReady' && data) {
                            handleDocumentEvent(printerId, data);
                        }
                    }
                }
            } catch (err) {
                if (!(err instanceof DOMException && err.name === 'AbortError')) {
                    console.error('Document stream error', err);
                }
            }
        }

        function stopDocumentStream() {
            if (documentStreamController) {
                documentStreamController.abort();
                documentStreamController = null;
                documentStreamPrinterId = null;
            }
        }

        function handleDocumentEvent(printerId, rawData) {
            try {
                const payload = typeof rawData === 'string' ? JSON.parse(rawData) : rawData;
                console.debug('Document event received', payload);
                const printer = getPrinterById(printerId);
                const mapped = DocumentsPanel.mapViewDocumentDto(payload, printer);
                const list = documents[printerId] || [];
                list.unshift(mapped);
                list.sort((a, b) => b.timestamp - a.timestamp);
                documents[printerId] = list.slice(0, 200);

                if (selectedPrinterId !== printerId) {
                    const target = getPrinterById(printerId);
                    if (target) target.newDocs += 1;
                }
                else
                {
                    const target = getPrinterById(printerId);
                    if (target)
                    {
                        target.lastDocumentAt = mapped.timestamp;
                    }
                }

                renderDocuments();
                renderSidebar();
            } catch (e) {
                console.error('Failed to parse document event', e);
            }
        }

        async function startRuntimeStream(printerId) {
            if (!accessToken || !workspaceToken || !printerId) {
                stopRuntimeStream();
                return;
            }

            if (runtimeStreamPrinterId === printerId && runtimeStreamController) {
                return;
            }

            stopRuntimeStream();

            const controller = new AbortController();
            runtimeStreamController = controller;
            runtimeStreamPrinterId = printerId;

            try {
                const response = await fetch(`/api/printers/${printerId}/runtime/stream`, {
                    method: 'GET',
                    headers: { ...authHeaders(), 'Accept': 'text/event-stream' },
                    signal: controller.signal
                });

                if (!response.ok || !response.body) {
                    throw new Error('Failed to start runtime stream');
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
                            handleRuntimeEvent(printerId, data);
                        }
                    }
                }
            } catch (err) {
                if (!(err instanceof DOMException && err.name === 'AbortError')) {
                    console.error('Runtime stream error', err);
                }
            }
        }

        function stopRuntimeStream() {
            if (runtimeStreamController) {
                runtimeStreamController.abort();
                runtimeStreamController = null;
                runtimeStreamPrinterId = null;
            }
        }

        function handleRuntimeEvent(printerId, rawData) {
            try {
                const payload = typeof rawData === 'string' ? JSON.parse(rawData) : rawData;
                console.debug('Runtime status event received', payload);

                const printer = getPrinterById(printerId);
                if (!printer) return;

                // Track which fields were present in the SSE payload (server only sends changed fields)
                const changedFields = {
                    runtimeStatus: false,
                    targetStatus: false,
                    operationalFlags: [],
                    bufferedBytes: false,
                    drawerStates: [],
                    lastDocumentAt: false,
                    settings: false
                };

                // Update runtime status - if field is present, it changed
                if (payload.runtime) {
                    if (payload.runtime.state !== undefined) {
                        printer.runtimeStatus = payload.runtime.state.toLowerCase();
                        changedFields.runtimeStatus = true;
                    }
                    if (payload.runtime.updatedAt) {
                        printer.runtimeStatusAt = new Date(payload.runtime.updatedAt);
                    }
                    if (payload.runtime.bufferedBytes !== undefined) {
                        printer.bufferedBytes = payload.runtime.bufferedBytes;
                        changedFields.bufferedBytes = true;
                    }
                    if (payload.runtime.bufferedBytesDeltaBps !== undefined) {
                        printer.bufferedBytesDeltaBps = payload.runtime.bufferedBytesDeltaBps;
                    }
                    if (payload.runtime.drawer1State !== undefined) {
                        printer.drawer1State = payload.runtime.drawer1State;
                        changedFields.drawerStates.push('drawer1State');
                    }
                    if (payload.runtime.drawer2State !== undefined) {
                        printer.drawer2State = payload.runtime.drawer2State;
                        changedFields.drawerStates.push('drawer2State');
                    }
                }

                // Update operational flags - if field is present, it changed
                if (payload.operationalFlags) {
                    if (payload.operationalFlags.targetState !== undefined) {
                        printer.targetStatus = payload.operationalFlags.targetState.toLowerCase();
                        changedFields.targetStatus = true;
                    }
                    if (payload.operationalFlags.isCoverOpen !== undefined) {
                        printer.isCoverOpen = payload.operationalFlags.isCoverOpen;
                        changedFields.operationalFlags.push('isCoverOpen');
                    }
                    if (payload.operationalFlags.isPaperOut !== undefined) {
                        printer.isPaperOut = payload.operationalFlags.isPaperOut;
                        changedFields.operationalFlags.push('isPaperOut');
                    }
                    if (payload.operationalFlags.isOffline !== undefined) {
                        printer.isOffline = payload.operationalFlags.isOffline;
                        changedFields.operationalFlags.push('isOffline');
                    }
                    if (payload.operationalFlags.hasError !== undefined) {
                        printer.hasError = payload.operationalFlags.hasError;
                        changedFields.operationalFlags.push('hasError');
                    }
                    if (payload.operationalFlags.isPaperNearEnd !== undefined) {
                        printer.isPaperNearEnd = payload.operationalFlags.isPaperNearEnd;
                        changedFields.operationalFlags.push('isPaperNearEnd');
                    }
                }

                // Update settings if provided (settings changes require full re-render)
                if (payload.settings) {
                    changedFields.settings = true;
                    printer.protocol = payload.settings.protocol;
                    printer.width = payload.settings.widthInDots;
                    printer.height = payload.settings.heightInDots;
                    printer.port = payload.settings.tcpListenPort;
                    printer.emulateBuffer = payload.settings.emulateBufferCapacity;
                    printer.bufferSize = payload.settings.bufferMaxCapacity || 0;
                    printer.drainRate = payload.settings.bufferDrainRate || 0;
                    printer.publicHost = payload.settings.publicHost;
                }

                // Update printer metadata if provided
                if (payload.printer) {
                    if (payload.printer.displayName !== undefined) {
                        changedFields.settings = true; // Name change requires full re-render
                        printer.name = payload.printer.displayName;
                    }
                    if (payload.printer.isPinned !== undefined) {
                        printer.pinned = payload.printer.isPinned;
                    }
                    if (payload.printer.lastDocumentReceivedAt !== undefined) {
                        printer.lastDocumentAt = payload.printer.lastDocumentReceivedAt
                            ? new Date(payload.printer.lastDocumentReceivedAt)
                            : null;
                        changedFields.lastDocumentAt = true;
                    }
                }

                // Always update sidebar (for all printers)
                renderSidebar();

                // Only update operations panel if this is the currently selected printer
                if (printerId === selectedPrinterId && window.OperationsPanel) {
                    if (changedFields.settings) {
                        // Settings changes require full re-render - reload panel
                        const container = document.getElementById('operationsPanel');
                        OperationsPanel.loadPanel(printerId, accessToken, container).then(() => {
                            OperationsPanel.restoreDangerZoneState();
                        }).catch(err => {
                            console.error('Failed to reload operations panel', err);
                        });
                    } else {
                        // Partial update for runtime/flags/drawers/buffer changes
                        const partialData = {};
                        if (changedFields.runtimeStatus || changedFields.targetStatus) {
                            partialData.runtimeStatus = {
                                state: printer.runtimeStatus,
                                updatedAt: printer.runtimeStatusAt
                            };
                        }
                        if (changedFields.bufferedBytes || changedFields.drawerStates.length > 0) {
                            partialData.runtimeStatus = partialData.runtimeStatus || {};
                            if (changedFields.bufferedBytes) {
                                partialData.runtimeStatus.bufferedBytes = printer.bufferedBytes;
                                if (printer.bufferedBytesDeltaBps !== undefined) {
                                    partialData.runtimeStatus.bufferedBytesDeltaBps = printer.bufferedBytesDeltaBps;
                                }
                            }
                            if (changedFields.drawerStates.includes('drawer1State')) {
                                partialData.runtimeStatus.drawer1State = printer.drawer1State;
                            }
                            if (changedFields.drawerStates.includes('drawer2State')) {
                                partialData.runtimeStatus.drawer2State = printer.drawer2State;
                            }
                        }
                        if (changedFields.operationalFlags.length > 0) {
                            partialData.operationalFlags = {};
                            if (changedFields.operationalFlags.includes('isCoverOpen')) {
                                partialData.operationalFlags.isCoverOpen = printer.isCoverOpen;
                            }
                            if (changedFields.operationalFlags.includes('isPaperOut')) {
                                partialData.operationalFlags.isPaperOut = printer.isPaperOut;
                            }
                            if (changedFields.operationalFlags.includes('isOffline')) {
                                partialData.operationalFlags.isOffline = printer.isOffline;
                            }
                            if (changedFields.operationalFlags.includes('hasError')) {
                                partialData.operationalFlags.hasError = printer.hasError;
                            }
                        }
                        if (changedFields.lastDocumentAt) {
                            partialData.printer = {
                                lastDocumentAt: printer.lastDocumentAt
                            };
                        }

                        OperationsPanel.applyPartialUpdate(partialData, printerId);
                    }
                }
            } catch (e) {
                console.error('Failed to parse runtime event', e);
            }
        }

        async function renderDocuments() {
            const operationsPanel = document.getElementById('operationsPanel');
            const documentsPanel = document.getElementById('documentsPanel');

            if (!workspaceToken) {
                if (window.OperationsPanel?.renderEmptyState) {
                    await OperationsPanel.renderEmptyState(
                        { title: 'No Workspace', body: 'Create or access workspace' },
                        operationsPanel);
                } else {
                    operationsPanel.textContent = 'No Workspace';
                }
                // Use DocumentsPanel module for no-workspace state
                if (window.DocumentsPanel?.renderNoWorkspace) {
                    await DocumentsPanel.renderNoWorkspace(documentsPanel);
                }
                return;
            }

            if (!selectedPrinterId) {
                const noPrinterCaption = printers.length === 0 ? 'No printers yet' : 'No Printer Selected';
                const noPrinterBody = printers.length === 0 ? '' : 'Select a printer from the list';
                if (window.OperationsPanel?.renderEmptyState) {
                    await OperationsPanel.renderEmptyState(
                        { title: noPrinterCaption, body: noPrinterBody },
                        operationsPanel);
                } else {
                    operationsPanel.textContent = noPrinterCaption;
                }
                // Use DocumentsPanel module for no-printer state
                if (window.DocumentsPanel?.renderNoPrinter) {
                    const greeting = await getWelcomeMessage();
                    const noPrintersMsg = printers.length === 0
                        ? 'No printers available. Add a printer to view documents.'
                        : 'Select a printer to view documents';
                    await DocumentsPanel.renderNoPrinter({
                        greeting: greeting,
                        message: noPrintersMsg
                    }, documentsPanel);
                }
                return;
            }

            const docs = documents[selectedPrinterId] || [];
            const printer = getPrinterById(selectedPrinterId);

            if (!printer) {
                if (window.OperationsPanel?.renderEmptyState) {
                    await OperationsPanel.renderEmptyState(
                        { title: 'Printer not found', body: '' },
                        operationsPanel);
                } else {
                    operationsPanel.textContent = 'Printer not found';
                }
                documentsPanel.innerHTML = '';
                return;
            }

            // Note: Operations panel is now rendered by OperationsPanel module
            // This function only handles the documents panel rendering

            // Render documents in documents panel using the module
            if (docs.length === 0) {
                if (window.DocumentsPanel?.renderNoDocuments) {
                    await DocumentsPanel.renderNoDocuments(printer, documentsPanel);
                }
                return;
            }

            if (window.DocumentsPanel?.renderDocumentsList) {
                await DocumentsPanel.renderDocumentsList(docs, printer, documentsPanel);
            }
          }

        function showMenu(event, printerId, isPinned, isStarted) {
            event.stopPropagation();

            const existingMenu = document.querySelector('.menu');
            if (existingMenu) existingMenu.remove();

            const menu = document.createElement('div');
            menu.className = 'menu';
            menu.style.position = 'fixed';
            menu.style.left = event.clientX + 'px';
            menu.style.top = event.clientY + 'px';

            // Play icon for Start, Square icon for Stop
            // Close menu explicitly since stopPropagation in start/stopPrinter prevents document click handler
            const startStopItem = isStarted
              ? `<div class="menu-item" onclick="document.querySelector('.menu')?.remove(); stopPrinter(event, '${printerId}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="6" width="12" height="12" rx="1"/></svg>
                  Stop
                </div>`
              : `<div class="menu-item menu-item-accent" onclick="document.querySelector('.menu')?.remove(); startPrinter(event, '${printerId}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                  Start
                </div>`;

            menu.innerHTML = `
            ${startStopItem}
            <div class="menu-divider"></div>
            <div class="menu-item" onclick="editPrinter('${printerId}')">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
              Edit
            </div>
            <div class="menu-item" onclick="togglePin('${printerId}')">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2l2.4 7.4h7.6l-6 4.6 2.3 7-6.3-4.6-6.3 4.6 2.3-7-6-4.6h7.6z"/></svg>
              ${isPinned ? 'Unpin' : 'Pin'}
            </div>
            <div class="menu-divider"></div>
            <div class="menu-item menu-item-danger" onclick="deletePrinter('${printerId}')">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
              Delete
            </div>
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

        async function deletePrinter(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            const docCount = (documents[printerId] || []).length;
            let message = `Are you sure you want to delete "<strong>${escapeHtml(printer.name)}</strong>"?`;

            if (docCount > 0) {
                message += `<br><br>This will also delete <strong>${docCount}</strong> document${docCount !== 1 ? 's' : ''}.`;
            }

            message += '<br><br>This action cannot be undone.';

            await ConfirmDialog.show(
                'Delete Printer',
                message,
                'Delete Printer',
                async () => {
                    try {
                        await apiRequest(`/api/printers/${printerId}`, { method: 'DELETE' });
                        if (selectedPrinterId === printerId) {
                            selectedPrinterId = null;
                        }
                        await loadPrinters();
                        renderDocuments();
                        showToast('Printer deleted');
                    } catch (err) {
                        console.error(err);
                        showToast(err.message || 'Failed to delete printer', true);
                    }
                },
                true
            );
        }

        async function setPrinterStatus(printerId, targetStatus) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            try {
                await apiRequest(`/api/printers/${printerId}/operational-flags`, {
                    method: 'PATCH',
                    body: JSON.stringify({ targetState: targetStatus })
                });
                await loadPrinters(printerId);
                const action = targetStatus.toLowerCase() === 'started' ? 'started' : 'stopped';
                showToast(`${printer.name} ${action}`);
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

        async function toggleOperationalFlag(printerId, flagName, value) {
            const printer = getPrinterById(printerId);
            if (!printer) return;

            try {
                const body = {};
                body[flagName] = value;
                await apiRequest(`/api/printers/${printerId}/operational-flags`, {
                    method: 'PATCH',
                    body: JSON.stringify(body)
                });
                // Update will come via SSE stream
            } catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to update flag', true);
                // Revert checkbox on error
                renderDocuments();
            }
        }

        async function setDrawerState(printerId, drawerName, state) {
            const printer = getPrinterById(printerId);
            if (!printer) return;

            try {
                const body = {};
                body[drawerName] = state;
                await apiRequest(`/api/printers/${printerId}/drawers`, {
                    method: 'PATCH',
                    body: JSON.stringify(body)
                });
                // Update will come via SSE stream
                showToast(`${drawerName} ${state}`);
            } catch (err) {
                console.error(err);
                showToast(err.message || 'Failed to update drawer state', true);
            }
        }

        function renderBufferProgress(bufferedBytes, bufferSize) {
            if (!bufferSize) return '';

            const bytes = bufferedBytes ?? 0;
            const percentage = Math.min((bytes / bufferSize) * 100, 100);

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

        async function clearDocuments(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            const docCount = (documents[printerId] || []).length;
            let message;

            if (docCount === 0) {
                message = `Delete document history for "<strong>${escapeHtml(printer.name)}</strong>"?`;
            } else if (docCount === 1) {
                message = `Delete <strong>1</strong> document from "<strong>${escapeHtml(printer.name)}</strong>"?<br><br>This cannot be undone.`;
            } else {
                message = `Delete all <strong>${docCount}</strong> documents from "<strong>${escapeHtml(printer.name)}</strong>"?<br><br>This cannot be undone.`;
            }

            await ConfirmDialog.show(
                'Delete Documents',
                message,
                'Delete Documents',
                async () => {
                    try {
                        // Delete server-side documents so the printer history is cleared.
                        await apiRequest(`/api/printers/${printerId}/documents`, { method: 'DELETE' });
                        // Reset cached documents to match the server state.
                        documents[printerId] = [];

                        if (selectedPrinterId === printerId) {
                            renderDocuments();
                        }

                        showToast('Documents deleted');
                    }
                    catch (err) {
                        console.error(err);
                        showToast(err.message || 'Failed to delete documents', true);
                    }
                },
                true
            );
        }

        function openNewPrinterDialog() {
            if (!workspaceToken) {
                WorkspaceDialog.show();
                return;
            }

            if (window.PrinterDialogue) {
                PrinterDialogue.showCreate();
            }
        }

        function editPrinter(printerId) {
            const printer = printers.find(p => p.id === printerId);
            if (!printer) return;

            if (window.PrinterDialogue) {
                PrinterDialogue.showEdit(printer);
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
        async function loginWithToken(token) {
            console.log('[main.js] loginWithToken - called with token:', token);

            try {
                const loginResponse = await apiRequest('/api/auth/login', {
                    method: 'POST',
                    body: JSON.stringify({ token }),
                    isTokenLogin: true  // Prevent auto-logout on 401
                });

                console.log('[main.js] loginWithToken - loginResponse:', loginResponse);

                accessToken = loginResponse.accessToken;
                updateWorkspaceToken(token); // This will invalidate cache if token changed
                const workspace = loginResponse.workspace;
                workspaceName = workspace?.name || null;
                workspaceCreatedAt = workspace?.createdAt ? new Date(workspace.createdAt) : new Date();

                console.log('[main.js] loginWithToken - workspace from login response:', workspace);
                console.log('[main.js] loginWithToken - workspaceName set to:', workspaceName);

                localStorage.setItem('accessToken', accessToken);
                if (workspaceName) {
                    localStorage.setItem('workspaceName', workspaceName);
                }
                else {
                    localStorage.removeItem('workspaceName');
                }
                if (workspaceCreatedAt) {
                    localStorage.setItem('workspaceCreatedAt', workspaceCreatedAt.toISOString());
                }

                // Fetch current workspace to confirm auth and get user info
                try {
                    const workspace = await apiRequest('/api/workspaces');
                    console.log('[main.js] loginWithToken - workspace from /api/workspaces:', workspace);
                    if (workspace && workspace.name) {
                        workspaceName = workspace.name;
                        localStorage.setItem('workspaceName', workspaceName);
                        console.log('[main.js] loginWithToken - updating WorkspaceMenu with workspaceName:', workspaceName);
                        window.WorkspaceMenu?.updateDisplay(workspaceToken, workspaceName);
                    }
                } catch (innerError) {
                    console.error('[main.js] loginWithToken - failed to fetch workspace after login:', innerError);
                    // Continue anyway - we have the workspace from the login response
                }

                startStatusStream();
                await loadPrinters();
                if (selectedPrinterId) {
                    await ensureDocumentsLoaded(selectedPrinterId);
                    startDocumentStream(selectedPrinterId);
                    startRuntimeStream(selectedPrinterId);
                }
            } catch (error) {
                console.error('Auth error:', error);
                throw error;
            }
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
            stopDocumentStream();
            stopRuntimeStream();

            invalidateGreetingCache();

            localStorage.removeItem('workspaceToken');
            localStorage.removeItem('workspaceName');
            localStorage.removeItem('accessToken');

            // Hide operations panel when logging out
            const container = document.querySelector('.container');
            container.classList.add('operations-hidden');
            localStorage.setItem('operationsHidden', true);

            WorkspaceMenu.updateDisplay(null, null);
            renderSidebar();
            renderDocuments();
        }

        // Theme Functions
        function isDocumentRawDataActive(doc) {
            // Global raw data overrides per-document selection.
            return debugMode || !!doc.debugEnabled;
        }

        function toggleDocumentDebug(documentId, isEnabled) {
            if (debugMode) {
                // Prevent per-document changes while the global switch is active.
                return;
            }

            const printerId = selectedPrinterId;
            if (!printerId || !documents[printerId]) {
                return;
            }

            const docIndex = documents[printerId].findIndex(doc => doc.id === documentId);
            if (docIndex === -1) {
                return;
            }

            const target = documents[printerId][docIndex];
            const updated = {
                ...target,
                debugEnabled: !!isEnabled
            };

            // Only re-render the requested document for performance.
            if (updated.canvases && updated.canvases.length > 0) {
                // Re-render all canvases with new debug mode
                updated.canvases = updated.canvases.map((canvas, index) => ({
                    ...canvas,
                    previewHtml: DocumentsPanel.renderViewDocument(
                        canvas.elements || [],
                        canvas.width,
                        canvas.heightInDots,
                        `${updated.id}-canvas-${index}`,
                        updated.errorMessages,
                        isDocumentRawDataActive(updated)
                    )
                }));
            } else {
                // Fallback for old single canvas format
                updated.previewHtml = DocumentsPanel.renderViewDocument(
                    updated.elements || [],
                    updated.widthInDots,
                    updated.heightInDots,
                    updated.id,
                    updated.errorMessages,
                    isDocumentRawDataActive(updated)
                );
            }

            documents[printerId][docIndex] = updated;
            renderDocuments();
        }

        function toggleDebugMode() {
            debugMode = !debugMode;

            // Re-render all cached documents with new debug mode
            for (const printerId in documents) {
                const docs = documents[printerId];
                const printer = getPrinterById(printerId);
                if (printer && docs) {
                    documents[printerId] = docs.map(doc => {
                        if (doc.canvases && doc.canvases.length > 0) {
                            // Re-render all canvases with new debug mode
                            return {
                                ...doc,
                                canvases: doc.canvases.map((canvas, index) => ({
                                    ...canvas,
                                    previewHtml: DocumentsPanel.renderViewDocument(
                                        canvas.elements || [],
                                        canvas.width,
                                        canvas.heightInDots,
                                        `${doc.id}-canvas-${index}`,
                                        doc.errorMessages,
                                        isDocumentRawDataActive(doc)
                                    )
                                }))
                            };
                        } else {
                            // Fallback for old single canvas format
                            return {
                                ...doc,
                                previewHtml: DocumentsPanel.renderViewDocument(
                                    doc.elements || [],
                                    doc.widthInDots,
                                    doc.heightInDots,
                                    doc.id,
                                    doc.errorMessages,
                                    isDocumentRawDataActive(doc)
                                )
                            };
                        }
                    });
                }
            }

            renderDocuments();
        }

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
            // Theme icons may not exist if topbar was removed
            if (!darkIcon || !lightIcon) return;

            // Action-oriented: show the icon for the theme you'll switch TO
            if (theme === 'dark') {
                darkIcon.style.display = 'none';
                lightIcon.style.display = 'block';
            } else {
                darkIcon.style.display = 'block';
                lightIcon.style.display = 'none';
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
            console.log('toggleSidebar called');
            console.log('Container before toggle:'+ container.className);

            container.classList.toggle('sidebar-minimized');

            const isHidden = container.classList.contains('sidebar-minimized');
            console.log('Container after toggle:', container.className);
            console.log('Sidebar is hidden:', isHidden);

            const toggleButton = document.getElementById('floatingSidebarToggle');
            console.log('Toggle button:', toggleButton);
            console.log('Toggle button display:', toggleButton ? window.getComputedStyle(toggleButton).display : 'button not found');

            localStorage.setItem('sidebarMinimized', isHidden);
        }
        function toggleOperations() {
            const container = document.querySelector('.container');
            container.classList.toggle('operations-hidden');

            const isHidden = container.classList.contains('operations-hidden');
            localStorage.setItem('operationsHidden', isHidden);
        }

        function openOperationsForPrinter(printerId) {
            // First select the printer
            selectPrinter(printerId);

            // Then ensure operations panel is visible
            const container = document.querySelector('.container');
            if (container.classList.contains('operations-hidden')) {
                container.classList.remove('operations-hidden');
                localStorage.setItem('operationsHidden', false);
            }
        }

        function toggleOperationsForPrinter(printerId) {
            // Don't show operations panel if not logged in
            if (!workspaceToken || !accessToken) {
                return;
            }

            const container = document.querySelector('.container');
            const isHidden = container.classList.contains('operations-hidden');

            // If secondary sidebar is open and clicking the same printer's gear icon
            if (!isHidden && selectedPrinterId === printerId) {
                // Hide secondary sidebar
                container.classList.add('operations-hidden');
                localStorage.setItem('operationsHidden', true);
            } else {
                // Switch to selected printer and open secondary sidebar
                selectPrinter(printerId);
                if (isHidden) {
                    container.classList.remove('operations-hidden');
                    localStorage.setItem('operationsHidden', false);
                }
            }
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
                const response = await fetch('/api/printers/sidebar/stream', {
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

                        if (eventName === 'sidebar' && data) {
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
            const printerId = payload?.printer?.id;
            if (!printerId) return;

            const idx = printers.findIndex(p => p.id === printerId);
            if (idx === -1) return;

            const updated = { ...printers[idx] };
            if (payload.printer?.displayName) {
                updated.name = payload.printer.displayName;
            }
            if (payload.printer?.isPinned !== undefined) {
                updated.pinned = payload.printer.isPinned;
            }

            const runtime = payload.runtimeStatus;
            if (runtime?.state) {
                updated.runtimeStatus = runtime.state.toLowerCase();
            }
            if (runtime?.updatedAt) {
                updated.runtimeStatusAt = new Date(runtime.updatedAt);
            }

            printers[idx] = updated;
            renderSidebar();
        }

        // Initialize
        initTheme();

        // Initialize Operations Panel module
        if (window.OperationsPanel) {
            OperationsPanel.init({
                onClose: () => toggleOperations(),
                onTogglePin: () => togglePin(selectedPrinterId),
                onEdit: () => editPrinter(selectedPrinterId),
                onStartStop: () => {
                    const printer = getPrinterById(selectedPrinterId);
                    if (printer) {
                        const isRunning = printer.runtimeStatus === 'started' || printer.runtimeStatus === 'starting';
                        if (isRunning) {
                            stopPrinter(event, selectedPrinterId);
                        } else {
                            startPrinter(event, selectedPrinterId);
                        }
                    }
                },
                onToggleFlag: (flag, value) => toggleOperationalFlag(selectedPrinterId, flag, value),
                onToggleDebug: (value) => {
                    debugMode = value;
                    // Re-render all cached documents with new debug mode
                    for (const printerId in documents) {
                        const docs = documents[printerId];
                        const printer = getPrinterById(printerId);
                        if (printer && docs) {
                            documents[printerId] = docs.map(doc => {
                                if (doc.canvases && doc.canvases.length > 0) {
                                    // Re-render all canvases with new debug mode
                                    return {
                                        ...doc,
                                        canvases: doc.canvases.map((canvas, index) => ({
                                            ...canvas,
                                            previewHtml: DocumentsPanel.renderViewDocument(
                                                canvas.elements || [],
                                                canvas.width,
                                                canvas.heightInDots,
                                                `${doc.id}-canvas-${index}`,
                                                doc.errorMessages,
                                                isDocumentRawDataActive(doc)
                                            )
                                        }))
                                    };
                                } else {
                                    // Fallback for old single canvas format
                                    return {
                                        ...doc,
                                        previewHtml: DocumentsPanel.renderViewDocument(
                                            doc.elements || [],
                                            doc.widthInDots,
                                            doc.heightInDots,
                                            doc.id,
                                            doc.errorMessages,
                                            isDocumentRawDataActive(doc)
                                        )
                                    };
                                }
                            });
                        }
                    }
                    renderDocuments();
                },
                onGetDebugMode: () => debugMode,
                onToggleDrawer: (drawerNumber) => {
                    const state = drawerNumber === 1
                        ? (getPrinterById(selectedPrinterId)?.drawer1State)
                        : (getPrinterById(selectedPrinterId)?.drawer2State);
                    const newState = (state === 'Closed' || !state) ? 'OpenedManually' : 'Closed';
                    const drawerProp = `drawer${drawerNumber}State`;
                    setDrawerState(selectedPrinterId, drawerProp, newState);
                },
                onClearDocuments: () => clearDocuments(selectedPrinterId),
                onDeletePrinter: () => deletePrinter(selectedPrinterId),
                onCopyAddress: (address) => copyToClipboard(address)
            });
        }

        // Initialize Workspace Menu module
        if (window.WorkspaceMenu) {
            WorkspaceMenu.init({
                workspaceToken: () => workspaceToken,
                workspaceName: () => workspaceName,
                onLogOut: () => logOut(),
                onShowWorkspaceDialog: (mode) => WorkspaceDialog.show(mode),
                onShowWorkspaceSettings: () => {
                    if (window.WorkspaceSettingsDialog) {
                        WorkspaceSettingsDialog.show();
                    }
                },
                onOpenDocs: (doc) => window.open(`/docs/${doc}`, '_blank')
            });
        }

        // Initialize Workspace Dialog module
        if (window.WorkspaceDialog) {
            WorkspaceDialog.init({
                apiRequest: (path, options) => apiRequest(path, options),
                loginWithToken: (token) => loginWithToken(token),
                closeModal: () => closeModal(),
                showToast: (msg, isError) => showToast(msg, isError),
                onWorkspaceCreated: (token, name) => {
                    console.log('[main.js] onWorkspaceCreated - token:', token, 'name:', name);
                    updateWorkspaceToken(token);
                    workspaceName = name;
                    console.log('[main.js] onWorkspaceCreated - workspaceName set to:', workspaceName);
                    WorkspaceMenu.updateDisplay(token, workspaceName);
                    renderSidebar();
                    renderDocuments();
                },
                onWorkspaceAccessed: (token) => {
                    WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                    renderSidebar();
                    renderDocuments();
                }
            });
        }

        // Initialize Workspace Settings Dialog module
        if (window.WorkspaceSettingsDialog) {
            WorkspaceSettingsDialog.init({
                apiRequest: (path, options) => apiRequest(path, options),
                closeModal: () => closeModal(),
                showToast: (msg, isError) => showToast(msg, isError),
                workspaceName: () => workspaceName,
                onWorkspaceUpdated: (settings) => {
                    workspaceName = settings.name;
                    if (settings.name) {
                        localStorage.setItem('workspaceName', settings.name);
                    }
                    WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                },
                onWorkspaceDeleted: () => {
                    logOut();
                }
            });
        }

        // Initialize Printer Dialogue module
        if (window.PrinterDialogue) {
            PrinterDialogue.init({
                apiRequest: (path, options) => apiRequest(path, options),
                normalizeProtocol: (protocol) => normalizeProtocol(protocol),
                loadPrinters: (selectId) => loadPrinters(selectId),
                closeModal: () => closeModal(),
                showToast: (msg, isError) => showToast(msg, isError)
            });
        }

        // Initialize Documents Panel module
        if (window.DocumentsPanel) {
            DocumentsPanel.init({
                onCreateWorkspace: () => WorkspaceDialog?.show?.('create'),
                onAccessWorkspace: () => WorkspaceDialog?.show?.('access'),
                onToggleDocumentDebug: (docId, enabled) => toggleDocumentDebug(docId, enabled),
                onCopyDocument: (text) => copyToClipboard(text),
                getWelcomeMessage: () => getWelcomeMessage(),
                getDebugMode: () => debugMode,
                getPrinterById: (id) => getPrinterById(id),
                isDocumentRawDataActive: (doc) => isDocumentRawDataActive(doc),
                escapeHtml: (text) => escapeHtml(text),
                resolveMediaUrl: (url) => resolveMediaUrl(url)
            });
        }

        // Restore workspace
        const savedToken = localStorage.getItem('workspaceToken');
        const savedName = localStorage.getItem('workspaceName');
        const savedAccessToken = localStorage.getItem('accessToken');
        const savedCreatedAt = localStorage.getItem('workspaceCreatedAt');

        console.log('Restoring workspace from localStorage:', {
            hasWorkspaceToken: !!savedToken,
            hasAccessToken: !!savedAccessToken,
            workspaceName: savedName
        });

        if (savedToken && savedAccessToken) {
            workspaceToken = savedToken;
            workspaceName = savedName;
            workspaceCreatedAt = savedCreatedAt ? new Date(savedCreatedAt) : null;
            accessToken = savedAccessToken;
            console.log('Workspace restored, verifying auth and loading data');

            // Verify auth and get workspace info
            (async () => {
                try {
                    const workspaceInfo = await apiRequest('/api/workspaces');
                    if (workspaceInfo && workspaceInfo.name) {
                        workspaceName = workspaceInfo.name;
                        localStorage.setItem('workspaceName', workspaceName);
                        WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                    }
                    startStatusStream();
                    loadPrinters();
                } catch (error) {
                    console.error('Auth verification failed on startup:', error);
                    logOut();
                }
            })();
        } else {
            console.warn('Cannot restore workspace - missing tokens');
        }

        WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
        renderSidebar();
        renderDocuments();

        // Restore sidebar state
        const sidebarMinimized = localStorage.getItem('sidebarMinimized') === 'true';
        if (sidebarMinimized) {
            document.querySelector('.container').classList.add('sidebar-minimized');
        }

        // Restore operations panel state
        // Always hide if not logged in, otherwise restore from localStorage (default: hidden)
        if (!workspaceToken || !accessToken) {
            document.querySelector('.container').classList.add('operations-hidden');
        } else {
            const operationsHidden = localStorage.getItem('operationsHidden');
            if (operationsHidden === null || operationsHidden === 'true') {
                document.querySelector('.container').classList.add('operations-hidden');
            }
        }
