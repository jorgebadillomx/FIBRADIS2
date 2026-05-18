# FIBRADIS — Patrones de API y Comunicación

## Endpoints Clave por Superficie

### Público (`/api/v1/`)

| Endpoint | Descripción |
|---|---|
| `GET /api/v1/fibras` | Catálogo maestro con paginación |
| `GET /api/v1/fibras/{ticker}` | Ficha pública completa |
| `GET /api/v1/fibras/search?q={query}` | Búsqueda autocomplete por ticker o nombre |
| `GET /api/v1/market/latest` | Resumen de mercado para Home (carrusel, top movers) |
| `GET /api/v1/market/{fibraId}/history` | Histórico de precio por periodo |
| `GET /api/v1/news` | Feed general de noticias |
| `GET /api/v1/news?fibraId={id}` | Noticias asociadas a una FIBRA |
| `GET /api/v1/fundamentals/{fibraId}/latest` | Último periodo de fundamentales |

### Privado (`/api/v1/`) — requiere `User`

| Endpoint | Descripción |
|---|---|
| `GET /api/v1/portfolio` | Portafolio del usuario con métricas calculadas |
| `POST /api/v1/portfolio/upload` | Carga de archivo Excel/CSV |
| `PUT /api/v1/portfolio/{positionId}` | Edición inline de posición |
| `DELETE /api/v1/portfolio/{positionId}` | Eliminar posición |
| `GET /api/v1/opportunities` | Ranking de oportunidades con score |
| `PUT /api/v1/opportunities/weights` | Actualizar pesos de score del usuario |
| `POST /api/v1/favorites/{fibraId}` | Marcar como favorita |
| `DELETE /api/v1/favorites/{fibraId}` | Quitar favorita |

### Ops (`/api/v1/ops/`) — requiere `AdminOps`

| Endpoint | Descripción |
|---|---|
| `GET /api/v1/ops/dashboard` | Estado global de pipelines y últimos errores |
| `POST /api/v1/ops/pipelines/market/run` | Run now — mercado |
| `POST /api/v1/ops/pipelines/news/run` | Run now — noticias |
| `POST /api/v1/ops/pipelines/{runId}/retry` | Retry de corrida fallida |
| `POST /api/v1/ops/fundamentals/import` | Importar payload JSON de fundamentales |
| `POST /api/v1/ops/fundamentals/{id}/confirm` | Confirmar registro pendiente |
| `POST /api/v1/ops/fundamentals/{id}/reprocess` | Reprocesar registro |
| `GET/POST/PUT /api/v1/ops/catalog` | CRUD de catálogo de FIBRAs |
| `GET/PUT /api/v1/ops/config` | Leer y actualizar configuración operativa |

## Contrato de Importación de Fundamentales

`POST /api/v1/ops/fundamentals/import`

```json
{
  "fibraId": "uuid",
  "period": "Q1-2025",
  "capRate": 0.072,
  "navPerCbfi": 18.50,
  "ltv": 0.38,
  "noiMargin": 0.71,
  "ffoMargin": 0.52,
  "quarterlyDistribution": 0.42,
  "summary": "Resumen ejecutivo del periodo...",
  "pdfReference": "filename-or-url"
}
```

Todos los campos numéricos son opcionales — null permitido por DR-22b. El sistema valida y muestra preview antes de que AdminOps confirme.

## Formato de Respuesta de Portafolio

```json
{
  "positions": [
    {
      "fibraId": "...",
      "ticker": "FUNO11",
      "shortName": "Fibra Uno",
      "titulos": 1000,
      "costoPromedio": 28.50,
      "costoTotalCompra": 29.07,
      "precioMercado": 31.20,
      "valorMercado": 31200,
      "plusvaliaPercent": 9.47,
      "gananciaAmount": 2700,
      "portfolioWeight": 0.35,
      "dataStatus": "fresh",
      "isFavorite": true,
      "signal": {
        "badge": "green",
        "navVsPrecio": 0.15,
        "tooltip": "Cotiza 15% por debajo de su NAV"
      }
    }
  ],
  "kpis": {
    "inversionTotal": 85000,
    "valorTotal": 92000,
    "plusvaliaTotalPercent": 8.23,
    "gananciaTotalAmount": 7000,
    "rentasAnualesBrutas": 4200,
    "rentasRealesBrutas": 3800,
    "rentasPortfolioPercent": 0.049
  }
}
```

## Patrones de Eventos de Dominio

Nombres en PascalCase, past-tense:
- `PdfReportDetected`
- `MarketSnapshotStored`
- `NewsItemIngested`
- `FundamentalsImported`
- `FundamentalsConfirmed`
- `PortfolioUploaded`

Payload mínimo: `{ id, occurredAt, correlationId, ...context_mínimo }`

## Idempotencia

Requerida en:
- Retry de jobs de mercado y noticias
- Reprocess de fundamentales
- Run now de pipelines (no debe duplicar datos)
- Carga de portafolio (idempotente si mismo archivo, reemplaza si diferente)

## Error Handling

```json
{
  "type": "https://fibradis.com/errors/validation",
  "title": "Validation failed",
  "status": 422,
  "domainCode": "PORTFOLIO_INVALID_TICKER",
  "correlationId": "abc-123",
  "errors": {
    "row": 3,
    "ticker": "BADTICKER",
    "message": "Ticker no encontrado en el catálogo. ¿Quisiste decir FUNO11?"
  }
}
```

## Generación del Cliente API Tipado

```bash
npm run codegen:api  # ejecuta scripts/codegen/generate-api-client.ps1
```

El cliente se genera en `src/Web/SharedApiClient/` desde el OpenAPI doc del backend. Ambos SPAs lo consumen. **Nunca editar manualmente** los archivos generados.

## Manejo de Estado Parcial en UI

- Siempre mostrar datos disponibles aunque falten otros
- Usar `—` para campos sin dato
- Badge `sin datos` / `parcial` / `no evaluable` donde aplica
- Nunca mostrar error fatal de UI por dato faltante individual
