# Arquitectura de Hosting — Oracle Cloud Free Tier

**Servidor:** Oracle Cloud Always Free — Ampere A1 (4 cores ARM64, 24 GB RAM, 200 GB SSD, Querétaro MX)  
**Modelo:** multi-proyecto con aislamiento por contenedor y routing automático por dominio

---

## Visión general

```
Internet
    │
    ▼
Traefik (contenedor, :80/:443)
    │   ← detecta proyectos por labels de Docker
    │   ← gestiona SSL automático por dominio (Let's Encrypt)
    │   ← redirige HTTP → HTTPS
    │
    ├── fibradis.mx          → stack: fibradis/
    ├── otroproyecto.com     → stack: proyecto-b/
    ├── api.algo.com         → stack: proyecto-c/
    └── midominio.com        → stack: static-sites/
```

Un solo servidor. Infinitos dominios. Cada dominio con su propio SSL automático. Cada proyecto completamente aislado en su propio stack de contenedores.

---

## Tecnologías soportadas

Todos tienen imágenes ARM64 oficiales:

| Tecnología | Imagen Docker | RAM idle aprox. |
|---|---|---|
| ASP.NET Core 10 | `mcr.microsoft.com/dotnet/aspnet:10.0` | ~100 MB |
| Node.js / Next.js | `node:22-alpine` | ~50 MB |
| Python / FastAPI | `python:3.12-slim` | ~40 MB |
| PostgreSQL 16 | `postgres:16-alpine` | ~30–80 MB |
| MongoDB 7 | `mongo:7` | ~100–150 MB |
| Nginx (sitio estático) | `nginx:alpine` | ~5 MB |
| Traefik v3 | `traefik:v3` | ~30 MB |

Con 24 GB de RAM disponibles, el límite práctico son decenas de proyectos activos, no la RAM.

---

## Estructura de carpetas en el servidor

```
/opt/
├── traefik/                    ← se instala UNA VEZ
│   ├── docker-compose.yml
│   ├── traefik.yml
│   └── acme.json               ← certificados SSL (chmod 600)
│
└── projects/
    ├── fibradis/               ← un directorio por proyecto
    │   ├── docker-compose.yml
    │   └── .env
    ├── proyecto-b/
    │   ├── docker-compose.yml
    │   └── .env
    └── static-sites/           ← todos los sitios casi-estáticos juntos
        ├── docker-compose.yml
        └── sites/
            ├── dominio1.com/   ← archivos HTML/CSS/JS
            └── dominio2.com/
```

---

## Setup inicial (solo una vez)

### 1. Instalar Docker en la VM

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Reconectar SSH para aplicar el grupo
```

### 2. Crear red compartida de Traefik

```bash
docker network create traefik_public
```

Todos los proyectos se conectan a esta red para que Traefik los descubra.

### 3. Traefik — `/opt/traefik/traefik.yml`

```yaml
entryPoints:
  web:
    address: ":80"
    http:
      redirections:
        entryPoint:
          to: websecure
          scheme: https
  websecure:
    address: ":443"

providers:
  docker:
    exposedByDefault: false
    network: traefik_public

certificatesResolvers:
  letsencrypt:
    acme:
      email: tu@email.com       # ← cambiar
      storage: /acme.json
      httpChallenge:
        entryPoint: web

api:
  dashboard: true               # opcional, útil para monitoreo
```

### 4. Traefik — `/opt/traefik/docker-compose.yml`

```yaml
services:
  traefik:
    image: traefik:v3
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./traefik.yml:/traefik.yml:ro
      - ./acme.json:/acme.json
    networks:
      - traefik_public

networks:
  traefik_public:
    external: true
```

```bash
touch /opt/traefik/acme.json && chmod 600 /opt/traefik/acme.json
cd /opt/traefik && docker compose up -d
```

**Listo. Traefik ya está corriendo y gestionando SSL.**

---

## Patrones por tipo de proyecto

### Proyecto .NET + PostgreSQL (ej. FIBRADIS)

```yaml
# /opt/projects/fibradis/docker-compose.yml
services:
  api:
    image: tu-registry/fibradis-api:latest
    restart: always
    environment:
      - ConnectionStrings__Default=${DB_CONNECTION}
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - postgres
    networks:
      - traefik_public
      - internal
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.fibradis.rule=Host(`fibradis.mx`) || Host(`www.fibradis.mx`)"
      - "traefik.http.routers.fibradis.tls.certresolver=letsencrypt"
      - "traefik.http.services.fibradis.loadbalancer.server.port=8080"

  postgres:
    image: postgres:16-alpine
    restart: always
    environment:
      - POSTGRES_DB=${POSTGRES_DB}
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks:
      - internal

