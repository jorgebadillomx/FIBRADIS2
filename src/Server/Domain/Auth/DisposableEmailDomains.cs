namespace Domain.Auth;

public static class DisposableEmailDomains
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com",
        "tempmail.org",
        "guerrillamail.com",
        "10minutemail.com",
        "throwam.com",
        "yopmail.com",
        "sharklasers.com",
        "trashmail.com",
        "maildrop.cc",
        "spamgourmet.com",
        "fakeinbox.com",
        "dispostable.com",
    };

    public static bool IsDisposable(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
            return false;

        var domain = email[(atIndex + 1)..].Trim();
        if (domain.Length == 0)
            return false;

        if (Domains.Contains(domain))
            return true;

        // Comprobar dominios padre para bloquear subdominios (e.g. sub.mailinator.com)
        var dot = domain.IndexOf('.');
        while (dot > 0 && dot < domain.Length - 1)
        {
            if (Domains.Contains(domain[(dot + 1)..]))
                return true;
            dot = domain.IndexOf('.', dot + 1);
        }

        return false;
    }
}
