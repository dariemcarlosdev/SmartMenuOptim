using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Auth.DTOs;

namespace SmartMenuOptim.Server.Features.Auth.Services;

/// <summary>
/// Defines the contract for the Blazor Server login page to exchange credentials via the backend API's
/// <c>SignInWithPasswordCommand</c> endpoint, communicating over HTTP like every other client service.
/// </summary>
public interface IAuthClientService
{
    Task<Result<SignInResultDTO>> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
}
