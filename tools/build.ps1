<#
.SYNOPSIS  One-command build of Paint Sort from the command line (close the Unity Editor first: a project can only be open once).
.EXAMPLE   .\tools\build.ps1 -Target Android -Development
.EXAMPLE   .\tools\build.ps1 -Target Android -Aab        # release: bumps the Play Store build code in version.json
.NOTES     Release builds (no -Development) bump the build code in version.json; development builds never do.
           Set UNITY_PATH to the Unity.exe of editor version 6000.6.7f1 if it is not in the default Hub location.
#>
param(
    [ValidateSet('Android', 'Windows')] [string] $Target = 'Android',
    [switch] $Development,
    [switch] $Aab,
    [string] $Output = ''
)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' }
if (-not (Test-Path $unity)) { throw "Unity not found at '$unity'. Set the UNITY_PATH environment variable." }

$method = if ($Target -eq 'Android') { 'PaintSort.EditorTools.BuildTools.BuildAndroid' } else { 'PaintSort.EditorTools.BuildTools.BuildWindows' }
$arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', $project, '-executeMethod', $method, '-logFile', (Join-Path $project 'Builds\build.log'))
if ($Development) { $arguments += '-development' }
if ($Aab)         { $arguments += '-aab' }
if ($Output)      { $arguments += @('-output', $Output) }

New-Item -ItemType Directory -Force (Join-Path $project 'Builds') | Out-Null
Write-Host "Building $Target (development=$($Development.IsPresent)) ..."
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru
if ($process.ExitCode -ne 0) { Write-Host "Build FAILED (exit $($process.ExitCode)). See Builds\build.log"; exit $process.ExitCode }
Write-Host 'Build succeeded.'
