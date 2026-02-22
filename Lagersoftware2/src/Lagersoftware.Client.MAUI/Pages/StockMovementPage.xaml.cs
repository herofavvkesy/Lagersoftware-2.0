using Lagersoftware.Client.Core.Services;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Enums;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class StockMovementPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<ProductDto> _products = new();
    private List<ProductDto> _filteredProducts = new();

    public StockMovementPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MovementTypePicker.SelectedIndex = 0;
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _products = await _apiService.GetProductsAsync();
            _filteredProducts = _products;
            ProductPicker.ItemsSource = _products.Select(p => p.Name).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private void OnProductSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? string.Empty;
        _filteredProducts = string.IsNullOrEmpty(query)
            ? _products
            : _products.Where(p => p.Name.ToLower().Contains(query) || p.Barcode.Contains(query)).ToList();
        ProductPicker.ItemsSource = _filteredProducts.Select(p => p.Name).ToList();
    }

    private async void OnBarcodeScanClicked(object sender, EventArgs e)
    {
        var barcode = BarcodeEntry.Text?.Trim();
        if (string.IsNullOrEmpty(barcode)) return;

        try
        {
            var product = await _apiService.GetProductByBarcodeAsync(barcode);
            if (product != null)
            {
                var idx = _filteredProducts.FindIndex(p => p.Id == product.Id);
                if (idx < 0)
                {
                    _filteredProducts = _products;
                    ProductPicker.ItemsSource = _products.Select(p => p.Name).ToList();
                    idx = _products.FindIndex(p => p.Id == product.Id);
                }
                ProductPicker.SelectedIndex = idx;
            }
            else
            {
                await DisplayAlert("Nicht gefunden", $"Produkt mit Barcode '{barcode}' nicht gefunden.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async void OnBookClicked(object sender, EventArgs e)
    {
        if (MovementTypePicker.SelectedIndex < 0 || ProductPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Fehler", "Bitte Buchungstyp und Produkt auswählen.", "OK");
            return;
        }
        if (!int.TryParse(QuantityEntry.Text, out var qty) || qty <= 0)
        {
            await DisplayAlert("Fehler", "Bitte eine gültige Menge eingeben.", "OK");
            return;
        }

        var movementType = (MovementType)(MovementTypePicker.SelectedIndex + 1);
        var product = _filteredProducts[ProductPicker.SelectedIndex];

        try
        {
            var dto = new CreateStockMovementDto
            {
                ProductId = product.Id,
                MovementType = movementType,
                Quantity = qty,
                Note = NoteEntry.Text ?? string.Empty
            };
            var result = await _apiService.CreateStockMovementAsync(dto);
            if (result != null)
            {
                ResultLabel.Text = $"✅ Buchung erfolgreich: {product.Name} {movementType} {qty} Stk.";
                ResultLabel.TextColor = Color.FromArgb("#107C10");
                ResultLabel.IsVisible = true;
                QuantityEntry.Text = string.Empty;
                NoteEntry.Text = string.Empty;
            }
            else
            {
                ResultLabel.Text = "❌ Buchung fehlgeschlagen.";
                ResultLabel.TextColor = Color.FromArgb("#D13438");
                ResultLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"❌ Fehler: {ex.Message}";
            ResultLabel.TextColor = Color.FromArgb("#D13438");
            ResultLabel.IsVisible = true;
        }
    }
}
