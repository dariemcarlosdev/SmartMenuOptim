using SmartMenuOptim.Shared.Models;

internal interface ISaleRecordService
{
    /// <summary>
    /// Retrieves a list of sale records for a specific product.
    /// </summary>
    /// <param name="productId">The ID of the product to retrieve sale records for.</param>
    /// <returns>A list of sale records for the specified product.</returns>
    Task<List<SaleRecord>> GetSaleRecordsAsync();
    /// <summary>
    /// Adds a new sale record for a product.
    /// </summary>
    /// <param name="saleRecord">The sale record to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddSaleRecordAsync(SaleRecord saleRecord);
}