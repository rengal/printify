# Printify — Technical Specification (Condensed)

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

## 9. Web UI (Detailed)

### Purpose

A fast, distraction-free viewer for rendered receipts with precise, debuggable context. Optimized for long sessions (operators/devs), large streams, and quick filtering.

### Layout & Navigation

* **Single-column feed** (centered, max content width \~900–1100px). **No left sidebar.**
* **Sticky top bar** with filter input, “Show all commands” toggle, and a live-updates switch.
* **Right metadata rail** (narrow fixed column) aligned with each document’s top. Shows compact meta and timeline ticks aligned to events inside the receipt.
* **Infinite scroll** with windowed virtualization (e.g., 25 items per page). Supports upwards and downwards loading, preserves scroll position on navigation.
* **Live updates** (SignalR): when enabled, inserts new documents at the top with a subtle highlight.

### Document Card

Each receipt is a “card” with:

* **Header strip**:
  `IP • timestamp • total size` (subtle gray). Hover shows full doc ID and retention deadline. Click IP to apply a filter.
* **Rendered receipt**: HTML/CSS that emulates paper width and monospaced layout; images merged from bands. Soft paper-like background, subtle shadow.
* **Inline badges** (when applicable): small tokens for non-printing events (Bell, Pulse, Buffer/Printer Status) positioned where they occurred (see below).
* **Actions** (right of header or kebab menu):

  * Copy raw bytes (hex)
  * Download `.bin`
  * View JSON (document + commands)
  * Toggle visibility of status/buffer events for this document only
  * Copy permalink

### Metadata Rail (Timeline)

* A slim vertical rail to the right mirrors activity **inside** the receipt:

  * **Top row** repeats: IP, timestamp, size (clickable).
  * **Timeline ticks**: tiny marks with icons for events (🔔 bell, ⚡ pulse, 🛈 status, ⌛ buffer).

    * Hover: exact sequence index, raw opcode, and parameters.
    * Click: smooth scroll to the corresponding inline badge in the receipt.
  * **Progress scale**: a faint ruler that approximates the receipt’s height for alignment.
* When **Show all commands** is off, the rail still shows ticks—even if inline badges are collapsed—so you never lose visibility of events.

### Command Visualization

* **Compact (default)**: consecutive non-printing commands are **collapsed** into a **Composite** badge (`3 events`) attached to the nearest printed line (previous by default). Zero visual elongation.
* **Expanded (Show all commands = on)**: every non-printing command renders as its own inline badge at the exact sequence position; the receipt becomes taller for full auditability.
* **Badges**:

  * Size and contrast tuned to remain unobtrusive.
  * Tooltip shows decoded details + raw bytes.
  * Click opens a flyout with the command’s JSON, copy buttons (JSON/hex), and “jump to next/prev same-type event”.
* **Placement rules**:

  * Between two printed lines: attach to previous (default) or next (configurable).
  * At document start or end: attach to document header/footer meta.

### Filters & Controls

* **Filter input** accepts IP, CIDR, CSV (e.g., `10.0.0.5,10.0.1.0/24`).

  * Press **Enter** or click **Apply** to refresh list.
  * Saved to cookie (`receipt_filters`) with timestamp.
* **Show all commands**: global toggle (also saved to cookie).
* **Live updates**: on/off; when off, a floating “New documents (N)” button appears if new items arrive.
* **Quick chips** below the input (optional): “has images”, “≥ size 1KB”, “has bell”, time ranges (“Last 1h/24h/7d”).

### Footer (Persistent)

A thin bottom bar across all pages:

* **Version / short commit** (click to copy)
* **Docs** link
* **Source** link
  Subtle gray, low visual weight; keyboard focusable.

### Performance & UX Niceties

* **Virtualized list** with dynamic height measurement; placeholders while rendering.
* **Progressive image decoding** for band-merged bitmaps.
* **Client cache** of recently viewed docs to avoid re-fetching.
* **Optimistic prepend** for live docs; reconcile on server ack.
* Smooth scroll and minimal animations (no large reflows).

### Empty / Loading / Error States

* **Empty with filter**: “No documents match this filter.” + “Clear filter” button.
* **Initial**: skeleton document cards (3–5).
* **Network error**: inline banner with retry; logs to console with correlation ID.

### Security/Safety in UI

* All user/ingested content is escaped; images are data URLs created client-side from safe blobs.
* JSON viewers use read-only pretty printers; copy actions sanitize control chars.

### Developer Hooks

* **Deep-linking**: `/doc/{id}` routes to a focused view with back-to-feed memory.
* **Query params**: `?ip=…&cidr=…&showAll=true&live=false`.
* **Event bus** (client): emitted on filter change, live state change, scroll to event, and doc loaded—useful for plugins/dev tools.

---

## 10. API
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

