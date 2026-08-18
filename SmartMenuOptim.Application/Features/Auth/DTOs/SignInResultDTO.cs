using SmartMenuOptim.Domain.Entities.GlobalEntities;

namespace SmartMenuOptim.Application.Features.Auth.DTOs;

/// <summary>
/// Claims needed by the Blazor Server login page to build its own sign-in <c>ClaimsPrincipal</c>
/// and cookie. Never exposes the domain <c>ApplicationUser</c> entity outside Application.
/// </summary>
public sealed record SignInResultDTO(string UserId, string Email, ProfileType ProfileType, int? RestaurantTenantId);
