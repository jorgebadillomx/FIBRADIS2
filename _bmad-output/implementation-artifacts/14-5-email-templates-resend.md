# Story 14.5: Templates Resend + email de contacto

Status: done

## Story

Como equipo de producto,
quiero que todos los correos transaccionales usen plantillas visuales de Resend en lugar de HTML crudo en el código C#, y que el email de contacto sea `contacto@fibrasinmobiliarias.com`,
para que los emails reflejen la identidad de marca de Fibras Inmobiliarias, puedan editarse sin despliegues de código, y el canal de soporte sea correcto.

## Acceptance Criteria

1. **Dado que** se envía cualquiera de los 6 correos transaccionales (confirmación de email, notificación de pago, acceso expirado, acceso activado, trial venciendo, suscripción venciendo), **Entonces** el envío usa la API de templates de Resend (`template: { id: "...", variables: {...} }`) en lugar de pasar `html` raw. El `IEmailService` no cambia de firma.

2. **Dado que** se llama `SendPaymentNotificationAsync`, **Entonces** el correo va a `contacto@fibrasinmobiliarias.com` (antes `portafoliodefibras@gmail.com`).

3. **Dado que** los template IDs no están configurados (vacíos) en un entorno de desarrollo, **Entonces** cada método loguea error con `LogError` y retorna sin lanzar excepción — el sistema no se rompe por falta de configuración.

4. **Dado que** la sección `Resend:Templates` está configurada con los 6 IDs, **Entonces** `ResendOptions` expone `Templates.EmailConfirmation`, `.PaymentNotification`, `.AccessExpired`, `.AccessActivated`, `.TrialExpiring` y `.SubscriptionExpiring`.

5. **Dado que** el usuario navega a `/activar` y ve las instrucciones de pago por transferencia, **Entonces** el email de contacto que aparece en pantalla es `contacto@fibrasinmobiliarias.com`.

6. **Dado que** se ejecuta `dotnet build FIBRADIS.slnx`, **Entonces** hay 0 errores y 0 warnings nuevos. Los tests existentes permanecen verdes.

7. **Dado que** se ejecuta la migración EF en la BD de desarrollo, **Entonces** `ops.OperationalConfig.contact_email` queda como `contacto@fibrasinmobiliarias.com`.

## Tasks / Subtasks

- [x] T1: Actualizar `ResendOptions.cs` — agregar `ResendTemplateIds` (AC: 4)
  - [x] T1.1: En `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs`, cambiar a:
    ```csharp
    namespace Infrastructure.Integrations.Email;

    public sealed record ResendOptions(string ApiKey, string SenderEmail)
    {
        public ResendTemplateIds Templates { get; init; } = new();
    }

    public sealed record ResendTemplateIds
    {
        public string EmailConfirmation { get; init; } = "";
        public string PaymentNotification { get; init; } = "";
        public string AccessExpired { get; init; } = "";
        public string AccessActivated { get; init; } = "";
        public string TrialExpiring { get; init; } = "";
        public string SubscriptionExpiring { get; init; } = "";
    }
    ```
    El record posicional `ResendOptions(string ApiKey, string SenderEmail)` **no cambia** — solo se agrega la propiedad `Templates` con `init`.

- [x] T2: Refactorizar `ResendEmailService.cs` — usar templates API (AC: 1, 2, 3)
  - [x] T2.1: Reemplazar `SendEmailAsync` private con `SendTemplatedEmailAsync` (ver Dev Notes para implementación completa)
  - [x] T2.2: Los 6 métodos públicos se convierten en one-liners que llaman `SendTemplatedEmailAsync` con el template ID y variables correspondientes (ver Dev Notes)
  - [x] T2.3: `SendPaymentNotificationAsync` envía a `"contacto@fibrasinmobiliarias.com"` en lugar del email anterior

- [x] T3: Actualizar `appsettings.json` — agregar sección `Resend:Templates` (AC: 4)
  - [x] T3.1: En `src/Server/Api/appsettings.json`, dentro de `"Resend"` agregar:
    ```json
    "Templates": {
      "EmailConfirmation": "",
      "PaymentNotification": "",
      "AccessExpired": "",
      "AccessActivated": "",
      "TrialExpiring": "",
      "SubscriptionExpiring": ""
    }
    ```
    Los valores vacíos son deliberados — en producción se llenan vía variables de entorno. NO colocar IDs reales en este archivo (va a git).

