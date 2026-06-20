# Story 14.12: Precios reales, CLABE y comprobante de pago

Status: done

## Story

Como usuario que desea suscribirse a Fibras Inmobiliarias,
quiero ver los precios correctos y los datos de pago reales (CLABE y banco), y poder adjuntar mi comprobante de pago al notificar al equipo,
para que el proceso de activación sea más rápido y el equipo pueda verificar el pago sin pedirme el comprobante por separado.

## Acceptance Criteria

1. **Dado que** navego a `/activar` o a `/suscripcion` (cualquier usuario autenticado),
   **Entonces** los planes muestran exactamente: **Mensual $39 MXN / mes**, **Anual $390 MXN / año**, **Lifetime $990 MXN** (pago único).

2. **Dado que** navego a `/activar` o `/suscripcion`,
   **Entonces** las instrucciones de pago muestran: **CLABE 722969010321418243**, **Banco Mercado Pago**, **Concepto Suscripción Fibras Inmobiliarias**, **Contacto contacto@fibrasinmobiliarias.com**.

3. **Dado que** hago clic en "Ya pagué — notificar al equipo" (en `/activar` o `/suscripcion`),
   **Entonces** el botón se expande mostrando un campo de carga de archivo con el texto "Adjunta tu comprobante (imagen o PDF, máx. 5 MB)" y dos acciones: botón primario "Enviar comprobante" (habilitado solo cuando hay un archivo seleccionado) y enlace secundario "Enviar sin comprobante".

4. **Dado que** el usuario selecciona un archivo válido (imagen o PDF ≤ 5 MB) y hace clic en "Enviar comprobante",
   **Entonces** el sistema envía la notificación con el archivo adjunto y muestra "✓ Comprobante enviado. Te contactaremos para activar tu acceso."

5. **Dado que** el usuario hace clic en "Enviar sin comprobante",
   **Entonces** el sistema envía la notificación sin adjunto y muestra el mismo mensaje de confirmación.

6. **Dado que** el usuario selecciona un archivo con tipo incorrecto (no imagen ni PDF),
   **Entonces** se muestra el mensaje de error inline "Solo se aceptan imágenes y PDF" sin enviar nada.

7. **Dado que** el usuario selecciona un archivo que supera 5 MB,
   **Entonces** se muestra el mensaje de error inline "El archivo supera el límite de 5 MB" sin enviar nada.

8. **Dado que** el administrador recibe el email de notificación de pago con comprobante adjunto,
   **Entonces** el email incluye el archivo como adjunto y el cuerpo indica userId y email del usuario.

9. **Dado que** corro `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main`,
   **Entonces** 0 errores TypeScript y 0 warnings de compilación.

10. **Dado que** corro `npm test --workspace=src/Web/Main`,
    **Entonces** los tests nuevos de validación de archivo pasan y los tests existentes no tienen regresiones.

## Tasks / Subtasks

- [x] T1: Crear `src/Web/Main/src/pages/payment-plans.ts` — constante compartida (AC: 1, 2)
  - [x] T1.1: Exportar `PLANES` con los nuevos precios ($39/mes, $390/año, $990 pago único)
  - [x] T1.2: Exportar `PAYMENT_INFO` con CLABE, banco, concepto y contacto
  - [x] T1.3: Exportar `MAX_RECEIPT_BYTES = 5 * 1024 * 1024` y `RECEIPT_ACCEPT = 'image/*,application/pdf'`

- [x] T2: Actualizar `ActivarPage.tsx` — consumir `payment-plans.ts` (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] T2.1: Eliminar el array `PLANES` hardcodeado; importar desde `./payment-plans`
  - [x] T2.2: Reemplazar los `[PENDIENTE]` de CLABE y banco con `PAYMENT_INFO`; actualizar email de contacto a `contacto@fibrasinmobiliarias.com`
  - [x] T2.3: En `TrialExpiredView`, reemplazar el botón directo "Ya pagué" por el nuevo componente `<NotifyWithReceiptButton />`

