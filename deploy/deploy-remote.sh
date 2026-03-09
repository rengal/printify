#!/usr/bin/env bash
set -euo pipefail

# All variables are injected by the caller (deploy-ubuntu.ps1) via SSH environment or leading env assignments.
# Required variables:
#   REMOTE_APP_DIR, REMOTE_DB_DIR, REMOTE_MEDIA_DIR
#   REMOTE_ARCHIVE_PATH, SERVICE_RUN_USER, SERVICE_NAME
#   SKIP_ARTIFACT_DEPLOY  (1 = skip, 0 = deploy)
#   PRESERVE_SETTINGS     (1 = preserve, 0 = don't)
#   REQUIRES_PRIVILEGED_PORT (1 = yes, 0 = no)

DOTNET_BIN=$(command -v dotnet || true)
if [ -z "$DOTNET_BIN" ]; then
  echo "ERROR: dotnet runtime is not installed or not available in PATH."
  exit 1
fi
DOTNET_PATH=$(readlink -f "$DOTNET_BIN" 2>/dev/null || true)
if [ -z "$DOTNET_PATH" ]; then
  DOTNET_PATH="$DOTNET_BIN"
fi

sudo mkdir -p "$REMOTE_APP_DIR"
sudo mkdir -p "$REMOTE_DB_DIR"
sudo mkdir -p "$REMOTE_MEDIA_DIR"

if id "$SERVICE_RUN_USER" >/dev/null 2>&1; then
  SERVICE_GROUP=$(id -gn "$SERVICE_RUN_USER")
  sudo chown -R "$SERVICE_RUN_USER:$SERVICE_GROUP" "$REMOTE_DB_DIR" "$REMOTE_MEDIA_DIR"
else
  echo "WARN: Service user '$SERVICE_RUN_USER' not found. Skipping ownership updates."
fi

if [ "$SKIP_ARTIFACT_DEPLOY" = "1" ]; then
  echo "Skipping artifact deployment steps."
else
  if [ ! -f "$REMOTE_ARCHIVE_PATH" ]; then
    echo "ERROR: Archive not found on remote host: $REMOTE_ARCHIVE_PATH"
    echo "Run deploy without -SkipArtifactDeploy at least once, or upload archive manually."
    exit 1
  fi

  if [ "$PRESERVE_SETTINGS" = "1" ] && [ -f "$REMOTE_APP_DIR/appsettings.Production.json" ]; then
    cp "$REMOTE_APP_DIR/appsettings.Production.json" "/tmp/appsettings.Production.json.bak"
  fi

  sudo rm -rf "$REMOTE_APP_DIR"/*
  sudo tar -xzf "$REMOTE_ARCHIVE_PATH" -C "$REMOTE_APP_DIR"

  if id "$SERVICE_RUN_USER" >/dev/null 2>&1; then
    sudo chown -R "$SERVICE_RUN_USER:$SERVICE_GROUP" "$REMOTE_APP_DIR"
  fi

  if [ "$PRESERVE_SETTINGS" = "1" ] && [ -f "/tmp/appsettings.Production.json.bak" ]; then
    mv "/tmp/appsettings.Production.json.bak" "$REMOTE_APP_DIR/appsettings.Production.json"
  fi

  rm -f "$REMOTE_ARCHIVE_PATH"
fi

# Build optional capability lines for the systemd unit.
CAPABILITY_LINES=""
if [ "$REQUIRES_PRIVILEGED_PORT" = "1" ]; then
  CAPABILITY_LINES="AmbientCapabilities=CAP_NET_BIND_SERVICE
CapabilityBoundingSet=CAP_NET_BIND_SERVICE
NoNewPrivileges=true"
fi

# Install or update systemd unit.
sudo tee "/etc/systemd/system/$SERVICE_NAME.service" > /dev/null <<EOF
[Unit]
Description=Printify
After=network.target

[Service]
WorkingDirectory=$REMOTE_APP_DIR
ExecStart=$DOTNET_PATH $REMOTE_APP_DIR/Printify.Web.dll
User=$SERVICE_RUN_USER
Group=$SERVICE_RUN_USER
$CAPABILITY_LINES
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=$SERVICE_NAME

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl stop "$SERVICE_NAME" || true
sudo systemctl start "$SERVICE_NAME"
sudo systemctl --no-pager --full status "$SERVICE_NAME" | head -n 25
