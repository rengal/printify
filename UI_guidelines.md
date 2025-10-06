# Lightweight UI Guidelines (Vanilla HTML + CSS) — Printer Management UI

These guidelines reflect the actual implementation of the Printer Management interface. Everything is self-contained with **embedded CSS** and **vanilla JavaScript**. No external dependencies required.

---

## 0) Drop-in CSS & Theme Switcher

Copy this into your HTML `<head>` or include as a shared stylesheet.

```html
<style>
/* -------------- COLOR TOKENS -------------- */
:root {
  --bg: #0e0f12;
  --bg-elev: #15171c;
  --text: #e9eaee;
  --muted: #b3b7c0;
  --border: #2a2d34;
  --accent: #10b981;
  --accent-hover: #0ea372;
  --danger: #ef4444;
  --warn: #f59e0b;
  --ok: #22c55e;
  --focus: #60a5fa;
  --hover-bg: rgba(255,255,255,0.08);
}

:root[data-theme="light"] {
  --bg: #f7f8fa;
  --bg-elev: #ffffff;
  --text: #0f1115;
  --muted: #585f6b;
  --border: #e5e7eb;
  --accent: #0ea372;
  --accent-hover: #0a8b61;
  --hover-bg: rgba(0,0,0,0.05);
}

/* -------------- BASE & TYPOGRAPHY -------------- */
html, body { height: 100%; }
html { color-scheme: light dark; }
body {
  margin: 0;
  background: var(--bg);
  color: var(--text);
  font: 400 16px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Ubuntu, Cantarell, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

h1 { font-size: 30px; line-height: 38px; font-weight: 600; margin: 0 0 12px; }
h2 { font-size: 24px; line-height: 32px; font-weight: 600; margin: 24px 0 12px; }
h3 { font-size: 20px; line-height: 28px; font-weight: 600; margin: 16px 0 8px; }
p, ul, ol { margin: 8px 0 12px; }
small { font-size: 14px; line-height: 20px; color: var(--muted); }

/* -------------- LAYOUT PRIMITIVES -------------- */
.container {
  display: grid;
  grid-template-columns: 300px 1fr;
  grid-template-rows: auto 1fr;
  grid-template-areas:
    "sidebar topbar"
    "sidebar main";
  height: 100dvh;
  transition: grid-template-columns cubic-bezier(0.4, 0, 0.2, 1);
}
.container.sidebar-hidden {
  grid-template-columns: 0px 1fr;
}
.sidebar { 
  grid-area: sidebar; 
  background: var(--bg-elev); 
  border-right: 1px solid var(--border); 
  display: flex; 
  flex-direction: column; 
  gap: 12px; 
  padding: 16px; 
  overflow-y: auto; 
  overflow-x: hidden; 
  transition: opacity cubic-bezier(0.4, 0, 0.2, 1), padding cubic-bezier(0.4, 0, 0.2, 1);
}
.container.sidebar-hidden .sidebar {
  opacity: 0;
  pointer-events: none;
  padding: 16px 0;
}
.topbar { 
  grid-area: topbar; 
  display: flex; 
  align-items: center; 
  justify-content: space-between; 
  padding: 0 16px; 
  border-bottom: 1px solid var(--border); 
  background: var(--bg); 
}
.main { 
  grid-area: main; 
  padding: 24px; 
  overflow-y: auto; 
}
.main-inner { 
  max-width: 1200px; 
  margin: 0 auto; 
  display: flex; 
  flex-direction: column; 
  gap: 16px; 
}

/* -------------- UTILITIES -------------- */
.w100 { width: 100% }
.row { display: flex; align-items: center; gap: 12px }
.col { display: flex; flex-direction: column; gap: 12px }
.muted { color: var(--muted) }
.hidden { display: none }

/* -------------- CARDS -------------- */
.card { 
  background: var(--bg-elev); 
  border: 1px solid var(--border); 
  border-radius: 12px; 
}
.card-header { 
  padding: 12px 16px; 
  border-bottom: 1px solid var(--border); 
  font-weight: 600; 
  display: flex; 
  align-items: center; 
  justify-content: space-between; 
}
.card-body { padding: 16px; }

/* -------------- BUTTONS -------------- */
.btn { 
  border-radius: 10px; 
  padding: 0 14px; 
  height: 36px; 
  border: 1px solid var(--border); 
  background: var(--bg-elev); 
  color: var(--text); 
  display: inline-flex; 
  align-items: center; 
  gap: 8px; 
  cursor: pointer; 
  user-select: none; 
  font-size: 14px; 
  font-weight: 500; 
}
.btn:hover { filter: brightness(1.1); }
.btn:active { transform: scale(0.985); }
.btn:focus { outline: 2px solid var(--focus); outline-offset: 2px; }
.btn[disabled] { opacity: .5; cursor: not-allowed; }

.btn-primary { background: var(--accent); border-color: transparent; color: #fff; }
.btn-primary:hover { background: var(--accent-hover); }
.btn-secondary { background: var(--bg-elev); border-color: var(--border); }
.btn-ghost { background: transparent; border-color: transparent; }
.btn-danger { background: var(--danger); border-color: transparent; color: #fff; }
.btn-sm { height: 28px; padding: 0 10px; border-radius: 8px; font-size: 13px; }

/* -------------- FORMS & INPUTS -------------- */
.label { font-weight: 600; font-size: 14px; margin-bottom: 6px; display: block; }
.field { display: flex; flex-direction: column; gap: 6px; margin-bottom: 12px; }
.input, select, textarea { 
  background: var(--bg-elev); 
  color: var(--text); 
  border: 1px solid var(--border); 
  border-radius: 10px; 
  padding: 10px 12px; 
  outline: none; 
  width: 100%; 
  box-sizing: border-box; 
  font-size: 14px; 
}
.input:focus, select:focus, textarea:focus { 
  outline: 2px solid var(--focus); 
  outline-offset: 2px; 
}
.input.invalid, select.invalid { 
  border-color: var(--danger); 
  outline: none; 
}
.input.invalid:focus, select.invalid:focus { 
  outline: 2px solid var(--danger); 
  outline-offset: 2px; 
}
.field-hint { color: var(--muted); font-size: 13px; margin-top: -2px; }
.field-error { color: var(--danger); font-size: 13px; margin-top: 2px; display: none; }
.field-error.show { display: block; }
.required::after { content: " *"; color: var(--danger); font-weight: 600; }
.form-actions { 
  display: flex; 
  gap: 12px; 
  justify-content: flex-end; 
  margin-top: 20px; 
  padding-top: 16px; 
  border-top: 1px solid var(--border); 
}
.checkbox-field { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
.checkbox-field input[type="checkbox"] { width: 18px; height: 18px; cursor: pointer; }
.checkbox-field label { font-size: 14px; font-weight: 500; margin: 0; cursor: pointer; }
.field-group { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.indent { margin-left: 26px; }

/* -------------- SIDEBAR COMPONENTS -------------- */
.sidebar-header { 
  display: flex; 
  align-items: center; 
  justify-content: space-between; 
  margin-bottom: 8px; 
}
.sidebar-title { font-weight: 600; font-size: 18px; }
.sidebar-footer { 
  margin-top: auto; 
  padding-top: 16px; 
  border-top: 1px solid var(--border); 
}
.user-menu { 
  display: flex; 
  align-items: center; 
  gap: 10px; 
  padding: 10px 12px; 
  border-radius: 10px; 
  cursor: pointer; 
  transition: background-color ease; 
  position: relative; 
}
.user-menu:hover { background: rgba(255,255,255,0.06); }
.user-avatar { 
  width: 32px; 
  height: 32px; 
  border-radius: 50%; 
  background: var(--accent); 
  color: white; 
  display: flex; 
  align-items: center; 
  justify-content: center; 
  font-size: 13px; 
  font-weight: 600; 
  flex-shrink: 0; 
}
.user-info { flex: 1; min-width: 0; }
.user-name { 
  font-size: 14px; 
  font-weight: 500; 
  white-space: nowrap; 
  overflow: hidden; 
  text-overflow: ellipsis; 
}
.icon-btn { 
  width: 32px; 
  height: 32px; 
  padding: 0; 
  display: flex; 
  align-items: center; 
  justify-content: center; 
  background: transparent; 
  border: none; 
  color: var(--text); 
  cursor: pointer; 
  border-radius: 8px; 
  transition: background-color ease; 
}
.icon-btn:hover { background: var(--hover-bg); }
.section-title { 
  font-size: 12px; 
  letter-spacing: .04em; 
  text-transform: uppercase; 
  color: var(--muted); 
  margin: 16px 0 8px; 
  font-weight: 600; 
}
.list { display: flex; flex-direction: column; gap: 2px; }
.list-item { 
  display: flex; 
  align-items: center; 
  gap: 10px; 
  padding: 10px 12px; 
  border-radius: 10px; 
  color: var(--text); 
  text-decoration: none; 
  cursor: pointer; 
  position: relative; 
  padding-right: 44px; 
  min-height: 44px; 
}
.list-item:hover { background: rgba(255,255,255,0.06); }
.list-item.active { 
  background: rgba(16,185,129,0.15); 
  border: 1px solid var(--accent); 
}
.list-item-icon { 
  flex-shrink: 0; 
  width: 16px; 
  height: 16px; 
  display: flex; 
  align-items: center; 
  justify-content: center; 
  color: var(--muted); 
}
.list-item-content { 
  flex: 1; 
  min-width: 0; 
  display: flex; 
  flex-direction: column; 
  gap: 2px; 
}
.list-item-line1 { 
  display: flex; 
  align-items: center; 
  gap: 6px; 
  font-weight: 600; 
  font-size: 15px; 
}
.list-item-name { 
  white-space: nowrap; 
  overflow: hidden; 
  text-overflow: ellipsis; 
}
.list-item-line2 { 
  font-size: 13px; 
  color: var(--muted); 
  line-height: 1.3; 
}
.list-item-badge { 
  background: var(--accent); 
  color: white; 
  border-radius: 10px; 
  padding: 1px 7px; 
  font-size: 11px; 
  font-weight: 600; 
  line-height: 1.4; 
  flex-shrink: 0; 
}
.list-item-menu { 
  position: absolute; 
  right: 8px; 
  top: 50%; 
  transform: translateY(-50%); 
  opacity: 0; 
  font-weight: 900; 
  font-size: 18px; 
  line-height: 1; 
  padding: 4px 8px; 
  border-radius: 6px; 
  transition: opacity ease, background-color ease; 
}
.list-item-menu:hover { background: var(--hover-bg); }
.list-item:hover .list-item-menu { opacity: 1; }
.list-item-new { 
  border: 1px dashed var(--border); 
  justify-content: center; 
  font-weight: 600; 
  padding-right: 12px; 
  min-height: 44px; 
}
.list-item-new.temporary { 
  border-style: dashed; 
  border-width: 2px; 
}

/* -------------- MENU -------------- */
.menu { 
  position: absolute; 
  background: var(--bg-elev); 
  border: 1px solid var(--border); 
  border-radius: 10px; 
  padding: 4px; 
  box-shadow: 0 4px 12px rgba(0,0,0,0.3); 
  z-index: 100; 
  min-width: 120px; 
}
.menu-item { 
  padding: 8px 12px; 
  border-radius: 8px; 
  cursor: pointer; 
  font-size: 14px; 
}
.menu-item:hover { background: rgba(255,255,255,0.06); }

/* -------------- MODAL -------------- */
.modal-overlay { 
  position: fixed; 
  inset: 0; 
  background: rgba(0,0,0,0.6); 
  display: flex; 
  align-items: center; 
  justify-content: center; 
  z-index: 200; 
}
.modal { 
  background: var(--bg-elev); 
  border: 1px solid var(--border); 
  border-radius: 12px; 
  width: 90%; 
  max-width: 500px; 
  max-height: 90vh; 
  overflow-y: auto; 
}
.modal-header { 
  padding: 16px 20px; 
  border-bottom: 1px solid var(--border); 
  font-weight: 600; 
  font-size: 18px; 
}
.modal-body { padding: 20px; }

/* -------------- DOCUMENTS -------------- */
.document-item { 
  background: var(--bg-elev); 
  border: 1px solid var(--border); 
  border-radius: 12px; 
  padding: 16px; 
  margin-bottom: 16px; 
}
.document-preview { 
  background: white; 
  color: black; 
  padding: 20px; 
  border-radius: 8px; 
  font-family: monospace; 
  font-size: 12px; 
  line-height: 1.4; 
  margin-bottom: 12px; 
  white-space: pre-wrap; 
}
.document-meta { 
  display: flex; 
  align-items: center; 
  gap: 12px; 
  color: var(--muted); 
  font-size: 14px; 
}
.document-actions { 
  margin-left: auto; 
  display: flex; 
  gap: 8px; 
}

/* -------------- TOAST -------------- */
.toast-host { 
  position: fixed; 
  top: 16px; 
  right: 16px; 
  display: flex; 
  flex-direction: column; 
  gap: 8px; 
  z-index: 300; 
}
.toast { 
  padding: 12px 16px; 
  border-radius: 10px; 
  border: 1px solid var(--border); 
  background: var(--bg-elev); 
  box-shadow: 0 4px 12px rgba(0,0,0,0.3); 
  animation: slideIn ease; 
}
.toast.ok { 
  border-color: var(--ok); 
  background: color-mix(in srgb, var(--ok) 10%, var(--bg-elev)); 
}
@keyframes slideIn { 
  from { transform: translateX(100%); opacity: 0; } 
  to { transform: translateX(0); opacity: 1; } 
}

/* -------------- TRANSITIONS -------------- */
* { 
  transition: background-color ease, color ease, border-color ease, filter ease, transform ease; 
}
@media (prefers-reduced-motion: reduce) { 
  * { transition: none !important; animation: none !important; } 
}
</style>

<!-- Theme Switcher (vanilla JS) -->
<script>
function initTheme() {
  const html = document.documentElement;
  const saved = localStorage.getItem('theme') || 'dark';
  html.setAttribute('data-theme', saved);
}

function toggleTheme() {
  const html = document.documentElement;
  const current = html.getAttribute('data-theme');
  const next = current === 'dark' ? 'light' : 'dark';
  html.setAttribute('data-theme', next);
  localStorage.setItem('theme', next);
}

// Initialize on page load
initTheme();
</script>
```

