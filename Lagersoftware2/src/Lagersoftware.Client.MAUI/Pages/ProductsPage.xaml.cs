using Lagersoftware.Client.Core.Services;
using Lagersoftware.Shared.DTOs;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class ProductsPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private List<ProductDto> _allProducts = new();

    public ProductsPage(ApiService apiService, SettingsService settingsService)
    {
        InitializeComponent();
        _apiService = apiService;
        _settingsService = settingsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AddButton.IsVisible = _settingsService.Settings.UserRole == "Administrator";
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = await _apiService.GetProductsAsync();
            ProductsList.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            ProductsList.ItemsSource = _allProducts;
            return;
        }
        ProductsList.ItemsSource = _allProducts
            .Where(p => p.Name.ToLower().Contains(query) || p.Barcode.ToLower().Contains(query) || (p.CategoryName?.ToLower().Contains(query) ?? false))
            .ToList();
    }

    private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ProductDto product)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Shell.Current.GoToAsync($"ProductDetailPage?id={product.Id}");
        }
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ProductDetailPage?id=0");
    }
}
