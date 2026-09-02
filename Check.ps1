$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$root = Join-Path $env:ProgramData 'X99D3M4FanInit'
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if (-not $task) { Write-Host '尚未安装计划任务。' -ForegroundColor Yellow; exit 1 }
$info = Get-ScheduledTaskInfo -TaskName $taskName
Write-Host "任务状态：$($task.State)"
Write-Host "上次运行：$($info.LastRunTime)"
Write-Host "返回代码：$($info.LastTaskResult)（0 表示成功）"
$log = Get-ChildItem -LiteralPath (Join-Path $root 'logs') -Filter '*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($log) { Write-Host "最新日志：$($log.FullName)"; Write-Host ''; Get-Content -LiteralPath $log.FullName }
else { Write-Host '尚未找到日志，请稍后重试。' -ForegroundColor Yellow }
