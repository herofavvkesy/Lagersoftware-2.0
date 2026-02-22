using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lagersoftware.Shared.DTOs;

namespace Lagersoftware.Client.Core.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;

    public ApiService(HttpClient httpClient, SettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    private void SetAuthHeader()
    {
        var token = _settingsService.Settings.AuthToken;
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private string BaseUrl => _settingsService.Settings.ServerUrl.TrimEnd('/');

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/login",
            new LoginRequestDto { Username = username, Password = password });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
    }

    public async Task<bool> IsServerReachableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/sync/dashboard",
                HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        }
        catch { return false; }
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/products");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>() ?? new List<ProductDto>();
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode)
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/products/barcode/{barcode}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/products", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/api/products/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        SetAuthHeader();
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/api/products/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/categories");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
    }

    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/categories", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CategoryDto>();
    }

    public async Task<List<StorageLocationDto>> GetStorageLocationsAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/storagelocations");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StorageLocationDto>>() ?? new List<StorageLocationDto>();
    }

    public async Task<StorageLocationDto?> CreateStorageLocationAsync(CreateStorageLocationDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/storagelocations", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<StorageLocationDto>();
    }

    public async Task<List<StockMovementDto>> GetStockMovementsAsync(int? productId = null, int page = 1)
    {
        SetAuthHeader();
        var url = $"{BaseUrl}/api/stockmovements?page={page}";
        if (productId.HasValue) url += $"&productId={productId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StockMovementDto>>() ?? new List<StockMovementDto>();
    }

    public async Task<StockMovementDto?> CreateStockMovementAsync(CreateStockMovementDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/stockmovements", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<StockMovementDto>();
    }

    public async Task<SyncResponseDto?> SyncAsync(SyncRequestDto request)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/sync", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SyncResponseDto>();
    }

    public async Task<DashboardDto?> GetDashboardAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/sync/dashboard");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DashboardDto>();
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/auth/users");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
    }

    public async Task<UserDto?> RegisterUserAsync(RegisterRequestDto dto)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/register", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        SetAuthHeader();
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/api/auth/users/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SupportResetAsync(string supportPassword)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/support-reset",
            new SupportResetRequestDto { SupportPassword = supportPassword });
        return response.IsSuccessStatusCode;
    }

    public async Task<SpeicherDatenDto?> ExportSpeicherDatenAsync()
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/api/sync/speicherdaten");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SpeicherDatenDto>();
    }

    public async Task<bool> ImportSpeicherDatenAsync(SpeicherDatenDto data)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/sync/speicherdaten", data);
        return response.IsSuccessStatusCode;
    }
}
