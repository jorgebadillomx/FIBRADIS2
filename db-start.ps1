$root = $PSScriptRoot

Write-Host "Levantando PostgreSQL..."
docker compose -f "$root\docker-compose.dev.yml" up -d

$elapsed = 0
do {
    Start-Sleep -Seconds 1
    $elapsed++
    $ready = docker exec fibradis-postgres-1 pg_isready -U fibradis_app -d fibradis_dev 2>$null
} while ($ready -notlike "*accepting connections*" -and $elapsed -lt 30)

if ($elapsed -ge 30) { Write-Error "PostgreSQL no respondio en 30s."; exit 1 }
Write-Host "PostgreSQL listo en puerto 5432."
