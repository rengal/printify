[CmdletBinding()]
param(
    [switch]$PreserveProductionSettings,

    [switch]$SkipArtifactDeploy,

    [switch]$SkipRestore,

    [switch]$WhatIf,

    # Selects the deployment target non-interactively (1-4); prompts when omitted.
    [ValidateSet("1", "2", "3", "4")]
    [string]$Target
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Deployment constants (no runtime input required).
$LocalServerHost = "virtual-printer.resto.lan"
$GlobalServerHost = "virtual-printer.online"
$NewHostServerHost = "31.76.96.105"
$NewHost2ServerHost = "166.1.160.233"
$LocalSshUser = "resto"
$GlobalSshUser = "root"
$NewHostSshUser = "root"
$NewHost2SshUser = "root"
$LocalServiceUser = "resto"
$GlobalServiceUser = "root"
$NewHostServiceUser = "root"
$NewHost2ServiceUser = "root"
$SshPort = 22
$ProjectPath = "src/Printify.Web/Printify.Web.csproj"
$LocalSettingsPath = "src/Printify.Web/appsettings.local.Production.json"
$GlobalSettingsPath = "src/Printify.Web/appsettings.global.Production.json"
$NewHostSettingsPath = "src/Printify.Web/appsettings.newhost.Production.json"
$NewHost2SettingsPath = "src/Printify.Web/appsettings.newhost2.Production.json"
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
    Write-Host "3. $NewHostServerHost (fresh server, installs .NET runtime)"
    Write-Host "4. $NewHost2ServerHost (fresh server, installs .NET runtime)"

    while ($true) {
        $selection = if ($Target) { $Target } else { Read-Host "Enter 1, 2, 3 or 4" }
        switch ($selection) {
            "1" {
                return @{
                    ServerHost = $LocalServerHost
                    SshUser = $LocalSshUser
                    ServiceUser = $LocalServiceUser
                    SettingsPath = $LocalSettingsPath
                    RequiresPrivilegedPort = $true
                    InstallRuntime = $false
                }
            }
            "2" {
                return @{
                    ServerHost = $GlobalServerHost
                    SshUser = $GlobalSshUser
                    ServiceUser = $GlobalServiceUser
                    SettingsPath = $GlobalSettingsPath
                    RequiresPrivilegedPort = $false
                    InstallRuntime = $false
                }
            }
            "3" {
                return @{
                    ServerHost = $NewHostServerHost
                    SshUser = $NewHostSshUser
                    ServiceUser = $NewHostServiceUser
                    SettingsPath = $NewHostSettingsPath
                    RequiresPrivilegedPort = $false
                    InstallRuntime = $true
                }
            }
            "4" {
                return @{
                    ServerHost = $NewHost2ServerHost
                    SshUser = $NewHost2SshUser
                    ServiceUser = $NewHost2ServiceUser
                    SettingsPath = $NewHost2SettingsPath
                    RequiresPrivilegedPort = $false
                    InstallRuntime = $true
                }
            }
            default {
                Write-Host "Invalid selection. Enter 1, 2, 3 or 4."
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
$InstallRuntime = [bool]$deploymentTarget.InstallRuntime

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

Invoke-Logged -Message "Restoring NuGet packages" -Action {
    if ($SkipArtifactDeploy) {
        Write-Host "Skipping restore because -SkipArtifactDeploy is enabled."
        return
    }

    if ($SkipRestore) {
        Write-Host "Skipping restore because -SkipRestore is enabled."
        return
    }

    $restoreArgs = @(
        "restore",
        $projectFullPath,
        "-r", $RuntimeIdentifier
    )

    if ($WhatIf) {
        Write-Host "dotnet $($restoreArgs -join ' ')"
    }
    else {
        Invoke-Native -FilePath "dotnet" -Arguments $restoreArgs
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
        "--no-restore",
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
    $localScriptPath = Join-Path $PSScriptRoot "deploy-remote.sh"
    $remoteScriptPath = "$RemoteTempDir/deploy-remote.sh"

    $preserveSettings = if ($PreserveProductionSettings -and -not $SkipArtifactDeploy) { "1" } else { "0" }
    $skipArtifact = if ($SkipArtifactDeploy) { "1" } else { "0" }
    $requiresPrivilegedPort = if ($RequiresPrivilegedPort) { "1" } else { "0" }
    $installRuntime = if ($InstallRuntime) { "1" } else { "0" }
    $useRootMode = if ($User -eq "root") { "1" } else { "0" }

    # Env vars exported before running the remote script.
    $envExports = "export REMOTE_APP_DIR='$RemoteAppDir'; " +
                  "export REMOTE_DB_DIR='$RemoteDbDir'; " +
                  "export REMOTE_MEDIA_DIR='$RemoteMediaDir'; " +
                  "export REMOTE_ARCHIVE_PATH='$remoteArchivePath'; " +
                  "export SERVICE_RUN_USER='$ServiceRunUser'; " +
                  "export SERVICE_NAME='$ServiceName'; " +
                  "export SKIP_ARTIFACT_DEPLOY='$skipArtifact'; " +
                  "export PRESERVE_SETTINGS='$preserveSettings'; " +
                  "export REQUIRES_PRIVILEGED_PORT='$requiresPrivilegedPort'; " +
                  "export INSTALL_RUNTIME='$installRuntime'; " +
                  "export ROOT_MODE='$useRootMode';"

    if ($WhatIf) {
        Write-Host "scp deploy-remote.sh ${sshTarget}:$remoteScriptPath"
        Write-Host "ssh $($sshArgs -join ' ') $sshTarget `"$envExports bash $remoteScriptPath`""
        return
    }

    # Normalize line endings to LF before uploading (file is edited on Windows).
    $lfScriptPath = Join-Path ([System.IO.Path]::GetTempPath()) "deploy-remote.sh"
    [System.IO.File]::WriteAllText($lfScriptPath,
        [System.IO.File]::ReadAllText($localScriptPath).Replace("`r`n", "`n").Replace("`r", "`n"))

    # Upload the script.
    $scpArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) {
        $scpArgs += @("-i", $SshKeyPath)
    }
    $scpArgs += @("-P", "$SshPort", $lfScriptPath, "${sshTarget}:$remoteScriptPath")
    Invoke-Native -FilePath "scp" -Arguments $scpArgs

    # Execute the script on the remote. Capture the script's exit code first,
    # always clean up the uploaded script, then propagate the original code so
    # a failed deploy does not get masked by the cleanup command's success.
    $remoteCommand = "$envExports bash $remoteScriptPath; rc=`$?; rm -f $remoteScriptPath; exit `$rc"
    $sshRunArgs = @("-T") + $sshArgs + $sshTarget + $remoteCommand
    Invoke-Native -FilePath "ssh" -Arguments $sshRunArgs
}

Write-Host "Deployment completed."
