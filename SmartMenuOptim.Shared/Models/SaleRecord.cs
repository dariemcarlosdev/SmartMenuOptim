namespace SmartMenuOptim.Shared.Models
{
    public class SaleRecord
    {
        public int Id { get; set; }
        public string DishName { get; set; }
        public int QuantitySold { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
