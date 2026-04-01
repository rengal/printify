/**
 * Documents Panel Module
 *
 * Manages the documents panel for displaying printer documents:
 * - Renders different states (no workspace, no printer, no documents, documents list)
 * - Renders off-DOM to avoid flicker
 * - Handles document debug toggle and copy actions
 * - Renders canvas document elements
 */
import { normalizeEscPosFont, toEscPosFontCssClass } from '../../assets/js/api/escpos-specs.js';

/**
 * Convert EPL font identifier (e.g. "EplFont0") to CSS class.
 * Falls back to "epl-font" for unknown fonts.
 */
function toEplFontCssClass(font) {
    const match = String(font ?? '').match(/(\d+)$/);
    return match ? `epl-font-${match[1]}` : 'epl-font';
}

// ============================================================================
// STATE
// ============================================================================

let templateDocument = null;
let templates = {};
let currentContainer = null;

// Callbacks for actions (set by main.js)
const callbacks = {
    onCreateWorkspace: null,
    onAccessWorkspace: null,
    onToggleDocumentDebug: null,
    onCopyDocument: null,
    getWelcomeMessage: null,
    getDebugMode: null,
    getPrinterById: null,
    isDocumentRawDataActive: null,
    escapeHtml: null,
    resolveMediaUrl: null
};

// ============================================================================
// PER-PRINTER STATE
// Each printer gets its own panel div and data cache.
// printerStates[printerId] = {
//   el,            — the panel div (child of host, display:none when inactive)
//   docs,          — Array of mapped document objects (newest first)
//   firstDocId,    — id of the newest cached doc (top of list)
//   lastDocId,     — id of the oldest cached doc (bottom of list, for beforeId)
//   hasMore,       — whether there are older pages to load
//   paginationLoading, — guard for concurrent load-more
//   nextBeforeId,  — cursor for next older page
// }
// ============================================================================

const printerStates = {};
let activePrinterId = null;

// Scroll observer (one at a time, for the active printer panel)
let _scrollObserver = null;
let _scrollSentinel = null;
let _scrollObserverPrinterId = null;

function getOrCreatePrinterState(printerId) {
    if (!printerStates[printerId]) {
        printerStates[printerId] = {
            el: null,
            docs: [],
            firstDocId: null,
            lastDocId: null,
            hasMore: false,
            paginationLoading: false,
            nextBeforeId: null
        };
    }
    return printerStates[printerId];
}

function getHostContainer() {
    return document.getElementById('documentsPanel');
}

function getPrinterPanel(printerId) {
    const state = printerStates[printerId];
    if (state?.el) return state.el;
    return null;
}

function getOrCreatePrinterPanel(printerId) {
    const state = getOrCreatePrinterState(printerId);
    if (!state.el) {
        const host = getHostContainer();
        if (!host) return null;
        const panel = document.createElement('div');
        panel.className = 'printer-doc-panel';
        panel.dataset.printerPanel = printerId;
        panel.style.display = 'none';
        host.appendChild(panel);
        state.el = panel;
    }
    return state.el;
}

function getOrCreateStubPanel() {
    const host = getHostContainer();
    if (!host) return null;
    let stub = host.querySelector('.docs-stub-panel');
    if (!stub) {
        stub = document.createElement('div');
        stub.className = 'docs-stub-panel';
        stub.style.display = 'none';
        host.appendChild(stub);
    }
    return stub;
}

function hideAllPanelsAndStub() {
    for (const id in printerStates) {
        if (printerStates[id].el) printerStates[id].el.style.display = 'none';
    }
    const stub = getHostContainer()?.querySelector('.docs-stub-panel');
    if (stub) stub.style.display = 'none';
}

function showPrinterPanel(printerId) {
    hideAllPanelsAndStub();
    const panel = getPrinterPanel(printerId);
    if (panel) panel.style.display = '';
}

function showStubPanel(contentNode) {
    hideAllPanelsAndStub();
    const stub = getOrCreateStubPanel();
    if (!stub) return;
    stub.innerHTML = '';
    if (contentNode) stub.appendChild(contentNode);
    stub.style.display = '';
}

function updatePrinterCursors(printerId) {
    const state = printerStates[printerId];
    if (!state || state.docs.length === 0) {
        state.firstDocId = null;
        state.lastDocId = null;
        return;
    }
    state.firstDocId = state.docs[0].id;
    state.lastDocId = state.docs[state.docs.length - 1].id;
}

// ============================================================================
// SCROLL OBSERVER (per active printer panel)
// ============================================================================

function attachScrollObserver(printerId) {
    detachScrollObserver();

    const panel = getPrinterPanel(printerId);
    if (!panel) return;

    const sentinel = document.createElement('div');
    sentinel.className = 'docs-scroll-spacer';
    panel.appendChild(sentinel);
    _scrollSentinel = sentinel;
    _scrollObserverPrinterId = printerId;

    _scrollObserver = new IntersectionObserver((entries) => {
        console.debug(`[pagination] sentinel intersecting=${entries[0].isIntersecting}`);
        if (entries[0].isIntersecting) {
            _loadMoreCallback?.(_scrollObserverPrinterId);
        }
    }, { root: panel.parentElement, rootMargin: '0px 0px 60px 0px', threshold: 0 });

    _scrollObserver.observe(sentinel);
    console.debug(`[pagination] scroll observer attached for printer=${printerId}`);
}