---

## 1) Layout

**Three-column grid structure:**

* **Sidebar** (collapsible): printer list, user menu, navigation
* **Top bar**: title, controls, theme toggle
* **Main area**: document viewer or content

The sidebar can be toggled with animated transitions. State persists to localStorage.

---

## 2) Typography

* **Base**: 16px with 1.5 line-height
* **Scale**: 12, 13, 14, 15, 18, 20, 24, 30
* **Weights**: 400 (normal), 500 (medium), 600 (semibold), 900 (black for menu dots)
* **Secondary text**: use `.muted` class

---

## 3) Components

### Cards

Structure: `.card` > `.card-header` + `.card-body`

```html
<section class="card">
  <div class="card-header">Printer Configuration</div>
  <div class="card-body">
    <!-- content -->
  </div>
</section>
```

### Buttons

* **Primary**: `.btn.btn-primary` (accent background)
* **Secondary**: `.btn.btn-secondary` (bordered)
* **Ghost**: `.btn.btn-ghost` (transparent)
* **Danger**: `.btn.btn-danger` (red background)
* **Small**: add `.btn-sm`
* **Icon buttons**: `.icon-btn` for square icon-only buttons

### Forms

Field structure with validation:

```html
<div class="field">
  <label class="label required">Printer Name</label>
  <input class="input" id="printerName" />
  <div class="field-hint">Human-readable name</div>
  <div class="field-error" id="nameError">Name is required</div>
</div>
```

