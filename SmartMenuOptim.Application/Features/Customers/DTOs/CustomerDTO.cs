using SmartMenuOptim.Application.Dtos.Common;

namespace SmartMenuOptim.Application.Features.Customers.DTOs;

/// <summary>
/// DTO for transferring Customer data between layers and for CRUD operations in Blazor user interfaces.
/// </summary>
public class CustomerDTO : UserBaseDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public string? Role { get; set; }
}
