#!/usr/bin/env bash
set -euo pipefail

# All variables are injected by the caller (deploy-ubuntu.ps1) via SSH environment or leading env assignments.
# Required variables:
#   REMOTE_APP_DIR, REMOTE_DB_DIR, REMOTE_MEDIA_DIR
#   REMOTE_ARCHIVE_PATH, SERVICE_RUN_USER, SERVICE_NAME
#   SKIP_ARTIFACT_DEPLOY  (1 = skip, 0 = deploy)
#   PRESERVE_SETTINGS     (1 = preserve, 0 = don't)
#   REQUIRES_PRIVILEGED_PORT (1 = yes, 0 = no)
#   INSTALL_RUNTIME       (1 = install aspnetcore runtime if missing, 0 = no)
#   ROOT_MODE             (1 = running as root, skip sudo)

# When running as root, sudo is unnecessary and unavailable without a tty.
if [ "${ROOT_MODE:-0}" = "1" ]; then
  SUDO=""
else
  SUDO="sudo"
fi

# The app targets net8.0, so it needs the ASP.NET Core 8.0 shared framework
# specifically — a different major version (e.g. 9.0) being present is not enough.
REQUIRED_ASPNET_MAJOR="8.0"

# Resolve the apt-provided dotnet explicitly. A stray dotnet-install.sh tree in
# /usr/share/dotnet can shadow the apt runtime on PATH and only report its own
# (wrong) version, so prefer the canonical /usr/bin/dotnet when it exists.
resolve_dotnet() {
  if [ -x /usr/bin/dotnet ]; then
    echo /usr/bin/dotnet
  else
    command -v dotnet || true
  fi
}

# Returns 0 if the required ASP.NET Core major version is installed for $1 (dotnet path).
has_required_runtime() {
  [ -n "$1" ] && [ -x "$1" ] || return 1
  "$1" --list-runtimes 2>/dev/null \
    | grep -q "^Microsoft.AspNetCore.App $REQUIRED_ASPNET_MAJOR\."
}

DOTNET_BIN=$(resolve_dotnet)

# Install the runtime from the OS package feed if it is missing.
if ! has_required_runtime "$DOTNET_BIN" && [ "${INSTALL_RUNTIME:-0}" = "1" ]; then
  echo "==> ASP.NET Core $REQUIRED_ASPNET_MAJOR runtime not found; installing via apt."
  export DEBIAN_FRONTEND=noninteractive
  $SUDO apt-get update
  $SUDO apt-get install -y aspnetcore-runtime-8.0
  DOTNET_BIN=$(resolve_dotnet)
fi

if [ -z "$DOTNET_BIN" ]; then
  echo "ERROR: dotnet runtime is not installed or not available in PATH."
  exit 1
fi
if ! has_required_runtime "$DOTNET_BIN"; then
  echo "ERROR: ASP.NET Core $REQUIRED_ASPNET_MAJOR runtime is required but not installed."
  echo "Installed runtimes:"
  "$DOTNET_BIN" --list-runtimes 2>/dev/null || true
  exit 1
fi
DOTNET_PATH=$(readlink -f "$DOTNET_BIN" 2>/dev/null || true)
if [ -z "$DOTNET_PATH" ]; then
  DOTNET_PATH="$DOTNET_BIN"
fi

$SUDO mkdir -p "$REMOTE_APP_DIR"
$SUDO mkdir -p "$REMOTE_DB_DIR"
$SUDO mkdir -p "$REMOTE_MEDIA_DIR"

if id "$SERVICE_RUN_USER" >/dev/null 2>&1; then
  SERVICE_GROUP=$(id -gn "$SERVICE_RUN_USER")
  $SUDO chown -R "$SERVICE_RUN_USER:$SERVICE_GROUP" "$REMOTE_DB_DIR" "$REMOTE_MEDIA_DIR"
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

  $SUDO rm -rf "$REMOTE_APP_DIR"/*
  $SUDO tar -xzf "$REMOTE_ARCHIVE_PATH" -C "$REMOTE_APP_DIR"

  if id "$SERVICE_RUN_USER" >/dev/null 2>&1; then
    $SUDO chown -R "$SERVICE_RUN_USER:$SERVICE_GROUP" "$REMOTE_APP_DIR"
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
$SUDO tee "/etc/systemd/system/$SERVICE_NAME.service" > /dev/null <<EOF
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

$SUDO systemctl daemon-reload
$SUDO systemctl enable "$SERVICE_NAME"
$SUDO systemctl stop "$SERVICE_NAME" || true
$SUDO systemctl start "$SERVICE_NAME"

# Give the service a moment to crash on startup, then verify it is actually up.
sleep 2
$SUDO systemctl --no-pager --full status "$SERVICE_NAME" | head -n 25 || true
if ! $SUDO systemctl is-active --quiet "$SERVICE_NAME"; then
  echo "ERROR: $SERVICE_NAME failed to start. Recent logs:"
  $SUDO journalctl -u "$SERVICE_NAME" --no-pager -n 40 || true
  exit 1
fi
echo "==> $SERVICE_NAME is active."
