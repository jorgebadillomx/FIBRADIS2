#!/bin/bash
# Deploy de FIBRADIS en producción
# Correr desde /opt/projects/fibradis en el servidor
set -e

echo "=== Construyendo imagen Docker ==="
cd ~/FIBRADIS
git pull
docker build -t fibradis:prod .

echo "=== Aplicando migraciones ==="
# Corre las migraciones contra la BD de producción usando las variables del .env
source /opt/projects/fibradis/.env
docker run --rm \
  --network fibradis_internal \
  -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  -e "Hangfire__UseInMemoryStorage=true" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  -e "Jwt__Secret=${JWT_SECRET}" \
  fibradis:prod \
  dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api || true

echo "=== Levantando servicios ==="
cd /opt/projects/fibradis
docker compose up -d --force-recreate

echo ""
echo "=== Deploy completo ==="
docker compose ps
