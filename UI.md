# Printer UI Specification

## Layout

* **App Shell:** lightweight, ChatGPT-like screen with two main regions:

  * **Left Sidebar (≈300px):** printer navigation.
  * **Main Pane:** documents for the selected printer, shown as a chat-like stream.

---

## Sidebar

### Top

* **Button:** `[ + New printer ]`

  * Opens a dialog for creating a new printer (name, protocol/profile, sharing).
  * Newly created printer is auto-pinned and auto-selected.

### Sections

#### Pinned Printers

* **Two-line row format:**

  * **Line 1:** printer name (bold, truncates if long) + **new documents badge** (`● N`, hidden if `0`).
  * **Line 2 (muted):** `by <username> · <lastDocumentAt>` (relative time).
  * **Pinned indicator:** leading ★ symbol.
  * **Hover/focus actions:** show `...` button on the right.

    * Popup menu: **Edit**, **Unpin**.
* **Sorting:** drag-and-drop order (per user, persisted).

#### Other Printers

* **Minimal row format:**

  * Printer name (bold) + muted `by <username>`.
  * No badge, no timestamp.
  * **Hover/focus actions:** show `...` button on the right.

    * Popup menu: **Edit**, **Pin**.
* **Sorting:** alphabetically by **owner username**, then by **printer name**.

#### Example Rendering

```
[ + New printer ]

Pinned
★ Kitchen Printer                  ● 3
  by Alexey Ivanov · 12:40

★ Terrace Printer
  by Alexey Ivanov · Yesterday

Other
  Printer 1
  by Sergey Serveev

  Printer 2
  by Sergey Serveev
```

---

## Main Pane (Documents)

* **Header:** printer name, owner username, “Settings” button.
* **Stream:** conversation-like list of documents.

  * Each document: rendered preview (receipt/label), timestamp, actions (Download, Replay).
* **Behavior:**

  * Auto-scroll if near bottom when new docs arrive.
  * Show “New documents” toast if user scrolled away.
  * Selecting a printer resets its new-doc badge.

---

## Interactions

* **Row click:** select printer and load its documents.
* **Hover/focus row:** reveal `...` button; menu remains visible while open, closes on outside click or Esc.
* **Keyboard shortcuts:**

  * ↑ / ↓: navigate list.
  * Enter: open selected printer.
  * p: pin/unpin (pinned printers only).
  * Drag: reorder pinned printers.
* **Create flow:** “New printer” → dialog → create → auto-pin and auto-select.

---