function detachScrollObserver() {
    if (_scrollObserver) {
        _scrollObserver.disconnect();
        _scrollObserver = null;
    }
    if (_scrollSentinel) {
        _scrollSentinel.remove();
        _scrollSentinel = null;
    }
    _scrollObserverPrinterId = null;
}

// Injected by main.js via init so panel can trigger load-more
let _loadMoreCallback = null;

// ============================================================================
// PUBLIC: PRINTER SELECTION
// Called by main.js when user selects a printer.
// Fetches latest docs (always), diffs against cache, prepends new ones.
// ============================================================================

export async function selectPrinter(printerId, printer, apiFetch) {
    activePrinterId = printerId;
    detachScrollObserver();

    const host = getHostContainer();
    if (!host) return;

    const state = getOrCreatePrinterState(printerId);
    const panel = getOrCreatePrinterPanel(printerId);

    const isFirstLoad = state.docs.length === 0;

    showPrinterPanel(printerId);
    await showState('loading', { printerId });

    await loadTemplateDocument();

    const t0 = performance.now();
    let fetchedDocs = [];
    let hasMore = false;
    let nextBeforeId = null;

    try {
        const response = await apiFetch(`/api/printers/${printerId}/documents/canvas?limit=20`);
        console.debug(`[selectPrinter] fetch: ${(performance.now() - t0).toFixed(0)}ms`);
        const result = response?.result;
        const items = result?.items || [];
        hasMore = result?.hasMore ?? false;
        nextBeforeId = result?.nextBeforeId ?? null;
        fetchedDocs = items.map(dto => mapViewDocumentDto(dto, printer));
    } catch (err) {
        console.error('[DocumentsPanel] Failed to fetch documents', err);
        panel.innerHTML = '';
        return;
    }

    if (fetchedDocs.length === 0 && state.docs.length === 0) {
        state.hasMore = false;
        state.nextBeforeId = null;
        updatePrinterCursors(printerId);
        await showState('no-documents', { printerId, printer });
        return;
    }

    // Diff: find docs from fetch that are not already cached
    const cachedIds = new Set(state.docs.map(d => d.id));
    const newDocs = fetchedDocs.filter(d => !cachedIds.has(d.id));

    if (newDocs.length > 0) {
        // Prepend new docs to cache (newest first)
        state.docs = [...newDocs, ...state.docs].slice(0, 200);
        state.hasMore = hasMore;
        state.nextBeforeId = nextBeforeId;
        updatePrinterCursors(printerId);
        console.debug(`[selectPrinter] ${newDocs.length} new docs prepended, total=${state.docs.length}`);
    } else {
        // No new docs — keep existing cache, but update pagination from fresh fetch
        state.hasMore = hasMore;
        state.nextBeforeId = nextBeforeId;
        console.debug(`[selectPrinter] no new docs, showing cached ${state.docs.length}`);
    }

    // Render full list into this printer's panel
    const t1 = performance.now();
    await _renderDocsInPanel(panel, state.docs, printerId);
    console.debug(`[selectPrinter] render: ${(performance.now() - t1).toFixed(0)}ms`);

    if (state.hasMore) {
        attachScrollObserver(printerId);
    }
}

// ============================================================================
// PUBLIC: PREPEND SINGLE DOCUMENT (from stream)
// ============================================================================

export function prependDocument(printerId, doc) {
    const state = getOrCreatePrinterState(printerId);

    // Update cache
    const existingIdx = state.docs.findIndex(d => d.id === doc.id);
    if (existingIdx !== -1) {
        state.docs[existingIdx] = doc;
    } else {
        state.docs.unshift(doc);
        state.docs.sort((a, b) => b.timestamp - a.timestamp);
        state.docs = state.docs.slice(0, 200);
    }
    updatePrinterCursors(printerId);

    const panel = getPrinterPanel(printerId);
    if (!panel || activePrinterId !== printerId) return; // panel not visible — cache updated, done

    // If panel currently shows empty/loading state, do a full render instead
    if (!panel.querySelector('.document-item')) {
        _renderDocsInPanel(panel, state.docs, printerId).then(() => {
            if (state.hasMore) attachScrollObserver(printerId);
        });
        return;
    }

    // Prepend single node to top of existing list
    loadTemplateDocument().then(() => {
        const el = renderDocumentItem(doc);
        if (!el) return;

        // Build off-screen for image loading
        const offscreen = document.createElement('div');
        offscreen.style.cssText = 'position:fixed;left:-9999px;top:0;opacity:0;pointer-events:none;';
        document.body.appendChild(offscreen);
        offscreen.appendChild(el);

        const imgs = Array.from(offscreen.querySelectorAll('img[src]')).filter(img => {
            const src = img.getAttribute('src') || '';
            return src.startsWith('/api/') || src.includes('/media/');
        });

        const waitForImages = imgs.length > 0
            ? Promise.all(imgs.map(img => {
                if (img.complete && img.naturalWidth > 0) return Promise.resolve();
                return new Promise(resolve => {
                    const t = setTimeout(resolve, 5000);
                    img.onload = img.onerror = () => { clearTimeout(t); resolve(); };
                    if (img.complete) { clearTimeout(t); resolve(); }
                });
            }))
            : Promise.resolve();

        waitForImages.then(() => {
            document.body.removeChild(offscreen);
            // Insert before the first .document-item
            const first = panel.querySelector('.document-item');
            if (first) {
                panel.insertBefore(el, first);
            } else {
                panel.insertBefore(el, panel.firstChild);
            }
        });
    });
}