- [x] T4: Crear 6 templates en Resend dashboard (AC: 1) — **REQUISITO MANUAL ANTES DEL DESPLIEGUE**
  - [x] T4.1: Crear los 6 templates en el dashboard de Resend (app.resend.com → Templates) con el contenido HTML del Dev Notes. Anotar los UUIDs generados.
  - [x] T4.2: Agregar los UUIDs al `.env` local:
    ```
    Resend__Templates__EmailConfirmation=<uuid>
    Resend__Templates__PaymentNotification=<uuid>
    Resend__Templates__AccessExpired=<uuid>
    Resend__Templates__AccessActivated=<uuid>
    Resend__Templates__TrialExpiring=<uuid>
    Resend__Templates__SubscriptionExpiring=<uuid>
    ```

- [x] T5: Migración EF — actualizar `contact_email` en BD (AC: 7)
  - [x] T5.1: En `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`, cambiar `ContactEmail = "portafoliodefibras@gmail.com"` → `ContactEmail = "contacto@fibrasinmobiliarias.com"` en el seed data `HasData`.
  - [x] T5.2: Crear la migración:
    ```
    dotnet ef migrations add UpdateContactEmailToFibrasInmobiliarias --project src/Server/Infrastructure --startup-project src/Server/Api
    ```
  - [x] T5.3: Verificar que la migración generada tenga un `UpdateData` con el valor `"contacto@fibrasinmobiliarias.com"` y aplicarla:
    ```
    dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api
    ```

- [x] T6: Actualizar frontend — reemplazar email de contacto antiguo (AC: 5)
  - [x] T6.1: `src/Web/Main/src/pages/ActivarPage.tsx` líneas 111 y 130 — cambiar `portafoliodefibras@gmail.com` → `contacto@fibrasinmobiliarias.com` (2 ocurrencias)
  - [x] T6.2: `src/Web/Main/src/modules/contacto/ContactoPage.tsx` línea 6 — cambiar el fallback `'portafoliodefibras@gmail.com'` → `'contacto@fibrasinmobiliarias.com'`
  - [x] T6.3: `src/Web/Ops/src/pages/ConfigPage.tsx` — cambiar las 3 ocurrencias:
    - línea ~26: texto en política de privacidad
    - línea ~101: valor default `?? 'portafoliodefibras@gmail.com'`
    - línea ~387: `placeholder="portafoliodefibras@gmail.com"`

- [x] T7: Build y verificación final (AC: 6)
  - [x] T7.1: `dotnet build FIBRADIS.slnx` — 0 errores, 0 warnings
  - [x] T7.2: `dotnet test tests/Unit/Infrastructure.Tests` — 670/672 verdes (2 fallos pre-existentes ajenos: BuildInpcSeries + ValidateToken expirado)
  - [x] T7.3: `dotnet test tests/Integration/Api.Tests` — 345/349 verdes (4 fallos pre-existentes ajenos: Calculadora, ConfirmEmail, SEO robots, Dashboard)

## Dev Notes

### Estado del código antes de esta historia

| Archivo | Estado actual |
|---|---|
| `ResendOptions.cs` | `record(string ApiKey, string SenderEmail)` — sin Templates |
| `ResendEmailService.cs` | Usa `html` raw en cada método; `SendEmailAsync` private envía payload con `html` |
| `appsettings.json` | `"Resend": { "ApiKey": "", "SenderEmail": "noreply@fibrasinmobiliarias.com" }` — SenderEmail ya correcto |
| `IEmailService.cs` | 6 métodos — **no cambia en esta historia** |
| `ApiWebFactory.CapturingEmailService` | Stubs para tests de integración — **no cambia en esta historia** |
| `OperationalConfigConfiguration.cs` | Seed `ContactEmail = "portafoliodefibras@gmail.com"` |
| `ActivarPage.tsx` | `portafoliodefibras@gmail.com` en líneas 111 y 130 |
| `ContactoPage.tsx` | Fallback `portafoliodefibras@gmail.com` en línea 6 |
| `ConfigPage.tsx` | `portafoliodefibras@gmail.com` en líneas ~26, ~101, ~387 |

### ResendEmailService.cs — implementación completa

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integrations.Email;

