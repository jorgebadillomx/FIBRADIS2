#!/bin/bash
# Setup inicial del servidor Oracle Cloud (Ubuntu 24.04, ARM64)
# Correr UNA SOLA VEZ como ubuntu: bash server-setup.sh
set -e

echo "=== 1. Actualizando sistema ==="
sudo apt update && sudo apt upgrade -y

echo "=== 2. Instalando Docker ==="
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER

echo "=== 3. Configurando firewall ==="
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw --force enable

echo "=== 4. Creando red de Traefik ==="
sudo docker network create traefik_public || true

echo "=== 5. Creando estructura de directorios ==="
sudo mkdir -p /opt/traefik
sudo mkdir -p /opt/projects/fibradis
sudo chown -R $USER:$USER /opt/traefik /opt/projects

echo "=== 6. Copiando configuración de Traefik ==="
# Los archivos vienen del repo clonado
cp ~/FIBRADIS/deploy/traefik/traefik.yml /opt/traefik/
cp ~/FIBRADIS/deploy/traefik/docker-compose.yml /opt/traefik/
touch /opt/traefik/acme.json
chmod 600 /opt/traefik/acme.json

echo "=== 7. Levantando Traefik ==="
cd /opt/traefik && docker compose up -d

echo ""
echo "=== Setup completo ==="
echo "IMPORTANTE: Cierra sesión y vuelve a conectar para que el grupo docker tome efecto."
echo "Luego continúa con el deploy de FIBRADIS."