// ============================================================================
// PUBLIC: LOAD MORE (infinite scroll — older pages)
// ============================================================================

export async function loadMore(printerId, printer, apiFetch) {
    const state = printerStates[printerId];
    if (!state || !state.hasMore || state.paginationLoading) {
        console.debug(`[pagination] loadMore skipped: hasMore=${state?.hasMore}, loading=${state?.paginationLoading}`);
        return;
    }

    state.paginationLoading = true;
    const panel = getPrinterPanel(printerId);
    if (panel) renderLoadingMore(panel);

    try {
        const url = `/api/printers/${printerId}/documents/canvas?limit=20&beforeId=${state.nextBeforeId}`;
        console.debug(`[pagination] fetching more: ${url}`);
        const response = await apiFetch(url);
        const result = response?.result;
        const items = result?.items || [];

        const existingIds = new Set(state.docs.map(d => d.id));
        const mapped = items
            .filter(dto => !existingIds.has(dto.id))
            .map(dto => mapViewDocumentDto(dto, printer));

        state.docs = [...state.docs, ...mapped];
        state.hasMore = result?.hasMore ?? false;
        state.nextBeforeId = result?.nextBeforeId ?? null;
        state.paginationLoading = false;
        updatePrinterCursors(printerId);

        console.debug(`[pagination] got ${mapped.length} new docs, hasMore=${state.hasMore}, total=${state.docs.length}`);

        if (panel) {
            removeLoadingMore(panel);
            if (mapped.length > 0) {
                await renderDocumentsList(mapped, printer, panel, { append: true });
            }
        }

        if (state.hasMore) {
            attachScrollObserver(printerId);
        } else {
            detachScrollObserver();
        }
    } catch (err) {
        if (state) state.paginationLoading = false;
        if (panel) removeLoadingMore(panel);
        console.error('[DocumentsPanel] Failed to load more documents', err);
    }
}

// ============================================================================
// PUBLIC: CLEAR PRINTER (after delete documents action)
// ============================================================================

export function clearPrinter(printerId) {
    const state = printerStates[printerId];
    if (!state) return;
    state.docs = [];
    state.firstDocId = null;
    state.lastDocId = null;
    state.hasMore = false;
    state.nextBeforeId = null;
    state.paginationLoading = false;
    if (state.el) state.el.innerHTML = '';
}

export function disposePrinter(printerId) {
    const state = printerStates[printerId];
    if (!state) return;
    state.el?.remove();
    delete printerStates[printerId];
}

// ============================================================================
// PUBLIC: DOC COUNT (for confirm dialogs in main.js)
// ============================================================================

export function getDocCount(printerId) {
    return printerStates[printerId]?.docs.length ?? 0;
}

// ============================================================================
// PUBLIC: RE-RENDER ALL DOCS (when debug mode toggles)
// ============================================================================

export function reRenderAll(isRawDataActiveFn) {
    for (const printerId in printerStates) {
        const state = printerStates[printerId];
        if (!state.docs.length) continue;

        state.docs = state.docs.map(doc => _reRenderDoc(doc, isRawDataActiveFn(doc)));

        // If this printer's panel is active, re-render into DOM
        if (activePrinterId === printerId && state.el) {
            _renderDocsInPanel(state.el, state.docs, printerId).then(() => {
                if (state.hasMore) attachScrollObserver(printerId);
            });
        }
    }
}

// ============================================================================
// PUBLIC: TOGGLE SINGLE DOCUMENT DEBUG
// ============================================================================

export function toggleDocumentDebug(printerId, documentId, isEnabled, isRawDataActiveFn) {
    const state = printerStates[printerId];
    if (!state) return;

    const idx = state.docs.findIndex(d => d.id === documentId);
    if (idx === -1) return;

    const updated = { ...state.docs[idx], debugEnabled: !!isEnabled };
    state.docs[idx] = _reRenderDoc(updated, isRawDataActiveFn(updated));

    // Update just this document's DOM node if panel is active
    if (activePrinterId === printerId && state.el) {
        const existingItem = state.el.querySelector(`.document-item[data-doc-id="${documentId}"]`);
        loadTemplateDocument().then(() => {
            const newEl = renderDocumentItem(state.docs[idx]);
            if (!newEl) return;
            if (existingItem) {
                existingItem.replaceWith(newEl);
            }
            if (callbacks.isDocumentRawDataActive?.(state.docs[idx])) {
                requestAnimationFrame(() => {
                    if (state.docs[idx].canvases?.length > 0) {
                        state.docs[idx].canvases.forEach((_, i) => {
                            adjustDebugYPositions(`doc-content-${documentId}-canvas-${i}`, true);
                        });
                    } else {
                        adjustDebugYPositions(`doc-content-${documentId}`, true);
                    }
                });
            }
        });
    }
}

