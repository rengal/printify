# Lightweight UI Guidelines (Razor + CSS) — Chat‑style, Dark/Light

These guidelines are paste‑ready for prompts and handoffs. Everything below is self‑contained with **embedded CSS** and a tiny **theme switcher** (vanilla JS). Stack: ASP.NET + Razor (no server‑side JS required). Licensable assets: Inter font (OFL), Lucide/Heroicons (MIT/ISC).

---

## 0) Drop‑in CSS & Theme Switcher (inline)

Copy this into your shared Razor layout (`_Layout.cshtml`) or a partial included on all pages.

```html
<!-- UI Core Styles + Theme Tokens -->
<style>
/* --------------
   COLOR TOKENS
   -------------- */
:root {
  /* Dark is the default to avoid a white flash when toggling */
  --bg: #0e0f12;           /* app background */
  --bg-elev: #15171c;      /* cards, panels */
  --text: #e9eaee;         /* primary text */
  --muted: #b3b7c0;        /* secondary text */
  --border: #2a2d34;       /* dividers, card borders */
  --accent: #10b981;       /* primary action */
  --accent-hover: #0ea372; /* primary hover */
  --danger: #ef4444;       /* errors */
  --warn: #f59e0b;         /* warnings */
  --ok: #22c55e;           /* success */
  --focus: #60a5fa;        /* focus ring */
}

:root[data-theme="light"] {
  --bg: #f7f8fa;
  --bg-elev: #ffffff;
  --text: #0f1115;
  --muted: #585f6b;
  --border: #e5e7eb;
  --accent: #0ea372;
  --accent-hover: #0a8b61;
}

/* --------------
   BASE & TYPOGRAPHY
   -------------- */
@font-face{
  font-family: 'Inter';
  font-style: normal;
  font-weight: 100 900;
  font-display: swap;
  src: local('Inter'), local('Inter Variable');
}
html, body { height: 100%; }
html { color-scheme: light dark; }
body {
  margin: 0;
  background: var(--bg);
  color: var(--text);
  font: 400 16px/1.5 Inter, ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, Ubuntu, Cantarell, Helvetica, Arial, "Apple Color Emoji", "Segoe UI Emoji";
  -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale;
}

h1 { font-size: 30px; line-height: 38px; font-weight: 600; margin: 0 0 12px; }
h2 { font-size: 24px; line-height: 32px; font-weight: 600; margin: 24px 0 12px; }
h3 { font-size: 20px; line-height: 28px; font-weight: 600; margin: 16px 0 8px; }
p, ul, ol { margin: 8px 0 12px; }
small { font-size: 14px; line-height: 20px; color: var(--muted); }

/* --------------
   LAYOUT PRIMITIVES
   -------------- */
.container {
  display: grid;
  grid-template-columns: 280px 1fr;
  grid-template-rows: 48px 1fr;
  grid-template-areas:
    "sidebar topbar"
    "sidebar main";
  height: 100dvh;
}
.sidebar { grid-area: sidebar; background: var(--bg-elev); border-right: 1px solid var(--border); display: flex; flex-direction: column; gap: 12px; padding: 16px; }
.topbar { grid-area: topbar; display: flex; align-items: center; justify-content: space-between; padding: 0 16px; border-bottom: 1px solid var(--border); background: var(--bg); }
.main { grid-area: main; padding: 24px; display: block; }
.main-inner { max-width: 1200px; margin: 0 auto; display: grid; gap: 16px; }

/* Responsive: collapse sidebar */
@media (max-width: 1024px) { .container { grid-template-columns: 72px 1fr; } .sidebar { padding: 12px 8px; } }
@media (max-width: 768px) {
  .container { grid-template-columns: 1fr; grid-template-rows: 48px 1fr; grid-template-areas: "topbar" "main"; }
  .sidebar { position: fixed; inset: 0 30% 0 0; transform: translateX(-100%); transition: transform .25s ease; z-index: 40; }
  .sidebar.is-open { transform: translateX(0); }
}

/* --------------
   UTILITIES
   -------------- */
.w100{width:100%}
.p8{padding:8px} .p12{padding:12px} .p16{padding:16px} .p24{padding:24px}
.g8{gap:8px} .g12{gap:12px} .g16{gap:16px} .g24{gap:24px}
.row{display:flex; align-items:center; gap:12px}
.col{display:flex; flex-direction:column; gap:12px}
.muted{color:var(--muted)}

/* --------------
   CARDS
   -------------- */
.card { background: var(--bg-elev); border:1px solid var(--border); border-radius:12px; }
.card:hover { box-shadow: 0 2px 10px rgba(0,0,0,0.15); }
.card-header { padding: 12px 16px; border-bottom: 1px solid var(--border); font-weight: 600; }
.card-body { padding: 16px; }
.card-footer { padding: 12px 16px; border-top: 1px solid var(--border); }

/* --------------
   BUTTONS
   -------------- */
.btn { border-radius:10px; padding:0 14px; height:36px; border:1px solid var(--border); background:var(--bg-elev); color:var(--text); display:inline-flex; align-items:center; gap:8px; cursor:pointer; user-select:none; }
.btn:hover { filter: brightness(1.03); }
.btn:active { transform: scale(0.985); }
.btn:focus { outline:2px solid var(--focus); outline-offset:2px; }
.btn[disabled], .btn:disabled { opacity:.5; cursor:not-allowed; }

.btn-primary { background: var(--accent); border-color: transparent; color:#fff; }
.btn-primary:hover { background: var(--accent-hover); }
.btn-secondary { background: var(--bg-elev); border-color: var(--border); }
.btn-ghost { background: transparent; border-color: transparent; }
.btn-danger { background: var(--danger); border-color: transparent; color:#fff; }

.btn-sm { height:28px; padding:0 10px; border-radius:8px; }
.btn-lg { height:44px; padding:0 18px; border-radius:12px; }

/* --------------
   FORMS & INPUTS
   -------------- */
.label { font-weight: 600; }
.field { display:flex; flex-direction:column; gap:6px; }
.input, select, textarea { background: var(--bg-elev); color: var(--text); border:1px solid var(--border); border-radius:10px; padding:10px 12px; outline:none; }
.input:focus, select:focus, textarea:focus { outline:2px solid var(--focus); outline-offset:2px; }
.input.is-invalid, select.is-invalid, textarea.is-invalid { border-color: var(--danger); }
.field-hint { color: var(--muted); font-size:14px; }
.field-error { color: var(--danger); font-size:14px; margin-top:-2px; }
.required::after { content: " *"; color: var(--danger); font-weight: 600; }

/* Inline validation icon in input (optional container) */
.input-wrap { position: relative; }
.input-wrap .input { padding-right: 34px; }
.input-icon { position:absolute; right:10px; top:50%; transform: translateY(-50%); font-size: 16px; }

/* Form actions */
.form-actions { display:flex; gap:12px; justify-content:flex-end; position: sticky; bottom:0; background: var(--bg); padding:12px 0; }

/* --------------
   SIDEBAR LISTS
   -------------- */
.sidebar .section-title { font-size:12px; letter-spacing:.04em; text-transform:uppercase; color:var(--muted); margin:8px 8px; }
.list { display:flex; flex-direction:column; gap:4px; }
.list-item { display:flex; align-items:center; gap:10px; padding:10px 10px; border-radius:10px; color: var(--text); text-decoration:none; }
.list-item:hover { background: rgba(255,255,255,0.04); }
.list-item.active { background: rgba(255,255,255,0.08); border: 1px solid var(--border); }
.list-item .meta { margin-left:auto; color: var(--muted); font-size:14px; }

/* --------------
   TABLES & LIST ROWS
   -------------- */
.table { width: 100%; border-collapse: separate; border-spacing: 0; }
.table th { text-align:left; font-weight:600; padding:12px; position: sticky; top:0; background: var(--bg); border-bottom:1px solid var(--border); }
.table td { padding:12px; border-bottom:1px solid var(--border); }
.table .row:hover { background: rgba(255,255,255,0.03); }

/* --------------
   FEEDBACK: TOASTS, SKELETONS, EMPTY, ERRORS
   -------------- */
.toast-host { position: fixed; top:16px; right:16px; display:flex; flex-direction:column; gap:8px; z-index: 60; }
.toast { padding:10px 12px; border-radius:10px; border:1px solid var(--border); background: var(--bg-elev); }
.toast.ok { border-color: rgba(34,197,94,.35); }
.toast.warn { border-color: rgba(245,158,11,.35); }
.toast.err { border-color: rgba(239,68,68,.35); }

.skeleton { display:block; height: 1em; background: linear-gradient(90deg, rgba(255,255,255,.06), rgba(255,255,255,.15), rgba(255,255,255,.06)); background-size: 200% 100%; animation: shimmer 1.2s infinite; border-radius: 8px; }
@keyframes shimmer { 0%{ background-position: 200% 0; } 100%{ background-position: -200% 0; } }

.empty { text-align:center; padding:24px; color: var(--muted); }
.error-card { border:1px solid var(--danger); background: color-mix(in srgb, var(--danger) 7%, var(--bg-elev)); color: var(--text); border-radius:12px; padding:16px; }

/* --------------
   LINKS & FOCUS
   -------------- */
.link, a { color: var(--accent); text-decoration: none; }
.link:hover, a:hover { color: var(--accent-hover); text-decoration: underline; }
:focus-visible { outline: 2px solid var(--focus); outline-offset: 2px; }

/* --------------
   REDUCED MOTION
   -------------- */
* { transition: background-color .2s ease, color .2s ease, border-color .2s ease, filter .15s ease, transform .08s ease; }
@media (prefers-reduced-motion: reduce) { * { transition: none !important; animation: none !important; } }
</style>

<!-- Theme Switcher (tiny, vanilla JS) -->
<button id="themeToggle" class="btn btn-ghost" style="position:fixed; bottom:16px; left:16px; z-index:70">Toggle theme</button>
<script>
(function(){
  const html = document.documentElement;
  const saved = localStorage.getItem('theme');
  if (saved) html.setAttribute('data-theme', saved);
  else html.setAttribute('data-theme','dark');
  document.getElementById('themeToggle')?.addEventListener('click', () => {
    const next = html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
  });
})();
</script>
```

