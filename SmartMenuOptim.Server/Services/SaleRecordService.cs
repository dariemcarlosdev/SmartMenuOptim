using SmartMenuOptim.Shared.Data.Dtos;
using SmartMenuOptim.Shared.Data.Entities;

internal class SaleRecordService : ISaleRecordService
{
    private readonly HttpClient _httpClient;

    public SaleRecordService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
    }

    public async Task AddSaleRecordAsync(SaleRecordDTO saleRecord)
    {
        var response = await _httpClient.PostAsJsonAsync("api/salerecords", saleRecord);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<SaleRecordDTO>> GetSaleRecordsAsync()
        // Retrieves all sale records from the API
       => await _httpClient.GetFromJsonAsync<List<SaleRecordDTO>>("api/SaleRecords") ?? [];

}