**Validation states:**
* Add `.invalid` class to input
* Add `.show` class to `.field-error` to display
* Focus ring changes to danger color automatically

**Field groups** (two-column layout):

```html
<div class="field-group">
  <div class="field"><!-- field 1 --></div>
  <div class="field"><!-- field 2 --></div>
</div>
```

**Checkbox fields:**

```html
<div class="checkbox-field">
  <input type="checkbox" id="option" />
  <label for="option">Enable this feature</label>
</div>
```

**Form actions** (submit bar):

```html
<div class="form-actions">
  <button class="btn btn-secondary">Cancel</button>
  <button class="btn btn-primary">Save</button>
</div>
```

### Sidebar Lists

**Section titles:**

```html
<div class="section-title">Pinned</div>
```

**List items with full metadata** (pinned printers):

```html
<div class="list-item active" onclick="selectPrinter(1)">
  <div class="list-item-icon">
    <svg><!-- icon --></svg>
  </div>
  <div class="list-item-content">
    <div class="list-item-line1">
      <span class="list-item-name">Kitchen Printer</span>
      <span class="list-item-badge">● 3</span>
    </div>
    <div class="list-item-line2">Last: 06.10.2025 14:30 · 2h ago</div>
  </div>
  <button class="btn btn-ghost btn-sm list-item-menu">⋯</button>
</div>
```

