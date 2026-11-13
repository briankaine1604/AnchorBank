using AnchorBank.Application.Auth.DTOs;
using AnchorBank.Application.Auth.Models;

namespace AnchorBank.Application.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
}