**Coverage check:** All classes referenced below (buttons, inputs, cards, lists, tables, toasts, skeletons, layout primitives) are defined in the CSS above.

---

## 1) Layout

* **Frame:** `.container` (grid) with `.sidebar`, `.topbar`, `.main` and inner wrapper `.main-inner` (max‑width 1200px).
* **Responsive:** sidebar collapses at 1024px; becomes off‑canvas under 768px (`.sidebar.is-open`).
* **Page padding:** Main uses 24px. Cards/panels use 16px.

### Sidebar content

* Top row: App logo/name and **New** action (use `.btn.btn-primary`).
* Sections: `Pinned`, `Other` with `.section-title` + `.list` of `.list-item` rows; active row uses `.active`.
* Footer: Theme toggle, Settings, Help (use `.btn-ghost`).

### Top bar

* Height 48px, border‑bottom using `--border`. Left: page title; right: global search + user menu.

---

## 2) Typography

* Base: 16/24. Scale: 12, 14, 16, 18, 20, 24, 30. Headings use 600 weight.
* Secondary text: `.muted`.

---

## 3) Components

### Cards

* Structure: `.card > .card-header | .card-body | .card-footer`.
* Hover: subtle elevation; never heavy drop shadows.

### Buttons

* Primary: `.btn.btn-primary` for main actions.
* Secondary: `.btn.btn-secondary` for neutral actions.
* Ghost: `.btn.btn-ghost` for minimal affordance links.
* Sizes: `.btn-sm`, default, `.btn-lg`. Disabled → reduced opacity.

