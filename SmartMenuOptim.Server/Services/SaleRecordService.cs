using SmartMenuOptim.Application.Common;

internal class SaleRecordService : ISaleRecordService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRecordService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory used to create the backend API client.</param>
    public SaleRecordService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
    }

    /// <summary>
    /// Adds a sale record by posting it to the backend API.
    /// </summary>
    /// <param name="saleRecord">The sale record to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="saleRecord"/> is null.</exception>
    public async Task AddSaleRecordAsync(SaleRecordDTO saleRecord)
    {
        if (saleRecord == null) throw new ArgumentNullException(nameof(saleRecord));

        var response = await _httpClient.PostAsJsonAsync("api/salerecords", saleRecord).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Retrieves all sale records from the API.
    /// </summary>
    /// <returns>A <see cref="List{SaleRecordDTO}"/> containing sale records. Returns an empty list if the API response is null or an error occurs.</returns>
    public async Task<List<SaleRecordDTO>> GetSaleRecordsAsync()
    {
        try
        {
            // Retrieves all sale records from the API
            return await _httpClient.GetFromJsonAsync<List<SaleRecordDTO>>("api/salerecords").ConfigureAwait(false) ?? new List<SaleRecordDTO>();
        }
        catch (HttpRequestException ex)
        {
            // Log the exception (using Console.WriteLine for demonstration purposes)
            Console.WriteLine($"API request failed: {ex.Message}");
            // Return an empty list to ensure the UI doesn't crash
            return new List<SaleRecordDTO>();
        }
    }
}