volumes:
  pgdata:

networks:
  traefik_public:
    external: true
  internal:           # ← postgres NO expuesto a internet
```

> PostgreSQL vive en la red `internal` — solo el API puede alcanzarlo. No expuesto a internet.

### Proyecto con MongoDB

```yaml
services:
  app:
    image: tu-registry/mi-app:latest
    # ... labels Traefik igual que arriba
    networks:
      - traefik_public
      - internal

  mongo:
    image: mongo:7
    restart: always
    volumes:
      - mongodata:/data/db
    environment:
      - MONGO_INITDB_ROOT_USERNAME=${MONGO_USER}
      - MONGO_INITDB_ROOT_PASSWORD=${MONGO_PASSWORD}
    networks:
      - internal        # ← igual, solo accesible internamente
```

### Sitio estático / casi estático

```yaml
# /opt/projects/static-sites/docker-compose.yml
services:
  dominio1:
    image: nginx:alpine
    restart: always
    volumes:
      - ./sites/dominio1.com:/usr/share/nginx/html:ro
    networks:
      - traefik_public
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.dominio1.rule=Host(`dominio1.com`)"
      - "traefik.http.routers.dominio1.tls.certresolver=letsencrypt"

  dominio2:
    image: nginx:alpine
    restart: always
    volumes:
      - ./sites/dominio2.com:/usr/share/nginx/html:ro
    networks:
      - traefik_public
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.dominio2.rule=Host(`dominio2.com`)"
      - "traefik.http.routers.dominio2.tls.certresolver=letsencrypt"

networks:
  traefik_public:
    external: true
```

Todos los sitios estáticos en un solo `compose.yml`. Agregar uno = copiar 8 líneas y poner los archivos en `sites/nuevo-dominio.com/`.

---

## Agregar un nuevo proyecto (flujo operativo)

1. Crear `/opt/projects/nuevo-proyecto/docker-compose.yml` con labels Traefik.
2. Apuntar el DNS del dominio a la IP del servidor.
3. `docker compose up -d`

Traefik detecta el nuevo contenedor, solicita el certificado SSL a Let's Encrypt y empieza a rutear. Sin reinicios, sin tocar configs globales.

---

## Uso estimado de recursos (referencia)

| Componente | RAM |
|---|---|
| Traefik | ~30 MB |
| FIBRADIS (.NET + Postgres) | ~200–500 MB |
| Proyecto Node.js + Postgres | ~150–300 MB |
| Proyecto Python + Postgres | ~120–250 MB |
| MongoDB | ~150–300 MB |
| Sitio estático (Nginx) | ~5–10 MB c/u |
| **Total estimado (10 proyectos medianos)** | **~3–5 GB** |
| **Disponible en free tier** | **24 GB** |

Hay margen para décadas de proyectos personales.

---

## Backups

PostgreSQL:

```bash
# Backup manual
docker exec nombre_postgres pg_dump -U user dbname > backup.sql

# Cron diario (crontab -e)
0 3 * * * docker exec fibradis_postgres pg_dump -U fibradis fibradis_prod | gzip > /opt/backups/fibradis_$(date +\%Y\%m\%d).sql.gz
```

MongoDB:

```bash
docker exec nombre_mongo mongodump --out /backup
```

Oracle Cloud incluye **Block Volume Backups** gratuitos — se pueden programar snapshots del disco completo desde la consola de Oracle.

---

## Imágenes Docker — ¿dónde almacenarlas?

Opciones para el registry de imágenes:

| Opción | Costo | Privacidad |
|---|---|---|
| Docker Hub (free) | Gratis (1 repo privado) | Pública por default |
| GitHub Container Registry (ghcr.io) | Gratis para repos públicos | Privada con GitHub free |
| Oracle Container Registry (OCIR) | 500 MB gratis en Oracle Cloud | Privada |
| Build directo en el servidor | Sin registry | Local |

Para proyectos personales: **GitHub Container Registry** o build directo en el servidor son las opciones más prácticas.

---

## Relación con FIBRADIS

La migración de FIBRADIS documentada en `oracle-postgresql.md` adopta este modelo de hosting. El deploy de FIBRADIS es un stack más dentro de `/opt/projects/fibradis/`.
