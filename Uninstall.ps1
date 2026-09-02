$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host '计划任务已删除。' -ForegroundColor Green
} else { Write-Host '未找到计划任务。' }
Write-Host "日志和程序目录予以保留：$(Join-Path $env:ProgramData 'X99D3M4FanInit')"
