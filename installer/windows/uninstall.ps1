$ErrorActionPreference = "Stop"

$serviceName = "FocusBlocker.Service"
$taskName = "FocusBlocker.UI"

if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
  Stop-Service -Name $serviceName -Force
  sc.exe delete $serviceName | Out-Null
}

if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
  Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

Write-Host "FocusBlocker uninstalled."
