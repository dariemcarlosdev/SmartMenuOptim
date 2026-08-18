using System.Security.Claims;
using MediatR;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Contracts.Identity;
using SmartMenuOptim.Application.Features.Auth.DTOs;

namespace SmartMenuOptim.Application.Features.Auth.Commands;

public sealed class SignInWithPasswordCommandHandler(
    IIdentityProviderAuthenticator authenticator,
    IUserProvisioningService userProvisioningService)
    : IRequestHandler<SignInWithPasswordCommand, Result<SignInResultDTO>>
{
    public async Task<Result<SignInResultDTO>> Handle(SignInWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var signIn = await authenticator.SignInWithPasswordAsync(request.Email, request.Password, cancellationToken);
        if (!signIn.Succeeded)
            return Result.Failure<SignInResultDTO>(signIn.Error ?? "Sign-in failed.");

        // Minimal principal carrying just the two claims JIT provisioning needs (sub, email).
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, signIn.Subject!),
            new Claim(ClaimTypes.Email, signIn.Email!),
        ], authenticationType: "IdentityProvider"));

        var user = await userProvisioningService.GetOrProvisionAsync(principal, cancellationToken);

        return Result.Success(new SignInResultDTO(user.Id, user.Email!, user.ProfileType, user.RestaurantTenantId));
    }
}
