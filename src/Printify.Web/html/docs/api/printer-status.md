# Printer Status API

This document describes printer status workflows: start/stop, flag updates, on-demand reads, and status streaming.

Terminology:
- targetStatus: desired lifecycle state requested by the operator.
- state: listener runtime state (Starting/Started/Stopped/Error).
- HasError: ESC/POS status flag (protocol-level error bit), not a listener failure.

## Workflow Summary

Start/stop:
- PATCH /api/printers/{id}/realtime-status with targetStatus.
- Server starts/stops the listener synchronously and persists targetStatus to realtime status.
- On success, SSE state stream emits state updates.
- On failure, server returns 500 with a generic message: "Printer failed to start."

Flags and drawers:
- PATCH /api/printers/{id}/realtime-status with the desired flag(s).
- Only provided fields are updated; missing fields are left unchanged.
- Drawer state can only be "Closed" or "OpenedManually" via API.

Streaming:
- GET /api/printers/status/stream?scope=state or scope=full&printerId=...
- State scope streams only lifecycle updates (targetStatus/state).
- Full scope streams all available realtime fields for a single printer.

## Endpoints

### PATCH /api/printers/{id}/realtime-status

Updates targetStatus and/or realtime flags.

Request (TypeScript):
```ts
type PrinterTargetStatus = "Started" | "Stopped";
type DrawerState = "Closed" | "OpenedManually";

type UpdatePrinterRealtimeStatusRequestDto = {
  targetStatus?: PrinterTargetStatus;
  isCoverOpen?: boolean;
  isPaperOut?: boolean;
  isOffline?: boolean;
  hasError?: boolean;
  isPaperNearEnd?: boolean;
  drawer1State?: DrawerState;
  drawer2State?: DrawerState;
};
```

Notes:
- Any field can be omitted (null) to leave it unchanged.
- drawer1State/drawer2State accept only "Closed" or "OpenedManually".
- "OpenedByCommand" is rejected with 400.

Response (TypeScript):
```ts
type PrinterListenerState = "Starting" | "Started" | "Stopped" | "Error";

type PrinterRealtimeStatusDto = {
  printerId: string;
  targetState: PrinterTargetStatus;
  state: PrinterListenerState;
  updatedAt: string;
  bufferedBytes: number | null;
  isCoverOpen: boolean | null;
  isPaperOut: boolean | null;
  isOffline: boolean | null;
  hasError: boolean | null;
  isPaperNearEnd: boolean | null;
  drawer1State: DrawerState | null;
  drawer2State: DrawerState | null;
};
```

Status codes:
- 200: updated successfully.
- 400: invalid input (scope/enum values).
- 404: printer not found.
- 500: printer failed to start.

### GET /api/printers/{id}

Returns the printer metadata plus the latest realtime snapshot (if any).

Response (TypeScript excerpt):
```ts
type PrinterResponseDto = {
  id: string;
  displayName: string;
  protocol: string;
  widthInDots: number;
  heightInDots: number | null;
  tcpListenPort: number;
  emulateBufferCapacity: boolean;
  bufferDrainRate: number | null;
  bufferMaxCapacity: number | null;
  realtimeStatus: PrinterRealtimeStatusDto | null;
  isPinned: boolean;
  lastViewedDocumentId: string | null;
  lastDocumentReceivedAt: string | null;
};
```

### GET /api/printers

Returns the list of printers with realtimeStatus snapshots (if available).

### GET /api/printers/status/stream

Server-sent events for status updates.

Query parameters:
- scope: "state" | "full" (default: "state")
- printerId: required for scope=full

Events:
- event: state
- event: full

Payload (TypeScript):
```ts
type PrinterRealtimeStatusUpdateDto = {
  printerId: string;
  updatedAt: string;
  targetState: PrinterTargetStatus | null;
  state: PrinterListenerState | null;
  bufferedBytes: number | null;
  isCoverOpen: boolean | null;
  isPaperOut: boolean | null;
  isOffline: boolean | null;
  hasError: boolean | null;
  isPaperNearEnd: boolean | null;
  drawer1State: DrawerState | null;
  drawer2State: DrawerState | null;
};
```

Scope behavior:
- scope=state: only targetState/state are sent; all realtime payload fields are null.
- scope=full: all available fields may be present; fields not changed are omitted (null).

## Error Responses

Errors use ProblemDetails-like JSON:
```ts
type ProblemDetails = {
  status: number;
  detail: string;
  instance: string;
};
```