**Simple list items** (other printers):

```html
<div class="list-item">
  <div class="list-item-icon">
    <svg><!-- icon --></svg>
  </div>
  <div class="list-item-content">
    <div class="list-item-line1">
      <span class="list-item-name">Bar Printer</span>
    </div>
    <div class="list-item-line2">by Alice Smith</div>
  </div>
  <button class="btn btn-ghost btn-sm list-item-menu">⋯</button>
</div>
```

**New action item:**

```html
<div class="list-item list-item-new">
  <span>+ New printer</span>
</div>
```

Add `.temporary` class for dashed border (anonymous mode).

### Context Menus

Position dynamically with JavaScript:

```html
<div class="menu" style="position: fixed; left: 100px; top: 200px;">
  <div class="menu-item" onclick="editPrinter()">Edit</div>
  <div class="menu-item" onclick="togglePin()">Pin</div>
</div>
```

### Modals

```html
<div class="modal-overlay">
  <div class="modal">
    <div class="modal-header">Edit Printer</div>
    <div class="modal-body">
      <!-- form content -->
    </div>
  </div>
</div>
```

Close by removing modal from DOM.

### User Menu (Sidebar Footer)

```html
<div class="sidebar-footer">
  <div class="user-menu" onclick="showUserMenu(event)">
    <div class="user-avatar">AI</div>
    <div class="user-info">
      <div class="user-name">Alexey Ivanov</div>
    </div>
  </div>
</div>
```

