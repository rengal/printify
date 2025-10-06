# Printify — Technical Specification

## 1. Overview

Cross-platform .NET service that:

* Listens for ESC/POS data on TCP (**9100**)
* Parses streams → typed commands → `Document`
* Persists docs via SQLite (images in blob storage)
* Renders receipts as HTML for browsing in a minimal Web UI (**8080**)

Target \~1,000 docs/day, moderate bursts.

**Branding**: *Printify* — *“Receive · Parse · Render receipts (multi-protocol)”*

---

## 2. Goals

* Accept ESC/POS streams, parse into object model
* Render text, styles, raster images (`GS v 0`)
* Persist for later viewing, with filters + infinite scroll UI
* Extensible for other protocols (ZPL, EPL, etc.)

**Non-Goals (MVP)**: full vendor-specific features, exact font emulation.

---

## 3. Architecture

```
[Clients] -> TCP:9100 -> EscPosTokenizer Session -> SQLite-backed IRecordStorage
```

Components:

* TcpListenerService
* EscPosTokenizer + session pipeline
* IRecordStorage abstraction (SQLite default) and rendering planned later
* Persistence (SQLite + blob storage adapter)
* ASP.NET Core host (health endpoint today; API/UI planned)

---

## 4. Configuration

JSON/YAML + env var overrides (`PRINTIFY_…`). Example:

```json
{
  "listeners": { "escpos_port": 9100, "http_port": 8080 },
  "epson": { "width_dots": 512, "fonts": { "f0": { "chars": 48 } } },
  "storage": { "provider": "sqlite", "databasePath": "printify.db", "retention_days": 90 },
  "ui": { "filterCookieName": "receipt_filters" }
}
```

---

## 5. Data Model

Documents are persisted through `IRecordStorage` with a SQLite backing store.

- `documents`: `id` INTEGER PRIMARY KEY AUTOINCREMENT, `timestamp` TEXT (UTC ISO-8601), `protocol` TEXT, `source_ip` TEXT NULL.
- `document_elements`: `document_id` INTEGER, `sequence` INTEGER, `type` TEXT, `payload` TEXT (JSON representation of the element record).

Elements capture semantic events only; raw ESC/POS buffers remain with the caller.

Indexes: primary key on `documents.id` plus optional composite on (`timestamp`, `source_ip`). Retention: caller-driven cleanup (default 90 days).

---

## 6. Object Model (C#)

```csharp
public sealed record Document(
    long Id,
    DateTimeOffset Timestamp,
    Protocol Protocol,
    string? SourceIp,
    IReadOnlyList<Element> Elements);

public abstract record Element(int Sequence);

public sealed record TextLine(int Sequence, string Text) : Element(Sequence);

public sealed record RasterImage(
    int Sequence,
    int Width,
    int Height,
    byte Mode,
    string BlobId,
    string ContentType,
    long ContentLength,
    string? Checksum) : Element(Sequence);

// Additional derived records: Bell, Pulse, BufferStatus, PrinterStatus, PageCut, Error, etc.
```

---

## 7. Parsing

* Tokenize ESC (0x1B) and GS (0x1D) sequences.
* Session appends concrete `Element` records directly—no separate document builder layer.
* Detect images (`GS v 0`, `GS ( 8`) and persist via blob storage when encountered.
* `Complete` flushes buffered text and callers wrap `ITokenizerSession.Elements` into a `Document`.

---

## 8. Rendering

* Monospaced font, CSS width ≈ configured chars
* Inline images as `<img>`/`<canvas>`
* Inline badges for Bell, Pulse, BufferStatus, PrinterStatus (compact or expanded)
* Line-level metatag conversion via `MetaTagConverter.ConvertLineToMetaTag()`
* Collapsing: group stacked non-printing commands into a `CompositeNonPrintingCommand`

---

## 9. Web UI

### Purpose

A printer management interface that allows operators to configure multiple printers, monitor their activity, and view documents processed by each printer. Optimized for managing fleets of receipt/label printers with different protocols and configurations.

### Layout & Navigation

* **Three-column grid layout**:
  * Left sidebar (collapsible) for printer list navigation
  * Top bar with sidebar toggle, current context title, and theme switcher
  * Main content area for document viewing or welcome screen
* **Sidebar organization**:
  * "New printer" action at top (shows as "temporary" when not signed in)
  * Pinned printers section (user-customizable)
  * Other printers section (grouped by owner)
  * User menu in footer with sign in/out controls
* **Responsive sidebar**: animated visibility toggle with state persisted to localStorage
* **No right metadata rail**: all printer/document metadata shown inline within main content

### Printer Management

Each printer in sidebar shows:

* **Icon** (pushpin for pinned, printer icon for others)
* **Name** with optional badge showing count of new/unread documents
* **Last document timestamp** (formatted as relative time + absolute datetime)
* **Context menu** for Edit/Pin/Unpin actions
* **Active state** highlighting when selected

