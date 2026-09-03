$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($null -ne $task) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host 'The scheduled task was removed.' -ForegroundColor Green
} else {
    Write-Host 'The scheduled task was not found.'
}
$installDir = Join-Path $env:ProgramData 'X99D3M4FanInit'
Write-Host "Program files and logs were retained at: $installDir"
