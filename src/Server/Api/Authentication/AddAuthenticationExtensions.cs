using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api.Authentication;

public static class AddAuthenticationExtensions
{
    private const string SecretPlaceholder = "CHANGE-ME-IN-PRODUCTION-VIA-ENV-VAR-OR-SECRET-MANAGER!!";

    public static WebApplicationBuilder AddFibradisAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, config) =>
            {
                var secret = config["Jwt:Secret"]
                    ?? throw new InvalidOperationException(
                        "JWT Secret no configurado. Establecer Jwt:Secret en appsettings o variable de entorno Jwt__Secret.");
                var issuer = config["Jwt:Issuer"] ?? "fibradis";
                var audience = config["Jwt:Audience"] ?? "fibradis-client";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            })
            // Fail at startup if Jwt:Secret is the placeholder. The build-time OpenAPI
            // generation tool (dotnet-getdocument.dll) also starts the host, but Api.csproj
            // sets ASPNETCORE_ENVIRONMENT=Development before that Exec so the subprocess
            // loads appsettings.Development.json (real dev secret) and validation passes.
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<JwtBearerOptions>>(sp =>
            new ValidateJwtSecret(sp.GetRequiredService<IConfiguration>()));

        return builder;
    }

    private sealed class ValidateJwtSecret(IConfiguration config)
        : IValidateOptions<JwtBearerOptions>
    {
        public ValidateOptionsResult Validate(string? name, JwtBearerOptions options)
        {
            if (name != null && name != JwtBearerDefaults.AuthenticationScheme)
                return ValidateOptionsResult.Success;

            var secret = config["Jwt:Secret"];
            if (string.IsNullOrEmpty(secret) || secret == SecretPlaceholder)
                return ValidateOptionsResult.Fail(
                    "Jwt:Secret está usando el placeholder por defecto. " +
                    "Configure una clave segura mediante variables de entorno (Jwt__Secret) o Secret Manager.");

            return ValidateOptionsResult.Success;
        }
    }
}
