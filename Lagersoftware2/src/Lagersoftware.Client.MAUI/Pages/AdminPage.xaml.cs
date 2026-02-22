using Lagersoftware.Client.Core.Services;
using Lagersoftware.Shared.DTOs;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class AdminPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly SyncService _syncService;

    public AdminPage(ApiService apiService, SyncService syncService)
    {
        InitializeComponent();
        _apiService = apiService;
        _syncService = syncService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _apiService.GetUsersAsync();
            UsersList.ItemsSource = users;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        var username = await DisplayPromptAsync("Neuer Benutzer", "Benutzername:");
        if (string.IsNullOrEmpty(username)) return;
        var password = await DisplayPromptAsync("Neuer Benutzer", "Passwort:", isPassword: true);
        if (string.IsNullOrEmpty(password)) return;
        var roleInput = await DisplayActionSheet("Rolle", "Abbrechen", null, "User", "Administrator");
        if (roleInput == "Abbrechen") return;

        var dto = new RegisterRequestDto { Username = username, Password = password, Role = roleInput };
        var result = await _apiService.RegisterUserAsync(dto);
        if (result != null)
        {
            await DisplayAlert("Erfolg", $"Benutzer '{username}' angelegt.", "OK");
            await LoadUsersAsync();
        }
        else
        {
            await DisplayAlert("Fehler", "Benutzer konnte nicht angelegt werden.", "OK");
        }
    }

    private async void OnDeleteUserClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int userId)
        {
            var confirm = await DisplayAlert("Löschen", "Benutzer wirklich löschen?", "Ja", "Nein");
            if (!confirm) return;
            await _apiService.DeleteUserAsync(userId);
            await LoadUsersAsync();
        }
    }

    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        var result = await _syncService.TrySyncAsync();
        await DisplayAlert("Sync", result.Message, "OK");
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            var data = await _apiService.ExportSpeicherDatenAsync();
            if (data != null)
                await DisplayAlert("Export", $"Daten exportiert: {data.Products.Count} Produkte, {data.Users.Count} Benutzer, exportiert am {data.ExportedAt:dd.MM.yyyy HH:mm}", "OK");
            else
                await DisplayAlert("Fehler", "Export fehlgeschlagen.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }
}