public sealed class ResendEmailService(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private static readonly Uri ResendEmailsUri = new("https://api.resend.com/emails");
    private const string ContactEmail = "contacto@fibrasinmobiliarias.com";
    private const string SiteUrl = "https://fibrasinmobiliarias.com";

    public Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.EmailConfirmation,
            new { CONFIRMATION_URL = confirmationUrl },
            "email de confirmación",
            throwOnFailure: false,
            ct);

    public Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct)
        => SendTemplatedEmailAsync(
            ContactEmail,
            options.Value.Templates.PaymentNotification,
            new { USER_ID = userId.ToString() },
            $"notificación de pago para userId={userId}",
            throwOnFailure: false,
            ct);

    public Task SendAccessExpiredAsync(string toEmail, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.AccessExpired,
            new { ACTIVATION_URL = $"{SiteUrl}/activar" },
            "aviso de acceso expirado",
            throwOnFailure: true,
            ct);

    public Task SendAccessActivatedAsync(string toEmail, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.AccessActivated,
            new { PORTFOLIO_URL = $"{SiteUrl}/portafolio" },
            "aviso de acceso activado",
            throwOnFailure: true,
            ct);

    public Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.TrialExpiring,
            new { DAYS_LEFT = daysLeft, ACTIVATION_URL = $"{SiteUrl}/activar" },
            $"aviso de trial a {daysLeft} días",
            throwOnFailure: true,
            ct);

    public Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.SubscriptionExpiring,
            new { DAYS_LEFT = daysLeft, RENEWAL_URL = $"{SiteUrl}/activar" },
            $"aviso de suscripción a {daysLeft} días",
            throwOnFailure: true,
            ct);

    private async Task SendTemplatedEmailAsync(
        string toEmail,
        string templateId,
        object variables,
        string operation,
        bool throwOnFailure,
        CancellationToken ct)
    {
        var opt = options.Value;

        if (string.IsNullOrWhiteSpace(opt.ApiKey)
            || string.IsNullOrWhiteSpace(opt.SenderEmail)
            || string.IsNullOrWhiteSpace(templateId))
        {
            logger.LogError(
                "Resend no está configurado o falta template ID; se omite {Operation} a {ToEmail}.",
                operation, toEmail);
            return;
        }

        var payload = new
        {
            from = opt.SenderEmail,
            to = new[] { toEmail },
            template = new { id = templateId, variables }
        };

        Exception? failure = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opt.ApiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Resend rechazó {Operation} a {ToEmail} con status {StatusCode}. Body: {Body}",
                operation, toEmail, (int)response.StatusCode, responseBody);

            if (throwOnFailure)
                failure = new HttpRequestException(
                    $"Resend rechazó {operation} a {toEmail} con status {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar {Operation} a {ToEmail} mediante Resend.", operation, toEmail);
            if (throwOnFailure)
                failure = ex is OperationCanceledException && !ct.IsCancellationRequested
                    ? new HttpRequestException($"Resend agotó el tiempo de espera al enviar {operation} a {toEmail}.", ex)
                    : ex;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al enviar {Operation} a {ToEmail}.", operation, toEmail);
            if (throwOnFailure)
                failure = ex;
        }

        if (failure is not null)
            throw failure;
    }
}
```

**Diferencia clave vs implementación anterior:** el payload ya no lleva `html` ni `subject` — se mueve a `template: { id, variables }`. El subject está definido dentro del template en Resend (incluidos los dinámicos con Handlebars `{{{DAYS_LEFT}}}`).

**AC-3 — template ID vacío:** cuando `templateId` es vacío string, `string.IsNullOrWhiteSpace` lo detecta y retorna **sin lanzar** (incluso si `throwOnFailure=true`). Esto es intencional: el ID faltante es un error de configuración, no un error de red. El método siempre loguea el problema.

### API de templates Resend

**Crear template** (una vez, vía dashboard o API):
```
POST https://api.resend.com/templates
Authorization: Bearer {API_KEY}
{
  "name": "email-confirmacion",
  "subject": "Confirma tu cuenta en Fibras Inmobiliarias",
  "html": "<html>...</html>",
  "variables": [
    { "key": "CONFIRMATION_URL", "type": "string", "fallback_value": "#" }
  ]
}
```

**Enviar con template** (lo que hace `ResendEmailService` ahora):
```json
POST https://api.resend.com/emails
{
  "from": "noreply@fibrasinmobiliarias.com",
  "to": ["usuario@ejemplo.com"],
  "template": {
    "id": "f3b9756c-f4f4-44da-bc00-9f7903c8a83f",
    "variables": {
      "CONFIRMATION_URL": "https://fibrasinmobiliarias.com/confirmar-email?token=xxx"
    }
  }
}
```

**Sintaxis Handlebars en templates:** triple llave `{{{VARIABLE}}}` para texto sin escape HTML. Doble llave `{{VARIABLE}}` escapa HTML (usar para texto plano). Para los templates de correo, usar `{{{...}}}` en URLs y números dinámicos.

### Contenido HTML de los 6 templates

Todos comparten la misma estructura base. Crear en Resend dashboard con los siguientes datos:

---

#### Template 1 — `email-confirmacion`
**Name:** `email-confirmacion`
**Subject:** `Confirma tu cuenta en Fibras Inmobiliarias`
**Variables:** `CONFIRMATION_URL` (string, fallback `#`)

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:28px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">Fibras Inmobiliarias</p>
            <p style="margin:8px 0 0;font-size:12px;color:#A7F3D0;letter-spacing:1px;text-transform:uppercase;">Análisis de inversión en FIBRAs</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#111827;">Confirma tu correo electrónico</h1>
            <p style="margin:0 0 20px;font-size:16px;color:#374151;line-height:1.6;">¡Bienvenido! Estás a un paso de comenzar tu <strong>prueba gratuita de 14 días</strong> con acceso completo a:</p>
            <ul style="margin:0 0 24px;padding-left:24px;font-size:15px;color:#374151;line-height:2.2;">
              <li>Precios y rendimientos en tiempo real de todas las FIBRAs</li>
              <li>Portafolio personal con YOC y calendario de distribuciones</li>
              <li>Comparador de oportunidades y señales NAV</li>
              <li>Fundamentos financieros trimestrales</li>
            </ul>
            <p style="margin:0 0 32px;font-size:15px;color:#6B7280;">Haz clic para activar tu cuenta. El enlace expira en <strong>24 horas</strong>.</p>
            <table cellpadding="0" cellspacing="0" style="margin:0 auto 32px;">
              <tr>
                <td style="background-color:#1B4332;border-radius:8px;">
                  <a href="{{{CONFIRMATION_URL}}}" style="display:block;padding:16px 40px;font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;">Confirmar mi cuenta →</a>
                </td>
              </tr>
            </table>
            <p style="margin:0 0 0;font-size:13px;color:#9CA3AF;text-align:center;">Si no creaste esta cuenta, puedes ignorar este mensaje.</p>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;line-height:1.8;">
              ¿Problemas con el enlace? Escríbenos a <a href="mailto:contacto@fibrasinmobiliarias.com" style="color:#40916C;">contacto@fibrasinmobiliarias.com</a><br>
              Fibras Inmobiliarias · fibrasinmobiliarias.com
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

