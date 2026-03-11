/*
 * File: SaleRecordService.cs
 * Sale Record Service implementation
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Implements sale record operations via HTTP client to backend API.
 */

using SmartMenuOptim.Application.Dtos.Sales;
using SmartMenuOptim.Server.Services.Abstractions;

namespace SmartMenuOptim.Server.Services.ClientServices;

/// <summary>
/// HTTP client-based implementation for sale record operations.
/// </summary>
public class SaleRecordService : ISaleRecordService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SaleRecordService> _logger;

    /// <summary>
    /// Initializes a new instance of SaleRecordService.
    /// </summary>
    public SaleRecordService(IHttpClientFactory httpClientFactory, ILogger<SaleRecordService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddSaleRecordAsync(SaleRecordDTO saleRecord)
    {
        ArgumentNullException.ThrowIfNull(saleRecord);

        var response = await _httpClient.PostAsJsonAsync("api/salerecords", saleRecord);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<List<SaleRecordDTO>> GetSaleRecordsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<SaleRecordDTO>>("api/salerecords") 
                   ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve sale records from API");
            return [];
        }
    }
}
