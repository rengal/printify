[CmdletBinding()]
param(
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ServerHost = "virtual-printer.online"
$User = "root"
$SshPort = 22
$SshKeyPath = ""
$LocalConfigPath = "deploy/nginx/printify.global.http.conf"
$RemoteTempPath = "/tmp/printify.nginx.conf"
$RemoteConfigPath = "/etc/nginx/sites-available/printify"
$RemoteEnabledPath = "/etc/nginx/sites-enabled/printify"

function Invoke-Native
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter()]
        [string[]]$Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-Logged
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Message"
    & $Action
}

function New-SshArgs
{
    param([string]$KeyPath, [int]$Port)

    $args = @("-p", "$Port")
    if (-not [string]::IsNullOrWhiteSpace($KeyPath))
    {
        $args += @("-i", $KeyPath)
    }

    return $args
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$configFullPath = Join-Path $root $LocalConfigPath
if (-not (Test-Path $configFullPath))
{
    throw "Nginx config file not found: $configFullPath"
}

$sshTarget = "$User@$ServerHost"
$sshArgs = New-SshArgs -KeyPath $SshKeyPath -Port $SshPort

Invoke-Logged -Message "Uploading HTTP-only nginx config to server ($sshTarget)" -Action {
    $scpArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($SshKeyPath))
    {
        $scpArgs += @("-i", $SshKeyPath)
    }

    $scpArgs += @(
        "-P", "$SshPort",
        $configFullPath,
        "${sshTarget}:$RemoteTempPath"
    )

    if ($WhatIf)
    {
        Write-Host "scp $($scpArgs -join ' ')"
    }
    else
    {
        Invoke-Native -FilePath "scp" -Arguments $scpArgs
    }
}

Invoke-Logged -Message "Installing HTTP-only nginx config and reloading nginx" -Action {
    $remoteScript = @"
set -euo pipefail
sudo mkdir -p /etc/nginx/sites-available /etc/nginx/sites-enabled
sudo mv "$RemoteTempPath" "$RemoteConfigPath"
sudo ln -sfn "$RemoteConfigPath" "$RemoteEnabledPath"
sudo nginx -t
if sudo systemctl is-active --quiet nginx; then
  sudo systemctl reload nginx
else
  sudo systemctl start nginx
fi
sudo systemctl --no-pager --full status nginx | head -n 20
"@

    if ($WhatIf)
    {
        Write-Host "ssh $($sshArgs -join ' ') $sshTarget `"bash -lc '$remoteScript'`""
    }
    else
    {
        $sshCommandArgs = @()
        $sshCommandArgs += @("-tt")
        $sshCommandArgs += $sshArgs
        $sshCommandArgs += $sshTarget
        $sshCommandArgs += "bash -lc '$remoteScript'"
        Invoke-Native -FilePath "ssh" -Arguments $sshCommandArgs
    }
}

Write-Host "HTTP-only nginx config deployment completed."