- [x] T3: Actualizar `SuscripcionPage.tsx` — consumir `payment-plans.ts` (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] T3.1: Eliminar el array `PLANES` hardcodeado; importar desde `./payment-plans`
  - [x] T3.2: Reemplazar `[CLABE PENDIENTE]`, `[BANCO PENDIENTE]` y email incorrecto con valores de `PAYMENT_INFO`
  - [x] T3.3: En `PaymentSection`, reemplazar el botón "Ya pagué" por `<NotifyWithReceiptButton />`

- [x] T4: Crear `src/Web/Main/src/pages/NotifyWithReceiptButton.tsx` (AC: 3, 4, 5, 6, 7)
  - [x] T4.1: Componente autocontenido con estado interno `'idle' | 'uploading' | 'sending' | 'sent' | 'error'`
  - [x] T4.2: Estado `idle`: mostrar botón "Ya pagué — notificar al equipo"
  - [x] T4.3: Estado `uploading` (tras primer clic): mostrar el campo de archivo + "Enviar comprobante" (disabled si no hay archivo) + enlace "Enviar sin comprobante"
  - [x] T4.4: Validar archivo: tipo con `file.type.startsWith('image/') || file.type === 'application/pdf'` y tamaño con `file.size <= MAX_RECEIPT_BYTES`; errores inline (AC: 6, 7)
  - [x] T4.5: "Enviar comprobante" → llama `notifyPayment(file)` con el archivo
  - [x] T4.6: "Enviar sin comprobante" → llama `notifyPayment()` sin archivo
  - [x] T4.7: Estado `sent`: mostrar "✓ Comprobante enviado. Te contactaremos para activar tu acceso."
  - [x] T4.8: Estado `error`: mostrar "No se pudo enviar. Escríbenos a contacto@fibrasinmobiliarias.com."
  - [x] T4.9: El input de archivo tiene `id="comprobante-file"`, `name="comprobante"`, `aria-label="Comprobante de pago"`

- [x] T5: Actualizar `authApi.ts` — `notifyPayment()` acepta archivo opcional (AC: 4, 5)
  - [x] T5.1: Modificar firma: `notifyPayment(comprobante?: File): Promise<void>`
  - [x] T5.2: Si `comprobante` presente: crear `FormData`, `formData.append('comprobante', comprobante)`, enviar con `body: formData` (sin `Content-Type` manual)
  - [x] T5.3: Si no hay archivo: POST sin body — comportamiento igual al anterior

