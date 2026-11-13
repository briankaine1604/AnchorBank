using AnchorBank.Application.Auth.DTOs;

namespace AnchorBank.Application.Auth.Models;

public enum LoginOutcome
{
    Success,
    InvalidCredentials
}

public record LoginResult(
    LoginOutcome Outcome,
    LoginResponse? Response = null
);
