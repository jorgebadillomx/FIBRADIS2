#!/bin/bash
# Deploy de FIBRADIS en producción
# Correr desde /opt/projects/fibradis en el servidor
set -e

echo "=== Construyendo imagen Docker ==="
cd ~/FIBRADIS
git pull
docker build -t ghcr.io/jorgebadillomx/fibradis:prod .

echo "=== Aplicando migraciones ==="
source /opt/projects/fibradis/.env
docker run --rm \
  --network fibradis_internal \
  -e "DatabaseProvider=SqlServer" \
  -e "ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=${MSSQL_DB};User Id=${MSSQL_USER};Password=${MSSQL_PASSWORD};Encrypt=True;TrustServerCertificate=True;" \
  -e "Hangfire__UseInMemoryStorage=true" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  -e "Jwt__Secret=${JWT_SECRET}" \
  ghcr.io/jorgebadillomx/fibradis:prod \
  dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api || true

echo "=== Levantando servicios ==="
cd /opt/projects/fibradis
docker compose up -d --force-recreate

echo ""
echo "=== Deploy completo ==="
docker compose ps
