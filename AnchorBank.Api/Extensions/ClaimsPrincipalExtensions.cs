using System.Security.Claims;

namespace AnchorBank.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Reads the "sub" claim exactly as minted by JwtAccessTokenGenerator.
    public static Guid? GetCustomerId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var customerId) ? customerId : null;
    }
}
