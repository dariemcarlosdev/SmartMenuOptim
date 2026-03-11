namespace SmartMenuOptim.Application.Dtos.Sales;

/// <summary>
/// DTO for category grouping of sale records.
/// </summary>
public class CategoryGroupDTO
{
    public required string CategoryName { get; set; }
    public required List<SaleRecordDTO> Records { get; set; }
}
