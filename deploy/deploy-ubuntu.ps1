[CmdletBinding()]
param(
    [switch]$PreserveProductionSettings,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Deployment constants (no runtime input required).
$ServerHost = "virtual-printer.resto.lan"
$User = "resto"
$SshPort = 22
$ProjectPath = "src/Printify.Web/Printify.Web.csproj"
$Configuration = "Release"
$RuntimeIdentifier = "linux-x64"
$SelfContained = "false"
$RemoteAppDir = "/opt/printify/app"
$ServiceName = "printify"
$RemoteTempDir = "/tmp"
$SshKeyPath = ""
$ServiceRunUser = "resto"
$RemoteDbDir = "/var/lib/printify/db"
$RemoteMediaDir = "/var/lib/printify/media"
$RequiredUiEntryRelativePath = "html/index.html"

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter()]
        [string[]]$Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-Logged {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Message"
    & $Action
}

function New-SshArgs {
    param([string]$KeyPath, [int]$Port)

    $args = @("-p", "$Port")
    if (-not [string]::IsNullOrWhiteSpace($KeyPath)) {
        $args += @("-i", $KeyPath)
    }

    return $args
}

function Test-SshConnectivity {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServerHost,
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    try {
        $result = Test-NetConnection -ComputerName $ServerHost -Port $Port -WarningAction SilentlyContinue
        return $result.TcpTestSucceeded -eq $true
    }
    catch {
        return $false
    }
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectFullPath = Join-Path $root $ProjectPath

if (-not (Test-Path $projectFullPath)) {
    throw "Project not found: $projectFullPath"
}

$publishRoot = Join-Path $root ".tmp/deploy-publish"
$archiveName = "printify-web.tgz"
$archivePath = Join-Path $root ".tmp/$archiveName"
$remoteArchivePath = "$RemoteTempDir/$archiveName"
$sshTarget = "$User@$ServerHost"
$sshArgs = New-SshArgs -KeyPath $SshKeyPath -Port $SshPort

Invoke-Logged -Message "Checking SSH connectivity to ${ServerHost}:${SshPort}" -Action {
    if ($WhatIf) {
        Write-Host "Test-NetConnection $ServerHost -Port $SshPort"
    }
    else {
        if (-not (Test-SshConnectivity -ServerHost $ServerHost -Port $SshPort)) {
            throw "SSH connectivity check failed for ${ServerHost}:${SshPort}. Deployment canceled."
        }
    }
}

if ($WhatIf) {
    Write-Host "WhatIf mode enabled. Commands will be printed but not executed."
}

Invoke-Logged -Message "Cleaning local publish directory" -Action {
    if (Test-Path $publishRoot) {
        if (-not $WhatIf) {
            Remove-Item $publishRoot -Recurse -Force
        }
    }
    if (-not $WhatIf) {
        New-Item -ItemType Directory -Path $publishRoot | Out-Null
    }
}

Invoke-Logged -Message "Publishing app ($Configuration)" -Action {
    $publishArgs = @(
        "publish",
        $projectFullPath,
        "-c", $Configuration,
        "-r", $RuntimeIdentifier,
        "--self-contained", $SelfContained,
        "-o", $publishRoot
    )

    if ($WhatIf) {
        Write-Host "dotnet $($publishArgs -join ' ')"
    }
    else {
        Invoke-Native -FilePath "dotnet" -Arguments $publishArgs
    }
}

Invoke-Logged -Message "Packing publish output" -Action {
    if (-not $WhatIf) {
        $requiredUiEntryPath = Join-Path $publishRoot $RequiredUiEntryRelativePath
        if (-not (Test-Path $requiredUiEntryPath)) {
            throw "Publish output is missing required UI entry file: $requiredUiEntryPath"
        }
    }

    if (Test-Path $archivePath) {
        if (-not $WhatIf) {
            Remove-Item $archivePath -Force
        }
    }

    $tarArgs = @(
        "-czf", $archivePath,
        "-C", $publishRoot,
        "."
    )

    if ($WhatIf) {
        Write-Host "tar $($tarArgs -join ' ')"
    }
    else {
        Invoke-Native -FilePath "tar" -Arguments $tarArgs
    }
}

Invoke-Logged -Message "Uploading archive to server ($sshTarget)" -Action {
    $scpArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) {
        $scpArgs += @("-i", $SshKeyPath)
    }

    $scpArgs += @(
        "-P", "$SshPort",
        $archivePath,
        "${sshTarget}:$remoteArchivePath"
    )

    if ($WhatIf) {
        Write-Host "scp $($scpArgs -join ' ')"
    }
    else {
        Invoke-Native -FilePath "scp" -Arguments $scpArgs
    }
}

