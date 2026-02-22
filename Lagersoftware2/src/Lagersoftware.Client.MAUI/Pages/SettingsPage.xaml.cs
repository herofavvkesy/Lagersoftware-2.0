using Lagersoftware.Client.Core.Services;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly SyncService _syncService;

    public SettingsPage(SettingsService settingsService, SyncService syncService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _syncService = syncService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var settings = _settingsService.Settings;
        ServerUrlEntry.Text = settings.ServerUrl;
        SyncIntervalEntry.Text = settings.SyncIntervalMinutes.ToString();
        AutoSyncSwitch.IsToggled = settings.AutoSyncEnabled;
        UserInfoLabel.Text = $"Benutzer: {settings.LastUsername}\nRolle: {settings.UserRole}";
        LastSyncLabel.Text = _syncService.LastSyncAt == DateTime.MinValue
            ? "Noch nicht synchronisiert"
            : $"Letzter Sync: {_syncService.LastSyncAt:dd.MM.yyyy HH:mm}";
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        _settingsService.UpdateSettings(s =>
        {
            s.ServerUrl = ServerUrlEntry.Text?.Trim() ?? "http://localhost:5000";
            s.SyncIntervalMinutes = int.TryParse(SyncIntervalEntry.Text, out var i) ? i : 5;
            s.AutoSyncEnabled = AutoSyncSwitch.IsToggled;
        });
        DisplayAlert("Gespeichert", "Einstellungen gespeichert.", "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _settingsService.UpdateSettings(s =>
        {
            s.AuthToken = null;
            s.UserRole = null;
            s.UserId = null;
            s.TokenExpiresAt = null;
        });
        _syncService.StopAutoSync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnSyncNowClicked(object sender, EventArgs e)
    {
        var result = await _syncService.TrySyncAsync();
        await DisplayAlert("Sync", result.Message, "OK");
        LastSyncLabel.Text = result.Success
            ? $"Letzter Sync: {result.SyncedAt:dd.MM.yyyy HH:mm}"
            : "Sync fehlgeschlagen";
    }
}
