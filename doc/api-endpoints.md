# Printify Web API — contract reference (for autotests)

Developer-facing contract for the Printify Web API. This file is **not** served to
end users (the served docs live in `src/Printify.Web/html/docs/`); it is the
reference for integration/autotests.

> **Scope — two protocols.** Managing workspaces, printers and reading documents is
> **HTTP/REST** (this document + Swagger). **Printing itself is raw TCP**: a client
> opens a socket to the printer's listener (`Settings.TcpListenPort` /
> `Settings.PublicHost`) and writes ESC/POS or EPL bytes. The printed document then
> becomes readable over HTTP. The TCP step is **not** in OpenAPI — see
> [End-to-end scenario](#end-to-end-scenario).

There is also a live OpenAPI spec generated from the controllers (covers the HTTP
surface only): **`/swagger`** (UI) and **`/swagger/v1/swagger.json`** (raw), enabled
in every environment except Production.

---

## Conventions

- **Base path:** all endpoints are under `/api` (except the SSE notes below, which are
  also under `/api`).
- **JSON casing:** **PascalCase** (no camelCase policy is configured). Properties are
  emitted as `Token`, `AccessToken`, `Id`, etc.
- **Null omission:** properties that are `null` are omitted from responses
  (`DefaultIgnoreCondition = WhenWritingNull`). The one exception is
  `Printer.LastDocumentReceivedAt`, which is always emitted (may be `null`).
- **Auth header:** `Authorization: Bearer <AccessToken>` on every `[Authorize]`
  endpoint. Get the token from [`POST /api/auth/login`](#post-apiauthlogin).
- **IDs are client-supplied GUIDs.** Create requests for workspaces and printers carry
  the `Id` — the caller generates it. This makes creates idempotent-ish and easy to
  assert against in tests.

### Error format

All unhandled exceptions are converted by `ExceptionHandlingMiddleware` to
`application/problem+json`:

```json
{ "Status": 404, "Detail": "Printer not found.", "Instance": "/api/printers/3f2a…" }
```

| Exception                          | HTTP status |
| ---------------------------------- | ----------- |
| `AuthenticationFailedException`    | **401**     |
| `ForbiddenException`               | **403**     |
| `PrinterNotFoundException`         | **404**     |
| `BadRequestException`, `ArgumentException`, `ValidationException` | **400** |
| `PrinterListenerStartFailedException` | **500**  |
| any other                          | **500**     |

`OperationCanceledException` and `StreamDisconnectedException` produce **no** response
body (expected client/SSE disconnects).

---

## End-to-end scenario

The canonical flow every printing test follows: **register → login → create printer →
print over TCP → verify the parsed document**. (This is the flow encoded in
`ProtocolTestsBase` / `PrintersControllerTests.Documents`.)

```
 1. POST /api/workspaces            ── HTTP ─▶  { Id, Name, Token }
 2. POST /api/auth/login (Token)    ── HTTP ─▶  { AccessToken }       → Authorization: Bearer
 3. POST /api/printers              ── HTTP ─▶  { …, Settings: { TcpListenPort, PublicHost } }
 4. connect PublicHost:TcpListenPort── TCP  ─▶  write ESC/POS bytes, then close (or idle-timeout)
 5. GET  …/{id}/documents/canvas    ── HTTP ─▶  CanvasDocumentListResponseDto  (assert here)
```

### 1. Register a workspace (anonymous)

```bash
curl -sX POST http://localhost:8080/api/workspaces \
  -H 'Content-Type: application/json' \
  -d '{ "Id": "11111111-1111-1111-1111-111111111111", "WorkspaceName": "autotest" }'
```
```json
{ "Id": "11111111-1111-1111-1111-111111111111", "Name": "autotest",
  "Token": "brave-tiger-1042-a1b2c3d4e5f60718" }
```
The `Token` is shown **once**, at creation. Persist it for step 2.

### 2. Login → access token (anonymous)

```bash
curl -sX POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{ "Token": "brave-tiger-1042-a1b2c3d4e5f60718" }'
```
```json
{ "AccessToken": "eyJhbGciOi…", "TokenType": "Bearer", "ExpiresInSeconds": 86400,
  "Workspace": { "Id": "1111…", "Name": "autotest", "Role": "User",
                 "DocumentRetentionDays": 90, "TcpWhitelistEnabled": false,
                 "TcpWhitelistEntries": "", "CreatedAt": "2026-06-15T10:00:00+00:00" } }
```
Use `AccessToken` as `Authorization: Bearer …` for all subsequent calls.

### 3. Create a printer

```bash
curl -sX POST http://localhost:8080/api/printers \
  -H 'Authorization: Bearer eyJhbGciOi…' -H 'Content-Type: application/json' \
  -d '{ "Printer":  { "Id": "22222222-2222-2222-2222-222222222222", "DisplayName": "T-88" },
        "Settings": { "Protocol": "EscPos", "WidthInDots": 512, "HeightInDots": null,
                      "EmulateBufferCapacity": false, "BufferDrainRate": null,
                      "BufferMaxCapacity": null } }'
```
Response (`PrinterResponseDto`) — note **`Settings.TcpListenPort`** and
**`Settings.PublicHost`**: that is where you print.
```json
{ "Printer":  { "Id": "2222…", "DisplayName": "T-88", "OwnerWorkspaceId": "1111…",
                "OwnerWorkspaceName": "autotest", "IsPinned": false,
                "LastViewedDocumentId": null, "LastDocumentReceivedAt": null },
  "Settings": { "Protocol": "EscPos", "WidthInDots": 512, "HeightInDots": null,
                "TcpListenPort": 9101, "EmulateBufferCapacity": false,
                "BufferDrainRate": null, "BufferMaxCapacity": null,
                "PublicHost": "localhost" },
  "OperationalFlags": { "PrinterId": "2222…", "TargetState": "Started", … },
  "RuntimeStatus":    { "PrinterId": "2222…", "State": "Started", … } }
```

### 4. Print — raw TCP (NOT HTTP)

Open a socket to `PublicHost:TcpListenPort` and write the print bytes. A document is
finalized when the connection closes **or** after the listener idle-timeout.

```bash
printf 'Hello, world\n\n\n' | nc localhost 9101      # or any ESC/POS byte stream
```
In C# tests this is short-circuited via `TestPrinterListenerFactory` /
`TestPrinterChannel.SendToServerAsync(bytes)` instead of a real socket — a **real
external integration test must open the actual TCP socket** on `TcpListenPort`.

### 5. Verify the parsed document (HTTP)

```bash
curl -s 'http://localhost:8080/api/printers/2222…/documents/canvas?limit=10' \
  -H 'Authorization: Bearer eyJhbGciOi…'
```
Returns `CanvasDocumentListResponseDto` — assert against
`Result.Items[].Canvases[].Items` (the rendered elements). To wait for the document
instead of polling, subscribe to the SSE stream
`GET …/{id}/documents/canvas/stream` (see [SSE](#sse-streaming-endpoints)).

---

## Endpoint reference

`A` = requires `Authorization: Bearer`. `—` = anonymous.

### Auth — `/api/auth`

#### POST /api/auth/login
`—` · Exchange a workspace token for a JWT. → **200** `LoginResponseDto`; **401** if the
token is unknown.
```
Request : LoginRequestDto(string Token)
Response: LoginResponseDto(string AccessToken, string TokenType, long ExpiresInSeconds, WorkspaceDto Workspace)
```

#### POST /api/auth/logout
`A` · No-op placeholder (JWT is stateless). → **200**.

### Workspaces — `/api/workspaces`

#### POST /api/workspaces
`—` · Create a workspace. → **200** `WorkspaceResponseDto` (**includes the one-time `Token`**).
```
Request : CreateWorkspaceRequestDto(Guid Id, string WorkspaceName)
Response: WorkspaceResponseDto(Guid Id, string Name, string Token)
```

#### GET /api/workspaces
`A` · Current workspace. → **200** `WorkspaceDto` (no `Token`).
```
WorkspaceDto(Guid Id, string Name, DateTimeOffset CreatedAt, string Role,
             int DocumentRetentionDays, bool TcpWhitelistEnabled, string TcpWhitelistEntries)
```

#### PATCH /api/workspaces
`A` · Partial update (all fields optional/nullable). → **200** `WorkspaceDto`; **400** on validation.
```
Request: UpdateWorkspaceRequestDto(string? Name, int? DocumentRetentionDays,
                                   bool? TcpWhitelistEnabled, string? TcpWhitelistEntries)
```

#### DELETE /api/workspaces
`A` · Delete the current workspace and its data. → **204**.

#### GET /api/workspaces/summary
`A` · → **200** `WorkspaceSummaryDto(int TotalPrinters, long TotalDocuments, long DocumentsLast24h, DateTimeOffset? LastDocumentAt, DateTimeOffset CreatedAt)`.

#### GET /api/workspaces/admin-statistics
`A` (admin) · → **200** `AdminWorkspaceStatisticsDto`; **403** for non-admin workspaces.
Contains aggregate counts plus `Workspaces: AdminWorkspaceStatisticsRowDto[]`
(per-workspace rows). See `AdminWorkspaceStatisticsDto.cs` for the full field list.

#### GET /api/workspaces/greeting
`—` · Localized greeting strings, cached 300 s. → **200**
`GreetingResponseDto(string? Morning, string? Afternoon, string? Evening, string General)`.

#### GET /api/workspaces/retention/cleanup-summary
`A` · Preview of what a retention cleanup would delete. → **200**
`DocumentRetentionCleanupSummaryDto(int ExpiredDocuments, int RetentionMediaFiles)`.

#### POST /api/workspaces/retention/cleanup
`A` · Run a cleanup. → **200** `DocumentRetentionCleanupResultDto(int DeletedDocuments, int DeletedMedia)`.
```
Request: RunDocumentRetentionCleanupRequestDto(int MaxDocuments, int? RetentionDaysOverride)
```
> Admin note: a `RetentionDaysOverride` of `0` deletes **everything**, across all
> workspaces. Use with care in shared test environments.

#### GET /api/workspaces/connections
`A` · Recent TCP connection attempts (for the whitelist UI). → **200**
`TcpConnectionEntryDto(string ClientIp, DateTimeOffset ConnectedAt, bool Allowed, string ConnectionType)[]`.

### Printers — `/api/printers`

#### POST /api/printers
`A` · Create a printer (and start its TCP listener). → **200** `PrinterResponseDto`.
```
Request : CreatePrinterRequestDto(PrinterDto Printer, PrinterSettingsDto Settings)
  PrinterDto(Guid Id, string DisplayName)
  PrinterSettingsDto(string Protocol, int WidthInDots, int? HeightInDots,
                     bool EmulateBufferCapacity, decimal? BufferDrainRate, int? BufferMaxCapacity)
Response: PrinterResponseDto(PrinterDto Printer, PrinterSettingsDto Settings,
                            PrinterOperationalFlagsDto? OperationalFlags,
                            PrinterRuntimeStatusDto? RuntimeStatus)
```
Response DTOs:
```
PrinterDto         (Guid Id, string DisplayName, Guid OwnerWorkspaceId, string? OwnerWorkspaceName,
                    bool IsPinned, Guid? LastViewedDocumentId, DateTimeOffset? LastDocumentReceivedAt)
PrinterSettingsDto (string Protocol, int WidthInDots, int? HeightInDots, int TcpListenPort,
                    bool EmulateBufferCapacity, decimal? BufferDrainRate, int? BufferMaxCapacity, string PublicHost)
PrinterOperationalFlagsDto(Guid PrinterId, string TargetState, DateTimeOffset UpdatedAt,
                    bool IsCoverOpen, bool IsPaperOut, bool IsOffline, bool HasError, bool IsPaperNearEnd)
PrinterRuntimeStatusDto(Guid PrinterId, string State, DateTimeOffset UpdatedAt,
                    int? BufferedBytes, int? BufferedBytesDeltaBps, string? Drawer1State, string? Drawer2State)
```
`Protocol` is `EscPos` or `Epl`. `State`/`TargetState` are `Started` / `Stopped`.

#### GET /api/printers
`A` · All printers for the workspace. → **200** `PrinterResponseDto[]`.

#### GET /api/printers/{id}
`A` · One printer. → **200** `PrinterResponseDto`; **404** if not in the workspace.

#### PUT /api/printers/{id}
`A` · Replace printer + settings (same body shape as create). → **200** `PrinterResponseDto`.
```
Request: UpdatePrinterRequestDto(PrinterDto Printer, PrinterSettingsDto Settings)
```

#### DELETE /api/printers/{id}
`A` · Soft-delete. → **204**.

#### POST /api/printers/{id}/pin
`A` · Pin/unpin. → **200** `PrinterResponseDto`.
```
Request: PinPrinterRequestDto(bool IsPinned)
```

#### PATCH /api/printers/{id}/operational-flags
`A` · Set emulated hardware flags and/or `TargetState` (Started/Stopped). → **200** `PrinterOperationalFlagsDto`.
```
Request: UpdatePrinterOperationalFlagsRequestDto(bool? IsCoverOpen, bool? IsPaperOut, bool? IsOffline,
                                                 bool? HasError, bool? IsPaperNearEnd, string? TargetState = null)
```

#### PATCH /api/printers/{id}/drawers
`A` · Set emulated cash-drawer state. → **200** `PrinterRuntimeStatusDto`.
```
Request: UpdatePrinterDrawerStateRequestDto(string? Drawer1State, string? Drawer2State)
```

#### GET /api/printers/{id}/documents/canvas
`A` · Paged rendered documents (newest first). → **200** `CanvasDocumentListResponseDto`.
Query: `GetDocumentsRequestDto(int Limit = 20, Guid? BeforeId = null)`.
```
CanvasDocumentListResponseDto(PagedResult<RenderedDocumentDto> Result)
PagedResult<T>(IReadOnlyList<T> Items, bool HasMore, Guid? NextBeforeId, DateTimeOffset? NextBeforeCreatedAt)
RenderedDocumentDto(Guid Id, Guid PrintJobId, Guid PrinterId, DateTimeOffset Timestamp, string Protocol,
                    CanvasDto[] Canvases, string? ClientAddress, int BytesReceived, int BytesSent,
                    string[]? ErrorMessages)
CanvasDto(int WidthInDots, int? HeightInDots, IReadOnlyList<CanvasElementDto> Items)
```
`CanvasElementDto` is a polymorphic hierarchy — `CanvasTextElementDto`,
`CanvasImageElementDto`, `CanvasLineElementDto`, `CanvasBoxElementDto`,
`CanvasDebugElementDto`. See `Canvas/Elements/CanvasElementDto.cs` for each shape.

#### DELETE /api/printers/{id}/documents
`A` · Clear all documents for the printer. → **204**.

#### POST /api/printers/{id}/documents/import
`A` · Import a raw print payload (base64) as if it had been printed. → **204**; **400** on invalid base64.
```
Request: ImportDocumentRequestDto(string Data)   // base64
```

#### POST /api/printers/{id}/documents/last-viewed
`A` · **Stub — returns 501 Not Implemented.** `SetLastViewedDocumentRequestDto(Guid DocumentId)`.

### Media — `/api/media`

#### GET /api/media/{mediaId}
`—` · Download media (image raster referenced by a canvas element). → **200** binary
with `ETag: "sha256:<checksum>"`; **404** if unknown.

---

## SSE (streaming) endpoints

`text/event-stream`; each event `data:` is the JSON of the noted DTO. These are not
request/response and are poorly represented in OpenAPI — documented here instead.
Cancel by closing the connection (server treats it as a normal disconnect).

| Endpoint | Auth | Emits |
| --- | --- | --- |
| `GET /api/printers/sidebar/stream` | `A` | `PrinterSidebarSnapshotDto` updates |
| `GET /api/printers/{id}/runtime/stream` | `A` | `PrinterRuntimeStatusDto` updates |
| `GET /api/printers/{id}/documents/canvas/stream` | `A` | `RenderedDocumentDto` on each completed print (**404** if the printer isn't visible) |

---

## Test-harness notes

- **JWT secret guard:** the app exits at startup if `Jwt:SecretKey` is missing, shorter
  than 32 chars, or still the `your-secret-key…` placeholder. Integration hosts must
  supply a valid secret (≥32 chars).
- **In-memory DB:** `dotnet test` swaps SQLite for a named in-memory database; a keeper
  connection in `ApiFactory` keeps it alive for the test's lifetime.
- **Static files / docs** are skipped when the environment is `Test`.
- **Document completion** happens on TCP socket close or after the listener idle-timeout
  (`PrinterConstants.ListenerIdleTimeoutMs`) — give the document a moment, or use the
  SSE stream, before asserting on `…/documents/canvas`.