function _reRenderDoc(doc, includeDebug) {
    if (doc.canvases && doc.canvases.length > 0) {
        return {
            ...doc,
            canvases: doc.canvases.map((canvas, index) => ({
                ...canvas,
                previewHtml: renderViewDocument(
                    canvas.elements || [],
                    canvas.width,
                    canvas.heightInDots,
                    `${doc.id}-canvas-${index}`,
                    doc.errorMessages,
                    includeDebug,
                    doc.protocol
                )
            }))
        };
    }
    return {
        ...doc,
        previewHtml: renderViewDocument(
            doc.elements || [],
            doc.widthInDots,
            doc.heightInDots,
            doc.id,
            doc.errorMessages,
            includeDebug,
            doc.protocol
        )
    };
}

// ============================================================================
// INTERNAL: RENDER DOCS INTO A PANEL DIV
// ============================================================================

async function _renderDocsInPanel(panel, docs, printerId) {
    if (docs.length === 0) return;
    await renderDocumentsList(docs, null, panel);
}

// ============================================================================
// PUBLIC API
// ============================================================================

/**
 * Initialize the documents panel module with action callbacks
 */
export function init(actionCallbacks) {
    Object.assign(callbacks, actionCallbacks);
    if (actionCallbacks.onLoadMore) {
        _loadMoreCallback = actionCallbacks.onLoadMore;
    }
}

// ============================================================================
// TEMPLATE LOADING
// ============================================================================

/**
 * Load the template document once and cache it
 */
async function loadTemplateDocument() {
    if (templateDocument) return templateDocument;

    const response = await fetch('features/documents-panel/documents-panel.html');
    const html = await response.text();
    const parser = new DOMParser();
    templateDocument = parser.parseFromString(html, 'text/html');

    // Cache all templates
    templates = {
        noWorkspace: templateDocument.querySelector('#docs-panel-no-workspace-template'),
        noPrinter: templateDocument.querySelector('#docs-panel-no-printer-template'),
        noDocuments: templateDocument.querySelector('#docs-panel-no-documents-template'),
        documentItem: templateDocument.querySelector('#docs-panel-document-item-template')
    };

    return templateDocument;
}

// ============================================================================
// RENDER FUNCTIONS
// ============================================================================

/**
 * Render the loading state (initial documents fetch in progress)
 */
// ============================================================================
// SINGLE POINT OF ENTRY — show one of 4 panel states
//
// States:
//   'no-workspace'  — not logged in (welcome + create/access buttons)
//   'no-printer'    — logged in, no printer selected (greeting message)
//   'no-documents'  — printer selected, no docs yet (setup instructions)
//                     options: { printerId, printer }
//   'loading'       — printer selected, fetching docs
//                     options: { printerId }
// ============================================================================

export async function showState(state, options = {}) {
    await loadTemplateDocument();

    switch (state) {
        case 'no-workspace': {
            const wrap = document.createElement('div');
            wrap.appendChild(templates.noWorkspace.content.cloneNode(true));
            wrap.querySelector('[data-action="create-workspace"]')
                ?.addEventListener('click', () => callbacks.onCreateWorkspace?.());
            wrap.querySelector('[data-action="access-workspace"]')
                ?.addEventListener('click', () => callbacks.onAccessWorkspace?.());
            showStubPanel(wrap);
            break;
        }
        case 'no-printer': {
            const wrap = document.createElement('div');
            wrap.appendChild(templates.noPrinter.content.cloneNode(true));
            const greetingEl = wrap.querySelector('[data-docs-greeting]');
            const messageEl  = wrap.querySelector('[data-docs-message]');
            if (greetingEl) greetingEl.textContent = options.greeting || 'Welcome!';
            if (messageEl)  messageEl.textContent  = options.message  || 'Select a printer to view documents';
            showStubPanel(wrap);
            break;
        }
        case 'no-documents': {
            const panel = options.printerId ? getOrCreatePrinterPanel(options.printerId) : null;
            if (!panel) break;
            const printer = options.printer;
            panel.innerHTML = '';
            const wrap = document.createElement('div');
            wrap.appendChild(templates.noDocuments.content.cloneNode(true));
            const hostEl     = wrap.querySelector('[data-docs-host]');
            const portEl     = wrap.querySelector('[data-docs-port]');
            const protocolEl = wrap.querySelector('[data-docs-protocol]');
            if (hostEl)     hostEl.textContent     = printer?.publicHost || 'localhost';
            if (portEl)     portEl.textContent     = printer?.port || 'not configured';
            if (protocolEl) protocolEl.textContent = (printer?.protocol || 'ESC/POS').toUpperCase();
            panel.appendChild(wrap);
            break;
        }
        case 'loading': {
            const panel = options.printerId ? getOrCreatePrinterPanel(options.printerId) : null;
            if (!panel) break;
            panel.innerHTML = `
                <div class="docs-loading">
                    <div class="docs-progress-bar"><div class="docs-progress-bar-fill"></div></div>
                    <span>Loading documents...</span>
                </div>`;
            break;
        }
    }
}

