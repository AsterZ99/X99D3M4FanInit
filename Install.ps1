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
$action = New-ScheduledTaskAction -Execute $exe -Argument '--unattended'
$trigger = New-ScheduledTaskTrigger -AtStartup
$trigger.Delay = 'PT15S'
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 2) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Ensure X99D3M4 NCT5532D/C56x SYS_FAN uses PWM output.' -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Host 'Installation succeeded. The task has been started once for verification.' -ForegroundColor Green
Write-Host "Install directory: $installDir"
Write-Host 'Wait a few seconds, then run Check.cmd.'
