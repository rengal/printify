# API Documentation

This folder contains developer-facing API docs used by the front-end.

## Interactive API reference (Swagger / OpenAPI)

The HTTP API is self-documented via Swagger, served on the **same port as the app**
(it is enabled in every environment except Production):

- **Swagger UI:** [`/swagger`](/swagger)
- **OpenAPI spec (raw):** [`/swagger/v1/swagger.json`](/swagger/v1/swagger.json)

> Printing itself is **raw TCP** to each printer's listener
> (`Settings.TcpListenPort` / `Settings.PublicHost`), not HTTP — so it is **not** in
> the OpenAPI spec. The full register → login → create printer → print → verify flow
> is described in the contract reference below.

## Contract reference (for integration tests)

`doc/api-endpoints.md` (repo root, not served) — per-endpoint contracts, the
`problem+json` error map, the end-to-end scenario, and SSE notes.

Structure:
- printer-status.md: endpoints and payloads for printer state, flags, and streaming.
