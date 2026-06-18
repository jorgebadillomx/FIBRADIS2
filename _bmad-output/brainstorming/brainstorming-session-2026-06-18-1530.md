---
stepsCompleted: [1, 2, 3]
inputDocuments: []
session_topic: 'Modelo de usuarios y flujo registro/trial/pago para Fibras Inmobiliarias'
session_goals: 'Definir modelo de datos de usuario, flujo registro→trial 14 días→expiración→pago, páginas públicas vs privadas, integración con SPA React + backend .NET'
selected_approach: 'user-selected'
techniques_used: ['Ideation Relay Race']
ideas_generated: [26]
context_file: ''
---

# Brainstorming Session Results

**Facilitador:** Jorge
**Fecha:** 2026-06-18
**Proyecto:** Fibras Inmobiliarias (FIBRADIS)

---

## Session Overview

**Tema:** Modelo de usuarios y flujo registro/trial/pago para Fibras Inmobiliarias

**Objetivos:**
- Definir el modelo de datos del usuario (campos de BD para trial y pago)
- Diseñar el flujo completo: registro → trial 14 días → expiración → pago
- Establecer qué páginas/features son públicas vs privadas
- Integrar todo con la arquitectura actual: SPA React + backend .NET + JWT auth

### Contexto del Proyecto

Sitio financiero de nicho en México (FIBRAs — fideicomisos inmobiliarios en BMV/BIVA). Stack: React SPA (Vite) + .NET backend con JWT auth ya implementado. La plataforma ya tiene un sistema de auth funcional pero sin flujo de registro público ni gestión de suscripción.

### Session Setup

Técnica usada: **Ideation Relay Race** — construcción rápida de ideas en cadena.

---

## Ideas Capturadas (26)

**#1 — Campo PaidAt base**
`PaidAt != NULL` = acceso de por vida. Sin renovaciones periódicas en fase inicial. **Evolucionado en idea #13.**

**#2 — TrialStartedAt on email confirmation**
El trial no arranca al registrarse sino al confirmar el email. `TrialStartedAt = EmailConfirmedAt`.

**#3 — TrialEndsAt materializado**
Campo `TrialEndsAt` almacenado (no calculado), seteado una sola vez: `EmailConfirmedAt + 14 días`.
Ventajas: índice directo, extensión de trial sin cambiar lógica, jobs de background simples.

**#4 — IsActive como propiedad calculada en dominio**
```csharp
public bool IsActive =>
    (SubscriptionType == Lifetime && SubscriptionStartedAt.HasValue) ||
    (SubscriptionEndsAt.HasValue && SubscriptionEndsAt > DateTimeOffset.UtcNow) ||
    (TrialEndsAt.HasValue && TrialEndsAt > DateTimeOffset.UtcNow);
```
Sin columna calculada en BD — se persiste vía job.

**#5 — /api/me devuelve isActive pre-calculado**
Backend calcula `isActive`, frontend solo consume. Cambios de lógica en un solo lugar.

**#6 — Middleware [RequiresActiveAccount]**
Atributo de autorización en .NET. Devuelve `403` con body `{ "reason": "trial_expired" | "trial_not_started" }`.

**#7 — Dos reason codes**
`trial_not_started` (email no confirmado) y `trial_expired` (venció el trial). `never_subscribed` descartado — no aplica con el modelo actual.

**#8 — Rutas públicas sin restricción**
`/`, `/fibras`, `/fibras/:ticker`, `/acerca`, `/faq`, `/calculadora` y todo lo no listado como privado. El paywall es sobre features, no sobre datos.

**#9 — Rutas privadas (requieren isActive)**
`/portafolio`, `/oportunidades`, `/herramientas`, `/reportes`, `/perfil`.

**#10 — ProtectedRoute + redirección a /activar**
Componente React que redirige a `/activar` (no a `/login`) cuando `isActive === false`.

**#11 — Dos rutas de conversión separadas**
`/confirmar-email?token=xxx` para confirmación de email y `/activar` para pago. Separadas por analytics, flujo y seguridad (el token de email no mezcla con la página de pago).

**#12 — Modelo de BD v1 (evolucionado en #19 y #22)**
Base del modelo discutida. Ver idea #19 para versión final.

**#13 — Suscripciones: Monthly | Annual | Lifetime**
Reemplaza el campo simple `PaidAt`. Tres tipos de suscripción.

**#14 — Endpoint admin PATCH /api/admin/users/{id}/subscription**
```json
{ "type": "Annual", "startedAt": "2026-06-18", "endsAt": "2027-06-18" }
```
Protegido con rol `Admin` (ya existe en el JWT). Panel Ops existente.