/**
 * Append a "loading more" indicator at the bottom of a printer panel
 */
export function renderLoadingMore(targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container || container.querySelector('#docs-loading-more-indicator')) return;
    const el = document.createElement('div');
    el.className = 'docs-loading-more';
    el.id = 'docs-loading-more-indicator';
    el.innerHTML = `
        <div class="docs-progress-bar"><div class="docs-progress-bar-fill"></div></div>
        <span>Loading more documents...</span>`;
    const sentinel = container.querySelector('.docs-scroll-spacer');
    if (sentinel) {
        container.insertBefore(el, sentinel);
    } else {
        container.appendChild(el);
    }
}

/**
 * Remove the "loading more" indicator if present
 */
export function removeLoadingMore(targetContainer) {
    const container = targetContainer || document.getElementById('documentsPanel');
    (container || document).querySelector('#docs-loading-more-indicator')?.remove();
}

/**
 * Render the documents list state
 * @param {Array} documents - Array of document objects
 * @param {Object} printer - Printer object
 * @param {Element} targetContainer - Optional target container
 */
export async function renderDocumentsList(documents, printer, targetContainer, { append = false } = {}) {
    const container = targetContainer || document.getElementById('documentsPanel');
    if (!container) return null;

    await loadTemplateDocument();

    // Build off-DOM wrapper so images can load before we touch the live DOM.
    // Must be visible (opacity:0 not visibility:hidden) so browsers actually load images.
    const offscreen = document.createElement('div');
    offscreen.style.cssText = 'position:fixed;left:-9999px;top:0;opacity:0;pointer-events:none;';
    document.body.appendChild(offscreen);

    for (const doc of documents) {
        const docElement = renderDocumentItem(doc);
        if (docElement) offscreen.appendChild(docElement);
    }

    // Wait for all document images to load or fail. Use a 5s timeout per image as safety net.
    const imgs = Array.from(offscreen.querySelectorAll('img[src]')).filter(img => {
        // Skip icon/UI images (svg icons, etc.) — only wait for document content images
        const src = img.getAttribute('src') || '';
        return src.startsWith('/api/') || src.includes('/media/');
    });
    if (imgs.length > 0) {
        await Promise.all(imgs.map(img => {
            if (img.complete && img.naturalWidth > 0) return Promise.resolve();
            return new Promise(resolve => {
                const timeout = setTimeout(() => { resolve(); }, 5000);
                img.onload = () => { clearTimeout(timeout); resolve(); };
                img.onerror = () => { clearTimeout(timeout); resolve(); };
                if (img.complete) { clearTimeout(timeout); resolve(); }
            });
        }));
    }

    // Move rendered nodes into a fragment
    const fragment = document.createDocumentFragment();
    while (offscreen.firstChild) fragment.appendChild(offscreen.firstChild);
    document.body.removeChild(offscreen);

    if (append) {
        // Remove existing loading-more indicator before appending
        container.querySelector('#docs-loading-more-indicator')?.remove();
        container.appendChild(fragment);
    } else {
        // Clear container and attach all at once
        container.innerHTML = '';
        container.appendChild(fragment);
    }

    // Adjust Y positions in debug mode after DOM insertion
    const debugDocs = documents.filter(doc => callbacks.isDocumentRawDataActive?.(doc));
    if (debugDocs.length > 0) {
        requestAnimationFrame(() => {
            debugDocs.forEach(doc => {
                if (doc.canvases && doc.canvases.length > 0) {
                    // Adjust each canvas individually
                    doc.canvases.forEach((canvas, index) => {
                        const contentId = `doc-content-${doc.id}-canvas-${index}`;
                        adjustDebugYPositions(contentId, true);
                    });
                } else {
                    // Fallback for old single canvas format
                    const contentId = `doc-content-${doc.id}`;
                    adjustDebugYPositions(contentId, true);
                }
            });
        });
    }

    currentContainer = container;
    return container;
}

/**
 * Render a single document item
 */
