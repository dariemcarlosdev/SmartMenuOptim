/*
 * File: ISaleRecordService.cs
 * Sale Record Service interface
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for sale record operations.
 */

using SmartMenuOptim.Application.Dtos.Sales;

namespace SmartMenuOptim.Server.Services.Abstractions;

/// <summary>
/// Defines the contract for sale record operations.
/// </summary>
public interface ISaleRecordService
{
    /// <summary>
    /// Retrieves all sale records.
    /// </summary>
    /// <returns>A list of sale records.</returns>
    Task<List<SaleRecordDTO>> GetSaleRecordsAsync();

    /// <summary>
    /// Adds a new sale record.
    /// </summary>
    /// <param name="saleRecord">The sale record to add.</param>
    Task AddSaleRecordAsync(SaleRecordDTO saleRecord);
}