Invoke-Logged -Message "Deploying on remote server and restarting service" -Action {
    $preserveSettingsScript = if ($PreserveProductionSettings) {
@"
if [ -f "$RemoteAppDir/appsettings.Production.json" ]; then
  cp "$RemoteAppDir/appsettings.Production.json" "$RemoteTempDir/appsettings.Production.json.bak"
fi
"@
    }
    else {
        ""
    }

    $restoreSettingsScript = if ($PreserveProductionSettings) {
@"
if [ -f "$RemoteTempDir/appsettings.Production.json.bak" ]; then
  mv "$RemoteTempDir/appsettings.Production.json.bak" "$RemoteAppDir/appsettings.Production.json"
fi
"@
    }
    else {
        ""
    }

    $remoteScript = @"
set -euo pipefail

sudo mkdir -p "$RemoteAppDir"
sudo mkdir -p "$RemoteDbDir"
sudo mkdir -p "$RemoteMediaDir"
if id "$ServiceRunUser" >/dev/null 2>&1; then
  SERVICE_GROUP=`$(id -gn "$ServiceRunUser")
  sudo chown -R "${ServiceRunUser}:`$SERVICE_GROUP" "$RemoteDbDir" "$RemoteMediaDir"
else
  echo "WARN: Service user '$ServiceRunUser' not found. Skipping ownership updates."
fi

$preserveSettingsScript
sudo rm -rf "$RemoteAppDir"/*
sudo tar -xzf "$remoteArchivePath" -C "$RemoteAppDir"
if id "$ServiceRunUser" >/dev/null 2>&1; then
  sudo chown -R "${ServiceRunUser}:`$SERVICE_GROUP" "$RemoteAppDir"
fi
$restoreSettingsScript
rm -f "$remoteArchivePath"

# Install or update systemd unit.
sudo tee "/etc/systemd/system/$ServiceName.service" > /dev/null <<EOF
[Unit]
Description=Printify
After=network.target

[Service]
WorkingDirectory=$RemoteAppDir
ExecStart=/usr/bin/dotnet $RemoteAppDir/Printify.Web.dll
User=$ServiceRunUser
Group=$ServiceRunUser
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=$ServiceName

[Install]
WantedBy=multi-user.target
EOF

DOTNET_PATH=`$(readlink -f "`$(command -v dotnet)")
if [ -z "`$DOTNET_PATH" ] || [ ! -x "`$DOTNET_PATH" ]; then
  echo "ERROR: dotnet runtime binary not found on server."
  exit 1
fi

sudo systemctl daemon-reload
sudo systemctl enable "$ServiceName"
sudo systemctl stop "$ServiceName" || true
sudo setcap 'cap_net_bind_service=+ep' "`$DOTNET_PATH"
sudo systemctl start "$ServiceName"
sudo systemctl --no-pager --full status "$ServiceName" | head -n 25
"@

    if ($WhatIf) {
        Write-Host "ssh $($sshArgs -join ' ') $sshTarget `"bash -lc '$remoteScript'`""
    }
    else {
        $sshCommandArgs = @()
        $sshCommandArgs += @("-tt")
        $sshCommandArgs += $sshArgs
        $sshCommandArgs += $sshTarget
        $sshCommandArgs += "bash -lc '$remoteScript'"
        Invoke-Native -FilePath "ssh" -Arguments $sshCommandArgs
    }
}

Write-Host "Deployment completed."
