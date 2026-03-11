namespace SmartMenuOptim.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a reservation.
/// </summary>
/// <remarks>
/// <para><strong>State Transitions:</strong></para>
/// <code>
///        Pending
///       /       \
///  Confirmed   Cancelled
///    /  |  \
/// Seated  |  NoShow
///    |    |
/// Completed Cancelled
/// </code>
/// </remarks>
public enum ReservationStatus
{
    /// <summary>
    /// Reservation has been created but not yet confirmed.
    /// Awaiting confirmation from customer or restaurant.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reservation has been confirmed by the restaurant.
    /// Customer is expected to arrive at the scheduled time.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Customer has arrived and been seated at the table.
    /// Reservation is currently active.
    /// </summary>
    Seated = 2,

    /// <summary>
    /// Reservation has been fulfilled and customers have left.
    /// Table is available for new reservations.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Reservation was cancelled by customer or restaurant.
    /// Table is available for other reservations.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Customer did not show up for their reservation.
    /// Table remained empty during the reserved time slot.
    /// </summary>
    NoShow = 5
}
