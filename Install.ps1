$ErrorActionPreference = 'Stop'
$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$installDir = Join-Path $env:ProgramData 'X99D3M4FanInit'
$sourceExe = Join-Path $PSScriptRoot 'X99D3M4FanInit.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw '请完整解压发布包后再安装。' }
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -File | Where-Object { $_.Extension -in '.exe','.dll','.json','.txt' } | Copy-Item -Destination $installDir -Force
$exe = Join-Path $installDir 'X99D3M4FanInit.exe'
$action = New-ScheduledTaskAction -Execute $exe -Argument '--unattended'
$trigger = New-ScheduledTaskTrigger -AtStartup
$trigger.Delay = 'PT15S'
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 2) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Ensure X99D3M4 NCT5532D/C56x SYS_FAN uses PWM output.' -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Host '安装成功，任务已立即试运行。' -ForegroundColor Green
Write-Host "安装目录：$installDir"
Write-Host '稍等几秒后运行 Check.cmd 查看结果。'
