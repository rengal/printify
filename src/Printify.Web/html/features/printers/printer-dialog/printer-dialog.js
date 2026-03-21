import { normalizeProtocol } from '../../../assets/js/api/protocol.js';
import { V } from '../../../assets/js/utils/app-version.js';
import { createUuid } from '../../../assets/js/utils/uuid.js';
import { escapeHtml } from '../../../assets/js/utils/html-utils.js';

let template = null;
let currentMode = null;
let currentPrinterId = null;
let currentPrinterName = '';
let currentOverlay = null;
let protocolDefaultsSet = false;

const callbacks = {
    apiRequest: null,
    normalizeProtocol: null,
    loadPrinters: null,
    closeModal: null,
    showToast: null
};

export function init(actionCallbacks)
{
    Object.assign(callbacks, actionCallbacks);
}

export async function showCreate()
{
    await show('create', null);
}

export async function showEdit(printer)
{
    if (!printer)
    {
        console.error('Printer data is required for edit mode');
        return;
    }

    await show('edit', printer);
}

export function close()
{
    if (!currentOverlay)
    {
        return;
    }

    if (currentOverlay.escapeHandler)
    {
        document.removeEventListener('keydown', currentOverlay.escapeHandler);
    }

    currentOverlay.remove();
    currentOverlay = null;
    currentMode = null;
    currentPrinterId = null;
    currentPrinterName = '';
}

async function show(mode, printer)
{
    close();

    if (!template)
    {
        await loadTemplate();
    }

    currentMode = mode;
    currentPrinterId = printer?.id ?? null;
    currentPrinterName = printer?.name ?? '';

    const overlay = template.content.cloneNode(true);
    const modalOverlay = overlay.querySelector('[data-printer-dialogue-overlay]');

    const elements = {
        overlay: modalOverlay,
        title: modalOverlay.querySelector('[data-printer-dialogue-title]'),
        closeBtn: modalOverlay.querySelector('[data-printer-dialogue-close]'),
        cancelBtn: modalOverlay.querySelector('[data-printer-dialogue-cancel]'),
        submitBtn: modalOverlay.querySelector('[data-printer-dialogue-submit]'),
        nameInput: modalOverlay.querySelector('[data-printer-dialogue-name-input]'),
        nameError: modalOverlay.querySelector('[data-printer-dialogue-name-error]'),
        protocolInput: modalOverlay.querySelector('[data-printer-dialogue-protocol-input]'),
        protocolHint: modalOverlay.querySelector('[data-printer-dialogue-protocol-hint]'),
        dimensionsGroup: modalOverlay.querySelector('[data-printer-dialogue-dimensions-group]'),
        widthInput: modalOverlay.querySelector('[data-printer-dialogue-width-input]'),
        heightField: modalOverlay.querySelector('[data-printer-dialogue-height-field]'),
        heightInput: modalOverlay.querySelector('[data-printer-dialogue-height-input]'),
        dimensionsAckSection: modalOverlay.querySelector('[data-printer-dialogue-dimensions-ack-section]'),
        dimensionsAckInput: modalOverlay.querySelector('[data-printer-dialogue-dimensions-ack-input]'),
        dimensionsAckError: modalOverlay.querySelector('[data-printer-dialogue-dimensions-ack-error]'),
        emulateBufferField: modalOverlay.querySelector('[data-printer-dialogue-emulate-buffer-field]'),
        emulateBufferInput: modalOverlay.querySelector('[data-printer-dialogue-emulate-buffer-input]'),
        bufferFields: modalOverlay.querySelector('[data-printer-dialogue-buffer-fields]'),
        bufferSizeInput: modalOverlay.querySelector('[data-printer-dialogue-buffer-size-input]'),
        drainRateInput: modalOverlay.querySelector('[data-printer-dialogue-drain-rate-input]'),
        clearDocumentsBtn: modalOverlay.querySelector('[data-printer-dialogue-clear-documents]'),
        deletePrinterBtn: modalOverlay.querySelector('[data-printer-dialogue-delete-printer]'),
        dangerHint: modalOverlay.querySelector('[data-printer-dialogue-danger-hint]')
    };

    setupTabs(modalOverlay);
    configureDialog(elements, mode, printer);
    bindEvents(elements);

    document.getElementById('modalContainer').appendChild(modalOverlay);
    currentOverlay = modalOverlay;

    setTimeout(() => {
        elements.nameInput?.focus();
    }, 50);
}

