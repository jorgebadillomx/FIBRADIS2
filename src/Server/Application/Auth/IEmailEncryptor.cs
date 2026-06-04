namespace Application.Auth;

public interface IEmailEncryptor
{
    string Encrypt(string plainEmail);
    string Decrypt(string storedEmail);
}
