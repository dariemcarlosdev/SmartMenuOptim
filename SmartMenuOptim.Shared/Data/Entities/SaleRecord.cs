namespace SmartMenuOptim.Shared.Data.Entities
{
    public class SaleRecord
    {
        public int Id { get; set; }
        public required string DishName { get; set; }
        public int QuantitySold { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
