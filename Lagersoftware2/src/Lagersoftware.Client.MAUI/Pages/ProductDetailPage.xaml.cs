using Lagersoftware.Client.Core.Services;
using Lagersoftware.Shared.DTOs;

namespace Lagersoftware.Client.MAUI.Pages;

[QueryProperty(nameof(ProductId), "id")]
public partial class ProductDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private List<CategoryDto> _categories = new();
    private List<StorageLocationDto> _locations = new();
    private ProductDto? _existingProduct;

    public string ProductId { get; set; } = "0";

    public ProductDetailPage(ApiService apiService, SettingsService settingsService)
    {
        InitializeComponent();
        _apiService = apiService;
        _settingsService = settingsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        bool isAdmin = _settingsService.Settings.UserRole == "Administrator";
        
        _categories = await _apiService.GetCategoriesAsync();
        _locations = await _apiService.GetStorageLocationsAsync();

        CategoryPicker.ItemsSource = _categories.Select(c => c.Name).ToList();
        LocationPicker.ItemsSource = _locations.Select(l => l.Name).ToList();

        if (int.TryParse(ProductId, out var id) && id > 0)
        {
            // Load existing product
            var products = await _apiService.GetProductsAsync();
            _existingProduct = products.FirstOrDefault(p => p.Id == id);
            if (_existingProduct != null)
            {
                Title = _existingProduct.Name;
                NameEntry.Text = _existingProduct.Name;
                DescriptionEditor.Text = _existingProduct.Description;
                BarcodeEntry.Text = _existingProduct.Barcode;
                QuantityEntry.Text = _existingProduct.Quantity.ToString();
                MinQuantityEntry.Text = _existingProduct.MinQuantity.ToString();
                PriceEntry.Text = _existingProduct.Price.ToString("F2");

                var catIndex = _categories.FindIndex(c => c.Id == _existingProduct.CategoryId);
                if (catIndex >= 0) CategoryPicker.SelectedIndex = catIndex;

                var locIndex = _locations.FindIndex(l => l.Id == _existingProduct.StorageLocationId);
                if (locIndex >= 0) LocationPicker.SelectedIndex = locIndex;
            }
            DeleteButton.IsVisible = isAdmin;
        }
        else
        {
            Title = "Neues Produkt";
        }

        // Read-only for non-admins
        bool enabled = isAdmin;
        NameEntry.IsEnabled = enabled;
        DescriptionEditor.IsEnabled = enabled;
        BarcodeEntry.IsEnabled = enabled;
        QuantityEntry.IsEnabled = enabled;
        MinQuantityEntry.IsEnabled = enabled;
        PriceEntry.IsEnabled = enabled;
        CategoryPicker.IsEnabled = enabled;
        LocationPicker.IsEnabled = enabled;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_settingsService.Settings.UserRole != "Administrator")
        {
            await DisplayAlert("Keine Berechtigung", "Nur Administratoren können Produkte bearbeiten.", "OK");
            return;
        }

        var dto = new CreateProductDto
        {
            Name = NameEntry.Text ?? string.Empty,
            Description = DescriptionEditor.Text ?? string.Empty,
            Barcode = BarcodeEntry.Text ?? string.Empty,
            Quantity = int.TryParse(QuantityEntry.Text, out var q) ? q : 0,
            MinQuantity = int.TryParse(MinQuantityEntry.Text, out var mq) ? mq : 0,
            Price = decimal.TryParse(PriceEntry.Text, out var p) ? p : 0,
            CategoryId = CategoryPicker.SelectedIndex >= 0 ? _categories[CategoryPicker.SelectedIndex].Id : null,
            StorageLocationId = LocationPicker.SelectedIndex >= 0 ? _locations[LocationPicker.SelectedIndex].Id : null
        };

        try
        {
            if (_existingProduct != null)
                await _apiService.UpdateProductAsync(_existingProduct.Id, new UpdateProductDto { Name = dto.Name, Description = dto.Description, Barcode = dto.Barcode, Quantity = dto.Quantity, MinQuantity = dto.MinQuantity, Price = dto.Price, CategoryId = dto.CategoryId, StorageLocationId = dto.StorageLocationId });
            else
                await _apiService.CreateProductAsync(dto);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_existingProduct == null) return;
        var confirm = await DisplayAlert("Löschen", $"'{_existingProduct.Name}' wirklich löschen?", "Ja", "Nein");
        if (!confirm) return;
        await _apiService.DeleteProductAsync(_existingProduct.Id);
        await Shell.Current.GoToAsync("..");
    }
}