function setupTabs(modalOverlay)
{
    const navItems = modalOverlay.querySelectorAll('.printer-dialog-nav-item');
    const tabContents = modalOverlay.querySelectorAll('.printer-dialog-tab-content');

    navItems.forEach((item) => {
        item.addEventListener('click', () => {
            const tabName = item.dataset.tab;

            navItems.forEach((navItem) => navItem.classList.remove('active'));
            item.classList.add('active');

            tabContents.forEach((content) => {
                content.classList.toggle('active', content.dataset.content === tabName);
            });
        });
    });
}

function bindEvents(elements)
{
    elements.closeBtn.addEventListener('click', close);
    elements.cancelBtn.addEventListener('click', close);
    elements.submitBtn.addEventListener('click', () => handleSubmit(elements));

    elements.protocolInput.addEventListener('change', () => {
        elements.protocolInput.classList.remove('invalid');
        updateProtocolFields(elements);
        updateSubmitButtonState(elements);
    });

    elements.nameInput.addEventListener('input', () => {
        clearNameError(elements);
        updateSubmitButtonState(elements);
    });

    elements.emulateBufferInput.addEventListener('change', () => toggleBufferFields(elements));
    elements.dimensionsAckInput?.addEventListener('change', () => {
        clearDimensionsAckError(elements);
        updateSubmitButtonState(elements);
    });

    elements.clearDocumentsBtn?.addEventListener('click', handleClearDocuments);
    elements.deletePrinterBtn?.addEventListener('click', handleDeletePrinter);

    elements.overlay.addEventListener('click', (event) => {
        if (event.target === elements.overlay)
        {
            close();
        }
    });

    const handleEscape = (event) => {
        if (event.key === 'Escape')
        {
            close();
        }
    };

    document.addEventListener('keydown', handleEscape);
    elements.overlay.escapeHandler = handleEscape;
}

function configureDialog(elements, mode, printer)
{
    const isEditMode = mode === 'edit';
    protocolDefaultsSet = false;

    elements.title.textContent = isEditMode ? 'Printer Settings' : 'Add Printer';
    elements.submitBtn.textContent = isEditMode ? 'Save Changes' : 'Create Printer';

    if (isEditMode)
    {
        elements.nameInput.value = printer.name ?? '';
        elements.protocolInput.value = normalizeProtocol(printer.protocol);
        elements.widthInput.value = printer.width ?? 512;
        elements.heightInput.value = printer.height ?? 310;
        elements.emulateBufferInput.checked = printer.emulateBuffer ?? false;
        elements.bufferSizeInput.value = printer.bufferSize ?? 4096;
        elements.drainRateInput.value = printer.drainRate ?? 2048;
        protocolDefaultsSet = true;
    }

    if (isEditMode)
    {
        elements.protocolInput.disabled = true;
        elements.protocolInput.classList.add('no-dropdown');
        elements.protocolHint.textContent = 'Protocol is set on creation and cannot be changed.';
        elements.protocolHint.style.display = 'block';
        elements.dimensionsAckSection.style.display = 'none';
    }
    else
    {
        elements.protocolInput.disabled = false;
        elements.protocolInput.classList.remove('no-dropdown');
        elements.protocolHint.style.display = 'none';
        elements.dimensionsAckSection.style.display = 'flex';
    }

    const hasPersistedPrinter = Boolean(currentPrinterId);
    elements.clearDocumentsBtn.disabled = !hasPersistedPrinter;
    elements.deletePrinterBtn.disabled = !hasPersistedPrinter;
    elements.dangerHint.style.display = hasPersistedPrinter ? 'none' : 'block';

    updateProtocolFields(elements);
    toggleBufferFields(elements);
    updateSubmitButtonState(elements);
}