function renderDocumentItem(doc) {
    if (!templates.documentItem) return null;

    const fragment = templates.documentItem.content.cloneNode(true);
    const item = fragment.querySelector('.document-item');
    if (item && doc.id) item.dataset.docId = doc.id;

    // Format datetime
    const dateTime = doc.timestamp?.toLocaleString(undefined, {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
    }) || '';
    const relativeTime = formatRelativeTime(doc.timestamp) || '';

    // Set datetime
    const datetimeEl = item.querySelector('[data-docs-datetime]');
    if (datetimeEl) {
        datetimeEl.textContent = `${dateTime} \u00B7 ${relativeTime}`;
    }

    // Set debug toggle
    const debugToggle = item.querySelector('[data-docs-debug-toggle]');
    const debugMode = callbacks.getDebugMode?.() || false;
    if (debugToggle) {
        debugToggle.checked = doc.debugEnabled || false;
        debugToggle.disabled = debugMode;
        debugToggle.addEventListener('change', (e) => {
            callbacks.onToggleDocumentDebug?.(doc.id, e.target.checked);
        });
    }

    // Set error icon if present
    const hasErrors = doc.errorMessages && doc.errorMessages.length > 0;
    const errorIcon = item.querySelector('[data-docs-error-icon]');
    if (errorIcon) {
        if (hasErrors) {
            errorIcon.style.display = '';
            errorIcon.title = doc.errorMessages.join('\n');
        } else {
            errorIcon.style.display = 'none';
        }
    }

    // Set canvases container (multiple canvases)
    const canvasesContainer = item.querySelector('[data-docs-canvases-container]');
    if (canvasesContainer) {
        const canvases = doc.canvases || [];
        if (canvases.length === 0) {
            // Fallback for old single canvas format
            canvasesContainer.innerHTML = doc.previewHtml || '';
        } else {
            // Render each canvas as a separate block with its own copy button
            const totalPages = canvases.length;
            const bytesText = formatByteCount(doc.bytesReceived);
            canvasesContainer.innerHTML = canvases.map((canvas, index) => {
                const isLastCanvas = index === totalPages - 1;
                return `
                <div class="document-canvas-block">
                    ${canvas.previewHtml || ''}
                    <div class="document-canvas-footer">
                        ${isLastCanvas ? `<span class="document-meta-text document-footer-text">Size: ${bytesText} bytes</span>` : '<span></span>'}
                        <div class="document-canvas-footer-right">
                            <span class="document-meta-text document-footer-text">Page ${index + 1}/${totalPages}</span>
                            <button class="copy-icon-btn document-copy-btn" data-docs-copy data-docs-canvas-index="${index}" title="Copy canvas content">
                                <img src="assets/icons/copy.svg" width="14" height="14" alt="Copy" class="themed-icon">
                            </button>
                        </div>
                    </div>
                </div>
            `}).join('');
        }
    }

    // Set copy buttons (one per canvas)
    const copyButtons = item.querySelectorAll('[data-docs-copy]');
    copyButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const canvasIndex = Number(btn.getAttribute('data-docs-canvas-index') || 0);
            const canvases = doc.canvases || [];
            const text = canvases[canvasIndex]?.plainText || doc.plainText || '';
            callbacks.onCopyDocument?.(text);
        });
    });

    return item;
}

// ============================================================================
// DOCUMENT RENDERING
// ============================================================================

/**
 * Render ViewDocument with absolute positioning
 * @param {Array} elements - Canvas elements to render
 * @param {number} documentWidth - Document width in dots
 * @param {number} documentHeight - Document height in dots (optional)
 * @param {string} docId - Document identifier
 * @param {Array} errorMessages - Array of error messages
 * @param {boolean} includeDebug - Whether to include debug information
 * @returns {string} HTML string
 */
export function renderViewDocument(elements, documentWidth, documentHeight, docId, errorMessages, includeDebug, protocol = 'escpos') {
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
        return renderViewElement(element, id, includeDebug, protocol);
    }).join('');

    const contentId = `doc-content-${docId}`;

    return `<div class="document-paper${errorClass}">
        <div class="document-content" id="${contentId}" style="width:${width}px; height:${height}px;" data-debug="${includeDebug}">
            ${elementsHtml}
        </div>
    </div>`;
}

/**
 * Render individual ViewElement
 * @param {Object} element - Element to render
 * @param {string} id - Element identifier
 * @param {boolean} includeDebug - Whether to include debug information
 * @returns {string} HTML string
 */
function renderViewElement(element, id, includeDebug, protocol = 'escpos') {
    const elementType = (element?.type || '').toLowerCase();

    switch (elementType) {
        case 'text':
            return renderViewTextElement(element, id, protocol);
        case 'image':
            return renderViewImageElement(element, id);
        case 'line':
            return renderViewLineElement(element, id);
        case 'debug':
        case 'none':
            // Debug-only element - only render debug table if debug mode enabled
            return includeDebug ? `<div id="${id}" data-element-type="debug" data-original-y="0">${renderDebugTable(element)}</div>` : '';
        default:
            return '';
    }
}

/**
 * Render text element with absolute positioning
 * @param {Object} element - Text element
 * @param {string} id - Element identifier
 * @returns {string} HTML string
 */