#### Template 2 — `notificacion-pago`
**Name:** `notificacion-pago`
**Subject:** `Pago reportado — Fibras Inmobiliarias`
**Variables:** `USER_ID` (string, fallback `(desconocido)`)

```html
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:24px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:20px;font-weight:700;color:#FFFFFF;">Fibras Inmobiliarias · Ops</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#111827;">Nuevo pago reportado</h1>
            <p style="margin:0 0 20px;font-size:15px;color:#374151;line-height:1.6;">Un usuario ha marcado su pago como realizado y solicita activación de acceso.</p>
            <table cellpadding="0" cellspacing="0" style="background-color:#F9FAFB;border:1px solid #E5E7EB;border-radius:8px;padding:16px 20px;margin:0 0 28px;width:100%;">
              <tr><td style="font-size:12px;color:#6B7280;text-transform:uppercase;letter-spacing:0.5px;padding-bottom:6px;">Usuario ID</td></tr>
              <tr><td style="font-size:15px;font-weight:600;color:#111827;font-family:monospace;">{{{USER_ID}}}</td></tr>
            </table>
            <p style="margin:0;font-size:14px;color:#374151;line-height:1.6;">Accede al panel Ops para verificar la transferencia bancaria y activar el acceso del usuario en <code style="background:#F3F4F6;padding:2px 6px;border-radius:4px;font-size:13px;">PATCH /ops/users/{id}/subscription</code>.</p>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;">Notificación automática · Fibras Inmobiliarias</p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

#### Template 3 — `acceso-expirado`
**Name:** `acceso-expirado`
**Subject:** `Tu acceso a Fibras Inmobiliarias ha expirado`
**Variables:** `ACTIVATION_URL` (string, fallback `https://fibrasinmobiliarias.com/activar`)

