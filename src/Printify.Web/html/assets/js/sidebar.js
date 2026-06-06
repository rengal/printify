(function () {
    function render(options) {
        const pinnedList = document.getElementById('pinnedList');
        const otherList = document.getElementById('otherList');
        const recentlyActiveList = document.getElementById('recentlyActiveList');
        const recentlyActiveDivider = document.getElementById('recentlyActiveDivider');

        if (!pinnedList || !otherList) {
            return;
        }

        const printers = options?.printers ?? [];
        const selectedPrinterId = options?.selectedPrinterId ?? null;
        const ownPrinters = printers.filter(printer => !printer.isForeign);
        // The "recently active" list shows any printer (own or from another workspace) that has a last
        // document, newest first. Printers without one are skipped so the list never shows empty entries.
        const recentlyActivePrinters = printers
            .filter(printer => printer.lastDocumentAt)
            .sort(compareByLastDocument);

        const pinnedPrinters = ownPrinters
            .filter(printer => printer.pinned)
            .sort((a, b) => a.pinOrder - b.pinOrder);
        const otherPrinters = ownPrinters
            .filter(printer => !printer.pinned)
            .sort((a, b) => a.name.localeCompare(b.name));

        pinnedList.innerHTML = pinnedPrinters.map(printer => renderPrinterItem(
            printer,
            selectedPrinterId,
            true)).join('');
        otherList.innerHTML = otherPrinters.map(printer => renderPrinterItem(
            printer,
            selectedPrinterId,
            false)).join('');

        if (recentlyActiveList && recentlyActiveDivider) {
            recentlyActiveDivider.style.display = recentlyActivePrinters.length > 0 ? 'flex' : 'none';
            recentlyActiveList.innerHTML = recentlyActivePrinters.map(printer => renderPrinterItem(
                printer,
                selectedPrinterId,
                false)).join('');
        }
    }

    function renderPrinterItem(printer, selectedPrinterId, isPinned) {
        const isStopped = printer.runtimeStatus === 'stopped';
        const pinIcon = isPinned
            ? '<svg class="pin-icon pin-icon-filled" width="12" height="12" viewBox="0 0 24 24" fill="#10b981" stroke="#10b981" stroke-width="2"><path d="M12 2l2.4 7.4h7.6l-6 4.6 2.3 7-6.3-4.6-6.3 4.6 2.3-7-6-4.6h7.6z"/></svg> '
            : '';
        const statusIcon = isStopped
            ? '<img class="stopped-icon" src="assets/icons/alert-triangle.svg" width="18" height="18" alt="Printer is stopped" title="Printer is stopped">'
            : '';
        const ownerLine = printer.isForeign
            ? `<span class="list-item-owner">${escapeHtml(printer.ownerWorkspaceName || 'Unknown workspace')}</span>`
            : '';

        return `
            <div class="list-item ${selectedPrinterId === printer.id ? 'active' : ''} ${isStopped ? 'has-status-icon' : ''} ${printer.isForeign ? 'is-foreign' : ''}" onclick="selectPrinter('${printer.id}')">
              <span class="list-item-body">
                <span class="list-item-name">${pinIcon}${escapeHtml(printer.name)}</span>
                ${ownerLine}
              </span>${statusIcon}
              <button class="list-item-gear" onclick="event.stopPropagation(); toggleOperationsForPrinter('${printer.id}')" title="Toggle operations">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="1"></circle>
                  <circle cx="12" cy="5" r="1"></circle>
                  <circle cx="12" cy="19" r="1"></circle>
                </svg>
              </button>
            </div>
          `;
    }

    function compareByLastDocument(a, b) {
        // Both printers are guaranteed to have a last document here; newest first, name as tie-breaker.
        return (b.lastDocumentAt - a.lastDocumentAt) || a.name.localeCompare(b.name);
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

    window.Sidebar = {
        render
    };
})();
