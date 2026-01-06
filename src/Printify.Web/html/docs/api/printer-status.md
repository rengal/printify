# Printer Status API

This document describes printer status workflows: start/stop, operational flags, drawer control, on-demand reads, and streaming.

Notes:
- JSON responses omit null fields globally.
- Drawer state "OpenedByCommand" is emitted by ESC/POS pulses and cannot be set via API.

## Types (TypeScript)

type PrinterTargetState = "Started" | "Stopped";

type PrinterListenerState = "Starting" | "Started" | "Stopped" | "Error";

type DrawerState = "Closed" | "OpenedManually" | "OpenedByCommand";

type PrinterDto = {
  id: string;
  displayName: string;
  isPinned: boolean;
  lastViewedDocumentId: string | null;
  lastDocumentReceivedAt: string | null;
};

type PrinterSettingsDto = {
  protocol: "EscPos";
  widthInDots: number;
  heightInDots: number | null;
  tcpListenPort: number;
  emulateBufferCapacity: boolean;
  bufferDrainRate: number | null;
  bufferMaxCapacity: number | null;
};

type PrinterOperationalFlagsDto = {
  printerId: string;
  targetState: PrinterTargetState;
  updatedAt: string;
  isCoverOpen: boolean;
  isPaperOut: boolean;
  isOffline: boolean;
  hasError: boolean;
  isPaperNearEnd: boolean;
};

type PrinterRuntimeStatusDto = {
  printerId: string;
  state: PrinterListenerState;
  updatedAt: string;
  bufferedBytes: number | null;
  drawer1State: DrawerState | null;
  drawer2State: DrawerState | null;
};

type PrinterResponseDto = {
  printer: PrinterDto;
  settings: PrinterSettingsDto;
  operationalFlags: PrinterOperationalFlagsDto | null;
  runtimeStatus: PrinterRuntimeStatusDto | null;
};

type PrinterSidebarSnapshotDto = {
  printer: PrinterDto;
  runtimeStatus?: PrinterRuntimeStatusDto;
};

type PrinterRuntimeStatusUpdateDto = {
  state?: PrinterListenerState;
  updatedAt: string;
  bufferedBytes?: number | null;
  drawer1State?: DrawerState | null;
  drawer2State?: DrawerState | null;
};

type PrinterOperationalFlagsUpdateDto = {
  printerId: string;
  updatedAt: string;
  targetState?: PrinterTargetState;
  isCoverOpen?: boolean;
  isPaperOut?: boolean;
  isOffline?: boolean;
  hasError?: boolean;
  isPaperNearEnd?: boolean;
};

type PrinterStatusUpdateDto = {
  printerId: string;
  updatedAt: string;
  runtime?: PrinterRuntimeStatusUpdateDto;
  operationalFlags?: PrinterOperationalFlagsUpdateDto;
  settings?: PrinterSettingsDto;
  printer?: PrinterDto;
};

## Requests

type CreatePrinterRequestDto = {
  printer: { id: string; displayName: string };
  settings: {
    protocol: "EscPos";
    widthInDots: number;
    heightInDots: number | null;
    emulateBufferCapacity: boolean;
    bufferDrainRate: number | null;
    bufferMaxCapacity: number | null;
  };
};

type UpdatePrinterRequestDto = {
  printer: { id: string; displayName: string };
  settings: {
    protocol: "EscPos";
    widthInDots: number;
    heightInDots: number | null;
    emulateBufferCapacity: boolean;
    bufferDrainRate: number | null;
    bufferMaxCapacity: number | null;
  };
};

type UpdatePrinterOperationalFlagsRequestDto = {
  isCoverOpen?: boolean;
  isPaperOut?: boolean;
  isOffline?: boolean;
  hasError?: boolean;
  isPaperNearEnd?: boolean;
  targetState?: PrinterTargetState;
};

type UpdatePrinterDrawerStateRequestDto = {
  drawer1State?: "Closed" | "OpenedManually";
  drawer2State?: "Closed" | "OpenedManually";
};

## Endpoints

### POST /api/printers
Creates a printer.

Response: PrinterResponseDto

### PUT /api/printers/{id}
Updates printer metadata and settings.

Response: PrinterResponseDto

### GET /api/printers
Lists full printer snapshots.

Response: PrinterResponseDto[]

### GET /api/printers/{id}
Reads a full printer snapshot.

Response: PrinterResponseDto

### PATCH /api/printers/{id}/operational-flags
Partial update of operational flags.
Setting targetState starts or stops the printer listener.

Response: PrinterOperationalFlagsDto

### PATCH /api/printers/{id}/drawers
Manual drawer open/close (OpenedByCommand cannot be set via API).

Response: PrinterRuntimeStatusDto

## Streaming

### GET /api/printers/sidebar
Returns sidebar snapshots (metadata + state-only runtime).

Response: PrinterSidebarSnapshotDto[]

### GET /api/printers/sidebar/stream
Server-sent events for sidebar updates.

Event: sidebar
Payload: PrinterSidebarSnapshotDto

### GET /api/printers/{id}/runtime/stream
Server-sent events for active printer updates.
Payload is partial; only changed sections are present (runtime/operationalFlags/settings/printer).

Event: status
Payload: PrinterStatusUpdateDto

## Error Responses

Errors use ProblemDetails-like JSON:

type ProblemDetails = {
  status: number;
  detail: string;
  instance: string;
};
