using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api.Authentication;

public static class AddAuthenticationExtensions
{
    public static WebApplicationBuilder AddFibradisAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        // Configure lazily so tests can override Jwt:Secret via in-memory config
        // before options are first accessed at request time.
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
            });

        return builder;
    }
}
