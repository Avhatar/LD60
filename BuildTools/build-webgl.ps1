[CmdletBinding()]
param(
    [switch]$Dev,
    [switch]$Clean,
    [switch]$Rebuild,
    [switch]$Graphics,
    [string]$Output,
    [string]$UnityExe,
    [ValidateSet('Idle', 'BelowNormal', 'Normal')]
    [string]$Priority = 'BelowNormal'
)

$ErrorActionPreference = 'Stop'

$repoRoot    = Split-Path -Parent $PSScriptRoot
$sourceProj  = Join-Path $repoRoot 'LD60_UnityProject'
$buildRoot   = Join-Path $repoRoot 'Build'
$cloneProj   = Join-Path $buildRoot 'clone'
$logDir      = Join-Path $buildRoot 'logs'
$injectDir   = Join-Path $cloneProj 'Assets\_Build\Editor'
$injectedCs  = Join-Path $injectDir 'HeadlessBuild.cs'
$sourceCs    = Join-Path $PSScriptRoot 'HeadlessBuild.cs'

if ([string]::IsNullOrWhiteSpace($Output)) { $Output = Join-Path $buildRoot 'WebGL' }

function Resolve-UnityExe {
    param([string]$Explicit, [string]$ProjectPath)
    if (-not [string]::IsNullOrWhiteSpace($Explicit)) { return $Explicit }
    $versionFile = Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $versionFile)) {
        throw "ProjectVersion.txt not found at $versionFile. Pass -UnityExe explicitly."
    }
    $match = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)'
    if (-not $match) {
        throw "m_EditorVersion missing from $versionFile. Pass -UnityExe explicitly."
    }
    $version = $match.Matches[0].Groups[1].Value
    return Join-Path 'C:\Program Files\Unity\Hub\Editor' (Join-Path $version 'Editor\Unity.exe')
}

function Sync-Folder {
    param([string]$Source, [string]$Destination, [string[]]$ExcludeDirs)
    $roboArgs = @($Source, $Destination, '/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/R:2', '/W:2', '/MT:16')
    foreach ($dir in $ExcludeDirs) { $roboArgs += @('/XD', $dir) }
    & robocopy.exe @roboArgs | Out-Null
    $code = $LASTEXITCODE
    $global:LASTEXITCODE = 0
    if ($code -ge 8) { throw "robocopy failed with code $code while mirroring $Source -> $Destination" }
}

$UnityExe = Resolve-UnityExe -Explicit $UnityExe -ProjectPath $sourceProj
if (-not (Test-Path $UnityExe)) {
    throw "Unity editor not found at $UnityExe. Install that version or pass -UnityExe <path to Unity.exe>."
}

$busy = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
        Where-Object { $_.CommandLine -and $_.CommandLine.Contains($cloneProj) }
if ($busy) {
    Write-Host "A background build is already running on $cloneProj (pid $($busy[0].ProcessId)). Aborting." -ForegroundColor Yellow
    exit 1
}

if ($Rebuild -and (Test-Path $cloneProj)) {
    Write-Host "Removing clone caches for a cold rebuild..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $cloneProj 'Library')
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $cloneProj 'Temp')
}
if ($Clean -and (Test-Path $Output)) {
    Write-Host "Clearing output $Output..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force $Output
}

New-Item -ItemType Directory -Force -Path $cloneProj, $logDir, $injectDir | Out-Null

# Optional machine-local exclusions: one folder name per line, '#' for comments.
# Anything listed here is kept out of the build clone, so local-only tooling
# never reaches a shipped player. The file is absent on most machines.
$localExclude = @()
$localExcludeFile = Join-Path $PSScriptRoot 'clone-exclude.local.txt'
if (Test-Path $localExcludeFile) {
    $localExclude = @(Get-Content $localExcludeFile |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith('#') })
    if ($localExclude.Count -gt 0) {
        Write-Host ("Local clone exclusions: {0}" -f ($localExclude -join ', ')) -ForegroundColor DarkGray
    }
}

Write-Host "Mirroring project into $cloneProj..." -ForegroundColor Cyan
$syncStart = Get-Date
Sync-Folder -Source (Join-Path $sourceProj 'Assets')          -Destination (Join-Path $cloneProj 'Assets') -ExcludeDirs (@('_Build') + $localExclude)
Sync-Folder -Source (Join-Path $sourceProj 'Packages')        -Destination (Join-Path $cloneProj 'Packages') -ExcludeDirs $localExclude
Sync-Folder -Source (Join-Path $sourceProj 'ProjectSettings') -Destination (Join-Path $cloneProj 'ProjectSettings') -ExcludeDirs $localExclude
Write-Host ("Mirror done in {0:n1}s" -f ((Get-Date) - $syncStart).TotalSeconds) -ForegroundColor Cyan

$needsInject = $true
if (Test-Path $injectedCs) {
    if ((Get-FileHash $sourceCs).Hash -eq (Get-FileHash $injectedCs).Hash) { $needsInject = $false }
}
if ($needsInject) { Copy-Item -Path $sourceCs -Destination $injectedCs -Force }

$logPath = Join-Path $logDir ('webgl_{0:yyyyMMdd_HHmmss}.log' -f (Get-Date))

$unityArgs = @(
    '-batchmode',
    '-silent-crashes',
    '-accept-apiupdate',
    '-projectPath', $cloneProj,
    '-buildTarget', 'webgl',
    '-executeMethod', 'LD60.BuildTools.HeadlessBuild.WebGL',
    '-buildOutput', $Output,
    '-logFile', $logPath
)
if (-not $Graphics) { $unityArgs += '-nographics' }
if ($Dev) { $unityArgs += '-buildDevelopment' }

Write-Host "Unity:  $UnityExe"
Write-Host "Output: $Output"
Write-Host "Log:    $logPath"
Write-Host "Building (first run imports the whole project, expect many minutes)..." -ForegroundColor Cyan

$buildStart = Get-Date
$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru -NoNewWindow
$null = $proc.Handle
try { $proc.PriorityClass = $Priority } catch { Write-Host "Could not lower process priority: $_" -ForegroundColor Yellow }

while (-not (Test-Path $logPath) -and -not $proc.HasExited) { Start-Sleep -Milliseconds 200 }

if (Test-Path $logPath) {
    $stream = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    $reader = New-Object System.IO.StreamReader($stream)
    while (-not $proc.HasExited) {
        $line = $reader.ReadLine()
        if ($null -ne $line) { Write-Host $line } else { Start-Sleep -Milliseconds 150 }
    }
    while ($null -ne ($line = $reader.ReadLine())) { Write-Host $line }
    $reader.Dispose()
    $stream.Dispose()
}

$proc.WaitForExit()
$elapsed = (Get-Date) - $buildStart

$exitCode = $proc.ExitCode
if ($null -eq $exitCode) { $exitCode = 1 }

if ($exitCode -eq 0) {
    Write-Host ("BUILD SUCCEEDED in {0:hh\:mm\:ss} -> {1}" -f $elapsed, $Output) -ForegroundColor Green
} else {
    Write-Host ("BUILD FAILED (exit {0}) after {1:hh\:mm\:ss}. Log: {2}" -f $exitCode, $elapsed, $logPath) -ForegroundColor Red
}
exit $exitCode