- [x] T6: Backend — extender endpoint `/api/v1/account/notify-payment` para aceptar archivo (AC: 8)
  - [x] T6.1: Agregar `IFormFile? comprobante` al endpoint + `.DisableAntiforgery()`
  - [x] T6.2: Validar `ContentType` (image/* o application/pdf) y tamaño (≤ 5_242_880); retornar `400 ValidationProblem` si inválido
  - [x] T6.3: Leer bytes con `MemoryStream`; pasar `(fileBytes, comprobante.FileName)` a `emailService.SendPaymentNotificationAsync()`
  - [x] T6.4: Si `comprobante == null`: pasar `(null, null)` — comportamiento igual al anterior

- [x] T7: Backend — extender `IEmailService` y `ResendEmailService` para adjuntos (AC: 8)
  - [x] T7.1: `IEmailService.cs` actualizado: `SendPaymentNotificationAsync(Guid userId, string userEmail, byte[]? fileContent, string? fileName, CancellationToken ct)`
  - [x] T7.2: `ResendEmailService.cs`: dos code paths — template cuando `fileContent == null`, raw email con attachments base64 cuando `fileContent != null`
  - [x] T7.3: 4 fakes actualizados: `ApiWebFactory.CapturingEmailService`, `ResendEmailServiceTests` (call site), `SubscriptionMaintenanceJobTests.FakeUserServiceEmail`, `UserServiceTests.CapturingEmailService`

- [x] T8: Tests y build (AC: 9, 10)
  - [x] T8.1: `notify-receipt-logic.test.ts` creado — 6 tests: tipo inválido, tamaño excedido, image/png, application/pdf, exactamente en el límite, text/plain rechazado
  - [x] T8.2: Agregado a la lista de tests en `package.json`
  - [x] T8.3: `npm run build --workspace=src/Web/Main` — 0 errores TypeScript ✓
  - [x] T8.4: `dotnet build FIBRADIS.slnx` — 0 errores C# ✓

### Review Findings

- [x] [Review][Patch] [HIGH] P1: Sanitizar `comprobante.FileName` con `Path.GetFileName` antes de pasar a `SendPaymentNotificationAsync` — nombre sin sanitizar permite path traversal/injection en `filename` del adjunto de Resend [AccountEndpoints.cs:notify-payment]
- [x] [Review][Patch] [MED] P2: Mover validación de tamaño DESPUÉS de `CopyToAsync`; comparar `ms.Length > 5_242_880` en lugar de `comprobante.Length` (declarado por el cliente); añadir guard de archivo vacío `ms.Length == 0` [AccountEndpoints.cs:notify-payment]
- [x] [Review][Patch] [MED] P3: Reemplazar `fileName ?? "comprobante"` por `!string.IsNullOrWhiteSpace(fileName) ? fileName : "comprobante"` en `SendRawEmailWithAttachmentAsync` — el operador `??` no cubre cadena vacía [ResendEmailService.cs:SendRawEmailWithAttachmentAsync]
- [x] [Review][Patch] [LOW] P4: Simplificar catch redundante en `handleSend` — ambas ramas hacen `setStep('error')`, eliminar if/else [NotifyWithReceiptButton.tsx:handleSend]
- [x] [Review][Patch] [LOW] P5: Añadir `role="status"` al `<p>` de confirmación de éxito — el estado error tiene `role="alert"` pero el estado sent no tiene región live para lectores de pantalla [NotifyWithReceiptButton.tsx:sent render]
- [x] [Review][Patch] [LOW] P6: Guard para `file.type === ""` en `validateReceiptFile` — algunos browsers omiten el MIME type para extensiones desconocidas; actualmente falla silenciosamente como `invalid_type` sin mensaje claro [notify-receipt-logic.ts:validateReceiptFile]
- [x] [Review][Patch] [LOW] P7: Guard `status === 'checking'` en `handleFirstClick` — si auth no ha resuelto y el usuario hace clic, redirige a `/login` aunque el usuario esté autenticado [NotifyWithReceiptButton.tsx:handleFirstClick]
- [x] [Review][Defer] D1: Rate limiting / `MaxRequestBodySize` explícito en endpoint [AccountEndpoints.cs] — deferred, infraestructura; Kestrel default de 30MB ya limita la superficie
- [x] [Review][Defer] D2: CLABE hardcodeada en bundle del cliente en lugar de `OperationalConfig` — deferred, arquitectura prescrita por el story spec
- [x] [Review][Defer] D3: Sin log en happy path de `SendRawEmailWithAttachmentAsync` — deferred, observabilidad futura
- [x] [Review][Defer] D4: Sin botón "Cancelar/Volver" en estado `uploading` de `NotifyWithReceiptButton` — deferred, mejora de UX fuera del spec
- [x] [Review][Defer] D5: `TrialNotStartedView` en `ActivarPage` no muestra planes ni instrucciones de pago — deferred, comportamiento pre-existente; esa vista es para usuarios sin email confirmado, no para pago
- [x] [Review][Defer] D6: `SuscripcionPage` oculta `PaymentSection` a usuarios lifetime — deferred, diseño pre-existente de story 14-10
- [x] [Review][Defer] D7: Errores de Resend en ruta con adjunto se silencian (log only) — deferred, patrón pre-existente idéntico a la ruta sin adjunto (`throwOnFailure: false`)
- [x] [Review][Defer] D8: `navigate('/login')` en `handleFirstClick` sin parámetro `?redirect=` — deferred, comportamiento pre-existente de `ActivarPage`; bajo impacto
- [x] [Review][Defer] D9: Sin tests de componente para los estados de UI de `NotifyWithReceiptButton` (error/sent/disabled) — deferred, AC10 solo exige tests de validación de archivo
- [x] [Review][Defer] D10: `CopyToAsync` no tiene try/catch explícito para `IOException` — deferred, cubierto por el handler global 500 del framework
- [x] [Review][Defer] D11: `ReadAsStringAsync` en rama de error de Resend puede lanzar — deferred, edge case en código de logging no crítico
- [x] [Review][Defer] D12: Race condition de doble envío — deferred, mitigado por React disabled state + transición de estado síncrona
- [x] [Review][Defer] D13: `userEmail` puede ser cadena vacía (claim ausente) — deferred, patrón pre-existente del endpoint; `?? ""` ya existía antes de este story

## Dev Notes

### Archivos a crear (NEW)

| Archivo | Descripción |
|---------|-------------|
| `src/Web/Main/src/pages/payment-plans.ts` | Constantes PLANES, PAYMENT_INFO, MAX_RECEIPT_BYTES, RECEIPT_ACCEPT |
| `src/Web/Main/src/pages/NotifyWithReceiptButton.tsx` | Componente de notificación con upload de comprobante |
| `src/Web/Main/src/pages/notify-receipt-logic.ts` | Función pura `validateReceiptFile` (testeable) |
| `src/Web/Main/src/pages/notify-receipt-logic.test.ts` | Tests unitarios de validación de archivo |

### Archivos a modificar (UPDATE)

| Archivo | Cambio |
|---------|--------|
| `src/Web/Main/src/pages/ActivarPage.tsx` | Importar desde `payment-plans.ts`; usar `<NotifyWithReceiptButton />` en `TrialExpiredView` |
| `src/Web/Main/src/pages/SuscripcionPage.tsx` | Importar desde `payment-plans.ts`; usar `<NotifyWithReceiptButton />` en `PaymentSection` |
| `src/Web/Main/src/modules/auth/authApi.ts` | `notifyPayment(comprobante?: File)` con soporte FormData |
| `src/Server/Application/Email/IEmailService.cs` | Agregar `fileContent` y `fileName` a `SendPaymentNotificationAsync` |
| `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` | Dos code paths: template sin adjunto, raw email con adjunto |
| `src/Server/Api/Endpoints/Private/AccountEndpoints.cs` | `IFormFile? comprobante` + `.DisableAntiforgery()` + validación |

### `payment-plans.ts` — implementación exacta

```typescript
export const PLANES = [
  { nombre: 'Mensual', precio: '$39 MXN / mes', descripcion: 'Acceso completo mensual' },
  { nombre: 'Anual', precio: '$390 MXN / año', descripcion: 'Ahorra un 17% vs. mensual' },
  { nombre: 'Lifetime', precio: '$990 MXN', descripcion: 'Pago único, acceso de por vida' },
]

export const PAYMENT_INFO = {
  clabe: '722969010321418243',
  banco: 'Mercado Pago',
  concepto: 'Suscripción Fibras Inmobiliarias',
  contacto: 'contacto@fibrasinmobiliarias.com',
} as const

export const MAX_RECEIPT_BYTES = 5 * 1024 * 1024  // 5 MB
export const RECEIPT_ACCEPT = 'image/*,application/pdf'
```

### `notify-receipt-logic.ts` — función pura testeable

```typescript
import { MAX_RECEIPT_BYTES } from './payment-plans'

export type ReceiptValidationResult =
  | { valid: true }
  | { valid: false; error: 'invalid_type' | 'too_large' }

export function validateReceiptFile(file: { type: string; size: number }): ReceiptValidationResult {
  const typeOk = file.type.startsWith('image/') || file.type === 'application/pdf'
  if (!typeOk) return { valid: false, error: 'invalid_type' }
  if (file.size > MAX_RECEIPT_BYTES) return { valid: false, error: 'too_large' }
  return { valid: true }
}
```

### Casos de test exactos (T8.1)

```typescript
import test from 'node:test'
import assert from 'node:assert/strict'
import { validateReceiptFile, MAX_RECEIPT_BYTES } from './notify-receipt-logic'  // re-exportar MAX desde aquí o importar desde payment-plans

test('validateReceiptFile rechaza tipo no-imagen no-pdf', () => {
  assert.deepEqual(validateReceiptFile({ type: 'application/zip', size: 100 }), { valid: false, error: 'invalid_type' })
})

test('validateReceiptFile rechaza archivo que supera 5 MB', () => {
  assert.deepEqual(validateReceiptFile({ type: 'image/png', size: MAX_RECEIPT_BYTES + 1 }), { valid: false, error: 'too_large' })
})

test('validateReceiptFile acepta image/png dentro del límite', () => {
  assert.deepEqual(validateReceiptFile({ type: 'image/png', size: 1000 }), { valid: true })
})

test('validateReceiptFile acepta application/pdf', () => {
  assert.deepEqual(validateReceiptFile({ type: 'application/pdf', size: MAX_RECEIPT_BYTES }), { valid: true })
})

test('validateReceiptFile acepta exactamente en el límite', () => {
  assert.deepEqual(validateReceiptFile({ type: 'image/jpeg', size: MAX_RECEIPT_BYTES }), { valid: true })
})
```

### `authApi.ts` — cambio en `notifyPayment`

```typescript
export async function notifyPayment(comprobante?: File): Promise<void> {
  const headers = { ...getMainAuthHeaders() }  // incluye Authorization

  let body: BodyInit | undefined
  if (comprobante) {
    const formData = new FormData()
    formData.append('comprobante', comprobante)
    body = formData
    // NO establecer Content-Type — el browser lo pone con boundary automáticamente
  }

  const res = await fetch('/api/v1/account/notify-payment', {
    method: 'POST',
    headers,
    body,
  })
  if (!res.ok) throw new AuthApiError('notify_payment_failed', 'Error al notificar el pago.')
}
```

**Importante**: cuando se envía `FormData`, NO añadir `Content-Type: application/json` ni ningún Content-Type manual. El navegador lo pone automáticamente como `multipart/form-data; boundary=...`. Si se añade manualmente, el servidor no puede parsear el body.

### Backend — endpoint con IFormFile

```csharp
app.MapPost("/api/v1/account/notify-payment", async (
    IFormFile? comprobante,
    IEmailService emailService,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var userEmail = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";

    byte[]? fileBytes = null;
    string? fileName = null;

    if (comprobante is not null)
    {
        // Validar tipo
        var ct2 = comprobante.ContentType;
        var typeOk = ct2.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                  || ct2.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!typeOk)
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["comprobante"] = ["Solo se aceptan imágenes y PDF."] });

        // Validar tamaño (5 MB)
        if (comprobante.Length > 5_242_880)
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["comprobante"] = ["El archivo supera el límite de 5 MB."] });

        using var ms = new MemoryStream();
        await comprobante.CopyToAsync(ms, ct);
        fileBytes = ms.ToArray();
        fileName = comprobante.FileName;
    }

    await emailService.SendPaymentNotificationAsync(userId, userEmail, fileBytes, fileName, ct);
    return Results.NoContent();
})
.RequireAuthorization()
.DisableAntiforgery()    // obligatorio para IFormFile en Minimal APIs
.WithTags("Account")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized);
```

### Backend — `ResendEmailService` con adjunto

La clave: Resend soporta `attachments` en el endpoint `/emails`, pero **no en el endpoint de templates**. Cuando hay adjunto, usar payload raw (subject + html); cuando no hay adjunto, usar el template existente.

```csharp
public Task SendPaymentNotificationAsync(
    Guid userId, string userEmail,
    byte[]? fileContent, string? fileName,
    CancellationToken ct)
{
    if (fileContent is null)
    {
        // Comportamiento actual: template Resend
        return SendTemplatedEmailAsync(
            ContactEmail,
            options.Value.Templates.PaymentNotification,
            new { USER_ID = userId.ToString(), USER_EMAIL = userEmail },
            $"notificación de pago para userId={userId}",
            throwOnFailure: false,
            ct);
    }

    // Con adjunto: email raw (no template)
    return SendRawEmailWithAttachmentAsync(
        ContactEmail,
        subject: $"Comprobante de pago — {userEmail}",
        html: $"<p>El usuario <strong>{userEmail}</strong> (ID: {userId}) ha notificado un pago y adjuntó un comprobante.</p>",
        fileName: fileName ?? "comprobante",
        fileContent: fileContent,
        operation: $"notificación de pago con adjunto para userId={userId}",
        ct);
}

private async Task SendRawEmailWithAttachmentAsync(
    string toEmail, string subject, string html,
    string fileName, byte[] fileContent, string operation,
    CancellationToken ct)
{
    var opt = options.Value;

    if (string.IsNullOrWhiteSpace(opt.ApiKey) || string.IsNullOrWhiteSpace(opt.SenderEmail))
    {
        logger.LogError("Resend no configurado; se omite {Operation}.", operation);
        return;
    }

    var base64Content = Convert.ToBase64String(fileContent);
    var payload = new
    {
        from = opt.SenderEmail,
        to = new[] { toEmail },
        subject,
        html,
        attachments = new[] { new { filename = fileName, content = base64Content } }
    };

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opt.ApiKey);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Resend rechazó {Operation} con status {StatusCode}. Body: {Body}",
                operation, (int)response.StatusCode, body);
        }
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
    {
        logger.LogError(ex, "No se pudo enviar {Operation}.", operation);
    }
}
```

### Mocks de `IEmailService` en tests

Buscar todas las clases que implementen `IEmailService` fuera de `ResendEmailService`:

```bash
grep -rn "IEmailService" tests/ src/Server/ --include="*.cs" -l
```

Cualquier fake o stub deberá actualizar la firma de `SendPaymentNotificationAsync`. Patrón mínimo:

```csharp
public Task SendPaymentNotificationAsync(
    Guid userId, string userEmail,
    byte[]? fileContent, string? fileName,
    CancellationToken ct) => Task.CompletedTask;
```

### `NotifyWithReceiptButton` — flujo UX

```
Estado idle:
  [Ya pagué — notificar al equipo]

Estado uploading (tras clic en botón):
  Label: "Adjunta tu comprobante (imagen o PDF, máx. 5 MB)"
  <input type="file" id="comprobante-file" name="comprobante"
         aria-label="Comprobante de pago" accept="image/*,application/pdf" />
  [error inline si aplica]
  [Enviar comprobante]  ← disabled si no hay archivo o hay error
  [Enviar sin comprobante]  ← siempre activo

Estado sending: spinner, ambas acciones disabled

Estado sent:
  ✓ Comprobante enviado. Te contactaremos para activar tu acceso.

Estado error:
  No se pudo enviar. Escríbenos a contacto@fibrasinmobiliarias.com.
```

### Verificación de fakes en tests existentes

Antes de modificar `IEmailService`, correr:
```bash
grep -rn "SendPaymentNotificationAsync\|IEmailService" tests/ --include="*.cs"
```

Si hay fakes, actualizar la firma allí también. Los tests de integración `AccountEndpointTests.cs` usan `WebApplicationFactory` con configuración real — verificar que la nueva firma compile.

### No hacer

- No exponer la CLABE en endpoints del servidor — es solo UI
- No almacenar el comprobante en base de datos — se envía directamente por email
- No hacer obligatoria la carga del comprobante — el flujo "sin comprobante" debe seguir funcionando
- No agregar `Content-Type` manual al `fetch` cuando se usa `FormData`
- No usar `IFormFileCollection` — solo un archivo por envío

### Security Checklist

- [ ] El endpoint valida `ContentType` server-side (no confiar solo en el frontend)
- [ ] El endpoint valida tamaño server-side con `comprobante.Length <= 5_242_880`
- [ ] El archivo NO se almacena en disco ni en BD — solo se lee y se envía por email
- [ ] El archivo se procesa en memoria (`MemoryStream`) — aceptable para ≤5 MB
- [ ] La CLABE se muestra en UI pero no se expone como dato sensible en API
- [ ] El endpoint mantiene `RequireAuthorization()` — solo usuarios autenticados pueden notificar

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `notify-receipt-logic.ts` importaba `payment-plans` sin extensión → Node.js no lo resolvía en `--experimental-strip-types`. Fix: cambiar a `./payment-plans.ts` (permitido por `allowImportingTsExtensions: true`).
- Build C#: 4 fakes de `IEmailService` con firma antigua. Fix: actualizar `SubscriptionMaintenanceJobTests.FakeUserServiceEmail`, `UserServiceTests.CapturingEmailService`, `ApiWebFactory.CapturingEmailService` y call site en `ResendEmailServiceTests`.

### Completion Notes List

- `payment-plans.ts` creado: `PLANES` con precios reales, `PAYMENT_INFO` con CLABE/banco/contacto, `MAX_RECEIPT_BYTES` y `RECEIPT_ACCEPT`.
- `notify-receipt-logic.ts` creado: `validateReceiptFile()` pura testeable.
- `NotifyWithReceiptButton.tsx` creado: estado idle→uploading→sending→sent/error, validación inline, `FormData` con archivo, "Enviar sin comprobante" siempre activo. Input con `id`, `name`, `aria-label`.
- `ActivarPage.tsx` y `SuscripcionPage.tsx` actualizados: PLANES e instrucciones desde `payment-plans.ts`, botón reemplazado por `<NotifyWithReceiptButton />`. Email incorrecto `portafoliodefibras@gmail.com` en SuscripcionPage corregido.
- `authApi.ts`: `notifyPayment(comprobante?: File)` con `FormData` cuando hay archivo, sin body cuando no.
- `AccountEndpoints.cs`: `IFormFile? comprobante` + validación tipo/tamaño server-side + `.DisableAntiforgery()`.
- `IEmailService.cs` y `ResendEmailService.cs`: nueva firma con `fileContent/fileName`; dos code paths: template Resend (sin archivo) y raw email con attachment base64 (con archivo).
- Tests: `notify-receipt-logic.test.ts` con 6 casos. 211/211 tests frontend verdes. Build C# 0 errores.

### File List

- `_bmad-output/implementation-artifacts/14-12-precios-clabe-y-comprobante.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Web/Main/src/pages/payment-plans.ts` (NEW)
- `src/Web/Main/src/pages/notify-receipt-logic.ts` (NEW)
- `src/Web/Main/src/pages/NotifyWithReceiptButton.tsx` (NEW)
- `src/Web/Main/src/pages/notify-receipt-logic.test.ts` (NEW)
- `src/Web/Main/src/pages/ActivarPage.tsx`
- `src/Web/Main/src/pages/SuscripcionPage.tsx`
- `src/Web/Main/src/modules/auth/authApi.ts`
- `src/Web/Main/package.json`
- `src/Server/Application/Email/IEmailService.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs`
- `src/Server/Api/Endpoints/Private/AccountEndpoints.cs`
- `tests/Integration/Api.Tests/ApiWebFactory.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Email/ResendEmailServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Subscriptions/SubscriptionMaintenanceJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs`

## Change Log

- 2026-06-20: Historia creada — precios reales ($39/$390/$990), CLABE 722969010321418243, Mercado Pago, comprobante de pago adjunto por email.
- 2026-06-20: Implementación completa — 4 archivos nuevos, 13 archivos modificados. 211/211 tests frontend verdes. Build C# 0 errores.
- 2026-06-20: Code review — 7 patches aplicados [P1 Path.GetFileName sanitización filename, P2 validación tamaño post-lectura + guard vacío, P3 IsNullOrWhiteSpace fallback fileName, P4 catch simplificado, P5 role=status en confirmación, P6 guard MIME vacío en validateReceiptFile, P7 guard status=checking en handleFirstClick]. 13 defers, 7 dismissed. 212/212 tests verdes, build limpio.
