$ErrorActionPreference = "Stop"

$basePath = Join-Path $env:ProgramData "FocusBlocker"
$serviceName = "FocusBlocker.Service"
$serviceExe = Join-Path $PSScriptRoot "..\..\src\FocusBlocker.Service\bin\Release\net8.0-windows\FocusBlocker.Service.exe"
$uiExe = Join-Path $PSScriptRoot "..\..\src\FocusBlocker.UI\bin\Release\net8.0-windows\FocusBlocker.UI.exe"

New-Item -ItemType Directory -Path $basePath -Force | Out-Null

if (-not (Test-Path (Join-Path $basePath "appsettings.json"))) {
  Copy-Item (Join-Path $PSScriptRoot "..\..\config\appsettings.default.json") (Join-Path $basePath "appsettings.json")
}

if (-not (Test-Path (Join-Path $basePath "domains.json"))) {
  Copy-Item (Join-Path $PSScriptRoot "..\..\config\domains.default.json") (Join-Path $basePath "domains.json")
}

if (-not (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
  New-Service -Name $serviceName -BinaryPathName $serviceExe -DisplayName "Focus Blocker Service" -StartupType Automatic
}

Start-Service -Name $serviceName

$taskName = "FocusBlocker.UI"
$action = New-ScheduledTaskAction -Execute $uiExe
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -UserId $env:UserName -RunLevel LeastPrivilege
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Force

Write-Host "FocusBlocker installed."