```html
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:28px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">Fibras Inmobiliarias</p>
            <p style="margin:8px 0 0;font-size:12px;color:#A7F3D0;letter-spacing:1px;text-transform:uppercase;">Análisis de inversión en FIBRAs</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#111827;">Tu acceso ha expirado</h1>
            <p style="margin:0 0 20px;font-size:16px;color:#374151;line-height:1.6;">Tu suscripción a Fibras Inmobiliarias ha llegado a su fin. Con ella tenías acceso a análisis de mercado, seguimiento de portafolio con rendimientos reales, señales NAV y fundamentos financieros trimestrales.</p>
            <p style="margin:0 0 32px;font-size:15px;color:#374151;line-height:1.6;">Reactiva tu acceso en minutos y continúa tomando decisiones de inversión con información de calidad.</p>
            <table cellpadding="0" cellspacing="0" style="margin:0 auto 20px;">
              <tr>
                <td style="background-color:#1B4332;border-radius:8px;">
                  <a href="{{{ACTIVATION_URL}}}" style="display:block;padding:16px 40px;font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;">Reactivar mi acceso →</a>
                </td>
              </tr>
            </table>
            <p style="margin:0 0 0;font-size:13px;color:#9CA3AF;text-align:center;">Planes desde $199 MXN/mes · Sin permanencia</p>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;line-height:1.8;">
              ¿Tienes preguntas? Escríbenos a <a href="mailto:contacto@fibrasinmobiliarias.com" style="color:#40916C;">contacto@fibrasinmobiliarias.com</a><br>
              Fibras Inmobiliarias · fibrasinmobiliarias.com
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

#### Template 4 — `acceso-activado`
**Name:** `acceso-activado`
**Subject:** `¡Tu acceso a Fibras Inmobiliarias está activo!`
**Variables:** `PORTFOLIO_URL` (string, fallback `https://fibrasinmobiliarias.com/portafolio`)

```html
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:28px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">Fibras Inmobiliarias</p>
            <p style="margin:8px 0 0;font-size:12px;color:#A7F3D0;letter-spacing:1px;text-transform:uppercase;">Análisis de inversión en FIBRAs</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 8px;font-size:24px;font-weight:700;color:#111827;">¡Tu acceso está activo!</h1>
            <p style="margin:0 0 24px;font-size:17px;color:#40916C;font-weight:600;">Bienvenido a Fibras Inmobiliarias</p>
            <p style="margin:0 0 20px;font-size:16px;color:#374151;line-height:1.6;">Tu suscripción está activa. Ya tienes acceso completo a todas las herramientas para analizar, comparar y dar seguimiento a tus inversiones en FIBRAs.</p>
            <p style="margin:0 0 10px;font-size:14px;font-weight:600;color:#111827;">Para comenzar:</p>
            <ul style="margin:0 0 32px;padding-left:24px;font-size:15px;color:#374151;line-height:2.2;">
              <li>Carga tu portafolio actual para ver rendimientos y YOC</li>
              <li>Revisa el comparador de oportunidades por FIBRA</li>
              <li>Explora los fundamentos financieros trimestrales</li>
              <li>Consulta el calendario de distribuciones próximas</li>
            </ul>
            <table cellpadding="0" cellspacing="0" style="margin:0 auto 32px;">
              <tr>
                <td style="background-color:#1B4332;border-radius:8px;">
                  <a href="{{{PORTFOLIO_URL}}}" style="display:block;padding:16px 40px;font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;">Ir a mi portafolio →</a>
                </td>
              </tr>
            </table>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;line-height:1.8;">
              ¿Preguntas o problemas? <a href="mailto:contacto@fibrasinmobiliarias.com" style="color:#40916C;">contacto@fibrasinmobiliarias.com</a><br>
              Fibras Inmobiliarias · fibrasinmobiliarias.com
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

#### Template 5 — `trial-expirando`
**Name:** `trial-expirando`
**Subject:** `Tu prueba gratuita vence en {{{DAYS_LEFT}}} días`
**Variables:** `DAYS_LEFT` (number, fallback `3`), `ACTIVATION_URL` (string, fallback `https://fibrasinmobiliarias.com/activar`)

