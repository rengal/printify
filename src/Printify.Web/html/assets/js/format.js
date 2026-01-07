function formatNumberWithNarrowNoBreakSpaces(value) {
    const normalized = Number.isFinite(value) ? Math.trunc(value) : 0;
    return normalized.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '\u202F');
}

function formatByteCount(bytes) {
    if (bytes === null || bytes === undefined) {
        return '0';
    }

    const normalized = Number(bytes);
    return formatNumberWithNarrowNoBreakSpaces(Number.isFinite(normalized) ? normalized : 0);
}
