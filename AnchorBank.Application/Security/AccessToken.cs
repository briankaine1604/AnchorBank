namespace AnchorBank.Application.Security;

public record AccessToken(
    string Value,
    DateTime ExpiresAt
);
