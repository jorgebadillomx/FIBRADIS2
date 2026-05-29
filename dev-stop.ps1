Get-Process -Name *dotnet*, *node* -ErrorAction SilentlyContinue | Stop-Process -Force

$pidFile = "$PSScriptRoot\.dev-wt-pid"
if (Test-Path $pidFile) {
    $wtPid = Get-Content $pidFile
    Get-Process -Id $wtPid -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-Item $pidFile
} else {
    Get-Process -Name "WindowsTerminal" -ErrorAction SilentlyContinue | Stop-Process -Force
}