### Inputs

* Field block: `.field` containing `.label.required` + `.input` (or `select`, `textarea`).
* Focus: 2px `--focus` ring.
* Invalid state: `.is-invalid` + `.field-error`. Optional right icon: wrap with `.input-wrap` and add `.input-icon`.

### Lists & Tables

* Lists: `.list` of `.list-item`, optional `.meta` on the right.
* Tables: `.table` with sticky header; rows get subtle hover.

### Feedback

* Toasts: host `.toast-host` with `.toast.ok | .toast.warn | .toast.err`.
* Skeletons: `.skeleton` for loading blocks.
* Empty state: `.empty` card with hint + primary action.
* Error card: `.error-card` with short message and retry button.

---

## 4) Patterns

### Form Validation (real‑time + on submit)

* On **change**: validate and toggle `.is-invalid` + `.field-error` below field.
* On **submit**: scroll to first invalid field, show a compact error summary at the top.
* Required fields: `.required` label marker; add a note at the form top: "* required".
* Protect unsaved edits using a confirm dialog (browser `beforeunload` or custom modal) when the form becomes dirty.

### Sidebar lists (Printers example)

* **Pinned**: user‑ordered (drag in app logic).
* **Other**: sort by owner then name. For these rows only show name + owner, not counters/dates.
* New action: a top **New printer** button.

