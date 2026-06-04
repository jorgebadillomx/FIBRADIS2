using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

public class EmailEncryptor : IEmailEncryptor
{
    private readonly byte[] _key;

    public EmailEncryptor(IConfiguration config)
    {
        // Si no hay clave configurada se usa un default interno.
        // Las emails no quedan en claro en BD aunque no se configure el secreto.
        var raw = config["Encryption:EmailKey"] ?? "FIBRADIS-EMAIL-KEY-DEFAULT-2026";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plainEmail)
    {
        var normalized = plainEmail.Trim().ToLowerInvariant();
        var iv = DeriveIV(normalized);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(normalized), iv);
        var combined = new byte[16 + cipher.Length];
        iv.CopyTo(combined, 0);
        cipher.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string storedEmail)
    {
        var raw = Convert.FromBase64String(storedEmail);
        var iv = raw[..16];
        var cipher = raw[16..];
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        return Encoding.UTF8.GetString(aes.DecryptCbc(cipher, iv));
    }

    private byte[] DeriveIV(string normalizedEmail)
        => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalizedEmail))[..16];
}