Avatar shows initials (2 letters from first/last name).

### Toast Notifications

```html
<div class="toast-host" id="toastHost"></div>

<script>
function showToast(message, type = 'ok') {
  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.textContent = message;
  document.getElementById('toastHost').appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}
</script>
```

### Document Cards

```html
<div class="document-item">
  <div class="document-preview">
Receipt content
in monospace
  </div>
  <div class="document-meta">
    <span>2h ago</span>
    <div class="document-actions">
      <button class="btn btn-secondary btn-sm">Download</button>
      <button class="btn btn-secondary btn-sm">Replay</button>
    </div>
  </div>
</div>
```

---

## 4) Patterns

### Sidebar Toggle

```javascript
function toggleSidebar() {
  const container = document.querySelector('.container');
  container.classList.toggle('sidebar-hidden');
  const isHidden = container.classList.contains('sidebar-hidden');
  localStorage.setItem('sidebarHidden', isHidden);
}

// Restore on page load
const sidebarHidden = localStorage.getItem('sidebarHidden') === 'true';
if (sidebarHidden) {
  document.querySelector('.container').classList.add('sidebar-hidden');
}
```

### Form Validation

Real-time validation on input/blur:

```javascript
function validateField(inputId, errorId, validator) {
  const input = document.getElementById(inputId);
  const error = document.getElementById(errorId);
  const isValid = validator(input.value);
  
  if (isValid) {
    input.classList.remove('invalid');
    error.classList.remove('show');
  } else {
    input.classList.add('invalid');
    error.classList.add('show');
  }
  
  return isValid;
}

// On submit
function submitForm() {
  const isNameValid = validateField('printerName', 'nameError', v => v.trim().length > 0);
  
  if (!isNameValid) {
    document.getElementById('printerName').focus();
    return;
  }
  
  // proceed with submission
}
```

### Context Menu Positioning

```javascript
function showMenu(event, printerId) {
  event.stopPropagation();
  
  // Remove existing menu
  const existingMenu = document.querySelector('.menu');
  if (existingMenu) existingMenu.remove();

  // Create new menu
  const menu = document.createElement('div');
  menu.className = 'menu';
  menu.style.position = 'fixed';
  menu.style.left = event.clientX + 'px';
  menu.style.top = event.clientY + 'px';
  menu.innerHTML = `
    <div class="menu-item" onclick="editPrinter(${printerId})">Edit</div>
    <div class="menu-item" onclick="togglePin(${printerId})">Pin</div>
  `;
  
  document.body.appendChild(menu);

  // Close on outside click
  setTimeout(() => {
    document.addEventListener('click', function closeMenu() {
      menu.remove();
      document.removeEventListener('click', closeMenu);
    });
  }, 0);
}
```