function toggleBufferFields(elements)
{
    if (elements.emulateBufferField.style.display === 'none')
    {
        elements.bufferFields.style.display = 'none';
        return;
    }

    elements.bufferFields.style.display = elements.emulateBufferInput.checked ? 'grid' : 'none';
}

const defaultWidthEscPos = 512;
const defaultWidthEpl = 412;
const defaultHeightEpl = 310;

function updateProtocolFields(elements)
{
    const protocol = elements.protocolInput.value;
    const isEscPos = protocol === 'escpos';
    const isEpl = protocol === 'epl';
    const hasProtocol = Boolean(protocol);

    elements.dimensionsGroup.style.display = hasProtocol ? 'grid' : 'none';
    elements.heightField.style.display = isEpl ? 'block' : 'none';
    elements.emulateBufferField.style.display = isEscPos ? 'flex' : 'none';

    if (!hasProtocol)
    {
        elements.bufferFields.style.display = 'none';
        elements.widthInput.value = '';
        elements.heightInput.value = '';
        return;
    }

    if (currentMode === 'create' && !protocolDefaultsSet)
    {
        if (isEscPos)
        {
            elements.widthInput.value = defaultWidthEscPos;
        }

        if (isEpl)
        {
            elements.widthInput.value = defaultWidthEpl;
            elements.heightInput.value = defaultHeightEpl;
        }

        protocolDefaultsSet = true;
    }

    if (!isEscPos)
    {
        elements.bufferFields.style.display = 'none';
    }
    else
    {
        toggleBufferFields(elements);
    }
}

function updateSubmitButtonState(elements)
{
    if (currentMode === 'edit')
    {
        elements.submitBtn.disabled = false;
        return;
    }

    const hasName = Boolean(elements.nameInput.value.trim());
    const hasProtocol = Boolean(elements.protocolInput.value);
    const hasAck = elements.dimensionsAckInput?.checked ?? false;
    elements.submitBtn.disabled = !(hasName && hasProtocol && hasAck);
}

function clearNameError(elements)
{
    elements.nameInput.classList.remove('invalid');
    elements.nameError.classList.remove('show');
}

function clearDimensionsAckError(elements)
{
    elements.dimensionsAckError.classList.remove('show');
}

async function handleSubmit(elements)
{
    if (currentMode === 'create')
    {
        await handleCreate(elements);
        return;
    }

    await handleEdit(elements);
}

async function handleCreate(elements)
{
    const name = elements.nameInput.value.trim();
    const protocol = elements.protocolInput.value;
    const width = parseInt(elements.widthInput.value, 10) || 512;
    const height = parseInt(elements.heightInput.value, 10) || 310;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value, 10) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value, 10) || 2048;
    const hasAck = elements.dimensionsAckInput.checked;

    clearNameError(elements);
    clearDimensionsAckError(elements);
    elements.protocolInput.classList.remove('invalid');

    if (!name)
    {
        elements.nameInput.classList.add('invalid');
        elements.nameError.classList.add('show');
        elements.nameInput.focus();
        return;
    }

    if (!protocol)
    {
        elements.protocolInput.classList.add('invalid');
        elements.protocolInput.focus();
        callbacks.showToast?.('Please select a protocol', true);
        return;
    }

    if (!hasAck)
    {
        elements.dimensionsAckError.classList.add('show');
        elements.dimensionsAckInput.focus();
        return;
    }

    try
    {
        const request = {
            printer: {
                id: createUuid(),
                displayName: name
            },
            settings: {
                protocol: callbacks.normalizeProtocol
                    ? callbacks.normalizeProtocol(protocol)
                    : normalizeProtocol(protocol),
                widthInDots: width,
                heightInDots: protocol === 'epl' ? height : null,
                emulateBufferCapacity: emulateBuffer,
                bufferDrainRate: drainRate,
                bufferMaxCapacity: bufferSize
            }
        };

        const created = await callbacks.apiRequest('/api/printers', {
            method: 'POST',
            body: JSON.stringify(request)
        });

        close();
        await callbacks.loadPrinters?.(created.printer.id);
        callbacks.showToast?.('Printer created successfully');
    }
    catch (err)
    {
        console.error(err);
        callbacks.showToast?.(err.message || 'Failed to create printer', true);
    }
}