function renderViewTextElement(element, id, protocol = 'escpos') {
    const x = Number(element.x) || 0;
    const y = Number(element.y) || 0;
    const width = Number(element.width) || 0;
    const height = Number(element.height) || 0;
    const zIndex = Number(element.zIndex) || 0;
    const text = element.text || '';
    const font = element.font ?? 'ESCPOS_A';
    const charSpacing = Number(element.charSpacing) || 0;
    const charScaleX = Number(element.charScaleX) || 1;
    const charScaleY = Number(element.charScaleY) || 1;

    const fontClass = protocol === 'escpos' ? toEscPosFontCssClass(font) : toEplFontCssClass(font);
    const transformCss = (charScaleX !== 1 || charScaleY !== 1)
        ? `scale(${charScaleX}, ${charScaleY})`
        : 'none';

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
        styles.push(`transform: ${transformCss}`);
        styles.push('transform-origin: left top');
    }

    // Apply text styling modifiers inline
    if (element.isBold) {
        styles.push('font-weight: 700');
    }
    if (element.isUnderline) {
        styles.push('text-decoration: underline');
    }
    if (element.isItalic) {
        styles.push('font-style: italic');
    }
    if (element.isReverse) {
        styles.push('background: #000');
        styles.push('color: #fff');
        styles.push('padding: 2px 4px');
        styles.push('border-radius: 3px');
    }

    const textContent = callbacks.escapeHtml?.(text) || text;

    return `<div id="${id}" data-element-type="text" data-original-y="${y}"><div class="view-text ${fontClass}" style="${styles.join('; ')};">${textContent}</div></div>`;
}

/**
 * Render image element with absolute positioning
 * @param {Object} element - Image element
 * @param {string} id - Element identifier
 * @returns {string} HTML string
 */
function renderViewImageElement(element, id) {
    const x = Number(element.x) || 0;
    const y = Number(element.y) || 0;
    const width = Number(element.width) || 0;
    const height = Number(element.height) || 0;
    const zIndex = Number(element.zIndex) || 0;

    const mediaUrl = callbacks.resolveMediaUrl?.(element?.media?.url || '') || element?.media?.url || '';
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
    const escapedUrl = callbacks.escapeHtml?.(mediaUrl) || mediaUrl;

    return `<div id="${id}" data-element-type="image" data-original-y="${y}"><img class="view-image" src="${escapedUrl}" alt="${altText}" style="${styles.join('; ')};" loading="lazy"></div>`;
}

/**
 * Render line element (DrawBox - draws a rectangular box)
 * @param {Object} element - Line element with x1, y1, x2, y2, thickness
 * @param {string} id - Element identifier
 * @returns {string} HTML string
 */
function renderViewLineElement(element, id) {
    const x1 = Number(element.x1) || 0;
    const y1 = Number(element.y1) || 0;
    const x2 = Number(element.x2) || 0;
    const y2 = Number(element.y2) || 0;
    const thickness = Number(element.thickness) || 1;

    // Calculate box bounds
    const minX = Math.min(x1, x2);
    const minY = Math.min(y1, y2);
    const maxX = Math.max(x1, x2);
    const maxY = Math.max(y1, y2);
    const width = maxX - minX;
    const height = maxY - minY;

    // Use SVG for drawing the box (rectangle with stroke).
    // Inset by 0.5px so the 1px stroke sits on integer pixel boundaries (crisp, no sub-pixel blur).
    return `
        <div id="${id}" data-element-type="line" data-original-y="${minY}" style="position: absolute; left: 0; top: 0; width: 100%; height: 100%; pointer-events: none;">
            <svg style="position: absolute; left: 0; top: 0; width: 100%; height: 100%; overflow: visible;" xmlns="http://www.w3.org/2000/svg">
                <rect x="${minX + 0.5}" y="${minY + 0.5}" width="${Math.max(0, width - 1)}" height="${Math.max(0, height - 1)}"
                      fill="none" stroke="currentColor" stroke-width="1" shape-rendering="crispEdges" />
            </svg>
        </div>
    `;
}

