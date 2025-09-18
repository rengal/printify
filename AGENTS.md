# AGENTS.md — Coding Guidelines

These rules are **mandatory** for all contributors and AI assistants working in this repo.

---

## 1. Style & Formatting

* Follow **.NET coding conventions** (naming, spacing, layout).
* **Braces**: Allman style.
* **Indentation**: 4 spaces.
* **Line length**: ≤ 120 chars.
* **Usings**: system → external → internal; remove unused.
* **Namespaces**: file-scoped.

---

## 2. Naming

* **Types/Methods/Properties/Constants**: PascalCase.
* **Private fields**: camelCase (no underscore).
* **Acronyms**: PascalCase form (e.g., `UiInterval`, `HttpClient`).
* **Timeouts/Intervals**: include unit in name (e.g., `...InMs`).

---

## 3. Nullability & Safety

* Enable **nullable reference types**.
* No `!` suppression without explanation.
* Use guard clauses: `ArgumentNullException.ThrowIfNull(x)`.

---

## 4. Language Features

* Use **records**, `required`, `init`, `switch expressions`, collection expressions (`[]`), target-typed `new()`, `using` declarations.
* Return immutable/read-only collections.
* Prefer `Try` methods over exceptions for flow.

---

## 5. Concurrency

* Protect shared state with `lock (gate)` or documented equivalent.
* No sync-over-async.

---

## 6. Logging & Comments

* Use structured logging; avoid string building if level is disabled.
* **All crucial lines must have comments in English** (domain logic, invariants, tricky conditions).
* Remove commented-out code.

---

## 7. API & Method Design

* Keep methods focused; extract helpers.
* Prefer option objects (e.g., `TrackSearchOptions`) over boolean flags.
* Public APIs return meaningful result types (not bare tuples).

---

## 8. Testing

* Write deterministic unit tests.
* Inject time sources (no real timers in tests).