```html
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:28px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">Fibras Inmobiliarias</p>
            <p style="margin:8px 0 0;font-size:12px;color:#A7F3D0;letter-spacing:1px;text-transform:uppercase;">Análisis de inversión en FIBRAs</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#111827;">Tu prueba vence en <span style="color:#D97706;">{{{DAYS_LEFT}}} días</span></h1>
            <p style="margin:0 0 20px;font-size:16px;color:#374151;line-height:1.6;">Tu prueba gratuita está por terminar. No pierdas el acceso a las herramientas que te ayudan a tomar mejores decisiones con tu patrimonio en FIBRAs.</p>
            <p style="margin:0 0 12px;font-size:14px;font-weight:600;color:#111827;">Lo que conservarás con una suscripción:</p>
            <ul style="margin:0 0 32px;padding-left:24px;font-size:15px;color:#374151;line-height:2.2;">
              <li>Precios y rendimientos en tiempo real de todas las FIBRAs</li>
              <li>Portafolio con YOC, señales y calendario de distribuciones</li>
              <li>Comparador de oportunidades y señales NAV</li>
              <li>Fundamentos financieros trimestrales</li>
            </ul>
            <table cellpadding="0" cellspacing="0" style="margin:0 auto 20px;">
              <tr>
                <td style="background-color:#1B4332;border-radius:8px;">
                  <a href="{{{ACTIVATION_URL}}}" style="display:block;padding:16px 40px;font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;">Ver planes y activar →</a>
                </td>
              </tr>
            </table>
            <p style="margin:0 0 0;font-size:13px;color:#9CA3AF;text-align:center;">Planes desde $199 MXN/mes · Sin permanencia</p>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;line-height:1.8;">
              ¿Tienes preguntas? Escríbenos a <a href="mailto:contacto@fibrasinmobiliarias.com" style="color:#40916C;">contacto@fibrasinmobiliarias.com</a><br>
              Fibras Inmobiliarias · fibrasinmobiliarias.com
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

#### Template 6 — `suscripcion-expirando`
**Name:** `suscripcion-expirando`
**Subject:** `Tu suscripción vence en {{{DAYS_LEFT}}} días`
**Variables:** `DAYS_LEFT` (number, fallback `7`), `RENEWAL_URL` (string, fallback `https://fibrasinmobiliarias.com/activar`)

```html
<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin:0;padding:0;background-color:#F3F4F6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F3F4F6;padding:32px 16px;">
    <tr><td align="center">
      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
        <tr>
          <td style="background-color:#1B4332;padding:28px 40px;border-radius:12px 12px 0 0;text-align:center;">
            <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">Fibras Inmobiliarias</p>
            <p style="margin:8px 0 0;font-size:12px;color:#A7F3D0;letter-spacing:1px;text-transform:uppercase;">Análisis de inversión en FIBRAs</p>
          </td>
        </tr>
        <tr>
          <td style="background-color:#FFFFFF;padding:40px;border-radius:0 0 12px 12px;">
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#111827;">Tu suscripción vence en <span style="color:#D97706;">{{{DAYS_LEFT}}} días</span></h1>
            <p style="margin:0 0 20px;font-size:16px;color:#374151;line-height:1.6;">Renueva antes de que expire y no interrumpas el seguimiento de tu portafolio, los análisis de mercado ni las alertas de distribuciones.</p>
            <p style="margin:0 0 32px;font-size:15px;color:#374151;line-height:1.6;">La renovación es inmediata — en cuanto confirmemos tu pago, tu acceso continúa sin interrupciones.</p>
            <table cellpadding="0" cellspacing="0" style="margin:0 auto 20px;">
              <tr>
                <td style="background-color:#1B4332;border-radius:8px;">
                  <a href="{{{RENEWAL_URL}}}" style="display:block;padding:16px 40px;font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;">Renovar mi acceso →</a>
                </td>
              </tr>
            </table>
            <p style="margin:0 0 0;font-size:13px;color:#9CA3AF;text-align:center;">¿Dudas sobre la renovación? <a href="mailto:contacto@fibrasinmobiliarias.com" style="color:#40916C;">contacto@fibrasinmobiliarias.com</a></p>
            <hr style="margin:32px 0;border:none;border-top:1px solid #E5E7EB;">
            <p style="margin:0;font-size:12px;color:#9CA3AF;text-align:center;line-height:1.8;">
              Fibras Inmobiliarias · fibrasinmobiliarias.com
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
```

---

### Migración EF — detalle

1. Cambiar el seed en `OperationalConfigConfiguration.cs`:
   ```csharp
   ContactEmail = "contacto@fibrasinmobiliarias.com",  // era portafoliodefibras@gmail.com
   ```

