$root = $PSScriptRoot

Get-Process -Name *dotnet*, *node* -ErrorAction SilentlyContinue | Stop-Process -Force

$pidFile = "$root\.dev-wt-pid"
if (Test-Path $pidFile) {
    $wtPid = Get-Content $pidFile
    Get-Process -Id $wtPid -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-Item $pidFile
} else {
    Write-Host "No se encontro PID de WindowsTerminal guardado."
}

Write-Host "Bajando PostgreSQL..."
docker compose -f "$root\docker-compose.dev.yml" down
