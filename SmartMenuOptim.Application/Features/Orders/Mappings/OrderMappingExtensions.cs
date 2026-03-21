using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Application.Features.Orders.Mappings;

/// <summary>
/// Extension methods for mapping Order-related entities to DTOs.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>These extensions keep mapping logic in the Application layer, preventing
/// Domain entities from having knowledge of DTOs.</para>
/// </remarks>
public static class OrderMappingExtensions
{
    // ═══════════════════════════════════════════════════════════════════════
    // ORDER MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps an Order entity to an OrderDTO (summary view).
    /// </summary>
    /// <param name="entity">The Order entity to map.</param>
    /// <returns>A new OrderDTO with mapped values.</returns>
    public static OrderDTO ToDto(this Order entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new OrderDTO
        {
            Id = entity.Id,
            RestaurantId = entity.RestaurantId,
            CustomerId = entity.CustomerId,
            CustomerName = entity.Customer?.Name,
            StatusName = entity.Status?.Name ?? string.Empty,
            StatusColorCode = entity.Status?.ColorCode,
            IsTerminal = entity.Status?.IsTerminal ?? false,
            TotalAmount = entity.TotalAmount,
            ItemCount = entity.Items.Count,
            OrderDate = entity.OrderDate,
            SpecialInstructions = entity.SpecialInstructions,
            HandledByStaffName = entity.HandledBy?.Name,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Maps an Order entity to an OrderDetailDTO (full detail view with items).
    /// </summary>
    /// <param name="entity">The Order entity to map.</param>
    /// <returns>A new OrderDetailDTO with mapped values including items.</returns>
    public static OrderDetailDTO ToDetailDto(this Order entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new OrderDetailDTO
        {
            Id = entity.Id,
            RestaurantId = entity.RestaurantId,
            CustomerId = entity.CustomerId,
            CustomerName = entity.Customer?.Name,
            OrderStatusId = entity.OrderStatusId,
            StatusName = entity.Status?.Name ?? string.Empty,
            StatusColorCode = entity.Status?.ColorCode,
            IsTerminal = entity.Status?.IsTerminal ?? false,
            TotalAmount = entity.TotalAmount,
            OrderDate = entity.OrderDate,
            SpecialInstructions = entity.SpecialInstructions,
            HandledByStaffId = entity.HandledByStaffId,
            HandledByStaffName = entity.HandledBy?.Name,
            Items = entity.Items.Select(i => i.ToDto()).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ORDER ITEM MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps an OrderItem entity to an OrderItemDTO.
    /// </summary>
    /// <param name="entity">The OrderItem entity to map.</param>
    /// <returns>A new OrderItemDTO with mapped values.</returns>
    public static OrderItemDTO ToDto(this OrderItem entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new OrderItemDTO
        {
            Id = entity.Id,
            DishId = entity.DishId,
            DishName = entity.Dish?.Name ?? "Unknown Dish",
            UnitPrice = entity.UnitPrice,
            Quantity = entity.Quantity,
            Subtotal = entity.Subtotal,
            SpecialInstructions = entity.SpecialInstructions
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ORDER STATUS MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps an OrderStatus entity to an OrderStatusDTO.
    /// </summary>
    /// <param name="entity">The OrderStatus entity to map.</param>
    /// <returns>A new OrderStatusDTO with mapped values.</returns>
    public static OrderStatusDTO ToDto(this OrderStatus entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new OrderStatusDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DisplayOrder = entity.DisplayOrder,
            IsTerminal = entity.IsTerminal,
            ColorCode = entity.ColorCode
        };
    }
}
