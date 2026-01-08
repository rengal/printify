
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
        let runtimeStreamController = null;
        let runtimeStreamPrinterId = null;
        let debugMode = false;

        // Icon cache
        const iconCache = {};

        async function loadIcon(name) {
            if (iconCache[name]) {
                return iconCache[name];
            }
            try {
                const response = await fetch(`assets/icons/${name}.svg`);
                const svgText = await response.text();
                iconCache[name] = svgText;
                return svgText;
            } catch (err) {
                console.error(`Failed to load icon ${name}:`, err);
                return '';
            }
        }

        function getIcon(name, options = {}) {
            const svg = iconCache[name] || '';
            if (!svg) return '';

            let result = svg;
            if (options.width) {
                result = result.replace(/width="[^"]*"/, `width="${options.width}"`);
            }
            if (options.height) {
                result = result.replace(/height="[^"]*"/, `height="${options.height}"`);
            }
            if (options.stroke) {
                result = result.replace(/stroke="[^"]*"/g, `stroke="${options.stroke}"`);
            }
            if (options.class) {
                result = result.replace(/<svg/, `<svg class="${options.class}"`);
            }
            return result;
        }

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

        // NEW: Render ViewDocument with absolute positioning
        function renderViewDocument(elements, documentWidth, documentHeight, docId, errorMessages, includeDebug) {
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

        // Render individual ViewElement
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

        // Render text element with absolute positioning
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

            const textContent = escapeHtml(text);

            return `<div id="${id}" data-element-type="text" data-original-y="${y}"><div class="view-text ${fontClass}" style="${styles.join('; ')};">${textContent}</div></div>`;
        }

        // Render image element with absolute positioning
        function renderViewImageElement(element, id) {
            const x = Number(element.x) || 0;
            const y = Number(element.y) || 0;
            const width = Number(element.width) || 0;
            const height = Number(element.height) || 0;
            const zIndex = Number(element.zIndex) || 0;

            const mediaUrl = resolveMediaUrl(element?.media?.url || '');
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

            return `<div id="${id}" data-element-type="image" data-original-y="${y}"><img class="view-image" src="${escapeHtml(mediaUrl)}" alt="${escapeHtml(altText)}" style="${styles.join('; ')};" loading="lazy"></div>`;
        }

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

        // Extract plain text from ViewDocument elements
        function extractViewDocumentText(elements) {
            return (elements || [])
                .filter(el => el.type === 'text')
                .map(el => el.text || '')
                .join('\n');
        }

        // Adjust Y positions in debug mode to account for debug table heights
        function adjustDebugYPositions(contentId, includeDebug) {
            if (!includeDebug) return;

            const container = document.getElementById(contentId);
            if (!container) return;

            // Get all element wrappers in DOM order (backend provides correct order)
            const elements = Array.from(container.querySelectorAll('[data-original-y]'));

            let currentY = 0; // Track current vertical position in the adjusted document

            elements.forEach((wrapper, index) => {
                const elementType = wrapper.getAttribute('data-element-type') || 'unknown';

                if (elementType === 'debug') {
                    // Debug-only element (type=debug or type=none from backend)
                    const debugTable = wrapper.querySelector('.debug-table');
                    if (debugTable) {
                        debugTable.style.top = `${currentY}px`;
                        const debugHeight = debugTable.offsetHeight || 20;
                        const debugDesc = debugTable.querySelector('.debug-desc')?.textContent?.trim() || '';

                        console.log(`[${index}] debug | Y=${currentY}px H=${debugHeight}px | ${debugDesc.substring(0, 50)}`);

                        currentY += debugHeight;
                    }
                } else if (elementType === 'text' || elementType === 'image') {
                    // Visual element (text or image)
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

            // Adjust container height to include all debug info
            const originalHeight = parseInt(container.style.height) || 0;
            if (currentY > originalHeight) {
                container.style.height = `${currentY}px`;
            }
        }

        // Map ViewDocumentDto to internal document object
        function mapViewDocumentDto(dto) {
            const width = Number(dto.widthInDots) || 384;
            const height = dto.heightInDots ?? null;
            const protocol = (dto.protocol || 'escpos').toLowerCase();
            const elements = dto.elements || [];
            const docId = dto.id || `doc-${Date.now()}`;
            const errorMessages = dto.errorMessages || null;
            // Respect the global debug toggle for newly loaded documents.
            const previewHtml = renderViewDocument(elements, width, height, docId, errorMessages, debugMode);
            const plainText = extractViewDocumentText(elements);
            return {
                id: dto.id,
                printerId: dto.printerId,
                timestamp: dto.timestamp ? new Date(dto.timestamp) : new Date(),
                errorMessages: errorMessages,
                protocol,
                width,
                widthInDots: width,
                heightInDots: height,
                bytesReceived: dto.bytesReceived ?? 0,
                bytesSent: dto.bytesSent ?? 0,
                elements, // Store raw elements for re-rendering
                debugEnabled: false,
                previewHtml,
                plainText
            };
        }

        async function ensureDocumentsLoaded(printerId) {
            if (documents[printerId]) {
                return;
            }

            console.debug('Loading documents for printer', printerId);
            const response = await apiRequest(`/api/printers/${printerId}/documents/view?limit=50`);
            const items = response?.result?.items || [];
            documents[printerId] = items.map(dto => mapViewDocumentDto(dto));
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
                const response = await fetch(`/api/printers/${printerId}/documents/view/stream`, {
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

                        if (eventName === 'documentViewReady' && data) {
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
                const mapped = mapViewDocumentDto(payload);
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
              <div style="max-width: 600px; margin: 30px auto; text-align: center;">
                <h1 style="margin-bottom: 12px;">Printer Management System</h1>
                <p style="color: var(--muted); font-size: 16px; margin: 0 0 24px; line-height: 1.5;">
                  Manage receipt and label printers with real-time document streaming
                </p>

                <div style="text-align: left; background: var(--bg-elev); border: 1px solid var(--border); border-radius: 12px; padding: 20px; margin-bottom: 20px;">
                  <h3 style="margin-top: 0; margin-bottom: 12px;">Features</h3>
                  <ul style="color: var(--muted); line-height: 1.6; padding-left: 24px; margin: 0;">
                    <li>Configure multiple printers with ESC/POS and ZPL protocols</li>
                    <li>Monitor print jobs in real-time with document preview</li>
                    <li>Replay and download previously printed documents</li>
                    <li>Share printers across devices with your workspace token</li>
                  </ul>
                </div>

                <div style="background: rgba(16,185,129,0.1); border: 1px solid var(--accent); border-radius: 12px; padding: 20px;">
                  <h3 style="margin-top: 0; margin-bottom: 8px; color: var(--accent);">Get Started</h3>
                  <p style="color: var(--muted); margin-bottom: 16px;">
                    Create a new workspace or access an existing one
                  </p>
                  <button class="btn btn-primary" onclick="WorkspaceDialog.show()">Create or Access Workspace</button>
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

            // Note: Operations panel is now rendered by OperationsPanel module
            // This function only handles the documents panel rendering

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
                  const byteCount = formatByteCount(doc.bytesReceived);

                // Check if document has errors
                  const hasErrors = doc.errorMessages && doc.errorMessages.length > 0;
                  const errorTooltip = hasErrors ? doc.errorMessages.join('\n') : '';
                  const errorTooltipHtml = escapeHtml(errorTooltip).replace(/\n/g, '&#10;');
                  const errorIcon = hasErrors ? `
                    <img class="document-error-icon" src="assets/icons/alert-triangle.svg" alt="Error" title="${errorTooltipHtml}">
                  ` : '';
                  return `
                <div class="document-item">
                    <div class="document-gutter document-gutter-header">
                    </div>
                    <div class="document-header">
                      <span class="document-meta-text">${dateTime} &middot; ${relativeTime}</span>
                      <label class="flag-switch document-debug-switch" title="Show raw data for this document">
                        <input type="checkbox" ${doc.debugEnabled ? 'checked' : ''} ${debugMode ? 'disabled' : ''}
                          onchange="toggleDocumentDebug('${doc.id}', this.checked)">
                        <span class="flag-label">Raw Data</span>
                      </label>
                    </div>
                    <div class="document-gutter document-gutter-preview">
                      ${errorIcon}
                    </div>
                    <div class="document-preview-wrap">
                      ${doc.previewHtml}
                    </div>
                    <div class="document-footer">
                      <span class="document-meta-text document-footer-text">Size: ${byteCount} bytes</span>
                      <button class="copy-icon-btn document-copy-btn" onclick="copyToClipboard(\`${doc.plainText.replace(/\`/g, '\\`')}\`)" title="Copy document content">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                          <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                      </button>
                    </div>
                  </div>
                `;
              }).join('');

            documentsPanel.innerHTML = documentsHtml;

            // Adjust Y positions in debug mode after DOM insertion
              const debugDocs = docs.filter(doc => isDocumentRawDataActive(doc));
              if (debugDocs.length > 0) {
                  // Use requestAnimationFrame to ensure DOM is fully rendered
                  requestAnimationFrame(() => {
                      debugDocs.forEach(doc => {
                          const contentId = `doc-content-${doc.id}`;
                          console.log(`\n=== Adjusting debug positions for document ${doc.id} ===`);
                          adjustDebugYPositions(contentId, true);
                      });
                  });
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
                        <option value="escpos">ESC/POS emulation</option>
                      </select>
                    </div>

                  <div class="field">
                    <label class="label">Width (dots)</label>
                    <input class="input" id="printerWidth" type="number" value="512" />
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

                <div style="background: rgba(239,68,68,0.08); border: 1px solid var(--danger); border-radius: 10px; padding: 12px; margin: 16px 0 12px 0;">
                  <div class="checkbox-field" style="margin-bottom: 0; align-items: flex-start;">
                    <input type="checkbox" id="securityAck" style="width: 18px; height: 18px; margin-top: 2px; flex-shrink: 0;" />
                    <label for="securityAck" style="color: var(--text); line-height: 1.4; font-size: 13px;">
                      I understand this printer uses raw TCP connection without encryption and transmitted data may be intercepted or modified. This is for testing/development only. I will not send sensitive data to this printer. <a href="/docs/security" target="_blank" style="color: var(--danger); text-decoration: underline;">Learn more</a>
                    </label>
                  </div>
                  <div class="field-error" id="securityAckError" style="margin-top: 6px; margin-left: 26px;">You must acknowledge the security notice</div>
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
                      <select class="input" id="printerProtocol" disabled>
                      <option value="escpos" ${printer.protocol.toLowerCase() === 'escpos' ? 'selected' : ''}>ESC/POS emulation</option>
                      </select>
                      <div class="field-hint">Protocol cannot be changed after creation</div>
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
            const width = parseInt(document.getElementById('printerWidth').value) || 512;
            const emulateBuffer = document.getElementById('emulateBuffer').checked;
            const bufferSize = parseInt(document.getElementById('bufferSize').value) || 4096;
            const drainRate = parseInt(document.getElementById('drainRate').value) || 4096;
            const securityAck = document.getElementById('securityAck').checked;

            const nameInput = document.getElementById('printerName');
            const nameError = document.getElementById('nameError');
            const securityAckError = document.getElementById('securityAckError');

            nameInput.classList.remove('invalid');
            nameError.classList.remove('show');
            securityAckError.classList.remove('show');

            if (!name) {
                nameInput.classList.add('invalid');
                nameError.classList.add('show');
                nameInput.focus();
                return;
            }

            if (!securityAck) {
                securityAckError.classList.add('show');
                document.getElementById('securityAck').focus();
                return;
            }

            try
            {
                const request = {
                    printer: {
                        id: crypto.randomUUID(),
                        displayName: name
                    },
                    settings: {
                        protocol: normalizeProtocol(protocol),
                        widthInDots: width,
                        heightInDots: null,
                        emulateBufferCapacity: emulateBuffer,
                        bufferDrainRate: drainRate,
                        bufferMaxCapacity: bufferSize
                    }
                };

                const created = await apiRequest('/api/printers', {
                    method: 'POST',
                    body: JSON.stringify(request)
                });

                await loadPrinters(created.printer.id);
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
            const width = parseInt(document.getElementById('printerWidth').value) || 512;
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
                    printer: {
                        id: printerId,
                        displayName: name
                    },
                    settings: {
                        protocol: normalizeProtocol(protocol),
                        widthInDots: width,
                        heightInDots: null,
                        emulateBufferCapacity: emulateBuffer,
                        bufferDrainRate: drainRate,
                        bufferMaxCapacity: bufferSize
                    }
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
                    WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                }
            startStatusStream();
            await loadWorkspaceSummary();
            await loadPrinters();
            if (selectedPrinterId) {
                await ensureDocumentsLoaded(selectedPrinterId);
                startDocumentStream(selectedPrinterId);
                startRuntimeStream(selectedPrinterId);
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
            stopRuntimeStream();

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
            updated.previewHtml = renderViewDocument(
                updated.elements || [],
                updated.widthInDots,
                updated.heightInDots,
                updated.id,
                updated.errorMessages,
                isDocumentRawDataActive(updated)
            );

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
                    documents[printerId] = docs.map(doc => ({
                        ...doc,
                        // Ensure global debug applies to every document, while preserving per-doc toggles.
                        previewHtml: renderViewDocument(
                            doc.elements || [],
                            doc.widthInDots,
                            doc.heightInDots,
                            doc.id,
                            doc.errorMessages,
                            isDocumentRawDataActive(doc)
                        )
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
                    renderDocuments();
                },
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
                onOpenDocs: (doc) => window.open(`/docs/${doc}`, '_blank')
            });
        }

        // Initialize Workspace Dialog module
        if (window.WorkspaceDialog) {
            WorkspaceDialog.init({
                apiRequest: (path, options) => apiRequest(path, options),
                loginWithToken: (token) => loginWithToken(token),
                closeModal: () => closeModal(),
                showTokenDialog: (token) => showTokenDialog(token),
                showToast: (msg, isError) => showToast(msg, isError),
                onWorkspaceCreated: (token, name) => {
                    workspaceToken = token;
                    workspaceName = name;
                    WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                    renderSidebar();
                    renderDocuments();
                },
                onWorkspaceAccessed: (token) => {
                    // workspaceName is already set by loginWithToken
                    WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
                    renderSidebar();
                    renderDocuments();
                }
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

            // Verify auth and get user info
            (async () => {
                try {
                    const userInfo = await apiRequest('/api/auth/me');
                    if (userInfo && userInfo.name) {
                        workspaceName = userInfo.name;
                        localStorage.setItem('workspaceName', workspaceName);
                        WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
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

        WorkspaceMenu.updateDisplay(workspaceToken, workspaceName);
        renderSidebar();
        renderDocuments();

        // Preload icons
        loadIcon('alert-triangle');

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
