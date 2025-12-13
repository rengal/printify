# API Endpoints (Printify Web API)

Auth
- `POST /api/auth/login` — exchange workspace token for JWT (anonymous). Request: `Auth.LoginRequestDto`; Response: `Auth.LoginResponseDto` (includes `AccessToken`, `Workspace`).
- `POST /api/auth/logout` — no-op placeholder; requires Bearer token.
- `GET /api/auth/me` — current workspace info; requires Bearer token. Response: `Workspaces.WorkspaceDto`.

Workspaces
- `POST /api/workspaces` — create workspace; anonymous. Request: `Workspaces.CreateWorkspaceRequestDto`; Response: `Workspaces.WorkspaceDto` (includes `Token` for login).

Printers (Bearer token required)
- `POST /api/printers` — create printer. Request: `Printers.CreatePrinterRequestDto`; Response: `Printers.PrinterResponseDto`.
- `GET /api/printers` — list printers for current workspace. Response: `List<Printers.PrinterResponseDto>`.
- `GET /api/printers/{id}` — get printer by id (scoped to workspace). Response: `Printers.PrinterResponseDto`.
- `PUT /api/printers/{id}` — update printer. Request: `Printers.UpdatePrinterRequestDto`; Response: `Printers.PrinterResponseDto`.
- `DELETE /api/printers/{id}` — soft-delete printer. Response: 204/404.
- `POST /api/printers/{id}/pin` — pin/unpin printer. Request: `Printers.PinPrinterRequestDto`; Response: `Printers.PrinterResponseDto`.
- `GET /api/printers/{id}/documents` — list documents (`beforeId`, `limit` query). Response: `DocumentListResponseDto` with `PagedResult<DocumentDto>`.
- `GET /api/printers/{printerId}/documents/{documentId}` — get single document. Response: `DocumentDto`.
- `GET /api/printers/{id}/documents/stream` — SSE stream of `DocumentDto` when completed.
- `POST /api/printers/{id}/documents/last-viewed` — stub (returns 501).

Media
- `GET /api/media/{mediaId}` — download media (requires valid JWT; scoped to workspace via document lookup). Response: file stream; headers include `ETag: "sha256:{checksum}"`.