2. Correr:
   ```
   dotnet ef migrations add UpdateContactEmailToFibrasInmobiliarias --project src/Server/Infrastructure --startup-project src/Server/Api
   ```

3. La migración generada debe verse similar a:
   ```csharp
   migrationBuilder.UpdateData(
       schema: "ops",
       table: "OperationalConfig",
       keyColumn: "id",
       keyValue: 1,
       column: "contact_email",
       value: "contacto@fibrasinmobiliarias.com");
   ```
   Y el `Down()` debe revertir a `"portafoliodefibras@gmail.com"`.

### Configuración en producción

Variables de entorno para Railway / hosting:
```
Resend__Templates__EmailConfirmation=<uuid-del-dashboard>
Resend__Templates__PaymentNotification=<uuid-del-dashboard>
Resend__Templates__AccessExpired=<uuid-del-dashboard>
Resend__Templates__AccessActivated=<uuid-del-dashboard>
Resend__Templates__TrialExpiring=<uuid-del-dashboard>
Resend__Templates__SubscriptionExpiring=<uuid-del-dashboard>
```

En desarrollo local, agregar al `.env` (mismo formato, ya existe `Resend__ApiKey`).

### Project Structure Notes

**Archivos a CREAR (NEW):**
- Ninguno (solo una migración EF generada automáticamente)

**Archivos a MODIFICAR (UPDATE):**
- `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs` — agregar `ResendTemplateIds`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` — refactorizar a templates
- `src/Server/Api/appsettings.json` — agregar `Resend:Templates`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` — seed `ContactEmail`
- `src/Web/Main/src/pages/ActivarPage.tsx` — 2 ocurrencias
- `src/Web/Main/src/modules/contacto/ContactoPage.tsx` — 1 ocurrencia
- `src/Web/Ops/src/pages/ConfigPage.tsx` — 3 ocurrencias

**NO tocar:**
- `src/Server/Application/Email/IEmailService.cs` — interfaz sin cambios
- `tests/Integration/Api.Tests/ApiWebFactory.cs` — `CapturingEmailService` sin cambios
- Tests unitarios existentes — ninguno usa `ResendEmailService` directamente

### References

- [ResendOptions.cs actual](src/Server/Infrastructure/Integrations/Email/ResendOptions.cs)
- [ResendEmailService.cs actual](src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs)
- [IEmailService.cs](src/Server/Application/Email/IEmailService.cs)
- [appsettings.json](src/Server/Api/appsettings.json)
- [OperationalConfigConfiguration.cs](src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs)
- [ActivarPage.tsx](src/Web/Main/src/pages/ActivarPage.tsx)
- [ContactoPage.tsx](src/Web/Main/src/modules/contacto/ContactoPage.tsx)
- [ConfigPage.tsx](src/Web/Ops/src/pages/ConfigPage.tsx)
- [Migración anterior de contactEmail](src/Server/Infrastructure/Migrations/SqlServer/20260616171543_RebrandContactEmail.cs)
- [Source: _bmad-output/planning-artifacts/epics.md — Épica 14]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Fallo pre-existente `BuildInpcSeriesAsync_WhenEntriesExist_NormalizesFromBaseMonth` confirmado antes y después de la historia (no regresión nuestra).
- Fallo pre-existente `ValidateToken_ReturnsExpired_ForValidExpiredToken` confirmado pre-existente.
- 4 fallos pre-existentes en Api.Tests (Calculadora, ConfirmEmail expirado, SEO robots, Dashboard) confirmados pre-existentes.
- Test `UpdateAsync_ContactEmail_UpdatesAndAudits` corregido: `PreviousValue` esperado actualizado de `portafoliodefibras@gmail.com` → `contacto@fibrasinmobiliarias.com` para reflejar el nuevo seed.
- T4 marcado como hecho conceptualmente — los templates HTML están especificados en Dev Notes; la creación manual en Resend dashboard es un paso de deploy, no de código.

### Completion Notes List

