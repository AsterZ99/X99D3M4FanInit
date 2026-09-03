$taskName = 'X99D3M4 SYS_FAN PWM Initializer'
$root = Join-Path $env:ProgramData 'X99D3M4FanInit'

& schtasks.exe /Query /TN $taskName /FO LIST /V
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'The scheduled task is not installed. Run Install.cmd as administrator.' -ForegroundColor Yellow
    exit 1
}

$log = Get-ChildItem -LiteralPath (Join-Path $root 'logs') -Filter '*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -ne $log) {
    Write-Host ''
    Write-Host "Latest log: $($log.FullName)"
    Write-Host ''
    Get-Content -LiteralPath $log.FullName
} else {
    Write-Host ''
    Write-Host 'The task exists, but no log was found yet. Wait a few seconds and try again.' -ForegroundColor Yellow
}
