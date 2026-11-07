<#
.SYNOPSIS  Runs the Paint Sort test suites headless (close the Unity Editor first). Exits non-zero if any test fails.
.EXAMPLE   .\tools\test.ps1            # EditMode + PlayMode
.EXAMPLE   .\tools\test.ps1 -Mode EditMode
#>
param([ValidateSet('All', 'EditMode', 'PlayMode')] [string] $Mode = 'All')

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$unity = if ($env:UNITY_PATH) { $env:UNITY_PATH } else { 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' }
if (-not (Test-Path $unity)) { throw "Unity not found at '$unity'. Set the UNITY_PATH environment variable." }

New-Item -ItemType Directory -Force (Join-Path $project 'Reports') | Out-Null
$failed = $false
foreach ($platform in @('EditMode', 'PlayMode')) {
    if ($Mode -ne 'All' -and $Mode -ne $platform) { continue }
    $results = Join-Path $project "Reports\tests_$platform.xml"
    Write-Host "Running $platform tests ..."
    $p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
        '-batchmode', '-nographics', '-projectPath', $project, '-runTests', '-testPlatform', $platform,
        '-testResults', $results, '-logFile', (Join-Path $project "Reports\tests_$platform.log"))
    if ($p.ExitCode -ne 0) { Write-Host "$platform tests FAILED (exit $($p.ExitCode)); see $results"; $failed = $true } else { Write-Host "$platform tests passed." }
}
if ($failed) { exit 1 }