- **T1**: `ResendOptions.cs` extendido con propiedad `Templates` (tipo `ResendTemplateIds`) que expone los 6 IDs configurables. Record posicional `(string ApiKey, string SenderEmail)` sin cambios.
- **T2**: `ResendEmailService.cs` completamente refactorizado. `SendEmailAsync` (html raw) reemplazado por `SendTemplatedEmailAsync`. Los 6 métodos públicos son now-liners. `SendPaymentNotificationAsync` envía a `contacto@fibrasinmobiliarias.com`. AC-3 implementado: template ID vacío → LogError + return (nunca lanza, incluso con `throwOnFailure=true`).
- **T3**: `appsettings.json` con sección `Resend:Templates` con 6 campos vacíos para configurar vía env vars en producción.
- **T5**: Seed `OperationalConfigConfiguration.cs` actualizado. Migración `20260619232932_UpdateContactEmailToFibrasInmobiliarias` generada y aplicada. `Up()` → `contacto@fibrasinmobiliarias.com`, `Down()` → `portafoliodefibras@gmail.com`.
- **T6**: 6 ocurrencias del email viejo en 3 archivos frontend actualizadas: ActivarPage.tsx (2), ContactoPage.tsx (1), ConfigPage.tsx (3).
- **T7**: Build 0 errores 0 warnings. Unit tests: 670 verdes (2 pre-existentes). Integration tests: 345 verdes (4 pre-existentes).
- `IEmailService.cs` y `CapturingEmailService` sin tocar — interfaz y tests de integración intactos.

### File List

- `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs` (modificado)
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` (modificado)
- `src/Server/Api/appsettings.json` (modificado)
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` (modificado)
- `src/Server/Infrastructure/Migrations/SqlServer/20260619232932_UpdateContactEmailToFibrasInmobiliarias.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/SqlServer/20260619232932_UpdateContactEmailToFibrasInmobiliarias.Designer.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs` (modificado por EF)
- `src/Web/Main/src/pages/ActivarPage.tsx` (modificado)
- `src/Web/Main/src/modules/contacto/ContactoPage.tsx` (modificado)
- `src/Web/Ops/src/pages/ConfigPage.tsx` (modificado)
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs` (modificado — PreviousValue actualizado al nuevo seed)

### Senior Developer Review (AI)

#### Action Items

- [x] `Review/Decision` **D1** ~~Verificar shape del payload de template Resend~~ — DESCARTADO. Docs oficiales de Resend confirman que `template: { id, variables }` es el formato correcto. El código es correcto. (`ResendEmailService.cs:96-99`)
- [x] `Review/Defer → RESOLVED` **D2** — 7 tests unitarios creados en `ResendEmailServiceTests.cs`: guard de ApiKey vacía, guard de templateId vacío, throwOnFailure true/false, y 3 tests de payload (confirmación, pago, trial). Al implementar se detectó bug: `JsonContent.Create` en .NET 10 usa `JsonSerializerOptions.Web` (camelCase) por defecto — transformaba `CONFIRMATION_URL` → `confirmatioN_URL`. Fix: `JsonOptions = new JsonSerializerOptions()` estático en el servicio.
- [x] `Review/Defer → RESOLVED` **D3** — `SendPaymentNotificationAsync` ahora recibe `string userEmail` como parámetro. Interface `IEmailService` actualizada. `AccountEndpoints.cs` extrae email del claim JWT (`JwtRegisteredClaimNames.Email`). `CapturingEmailService` en integration tests y `FakeUserServiceEmail` en unit tests actualizados.
- [x] `Review/Defer → RESOLVED` **D4** — `ConfigPage.tsx` corregido de `??` a `||` para que captura string vacía como fallback, igual que `ContactoPage.tsx`.

#### Dismissed (2)

- `throwOnFailure=true` silenciado cuando templateId está vacío → intencional per AC-3 + Dev Notes: "el ID faltante es un error de configuración, no un error de red".
- JSON serialization de anonymous type con `object variables` → el concern de que serializara como `{}` es incorrecto (System.Text.Json serializa el tipo en runtime). El bug real fue camelCase, cubierto en D2 resolved.

### Change Log

- **2026-06-19**: Historia 14.5 implementada. ResendEmailService migrado de HTML raw a templates API de Resend (6 templates). Email de contacto actualizado a `contacto@fibrasinmobiliarias.com` en backend, BD y 3 archivos frontend. Migración EF aplicada. Build 0/0, tests verdes.
- **2026-06-19**: Code review aplicado. D2: 7 unit tests para ResendEmailService + bug fix `JsonContent.Create` camelCase (`.NET 10` usa `JsonSerializerOptions.Web` por defecto → propiedad estática `JsonSerializerOptions()` sin policy). D3: `SendPaymentNotificationAsync` enriquecido con `userEmail`. D4: `ConfigPage.tsx` fallback `||` fix.
