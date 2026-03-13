namespace SmartMenuOptim.Application.Features.Admin.DTOs;

/// <summary>
/// DTO for transferring business rule data between layers and for CRUD operations in Blazor user interfaces.
/// </summary>
public class BusinessRuleDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public int AdminUserId { get; set; }
}