/**
 * Render debug table element
 * @param {Object} element - Debug element
 * @returns {string} HTML string
 */
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
                  <td class="debug-desc">${(callbacks.escapeHtml?.(descFormatted) || descFormatted).replace(/\n/g, '<br>') || '<span class="debug-missing">??</span>'}</td>
            </tr>
        </table>
    `;
}

/**
 * Format hex command for display
 * @param {string} commandRaw - Raw hex command
 * @returns {string} Formatted hex string
 */
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

/**
 * Truncate text in description to max 40 characters
 * @param {string} desc - Description text
 * @returns {string} Truncated description
 */
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

/**
 * Extract plain text from ViewDocument elements
 * @param {Array} elements - Array of elements
 * @returns {string} Extracted plain text
 */
export function extractViewDocumentText(elements) {
    return (elements || [])
        .filter(el => el.type === 'text')
        .map(el => el.text || '')
        .join('\n');
}

/**
 * Map RenderedDocumentDto to internal document object
 * @param {Object} dto - RenderedDocumentDto from API
 * @param {Object} printer - Printer object (for default width)
 * @returns {Object} Internal document object
 */
export function mapViewDocumentDto(dto, printer) {
    const canvases = dto.canvases || [];
    const protocol = (dto.protocol || 'escpos').toLowerCase();
    const errorMessages = dto.errorMessages || null;
    const debugMode = callbacks.getDebugMode?.() || false;
    const docId = dto.id || `doc-${Date.now()}`;

    // Build canvas previews - one entry per canvas
    const canvasPreviews = canvases.map((canvas, index) => {
        const width = Number(canvas.widthInDots) || printer?.width || 384;
        const height = canvas.heightInDots ?? null;
        const elements = normalizeCanvasElements(canvas.items || [], protocol);
        const canvasId = `${docId}-canvas-${index}`;

        const previewHtml = renderViewDocument(elements, width, height, canvasId, errorMessages, debugMode, protocol);
        const plainText = extractViewDocumentText(elements);

        return {
            index,
            width,
            heightInDots: height,
            elements,
            previewHtml,
            plainText
        };
    });

    // Use first canvas for backward compatibility (width, elements)
    const firstCanvas = canvasPreviews[0] || {};
    const width = firstCanvas.width || printer?.width || 384;

    return {
        id: dto.id,
        printerId: dto.printerId,
        timestamp: dto.timestamp ? new Date(dto.timestamp) : new Date(),
        errorMessages: errorMessages,
        protocol,
        width,
        widthInDots: width,
        heightInDots: firstCanvas.heightInDots || null,
        bytesReceived: dto.bytesReceived ?? 0,
        bytesSent: dto.bytesSent ?? 0,
        elements: firstCanvas.elements || [], // Store raw elements for re-rendering
        debugEnabled: false,
        canvases: canvasPreviews // Multiple canvases
    };
}

function normalizeCanvasElements(elements, protocol) {
    return elements.map(element => {
        if (!element || (element.type || '').toLowerCase() !== 'text') {
            return element;
        }

        const apiFont = element.font ?? element.fontName ?? null;
        const normalizedFont = protocol === 'escpos'
            ? normalizeEscPosFont(apiFont)
            : apiFont;

        return {
            ...element,
            font: normalizedFont
        };
    });
}

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Format relative time (e.g., "2h ago")
 */
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

/**
 * Format byte count with narrow no-break spaces
 */
function formatByteCount(bytes) {
    if (bytes == null) return '0';
    const normalized = Math.trunc(Number(bytes)) || 0;
    return normalized.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '\u202F');
}

/**
 * Adjust Y positions in debug mode to account for debug table heights
 */
function adjustDebugYPositions(contentId, includeDebug) {
    if (!includeDebug) return;

    const container = document.getElementById(contentId);
    if (!container) return;

    const elements = Array.from(container.querySelectorAll('[data-original-y]'));
    let currentY = 0;

    elements.forEach((wrapper, index) => {
        const elementType = wrapper.getAttribute('data-element-type') || 'unknown';

        if (elementType === 'debug') {
            const debugTable = wrapper.querySelector('.debug-table');
            if (debugTable) {
                debugTable.style.top = `${currentY}px`;
                const debugHeight = debugTable.offsetHeight || 20;
                const debugDesc = debugTable.querySelector('.debug-desc')?.textContent?.trim() || '';
                currentY += debugHeight;
            }
        } else if (elementType === 'text' || elementType === 'image') {
            const visualElement = wrapper.querySelector('.view-text, .view-image');
            if (visualElement) {
                const elementHeight = parseInt(visualElement.style.height) || 0;
                const elementText = visualElement.textContent?.trim() || visualElement.alt || '';
                visualElement.style.top = `${currentY}px`;
                currentY += elementHeight;
            }
        } else if (elementType === 'line') {
            // For line/box elements, we need to adjust the SVG rect coordinates
            const svg = wrapper.querySelector('svg');
            if (svg) {
                const rect = svg.querySelector('rect');
                if (rect) {
                    const originalY = parseInt(wrapper.getAttribute('data-original-y')) || 0;
                    const originalHeight = parseInt(rect.getAttribute('height')) || 0;

                    // Adjust rect position
                    const newY = currentY + (originalY - originalY); // offset from original Y
                    const yOffset = currentY - originalY;

                    rect.setAttribute('y', parseInt(rect.getAttribute('y')) + yOffset);

                    // Move currentY down by the box height (using box height, not line thickness)
                    currentY += originalHeight;
                }
            }
        }
    });

    const originalHeight = parseInt(container.style.height) || 0;
    if (currentY > originalHeight) {
        container.style.height = `${currentY}px`;
    }
}

// ============================================================================
// WINDOW EXPORTS (for non-module scripts like main.js)
// ============================================================================

window.DocumentsPanel = {
    init,
    // Single entry point for all panel states
    showState,
    // Per-printer panel management
    selectPrinter,
    prependDocument,
    loadMore,
    clearPrinter,
    disposePrinter,
    getDocCount,
    reRenderAll,
    toggleDocumentDebug,
    // Pagination indicators (used internally and by loadMore)
    renderLoadingMore,
    removeLoadingMore,
    // Used by main.js for re-rendering
    renderViewDocument,
    extractViewDocumentText,
    mapViewDocumentDto
};
