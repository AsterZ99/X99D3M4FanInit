$ErrorActionPreference = 'Stop'
$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$installDir = Join-Path $env:ProgramData 'X99D3M4FanInit'
$sourceExe = Join-Path $PSScriptRoot 'X99D3M4FanInit.exe'

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw 'X99D3M4FanInit.exe is missing. Extract the complete release package before installation.'
}

New-Item -ItemType Directory -Path $installDir -Force | Out-Null
$allowedExtensions = @('.exe', '.dll', '.json', '.txt')
Get-ChildItem -LiteralPath $PSScriptRoot -File |
    Where-Object { $allowedExtensions -contains $_.Extension } |
    Copy-Item -Destination $installDir -Force

$exe = Join-Path $installDir 'X99D3M4FanInit.exe'
$taskCommand = '"' + $exe + '" --unattended'

& schtasks.exe /Create /TN $taskName /TR $taskCommand /SC ONSTART /DELAY 0000:15 /RU SYSTEM /RL HIGHEST /F
if ($LASTEXITCODE -ne 0) {
    throw "schtasks.exe failed to create the task. Exit code: $LASTEXITCODE"
}

& schtasks.exe /Query /TN $taskName | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'The task creation command reported success, but the task could not be found afterward.'
}

& schtasks.exe /Run /TN $taskName
if ($LASTEXITCODE -ne 0) {
    throw "The task was created, but its first run could not be started. Exit code: $LASTEXITCODE"
}

Write-Host 'Installation succeeded and the scheduled task was verified.' -ForegroundColor Green
Write-Host "Task name: $taskName"
Write-Host "Install directory: $installDir"
Write-Host 'Wait a few seconds, then run Check.cmd.'