async function handleEdit(elements)
{
    if (!currentPrinterId)
    {
        console.error('Printer ID is required for edit mode');
        return;
    }

    const name = elements.nameInput.value.trim();
    const protocol = elements.protocolInput.value;
    const width = parseInt(elements.widthInput.value, 10) || 512;
    const height = parseInt(elements.heightInput.value, 10) || 310;
    const emulateBuffer = elements.emulateBufferInput.checked;
    const bufferSize = parseInt(elements.bufferSizeInput.value, 10) || 4096;
    const drainRate = parseInt(elements.drainRateInput.value, 10) || 2048;

    clearNameError(elements);

    if (!name)
    {
        elements.nameInput.classList.add('invalid');
        elements.nameError.classList.add('show');
        elements.nameInput.focus();
        return;
    }

    try
    {
        const request = {
            printer: {
                id: currentPrinterId,
                displayName: name
            },
            settings: {
                protocol: callbacks.normalizeProtocol
                    ? callbacks.normalizeProtocol(protocol)
                    : normalizeProtocol(protocol),
                widthInDots: width,
                heightInDots: protocol === 'epl' ? height : null,
                emulateBufferCapacity: emulateBuffer,
                bufferDrainRate: drainRate,
                bufferMaxCapacity: bufferSize
            }
        };

        await callbacks.apiRequest(`/api/printers/${currentPrinterId}`, {
            method: 'PUT',
            body: JSON.stringify(request)
        });

        close();
        await callbacks.loadPrinters?.(currentPrinterId);
        callbacks.showToast?.('Printer updated successfully');
    }
    catch (err)
    {
        console.error(err);
        callbacks.showToast?.(err.message || 'Failed to update printer', true);
    }
}

async function handleClearDocuments()
{
    if (!currentPrinterId)
    {
        return;
    }

    const printerName = escapeHtml(currentPrinterName || 'this printer');
    const message = `Delete all documents for <strong>${printerName}</strong>?<br><br>This action cannot be undone.`;

    if (!window.ConfirmDialog)
    {
        return;
    }

    ConfirmDialog.show(
        'Delete All Documents',
        message,
        'Delete Documents',
        async () => {
            try
            {
                await callbacks.apiRequest(`/api/printers/${currentPrinterId}/documents`, { method: 'DELETE' });
                callbacks.showToast?.('All documents deleted');
                await callbacks.loadPrinters?.(currentPrinterId);
            }
            catch (err)
            {
                console.error(err);
                callbacks.showToast?.(err.message || 'Failed to delete documents', true);
            }
        },
        true
    );
}

async function handleDeletePrinter()
{
    if (!currentPrinterId)
    {
        return;
    }

    const printerName = escapeHtml(currentPrinterName || 'this printer');
    const message = `Delete <strong>${printerName}</strong>?<br><br>This action cannot be undone.`;

    if (!window.ConfirmDialog)
    {
        return;
    }

    ConfirmDialog.show(
        'Delete Printer',
        message,
        'Delete Printer',
        async () => {
            try
            {
                await callbacks.apiRequest(`/api/printers/${currentPrinterId}`, { method: 'DELETE' });
                close();
                callbacks.showToast?.('Printer deleted');
                await callbacks.loadPrinters?.();
            }
            catch (err)
            {
                console.error(err);
                callbacks.showToast?.(err.message || 'Failed to delete printer', true);
            }
        },
        true
    );
}

async function loadTemplate()
{
    const response = await fetch('features/printers/printer-dialog/printer-dialog.html' + V);
    const html = await response.text();
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    template = doc.querySelector('template');
}

window.PrinterDialogue = {
    init,
    showCreate,
    showEdit,
    close
};
