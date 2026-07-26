namespace SmartMenuOptim.Application.Dtos.Common;

/// <summary>
/// Abstract base DTO for user-related data transfer and CRUD operations in Blazor user interfaces.
/// </summary>
/// <remarks>
/// <para><strong>Blazor CRUD Best Practices:</strong></para>
/// <list type="bullet">
///   <item><description>All properties are mutable (get/set)</description></item>
///   <item><description>Use nullable types for optional fields</description></item>
///   <item><description>Use default values for collections</description></item>
///   <item><description>Simple POCOs suitable for model binding and form editing</description></item>
/// </list>
/// </remarks>
public abstract class UserBaseDTO
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
