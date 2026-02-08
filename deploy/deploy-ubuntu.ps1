[CmdletBinding()]
param(
    [switch]$PreserveProductionSettings,

    [switch]$SkipArtifactDeploy,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Deployment constants (no runtime input required).
$LocalServerHost = "virtual-printer.resto.lan"
$GlobalServerHost = "virtual-printer.online"
$LocalSshUser = "resto"
$GlobalSshUser = "root"
$LocalServiceUser = "resto"
$GlobalServiceUser = "root"
$SshPort = 22
$ProjectPath = "src/Printify.Web/Printify.Web.csproj"
$LocalSettingsPath = "src/Printify.Web/appsettings.local.Production.json"
$GlobalSettingsPath = "src/Printify.Web/appsettings.global.Production.json"
$Configuration = "Release"
$RuntimeIdentifier = "linux-x64"
$SelfContained = "false"
$RemoteAppDir = "/opt/printify/app"
$ServiceName = "printify"
$RemoteTempDir = "/tmp"
$SshKeyPath = ""
$RemoteDbDir = "/var/lib/printify/db"
$RemoteMediaDir = "/var/lib/printify/media"
$RequiredUiEntryRelativePath = "html/index.html"

function Get-DeploymentTarget {
    Write-Host "Select deployment target:"
    Write-Host "1. $LocalServerHost"
    Write-Host "2. $GlobalServerHost"

    while ($true) {
        $selection = Read-Host "Enter 1 or 2"
        switch ($selection) {
            "1" {
                return @{
                    ServerHost = $LocalServerHost
                    SshUser = $LocalSshUser
                    ServiceUser = $LocalServiceUser
                    SettingsPath = $LocalSettingsPath
                    RequiresPrivilegedPort = $true
                }
            }
            "2" {
                return @{
                    ServerHost = $GlobalServerHost
                    SshUser = $GlobalSshUser
                    ServiceUser = $GlobalServiceUser
                    SettingsPath = $GlobalSettingsPath
                    RequiresPrivilegedPort = $false
                }
            }
            default {
                Write-Host "Invalid selection. Enter 1 or 2."
            }
        }
    }
}

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

$deploymentTarget = Get-DeploymentTarget
$ServerHost = $deploymentTarget.ServerHost
$User = $deploymentTarget.SshUser
$ServiceRunUser = $deploymentTarget.ServiceUser
$SelectedSettingsPath = $deploymentTarget.SettingsPath
$RequiresPrivilegedPort = [bool]$deploymentTarget.RequiresPrivilegedPort

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectFullPath = Join-Path $root $ProjectPath
$selectedSettingsFullPath = Join-Path $root $SelectedSettingsPath

if (-not (Test-Path $projectFullPath)) {
    throw "Project not found: $projectFullPath"
}

if (-not (Test-Path $selectedSettingsFullPath)) {
    throw "Selected settings file not found: $selectedSettingsFullPath"
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
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping local publish cleanup because -SkipArtifactDeploy is enabled."
        return
    }

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
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping publish because -SkipArtifactDeploy is enabled."
        return
    }

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

Invoke-Logged -Message "Applying deployment settings ($SelectedSettingsPath)" -Action {
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping appsettings copy because -SkipArtifactDeploy is enabled."
        return
    }

    $targetSettingsPath = Join-Path $publishRoot "appsettings.Production.json"

    if ($WhatIf) {
        Write-Host "Copy-Item $selectedSettingsFullPath $targetSettingsPath -Force"
    }
    else {
        Copy-Item -Path $selectedSettingsFullPath -Destination $targetSettingsPath -Force
    }
}

Invoke-Logged -Message "Packing publish output" -Action {
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping archive packing because -SkipArtifactDeploy is enabled."
        return
    }

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
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping archive upload because -SkipArtifactDeploy is enabled."
        return
    }

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
    $preserveSettingsScript = if ($PreserveProductionSettings -and -not $SkipArtifactDeploy) {
@"
if [ -f "$RemoteAppDir/appsettings.Production.json" ]; then
  cp "$RemoteAppDir/appsettings.Production.json" "$RemoteTempDir/appsettings.Production.json.bak"
fi
"@
    }
    else {
        ""
    }

$restoreSettingsScript = if ($PreserveProductionSettings -and -not $SkipArtifactDeploy) {
@"
if [ -f "$RemoteTempDir/appsettings.Production.json.bak" ]; then
  mv "$RemoteTempDir/appsettings.Production.json.bak" "$RemoteAppDir/appsettings.Production.json"
fi
"@
    }
    else {
        ""
    }

    $setcapScript = if ($RequiresPrivilegedPort) {
        "sudo setcap 'cap_net_bind_service=+ep' `"`$DOTNET_PATH`""
    }
    else {
        "echo `"Skipping setcap: application uses non-privileged port.`""
    }

    $artifactDeployScript = if ($SkipArtifactDeploy) {
@"
echo "Skipping artifact deployment steps."
"@
    }
    else {
@"
if [ ! -f "$remoteArchivePath" ]; then
  echo "ERROR: Archive not found on remote host: $remoteArchivePath"
  echo "Run deploy without -SkipArtifactDeploy at least once, or upload archive manually."
  exit 1
fi

$preserveSettingsScript
sudo rm -rf "$RemoteAppDir"/*
sudo tar -xzf "$remoteArchivePath" -C "$RemoteAppDir"
if id "$ServiceRunUser" >/dev/null 2>&1; then
  sudo chown -R "${ServiceRunUser}:`$SERVICE_GROUP" "$RemoteAppDir"
fi
$restoreSettingsScript
rm -f "$remoteArchivePath"
"@
    }

$remoteScript = @"
set -euo pipefail

DOTNET_BIN=`$(command -v dotnet || true)
if [ -z "`$DOTNET_BIN" ]; then
  echo "ERROR: dotnet runtime is not installed or not available in PATH."
  exit 1
fi
DOTNET_PATH=`$(readlink -f "`$DOTNET_BIN" 2>/dev/null || true)
if [ -z "`$DOTNET_PATH" ]; then
  DOTNET_PATH="`$DOTNET_BIN"
fi

sudo mkdir -p "$RemoteAppDir"
sudo mkdir -p "$RemoteDbDir"
sudo mkdir -p "$RemoteMediaDir"
if id "$ServiceRunUser" >/dev/null 2>&1; then
  SERVICE_GROUP=`$(id -gn "$ServiceRunUser")
  sudo chown -R "${ServiceRunUser}:`$SERVICE_GROUP" "$RemoteDbDir" "$RemoteMediaDir"
else
  echo "WARN: Service user '$ServiceRunUser' not found. Skipping ownership updates."
fi

$artifactDeployScript

# Install or update systemd unit.
sudo tee "/etc/systemd/system/$ServiceName.service" > /dev/null <<EOF
[Unit]
Description=Printify
After=network.target

[Service]
WorkingDirectory=$RemoteAppDir
ExecStart=`$DOTNET_PATH $RemoteAppDir/Printify.Web.dll
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

sudo systemctl daemon-reload
sudo systemctl enable "$ServiceName"
sudo systemctl stop "$ServiceName" || true
$setcapScript
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
