namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Defines the contract for communicating with an external pricing API.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture:</strong></para>
/// <para>Interface defined in Application layer (port), implementations in Infrastructure layer (adapter).
/// This allows the application to remain decoupled from any specific external pricing provider.</para>
/// </remarks>
public interface IExternalPricingApi
{
}
