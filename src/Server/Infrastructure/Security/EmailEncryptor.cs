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
        var raw = config["Encryption:EmailKey"]
            ?? throw new InvalidOperationException("Encryption:EmailKey no está configurado.");
        _key = Convert.FromBase64String(raw);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:EmailKey debe ser de 32 bytes (base64 de 44 chars).");
    }

    public string Encrypt(string plainEmail)
    {
        var normalized = plainEmail.Trim().ToLowerInvariant();
        var emailBytes = Encoding.UTF8.GetBytes(normalized);
        var iv = DeriveIV(normalized);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        var cipher = aes.EncryptCbc(emailBytes, iv);
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

        var plain = aes.DecryptCbc(cipher, iv);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] DeriveIV(string normalizedEmail)
        => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalizedEmail))[..16];
}
