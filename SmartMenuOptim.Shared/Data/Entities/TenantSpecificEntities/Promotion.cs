public class Promotion : TenantEntityBase
{

    public string Name { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}