**#15 — /activar fase 1: instrucciones de transferencia**
Sin pasarela de pagos. Muestra CLABE + precios + botón "Ya pagué, avísanos" que dispara email a portafoliodefibras@gmail.com con el UserId. Cero desarrollo de pasarela.

**#16 — Emails automáticos con Resend (gratis, 3k/mes)**
1. Registro → "Confirma tu email" (con token)
2. Día 11 → "Te quedan 3 días de prueba"
3. Día 14 → "Tu prueba terminó"
4. Admin activa → "¡Ya tienes acceso completo!"
5. Renovación: 3 días antes para Monthly, 30 días antes para Annual

**#17 — Formulario de /registro**
Campos: Email (req), Contraseña (req), Nombre (opcional), ¿Cómo nos encontraste? (select).

**#18 — HowDidYouHear como enum**
`Google | RedesSociales | Recomendacion | Otro`. Fácil de reportar en panel Ops.

**#19 — Modelo final de BD (migration sobre tabla existente)**

Tabla existente en producción `[auth].[User]` ya tiene: `Id`, `Email`, `Apodo`, `PasswordHash`, `Role`, `CreatedAt`, `IsActive` (bit), `HasAcceptedTerms`, `TermsAcceptedAt`, `Pago`, `FechaPago`.

Campos a AGREGAR:

```sql
+ EmailConfirmedAt    datetime2 NULL
+ TrialEndsAt         datetime2 NULL
+ SubscriptionType    nvarchar(16) NULL   -- 'Monthly'|'Annual'|'Lifetime'
+ SubscriptionEndsAt  datetime2 NULL
+ HowDidYouHear       nvarchar(32) NULL
```

`FechaPago` → se reutiliza como `SubscriptionStartedAt` semánticamente.
`IsActive` → stored bit, recalculado por job diario.

**#20 — SubscriptionMaintenanceJob en Hangfire (único job)**
Corre diario. Hace todo: expirar suscripciones vencidas, recordatorios de trial, recordatorios de renovación.

**#21 — Tres responsabilidades del job**
1. Desactivar usuarios con `SubscriptionEndsAt < hoy` → `IsActive = 0` + email
2. Detectar trials que vencen en 3 días → email recordatorio
3. Detectar suscripciones que vencen en 3 días (Monthly) o 30 días (Annual) → email

**#22 — Migration de usuarios existentes**
Todos los usuarios con `IsActive = 1` → `SubscriptionType = 'Lifetime'`, `SubscriptionStartedAt = ISNULL(FechaPago, GETUTCDATE())`, `SubscriptionEndsAt = NULL`.

```sql
UPDATE [auth].[User]
SET SubscriptionType      = 'Lifetime',
    SubscriptionStartedAt = ISNULL(FechaPago, GETUTCDATE()),
    SubscriptionEndsAt    = NULL,
    IsActive              = 1
WHERE IsActive = 1;
```

**#23 — Script de migration completo**
Ver idea #22. Un solo UPDATE sin casos borde.

**#24 — Anti-abuse del trial**

| Fase | Nivel | Implementación |
|------|-------|----------------|
| Fase 1 | Nivel 1 | Bloqueo de dominios desechables (lista negra ~200 dominios) |
| Fase 2 | Nivel 3 | Firebase Phone Auth — 10k SMS/mes gratis, 1 teléfono = 1 trial |

Nivel 2 (IP rate limit) descartado.

**#25 — Panel admin /admin/usuarios en Ops**
Pendiente de definir — ver y activar usuarios manualmente desde el panel Ops existente.

**#26 — Fases del proyecto**

| | Fase 1 | Fase 2 |
|---|---|---|
| Pago | Manual (transferencia bancaria) | Pasarela (MercadoPago o Stripe) |
| Anti-abuse | Bloqueo dominios desechables | Firebase Phone Auth |
| Email | Resend (gratis) | Resend |

---

## Decisiones Clave

1. `IsActive` = stored bit (no computed column), recalculado por Hangfire job diario
2. Trial arranca on email confirmation, no on signup
3. `TrialEndsAt` materializado (no calculado) para permitir extensiones y jobs simples
4. Suscripción: Monthly / Annual / Lifetime — sin renovación automática en fase 1
5. Fase 1 es 100% manual en pagos; modelo de BD soporta fase 2 sin cambios de esquema
6. Usuarios existentes activos → migrar a Lifetime automáticamente
