$root = $PSScriptRoot

Write-Host "Bajando PostgreSQL..."
docker compose -f "$root\docker-compose.dev.yml" down
