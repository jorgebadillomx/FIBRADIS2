$root = $PSScriptRoot

# Detener procesos de dev anteriores
Get-Process -Name *dotnet*, *node* -ErrorAction SilentlyContinue | Stop-Process -Force
$pidFile = "$root\.dev-wt-pid"
if (Test-Path $pidFile) {
    $wtPid = Get-Content $pidFile
    Get-Process -Id $wtPid -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-Item $pidFile
}
Start-Sleep -Seconds 1

# Levantar PostgreSQL
Write-Host "Levantando PostgreSQL..."
docker compose -f "$root\docker-compose.dev.yml" up -d

$elapsed = 0
do {
    Start-Sleep -Seconds 1
    $elapsed++
    $ready = docker exec fibradis-postgres-1 pg_isready -U fibradis_app -d fibradis_dev 2>$null
} while ($ready -notlike "*accepting connections*" -and $elapsed -lt 30)

if ($elapsed -ge 30) { Write-Error "PostgreSQL no respondio en 30s."; exit 1 }
Write-Host "PostgreSQL listo."

# Abrir Windows Terminal con API + Main + Ops
$wtArgs = "new-tab --title API --startingDirectory `"$root`" pwsh -NoExit -Command `"dotnet run --project src/Server/Api/Api.csproj`" " +
        "; split-pane --title Main --startingDirectory `"$root`" pwsh -NoExit -Command `"npm run dev:main`" " +
        "; split-pane --title Ops --startingDirectory `"$root`" pwsh -NoExit -Command `"npm run dev:ops`""

$beforePids = Get-Process -Name "WindowsTerminal" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id
Start-Process wt -ArgumentList $wtArgs
Start-Sleep -Seconds 2

$newPid = (Get-Process -Name "WindowsTerminal" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id) |
          Where-Object { $_ -notin $beforePids } |
          Select-Object -First 1

if ($newPid) { $newPid | Set-Content $pidFile }
