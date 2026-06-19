using Domain.Auth;

namespace Domain.Tests.Auth;

public class DisposableEmailDomainsTests
{
    [Theory]
    [InlineData("user@mailinator.com")]
    [InlineData("user@MAILINATOR.COM")]
    [InlineData("user@GuErRiLlAmAiL.com")]
    public void IsDisposable_ReturnsTrue_ForKnownDisposableDomains(string email)
    {
        Assert.True(DisposableEmailDomains.IsDisposable(email));
    }

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("persona@empresa.mx")]
    [InlineData("user@outlook.com")]
    public void IsDisposable_ReturnsFalse_ForNonDisposableDomains(string email)
    {
        Assert.False(DisposableEmailDomains.IsDisposable(email));
    }

    [Theory]
    [InlineData("user@sub.mailinator.com")]
    [InlineData("x@deep.sub.yopmail.com")]
    public void IsDisposable_ReturnsTrue_ForSubdomainsOfDisposableDomains(string email)
    {
        Assert.True(DisposableEmailDomains.IsDisposable(email));
    }
}
