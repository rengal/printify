
        console.info('main.js loaded - revision 2', '2025-02-20T12:00:00Z');

        // API + Workspace State
        const apiBase = '';
        let workspaceToken = null;
        let workspaceName = null;
        let workspaceCreatedAt = null;
        let accessToken = null;
        let workspaceSummary = null;

        // Data
        let printers = [];
        let documents = {};
        let selectedPrinterId = null;
        let statusStreamController = null;
        let documentStreamController = null;
        let documentStreamPrinterId = null;
        let debugMode = false;

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
                // Handle 401/403 - authentication/authorization failures
                if (response.status === 401 || response.status === 403) {
                    console.error(`Auth failed (${response.status}) for ${path}, logging out`);
                    // Only log out if we have a workspace token (avoid loops)
                    if (workspaceToken) {
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

        function getPrinterById(id) {
            return printers.find(p => p.id === id) || null;
        }

        async function loadWorkspaceSummary() {
            try {
                workspaceSummary = await apiRequest('/api/workspaces/summary');
                console.debug('loadWorkspaceSummary fetched', workspaceSummary);
            } catch (err) {
                console.error('Failed to load workspace summary:', err);
                workspaceSummary = null;
            }
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
                statusIcon = '<svg class="stopped-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="2.5" stroke-linecap="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>';
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
                renderDocuments();
                try {
                    await ensureDocumentsLoaded(id);
                    startDocumentStream(id);
                } catch (err) {
                    console.error('Failed to load documents', err);
                    showToast('Unable to load documents', true);
                }
                renderDocuments();
            }
        }

        function getDefaultState() {
            return {
                bold: false,
                underline: false,
                reverse: false,
                justify: 'left',
                lineSpacing: 0,
                lineInterval: 4, // Vertical spacing between lines in pixels
                fontNumber: 0, // 0 = Font A, 1 = Font B
                scaleX: 1,
                scaleY: 1
            };
        }

        function renderEscPosDocument(elements, printerWidth) {
            const state = getDefaultState();
            const rows = []; // Array of {lineHtml, element}
            const width = Math.max(printerWidth || 384, 200);
            let lineBuffer = '';

            for (const element of elements || []) {
                const elementType = (element?.type || '').toLowerCase();

                switch (elementType) {
                    case 'appendtolinebuffer':
                        lineBuffer += element.text || '';
                        if (debugMode) {
                            rows.push({ lineHtml: '', element: element });
                        }
                        break;
                    case 'flushlinebufferandfeed':
                        rows.push({
                            lineHtml: renderTextLine(lineBuffer, state),
                            element: element
                        });
                        lineBuffer = '';
                        break;
                    case 'rasterimage':
                        rows.push({
                            lineHtml: renderRasterImage(element, state),
                            element: element
                        });
                        break;
                    case 'setjustification':
                        state.justify = (element.justification || 'left').toLowerCase();
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'setboldmode':
                        state.bold = !!element.isEnabled;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'setunderlinemode':
                        state.underline = !!element.isEnabled;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'setreversemode':
                        state.reverse = !!element.isEnabled;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'setlinespacing':
                        state.lineSpacing = Number(element.spacing) || 0;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'resetlinespacing':
                        state.lineSpacing = 0;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'setfont':
                        // Font A (0) or Font B (1)
                        state.fontNumber = Number(element.fontNumber) || 0;
                        state.scaleX = element.isDoubleWidth ? 2 : 1;
                        state.scaleY = element.isDoubleHeight ? 2 : 1;
                        rows.push({ lineHtml: '', element: element });
                        break;
                    case 'resetprinter':
                        // ESC @ - Reset printer to power-on state
                        Object.assign(state, getDefaultState());
                        rows.push({ lineHtml: '', element: element });
                        break;
                    default:
                        rows.push({ lineHtml: '', element: element });
                        break;
                }
            }

            if (rows.length === 0) {
                rows.push({
                    lineHtml: '<div class="doc-line muted">No printable text</div>',
                    element: null
                });
            }

            if (debugMode) {
                // Render with debug table above each element
                const rowsHtml = rows.map(row => {
                    const debugTable = row.element ? renderDebugTable(row.element) : '';
                    return debugTable + row.lineHtml;
                }).join('');
                return `<div class="document-paper" style="width:${width}px">${rowsHtml}</div>`;
            } else {
                // Normal rendering without debug
                const linesHtml = rows.map(row => row.lineHtml).join('');
                return `<div class="document-paper" style="width:${width}px">${linesHtml}</div>`;
            }
        }

        function renderDebugTable(element) {
            const commandRaw = element.commandRaw || '';
            const commandDescription = Array.isArray(element.commandDescription)
                ? element.commandDescription.join('\n')
                : (element.commandDescription || '');

            // Format hex command with spaces
            const hexFormatted = formatHexCommand(commandRaw);

            // Truncate long text in descriptions
            const descFormatted = truncateTextInDescription(commandDescription);

            return `
                <table class="debug-table">
                    <tr>
                        <td class="debug-hex">${hexFormatted}</td>
                          <td class="debug-desc">${escapeHtml(descFormatted).replace(/\n/g, '<br>') || '<span class="debug-missing">??</span>'}</td>
                    </tr>
                </table>
            `;
        }

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

        function formatHexCommand(commandRaw) {
            if (!commandRaw || commandRaw.trim() === '') {
                return '<span class="debug-missing">??</span>';
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

        function renderRasterImage(element, state) {
            const mediaUrl = resolveMediaUrl(element?.media?.url || '');
            if (!mediaUrl) {
                return '<div class="doc-line muted">[Image unavailable]</div>';
            }

            const justify = (state.justify || 'left').toLowerCase();
            const justifyContent = justify === 'right'
                ? 'flex-end'
                : justify === 'center'
                    ? 'center'
                    : 'flex-start';

            const bottomMargin = Math.max(0, (state.lineInterval || 0) + (state.lineSpacing || 0));
            const wrapperStyles = [`justify-content: ${justifyContent}`];
            if (bottomMargin > 0) {
                wrapperStyles.push(`margin-bottom: ${bottomMargin}px`);
            }

            const width = Number(element.width) || 0;
            const height = Number(element.height) || 0;
            const imageStyles = ['max-width: 100%', 'height: auto'];
            if (width > 0) {
                imageStyles.push(`width: ${width}px`);
            }

            if (height > 0) {
                imageStyles.push(`height: ${height}px`);
            }

            const altText = width > 0 && height > 0
                ? `Raster image ${width}x${height}`
                : 'Raster image';

            return `<div class="doc-image-row" style="${wrapperStyles.join(';')}">` +
                `<img class="doc-image" src="${escapeHtml(mediaUrl)}" alt="${escapeHtml(altText)}" ` +
                `style="${imageStyles.join(';')}" loading="lazy"></div>`;
        }

        function renderTextLine(text, state) {
            const classes = ['doc-line'];
            if (state.bold) classes.push('bold');
            if (state.underline) classes.push('underline');
            if (state.reverse) classes.push('reverse');
            classes.push(`justify-${state.justify || 'left'}`);

            // Font A or Font B
            if (state.fontNumber === 1) {
                classes.push('font-b');
            } else {
                classes.push('font-a');
            }

            const styles = [];

            // Calculate base line height based on font
            // Font A: 12×24 dots, Font B: 9×17 dots
            const baseLineHeight = state.fontNumber === 1 ? 17 : 24;

            // Calculate total bottom margin: lineInterval + scaling margin + lineSpacing
            let bottomMargin = state.lineInterval || 0;

            if (state.scaleX !== 1 || state.scaleY !== 1) {
                styles.push(`transform: scale(${state.scaleX}, ${state.scaleY})`);
                styles.push('transform-origin: left top');
                // Add margin to account for scaled height to prevent overlap
                const scaledLineHeight = baseLineHeight * state.scaleY;
                const extraMargin = Math.max(0, scaledLineHeight - baseLineHeight);
                bottomMargin += extraMargin;
            }

            if (state.lineSpacing > 0) {
                bottomMargin += state.lineSpacing;
            }

            if (bottomMargin > 0) {
                styles.push(`margin-bottom: ${bottomMargin}px`);
            }

            const textContent = escapeHtml(text);

            return `<div class="${classes.join(' ')}"${styles.length ? ` style="${styles.join(';')}"` : ''}>${textContent}</div>`;
        }

        function extractDocumentText(elements) {
            const lines = [];
            let lineBuffer = '';
            for (const element of elements || []) {
                const elementType = (element?.type || '').toLowerCase();
                if (elementType === 'appendtolinebuffer') {
                    lineBuffer += element.text || '';
                } else if (elementType === 'flushlinebufferandfeed') {
                    lines.push(lineBuffer);
                    lineBuffer = '';
                }
            }
            return lines.join('\n');
        }

        function mapDocumentDto(dto) {
            const width = Number(dto.widthInDots) || 384;
            const height = dto.heightInDots ?? null;
            const protocol = (dto.protocol || 'escpos').toLowerCase();
            const elements = dto.elements || [];
            const previewHtml = renderEscPosDocument(elements, width);
            const plainText = extractDocumentText(elements);
            return {
                id: dto.id,
                printerId: dto.printerId,
                timestamp: dto.timestamp ? new Date(dto.timestamp) : new Date(),
                protocol,
                width,
                widthInDots: width,
                heightInDots: height,
                elements, // Store raw elements for re-rendering
                previewHtml,
                plainText
            };
        }

        async function ensureDocumentsLoaded(printerId) {
            if (documents[printerId]) {
                return;
            }

            console.debug('Loading documents for printer', printerId);
            const response = await apiRequest(`/api/printers/${printerId}/documents?limit=50`);
            const items = response?.result?.items || [];
            documents[printerId] = items.map(dto => mapDocumentDto(dto));
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
                const response = await fetch(`/api/printers/${printerId}/documents/stream`, {
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

                        if (eventName === 'documentReady' && data) {
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
                const mapped = mapDocumentDto(payload);
                const list = documents[printerId] || [];
                list.unshift(mapped);
                documents[printerId] = list.slice(0, 200);

                if (selectedPrinterId !== printerId) {
                    const target = getPrinterById(printerId);
                    if (target) target.newDocs += 1;
                }

                renderDocuments();
                renderSidebar();
            } catch (e) {
                console.error('Failed to parse document event', e);
            }
        }

        function renderDocuments() {
            const operationsPanel = document.getElementById('operationsPanel');
            const documentsPanel = document.getElementById('documentsPanel');

            if (!workspaceToken) {
                operationsPanel.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h3>No Workspace</h3>
                <p>Create or access workspace</p>
              </div>
            `;
                documentsPanel.innerHTML = `
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
                operationsPanel.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h3>No Printer Selected</h3>
                <p>Select a printer from the list</p>
              </div>
            `;
                documentsPanel.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h2>${getWelcomeMessage(workspaceName, workspaceSummary)}</h2>
                <p>Select a printer to view documents</p>
              </div>
            `;
                return;
            }

            const docs = documents[selectedPrinterId] || [];
            const printer = getPrinterById(selectedPrinterId);

            if (!printer) {
                operationsPanel.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h3>Printer not found</h3>
              </div>
            `;
                documentsPanel.innerHTML = '';
                return;
            }

            // Render printer info in properties panel
            // Use runtimeStatus to determine button state (not targetStatus)
            // If printer is actually running (started/starting), show Stop button
            // If printer is stopped/error, show Start button
            const isRunning = printer.runtimeStatus === 'started' || printer.runtimeStatus === 'starting';
            const statusClass = runtimeStatusClass(printer.runtimeStatus);
            const statusText = formatRuntimeStatus(printer.runtimeStatus);
            const lastDocText = printer.lastDocumentAt ? formatRelativeTime(printer.lastDocumentAt) : 'Never';
            const printerAddress = formatPrinterAddress(printer);
            const protocolFormatted = printer.protocol.toLowerCase() === 'escpos' ? 'ESC/POS' : printer.protocol.toUpperCase();

            // Debug logging
            console.log('Printer status debug:', {
                printerName: printer.name,
                targetStatus: printer.targetStatus,
                runtimeStatus: printer.runtimeStatus,
                isRunning: isRunning,
                buttonToShow: isRunning ? 'Stop' : 'Start',
                displayedStatus: statusText
            });

            const hasLastDocument = !!printer.lastDocumentAt;
            const lastDocDateTime = hasLastDocument
                ? printer.lastDocumentAt.toLocaleString(undefined, {
                    year: 'numeric', month: '2-digit', day: '2-digit',
                    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
                  })
                : '';
            const lastDocRelative = hasLastDocument ? lastDocText : '';

            operationsPanel.innerHTML = `
              <div class="operations-header">
                <span class="operations-printer-name">${escapeHtml(printer.name)}</span>
                <button class="icon-btn" onclick="toggleOperations()" title="Close operations">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="6" x2="6" y2="18"></line>
                    <line x1="6" y1="6" x2="18" y2="18"></line>
                  </svg>
                </button>
              </div>
              <div class="operations-info">
                <div class="operations-table">
                  <div class="operations-row">
                    <div class="operations-cell operations-cell-label">
                      <span class="${statusClass}">${statusText}</span>
                    </div>
                    <div class="operations-cell operations-cell-value">
                      <div>
                        <span>${printerAddress}</span>
                        <button class="copy-icon-btn" onclick="copyToClipboard('${printerAddress}')" title="Copy address">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="operations-last-doc">
                  <div class="operations-last-doc-label">Last document:</div>
                  <div class="operations-last-doc-value">${hasLastDocument
                    ? `${lastDocDateTime} (${lastDocRelative})`
                    : `-`}</div>
                </div>
              </div>
              <div class="operations-actions">
                ${isRunning
                  ? `<button class="btn btn-outline-danger btn-sm" onclick="stopPrinter(event, '${printer.id}')">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="6" width="12" height="12" rx="1"/></svg>
                      Stop
                    </button>`
                  : `<button class="btn btn-primary btn-sm" onclick="startPrinter(event, '${printer.id}')">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                      Start
                    </button>`
                }
                <button class="btn btn-secondary btn-sm" onclick="clearDocuments('${printer.id}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                  Clear
                </button>
                <button class="btn btn-${debugMode ? 'primary' : 'secondary'} btn-sm" onclick="toggleDebugMode()">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 9h.01M15 9h.01M9 15h6"/></svg>
                  Debug
                </button>
                <button class="btn btn-secondary btn-sm" onclick="editPrinter('${printer.id}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                  Edit
                </button>
                <button class="btn btn-ghost btn-sm" onclick="togglePin('${printer.id}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2l2.4 7.4h7.6l-6 4.6 2.3 7-6.3-4.6-6.3 4.6 2.3-7-6-4.6h7.6z"/></svg>
                  ${printer.pinned ? 'Unpin' : 'Pin'}
                </button>
                <button class="btn btn-ghost btn-sm" onclick="deletePrinter('${printer.id}')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                  Delete
                </button>
              </div>
            `;

            // Render documents in documents panel
            if (docs.length === 0) {
                documentsPanel.innerHTML = `
              <div style="text-align: center; padding: 60px 20px; color: var(--muted);">
                <h3>No documents yet</h3>
                <p>Documents will appear here when they are printed</p>
              </div>
            `;
                return;
            }

            const documentsHtml = docs.map(doc => {
                const dateTime = doc.timestamp.toLocaleString(undefined, {
                    year: 'numeric', month: '2-digit', day: '2-digit',
                    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
                });
                const relativeTime = formatRelativeTime(doc.timestamp);

                return `
                <div class="document-item">
                  <div class="document-content">
                    <div class="document-header">
                      <span class="document-meta-text">${dateTime} · ${relativeTime}</span>
                      <button class="copy-icon-btn document-copy-btn" onclick="copyToClipboard(\`${doc.plainText.replace(/\`/g, '\\\\`')}\`)" title="Copy document content">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                          <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                      </button>
                    </div>
                    ${doc.previewHtml}
                  </div>
                </div>
              `;
            }).join('');

            documentsPanel.innerHTML = documentsHtml;
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
            if (!confirm('Delete this printer?')) {
                return;
            }

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

        function clearDocuments(printerId) {
            // TODO: Add clear documents API endpoint
            if (!confirm('Clear all documents for this printer?')) {
                return;
            }
            showToast('Clear documents operation - API endpoint not implemented yet', 'warn');
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
            workspaceToken = token;
            const workspace = loginResponse.workspace;
            workspaceName = workspace?.ownerName || null;
            workspaceCreatedAt = workspace?.createdAt ? new Date(workspace.createdAt) : new Date();

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
                const userInfo = await apiRequest('/api/auth/me');
                if (userInfo && userInfo.name) {
                    workspaceName = userInfo.name;
                    localStorage.setItem('workspaceName', workspaceName);
                    updateUserDisplay();
                }
            startStatusStream();
            await loadWorkspaceSummary();
            await loadPrinters();
            if (selectedPrinterId) {
                await ensureDocumentsLoaded(selectedPrinterId);
                startDocumentStream(selectedPrinterId);
            }
            } catch (error) {
                console.error('Auth error:', error);
                // Auth failed, log out
                logOut();
            }
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
            stopDocumentStream();

            localStorage.removeItem('workspaceToken');
            localStorage.removeItem('workspaceName');
            localStorage.removeItem('accessToken');

            updateUserDisplay();
            renderSidebar();
            renderDocuments();
        }

        // Theme Functions
        function toggleDebugMode() {
            debugMode = !debugMode;

            // Re-render all cached documents with new debug mode
            for (const printerId in documents) {
                const docs = documents[printerId];
                const printer = getPrinterById(printerId);
                if (printer && docs) {
                    documents[printerId] = docs.map(doc => ({
                        ...doc,
                        previewHtml: renderEscPosDocument(doc.elements || [], doc.width)
                    }));
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

            // Verify auth and get user info
            (async () => {
                try {
                    const userInfo = await apiRequest('/api/auth/me');
                    if (userInfo && userInfo.name) {
                        workspaceName = userInfo.name;
                        localStorage.setItem('workspaceName', workspaceName);
                        updateUserDisplay();
                    }
                    startStatusStream();
                    loadWorkspaceSummary();
                    loadPrinters();
                } catch (error) {
                    console.error('Auth verification failed on startup:', error);
                    logOut();
                }
            })();
        } else {
            console.warn('Cannot restore workspace - missing tokens');
        }

        updateUserDisplay();
        renderSidebar();
        renderDocuments();

        // Restore sidebar state
        const sidebarMinimized = localStorage.getItem('sidebarMinimized') === 'true';
        if (sidebarMinimized) {
            document.querySelector('.container').classList.add('sidebar-minimized');
        }
    
        // Restore operations panel state (default: hidden)
        const operationsHidden = localStorage.getItem('operationsHidden');
        // If not set in localStorage, default to hidden
        if (operationsHidden === null || operationsHidden === 'true') {
            document.querySelector('.container').classList.add('operations-hidden');
        }
