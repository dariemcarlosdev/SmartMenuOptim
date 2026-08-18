using MediatR;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Auth.DTOs;

namespace SmartMenuOptim.Application.Features.Auth.Commands;

/// <summary>
/// Exchanges end-user credentials for a local sign-in via the identity provider's password grant,
/// JIT-provisioning the local <c>ApplicationUser</c> shadow record on first sign-in.
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §4 and ADR-006.
/// </summary>
public sealed record SignInWithPasswordCommand(string Email, string Password) : IRequest<Result<SignInResultDTO>>;