### Modal Management

```javascript
function openModal(content) {
  const modal = document.createElement('div');
  modal.className = 'modal-overlay';
  modal.innerHTML = content;
  document.getElementById('modalContainer').appendChild(modal);
}

function closeModal() {
  document.getElementById('modalContainer').innerHTML = '';
}
```

### User State Management

```javascript
let currentUser = null;

// Initialize from localStorage
const savedUser = localStorage.getItem('currentUser');
if (savedUser) {
  currentUser = savedUser;
  updateUserDisplay();
}

function updateUserDisplay() {
  const avatar = document.getElementById('userAvatar');
  const userName = document.getElementById('userName');
  
  if (currentUser) {
    avatar.textContent = getInitials(currentUser);
    userName.textContent = currentUser;
  } else {
    avatar.textContent = '?';
    userName.textContent = 'Not signed in';
  }
}

function getInitials(name) {
  if (!name) return '?';
  const parts = name.trim().split(' ');
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return parts[0].substring(0, 2).toUpperCase();
}
```

---

## 5) Accessibility

* Maintain **4.5:1 contrast** for text (checked in dark and light modes)
* **Focus rings** (`--focus` color) on all interactive elements
* **Keyboard navigation**: all buttons, inputs, and links are keyboard accessible
* **ARIA attributes**: add `aria-invalid="true"` and `aria-describedby` for form validation
* **Reduced motion**: animations disabled via media query

---

## 6) State Persistence

Store user preferences in localStorage:

* `theme`: 'dark' or 'light'
* `sidebarHidden`: 'true' or 'false'
* `currentUser`: username string
* `receipt_filters`: JSON object for filters (reserved for future)

---

## 7) Empty States

**Not signed in:**
```html
<div style="max-width: 600px; margin: 60px auto; text-align: center;">
  <h1>Printer Management System</h1>
  <p style="color: var(--muted); font-size: 18px;">
    Manage receipt and label printers with real-time document streaming
  </p>
  <button class="btn btn-primary" onclick="showLoginDialog()">Sign in</button>
</div>
```

**No printer selected:**
```html
<div style="text-align: center; padding: 60px 20px; color: var(--muted);">
  <h2>Welcome back, Alice</h2>
  <p>Select a printer from the sidebar to view documents</p>
</div>
```

**No documents:**
```html
<div style="text-align: center; padding: 60px 20px; color: var(--muted);">
  <h3>No documents yet</h3>
  <p>Documents will appear here when they are printed</p>
</div>
```

---

## 8) Full Page Example

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Printer Management</title>
  <!-- Include CSS from section 0 -->
</head>
<body>

<div class="container">
  <aside class="sidebar" id="sidebar">
    <div class="sidebar-header">
      <span class="sidebar-title">Printers</span>
    </div>

    <nav class="list">
      <div class="list-item list-item-new" onclick="openNewPrinterDialog()">
        <span>+ New printer</span>
      </div>
    </nav>

    <div class="section-title">Pinned</div>
    <nav class="list" id="pinnedList">
      <!-- Pinned printers rendered here -->
    </nav>

    <div class="section-title">Other</div>
    <nav class="list" id="otherList">
      <!-- Other printers rendered here -->
    </nav>

    <div class="sidebar-footer">
      <div class="user-menu" onclick="showUserMenu(event)">
        <div class="user-avatar" id="userAvatar">?</div>
        <div class="user-info">
          <div class="user-name" id="userName">Not signed in</div>
        </div>
      </div>
    </div>
  </aside>

  <header class="topbar">
    <div class="row" style="gap: 8px;">
      <button class="icon-btn" onclick="toggleSidebar()">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="9" y1="3" x2="9" y2="21"></line>
        </svg>
      </button>
      <div id="topbarTitle">Select a printer</div>
    </div>
    <div class="row">
      <button class="icon-btn" onclick="toggleTheme()">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
        </svg>
      </button>
    </div>
  </header>

  <main class="main">
    <div class="main-inner" id="mainContent">
      <!-- Content rendered here -->
    </div>
  </main>
</div>

<div class="toast-host" id="toastHost"></div>
<div id="modalContainer"></div>

<!-- Include JavaScript from above patterns -->

</body>
</html>
```