namespace Lagersoftware.Client.MAUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute("LoginPage", typeof(Pages.LoginPage));
        Routing.RegisterRoute("DashboardPage", typeof(Pages.DashboardPage));
        Routing.RegisterRoute("ProductsPage", typeof(Pages.ProductsPage));
        Routing.RegisterRoute("ProductDetailPage", typeof(Pages.ProductDetailPage));
        Routing.RegisterRoute("StockMovementPage", typeof(Pages.StockMovementPage));
        Routing.RegisterRoute("SettingsPage", typeof(Pages.SettingsPage));
        Routing.RegisterRoute("AdminPage", typeof(Pages.AdminPage));
    }
}
