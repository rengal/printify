# Deploying Printify with Docker + Caddy

## Prereqs on the remote host
- Docker Engine + Docker Compose v2
- A public DNS record for your domain pointing to the host
- Firewall rules allowing 80, 443, and 9100-15000 TCP

## Setup
1. Copy the repo to the host (git clone or rsync).
2. Create `deploy/.env` from `deploy/.env.example`.
3. Set `PUBLIC_HOST` to your domain and `JWT_SECRET_KEY` to a 32+ char secret.

## Run
```bash
cd deploy
docker compose up -d --build
```

## Notes
- HTTPS is terminated by Caddy; HTTP is redirected to HTTPS automatically.
- The app listens internally on `:8080`.
- Printer TCP listeners are exposed on `9100-15000` via host port mapping.
- SQLite and media files are stored in the `printify-data` Docker volume.