**Printer configuration includes**:
* Name, owner, protocol (ESC/POS, Star Line Mode, ZPL, EPL)
* Paper width in dots
* Optional buffer emulation (size in bytes, drain rate)
* Pin status and ordering

### Document Display

* **Per-printer view**: documents shown only for currently selected printer
* **Document cards** with:
  * White monospace preview area showing receipt content
  * Timestamp (relative format)
  * Action buttons: Download, Replay
* **Empty states**:
  * Not signed in: feature overview with sign-in prompt
  * Signed in, no printer selected: welcome message
  * Printer selected, no documents: "No documents yet" placeholder

### User System

* **Anonymous mode**: users can create temporary printers without signing in
  * Temporary printers marked with dashed border
  * Warning shown that printers will be lost on session end
* **Signed-in mode**:
  * User avatar with initials in sidebar footer
  * Option to persist temporary printers on login
  * Printers owned by user, organized by name
* **Authentication UI**:
  * Login dialog with user selection/creation
  * Switch user capability
  * Logout with cleanup of temporary data
  * State persisted to localStorage

### Modals & Dialogs

* **New/Edit Printer**: modal form with validation
  * Required fields marked with asterisk
  * Inline error display for validation failures
  * Field groups for related settings
  * Conditional buffer emulation fields
* **Login Dialog**: simple user selection with datalist
  * Optional checkbox to save temporary printers
  * Counter showing number of printers to persist

### Filters & Controls

**Current implementation**: None (documents filtered by printer selection only)

**Planned**: IP filtering, date ranges, command type filters for future document feed view

### Theme System

* **Dark mode** (default): high-contrast with accent color
* **Light mode**: clean white with subtle borders
* Toggle button in top bar with animated icon transition
* Preference saved to localStorage
* Smooth color transitions on theme change

### Toast Notifications

* Fixed positioning (top-right corner)
* Success/info states with color coding
* Auto-dismiss with timer
* Slide-in animation from right

### Performance & UX

* **Grid layout** with CSS transitions for smooth state changes
* **Keyboard focus** management for accessibility
* **Hover states** and active feedback on interactive elements
* **Menu positioning**: context-sensitive placement for printer/user menus
* **State persistence**: sidebar visibility, theme, user session, filter preferences

### Security/Safety in UI

* Content escaping for user-provided printer names and metadata
* Client-side validation before submission
* No authentication backend in MVP (trusted network assumption)

### Developer Hooks

* localStorage keys: `theme`, `sidebarHidden`, `currentUser`, `receipt_filters`
* Modal system via DOM injection
* Toast system with auto-cleanup
* Theme attribute on root element

## 9. Web UI

---

## 10. API

* `GET /api/documents?limit&beforeId&sourceIp` � paged document feed (newest first). Limit defaults to 20.
* `GET /api/documents/{id}?includeContent` � full document payload. Set `includeContent=true` to hydrate raster bytes.
* `POST /api/users` � create a user (`SaveUserRequest`).
* `GET /api/users/{id}` � fetch a user.
* `POST /api/printers` � register a printer (`SavePrinterRequest`).
* `GET /api/printers/{id}` � fetch printer metadata.
* `GET /api/media/{mediaId}` � stream raster/image bytes for a single blob.

*Health/metrics endpoints remain `/health` today; `/healthz`, `/ready`, `/metrics` stay on the backlog alongside live updates (`/hub/notifications`).

---
## 11. Security & Ops

* No auth / no rate limit (MVP, trusted nets)
* Input sanitization required
* Monitoring: logs, error counters, metrics
* Graceful shutdown on SIGTERM
* Secrets/config via env vars

---

## 12. Testing

* Unit tests: tokenizer, command factory
* Integration: TCP client → doc persistence/rendering
* Fuzzing: random bytes
* UI E2E: infinite scroll, filters, cookie persistence

---

## 13. Roadmap (epics)

1. Skeleton + DB schema + TCP listener
2. Tokenizer + basic parsing + persistence
3. Renderer + UI feed + filters
4. Raster image parsing + storage
5. SignalR live updates + pruning
6. Hardening: monitoring, CI/CD, containerization

---

## 14. Containerization

* Env var overrides (12-factor)
* Ports: 8080 (web), 9100 (tcp)
* Health/readiness endpoints
* Log to stdout/stderr
* Run as non-root, handle SIGTERM gracefully
* Dockerfile + docker-compose (SQLite volume) + K8s probes

---

## 15. Extensibility

* `IPrinterProtocol` interface for protocol plugins
* Config: `supported_protocols: ["escpos","zpl"]`
* Status reply support (`ready` by default)
* Extra commands: PageCut, Bell, Pulse, BufferStatus, PrinterStatus
* Config flags: `replyToStatus`, `recordBellPulse`, `resetBufferOnStatus`

---

