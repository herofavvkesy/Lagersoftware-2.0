using Lagersoftware.Client.Core.Services;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly SyncService _syncService;
    private readonly SettingsService _settingsService;

    public DashboardPage(ApiService apiService, SyncService syncService, SettingsService settingsService)
    {
        InitializeComponent();
        _apiService = apiService;
        _syncService = syncService;
        _settingsService = settingsService;

        _syncService.OnlineStatusChanged += OnOnlineStatusChanged;
        _syncService.SyncCompleted += OnSyncCompleted;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Show admin button if admin
        AdminButton.IsVisible = _settingsService.Settings.UserRole == "Administrator";
        
        await LoadDashboardAsync();
        
        if (_settingsService.Settings.AutoSyncEnabled)
            _syncService.StartAutoSync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _syncService.StopAutoSync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            var dashboard = await _apiService.GetDashboardAsync();
            if (dashboard != null)
            {
                TotalProductsLabel.Text = dashboard.TotalProducts.ToString();
                LowStockLabel.Text = dashboard.LowStockProducts.ToString();
                MovementsTodayLabel.Text = dashboard.TotalMovementsToday.ToString();
                LocationsLabel.Text = dashboard.TotalStorageLocations.ToString();
                RecentMovementsList.ItemsSource = dashboard.RecentMovements;

                StatusDot.Fill = new SolidColorBrush(Color.FromArgb("#107C10"));
                StatusLabel.Text = "Online";
            }
        }
        catch
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromArgb("#D13438"));
            StatusLabel.Text = "Offline";
        }
    }

    private void OnOnlineStatusChanged(object? sender, bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusDot.Fill = new SolidColorBrush(isOnline ? Color.FromArgb("#107C10") : Color.FromArgb("#D13438"));
            StatusLabel.Text = isOnline ? "Online" : "Offline";
        });
    }

    private void OnSyncCompleted(object? sender, SyncEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDashboardAsync();
        });
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        var result = await _syncService.TrySyncAsync();
        await DisplayAlert("Sync", result.Message, "OK");
        await LoadDashboardAsync();
    }

    private async void OnProductsClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("ProductsPage");
    private async void OnMovementClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("StockMovementPage");
    private async void OnSettingsClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("SettingsPage");
    private async void OnAdminClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("AdminPage");
}
