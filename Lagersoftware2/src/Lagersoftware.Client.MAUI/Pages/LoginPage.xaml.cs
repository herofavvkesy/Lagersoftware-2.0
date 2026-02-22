using Lagersoftware.Client.Core.Services;

namespace Lagersoftware.Client.MAUI.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;

    public LoginPage(ApiService apiService, SettingsService settingsService)
    {
        InitializeComponent();
        _apiService = apiService;
        _settingsService = settingsService;

        ServerUrlEntry.Text = _settingsService.Settings.ServerUrl;
        UsernameEntry.Text = _settingsService.Settings.LastUsername ?? string.Empty;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        var serverUrl = ServerUrlEntry.Text?.Trim();
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Bitte alle Felder ausfüllen.");
            return;
        }

        _settingsService.UpdateSettings(s => s.ServerUrl = serverUrl);

        try
        {
            var response = await _apiService.LoginAsync(username, password);
            if (response == null)
            {
                ShowError("Ungültige Anmeldedaten oder Server nicht erreichbar.");
                return;
            }

            _settingsService.UpdateSettings(s =>
            {
                s.AuthToken = response.Token;
                s.LastUsername = response.Username;
                s.UserRole = response.Role;
                s.UserId = response.UserId;
                s.TokenExpiresAt = response.ExpiresAt;
            });

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (Exception ex)
        {
            ShowError($"Fehler: {ex.Message}");
        }
    }

    private async void OnSupportResetClicked(object sender, EventArgs e)
    {
        var supportPw = await DisplayPromptAsync("Support-Reset", "Support-Passwort eingeben:", "Zurücksetzen", "Abbrechen", isPassword: true);
        if (string.IsNullOrEmpty(supportPw)) return;

        try
        {
            var success = await _apiService.SupportResetAsync(supportPw);
            if (success)
                await DisplayAlert("Erfolg", "Alle Passwörter wurden auf 'password123' zurückgesetzt.", "OK");
            else
                await DisplayAlert("Fehler", "Falsches Support-Passwort.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private void ShowError(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
