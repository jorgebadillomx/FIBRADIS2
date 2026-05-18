# FIBRADIS — Visión General del Proyecto

## Qué es

**FIBRADIS** = Financial Bonds & Fixed-Income Discovery System

Plataforma web integral de análisis y gestión de **FIBRAs inmobiliarias mexicanas** (REITs mexicanos que cotizan en la BMV). No ejecuta operaciones bursátiles. Su objetivo es convertir información dispersa y heterogénea (PDFs de reportes trimestrales, precios Yahoo Finance, noticias Google News) en métricas estructuradas, señales explicables, score configurable y herramientas prácticas de decisión.

## Tres Superficies de Producto

1. **Mundo Público** — sin autenticación
   - Home con búsqueda global, carrusel de precios, top movers, noticias
   - Catálogo maestro de FIBRAs
   - Ficha pública por FIBRA (mercado + fundamentales + distribuciones + noticias + reportes)
   - (Growth) Comparador público `/comparar?fibras=FUNO11,FMTY14`

2. **Mundo Privado** — autenticación requerida, suscripción de pago
   - Portafolio unificado `/portafolio` (carga Excel/CSV + dashboard + KPIs)
   - Oportunidades con score configurable por usuario
   - Favoritos integrados en ficha, portafolio y oportunidades

3. **Centro de Procesos `/ops`** — solo `AdminOps`
   - Dashboard operativo de pipelines
   - Control de Mercado, Noticias, Fundamentales, Catálogo
   - Configuración sin redeploy: AI_MODE, schedules, commission_factor, avg_periods, blocklist

## Modelo de Negocio

- Mundo público: acceso libre sin registro
- Mundo privado: suscripción de pago
- Centro de Procesos: uso interno exclusivo

## Capacidades Diferenciales

- Extracción automática de fundamentales desde PDFs oficiales con histórico por periodo
- Score configurable por usuario (no ranking opaco impuesto)
- Tolerancia a datos incompletos con estados claros y degradación transparente
- Centro de Procesos interno integrado — operación sin redeploy

## Usuarios Objetivo

- **Visitante público**: descubre el universo de FIBRAs
- **Usuario `User`**: gestiona portafolio, evalúa oportunidades
- **`AdminOps`**: opera pipelines, corrige errores, configura el sistema

## AI_MODE

Tres modos de operación para fundamentales y noticias:
- `Off` (default MVP): sin IA, PDFs almacenados sin procesar, noticias sin resumen
- `Manual`: AdminOps dispara procesamiento mediante skill externo (Claude Code CLI)
- `Api` (Growth): detección y procesamiento automático via proveedor IA

## Lo que FIBRADIS NO hace

- Ejecutar órdenes bursátiles ni integrarse con brokers
- Trading automático ni recomendaciones de compra/venta sin explicación
- Histórico transaccional completo del portafolio
- Cálculo fiscal o conciliación de movimientos
- Notificaciones push/email en MVP
