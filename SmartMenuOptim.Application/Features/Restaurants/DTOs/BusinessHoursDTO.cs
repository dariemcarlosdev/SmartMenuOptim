/*
 * File: BusinessHoursDTO.cs
 * Data Transfer Object for BusinessHours entity
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents operating hours for a specific day of the week.
 * This DTO maps to the BusinessHours child entity in the Domain layer.
 * 
 * Usage: Used in Restaurant DTOs for operating hours configuration.
 */

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for BusinessHours entity.
/// </summary>
/// <remarks>
/// <para>Maps to <see cref="Domain.Aggregates.RestaurantAggregate.BusinessHours"/> entity.</para>
/// <para>All properties are mutable for Blazor form binding and CRUD operations.</para>
/// </remarks>
public class BusinessHoursDTO
{
    /// <summary>
    /// Unique identifier for this business hours record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The day of the week these hours apply to.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Opening time for this day (e.g., "09:00").
    /// </summary>
    public TimeSpan OpenTime { get; set; }

    /// <summary>
    /// Closing time for this day (e.g., "22:00").
    /// </summary>
    public TimeSpan CloseTime { get; set; }

    /// <summary>
    /// Indicates if the restaurant is closed all day.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Gets the formatted hours display string.
    /// </summary>
    public string FormattedHours =>
        IsClosed
            ? "Closed"
            : $"{OpenTime:hh\\:mm} - {CloseTime:hh\\:mm}";

    /// <summary>
    /// Gets the day name for display.
    /// </summary>
    public string DayName => DayOfWeek.ToString();
}