### Loading & Errors

* Prefer skeletons for primary content areas; spinners only for small inline actions.
* Inline errors near the problem area; page‑level fallback uses `.error-card`.

---

## 5) Accessibility

* Target WCAG 2.1 AA: maintain ≥4.5:1 contrast for text, visible focus, keyboard navigation.
* Add `aria-invalid="true"` and `aria-describedby` for fields with errors.
* Respect reduced motion via media query already included.

---

## 6) Minimal HTML Snippets (Usage)

### Page frame

```html
<div class="container">
  <aside class="sidebar">
    <div class="row"><strong>App</strong><button class="btn btn-primary btn-sm" style="margin-left:auto">New</button></div>
    <div class="section-title">Pinned</div>
    <nav class="list">
      <a class="list-item active" href="#">Kitchen Printer <span class="meta">★</span></a>
      <a class="list-item" href="#">Terrace Printer <span class="meta">★</span></a>
    </nav>
    <div class="section-title">Other</div>
    <nav class="list">
      <a class="list-item" href="#">Alice / Front Desk</a>
      <a class="list-item" href="#">Bob / Bar</a>
    </nav>
  </aside>

  <header class="topbar">
    <div>Printers</div>
    <div class="row">
      <input class="input" placeholder="Search" />
      <button class="btn btn-secondary">Settings</button>
    </div>
  </header>

  <main class="main">
    <div class="main-inner">
      <section class="card">
        <div class="card-header">Add Printer</div>
        <div class="card-body col">
          <div class="field">
            <label class="label required">Name</label>
            <input class="input" />
            <div class="field-hint">Human‑readable printer name.</div>
          </div>
          <div class="field">
            <label class="label required">IP Address</label>
            <div class="input-wrap">
              <input class="input is-invalid" aria-invalid="true" aria-describedby="ipErr" />
              <span class="input-icon">!</span>
            </div>
            <div id="ipErr" class="field-error">Invalid IPv4 (e.g., 192.168.1.20).</div>
          </div>
          <div class="form-actions">
            <button class="btn btn-secondary">Cancel</button>
            <button class="btn btn-primary">Save</button>
          </div>
        </div>
      </section>
    </div>
  </main>
</div>

<div class="toast-host">
  <div class="toast ok">Saved successfully</div>
</div>
```

---

## 7) Asset Licenses (safe choices)

* **Icons:** Lucide (ISC) or Heroicons (MIT)
* **Font:** Inter (OFL) or system stack
