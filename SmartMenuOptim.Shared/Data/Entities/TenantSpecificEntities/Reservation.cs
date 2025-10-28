public class Reservation : TenantEntityBase
{

    public int TableId { get; set; }
    public DateTime ReservationTime { get; set; }

    public Table? Table { get; set; }
}
