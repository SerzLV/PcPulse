# Builds PC Pulse and installs it to Program Files, where only administrators can write.
# Since the app always runs elevated, it must not load its DLLs from a folder ordinary users can change.
#
#   powershell -ExecutionPolicy Bypass -File tools\install.ps1
#
# The build runs as the current user; only the copy step asks for elevation (one UAC prompt).
param(
    [string]$Target = (Join-Path $env:ProgramFiles 'PC Pulse'),
    [switch]$Elevated,
    [string]$Stage
)
$ErrorActionPreference = 'Stop'

if ($Elevated) {
    # Stop a running copy so its files can be replaced.
    Get-Process PcPulse -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1

    New-Item -ItemType Directory -Force $Target | Out-Null
    robocopy $Stage $Target /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with code $LASTEXITCODE" }

    # Program Files subfolders inherit admin-only write access; reset in case the folder pre-existed with other rights.
    icacls $Target /reset /T /Q | Out-Null

    $shortcut = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'PC Pulse.lnk'
    $link = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcut)
    $link.TargetPath = Join-Path $Target 'PcPulse.exe'
    $link.WorkingDirectory = $Target
    $link.IconLocation = (Join-Path $Target 'PcPulse.exe') + ',0'
    $link.Description = 'Температуры CPU/GPU и FPS'
    $link.Save()
    exit 0
}

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'PcPulse\PcPulse.csproj'

Write-Host 'Тесты...'
dotnet test (Join-Path $root 'PcPulse.Tests\PcPulse.Tests.csproj') --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Тесты не прошли — установка отменена' }
$stageDir = Join-Path $env:TEMP 'PcPulse-publish'
$artifacts = Join-Path $env:TEMP 'PcPulse-artifacts'
foreach ($d in $stageDir, $artifacts) { if (Test-Path $d) { Remove-Item $d -Recurse -Force } }

Write-Host 'Сборка...'
# Separate artifacts path: a running copy of the app can't lock the build output.
dotnet publish $project -c Release -o $stageDir --artifacts-path $artifacts --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Write-Host "Установка в $Target (нужно подтвердить UAC)..."
$elevatedArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"",
                  '-Elevated', '-Stage', "`"$stageDir`"", '-Target', "`"$Target`"")
$proc = Start-Process powershell -Verb RunAs -WindowStyle Hidden -ArgumentList $elevatedArgs -Wait -PassThru
if ($proc.ExitCode -ne 0) { throw "Установка не удалась (код $($proc.ExitCode))" }

Remove-Item $stageDir, $artifacts -Recurse -Force
Write-Host "Готово: $Target\PcPulse.exe, ярлык «PC Pulse» в меню Пуск."
