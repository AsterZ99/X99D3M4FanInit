$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$root = Join-Path $env:ProgramData 'X99D3M4FanInit'
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($null -eq $task) {
    Write-Host 'The scheduled task is not installed.' -ForegroundColor Yellow
    exit 1
}
$info = Get-ScheduledTaskInfo -TaskName $taskName
Write-Host "Task state: $($task.State)"
Write-Host "Last run: $($info.LastRunTime)"
Write-Host "Result code: $($info.LastTaskResult) (0 means success)"
$log = Get-ChildItem -LiteralPath (Join-Path $root 'logs') -Filter '*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -ne $log) {
    Write-Host "Latest log: $($log.FullName)"
    Write-Host ''
    Get-Content -LiteralPath $log.FullName
} else {
    Write-Host 'No log was found yet. Wait a few seconds and try again.' -ForegroundColor Yellow
}
