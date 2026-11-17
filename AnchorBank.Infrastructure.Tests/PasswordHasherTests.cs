using AnchorBank.Infrastructure.Security;

namespace AnchorBank.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesVersionedHashThatVerifies()
    {
        const string password = "correct horse battery staple";

        var hash = _hasher.Hash(password);

        Assert.StartsWith("PBKDF2-SHA256$600000$", hash);
        Assert.DoesNotContain(password, hash);
        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void SamePassword_ProducesDifferentHashes()
    {
        var first = _hasher.Hash("same-password");
        var second = _hasher.Hash("same-password");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("same-password", first));
        Assert.True(_hasher.Verify("same-password", second));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var hash = _hasher.Hash("right-password");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2-SHA256$600000$not-base64$also-not")]
    [InlineData("PBKDF2-SHA256$0$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$999999999$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA512$600000$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$600000$AAAAAAAAAAA=$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAA")]
    [InlineData("PBKDF2-SHA256$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")]
    public void Verify_RejectsMalformedStoredHash(string? storedHash)
    {
        Assert.False(_hasher.Verify("any-password", storedHash));
    }
}
