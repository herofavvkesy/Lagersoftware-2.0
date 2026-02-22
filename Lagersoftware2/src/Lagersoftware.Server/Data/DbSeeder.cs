using Lagersoftware.Shared.Enums;
using Lagersoftware.Shared.Models;

namespace Lagersoftware.Server.Data;

public static class DbSeeder
{
    public static async Task SeedDemoDataAsync(AppDbContext context)
    {
        // Remove all existing data
        context.StockMovements.RemoveRange(context.StockMovements);
        context.Products.RemoveRange(context.Products);
        context.Categories.RemoveRange(context.Categories);
        context.StorageLocations.RemoveRange(context.StorageLocations);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // Create users
        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = UserRole.Administrator,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Username = "user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(admin, user);

        // Create categories
        var catElektronik = new Category { Name = "Elektronik", Description = "Elektronische Geräte und Komponenten", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var catBüro = new Category { Name = "Bürobedarf", Description = "Büromaterialien und -zubehör", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var catWerkzeug = new Category { Name = "Werkzeug", Description = "Handwerkzeuge und Maschinen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Categories.AddRange(catElektronik, catBüro, catWerkzeug);

        // Create storage locations
        var hauptlager = new StorageLocation { Name = "Hauptlager", Description = "Zentrales Hauptlager", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var nebenlager1 = new StorageLocation { Name = "Nebenlager 1", Description = "Nebenlager Erdgeschoss", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var nebenlager2 = new StorageLocation { Name = "Nebenlager 2", Description = "Nebenlager Obergeschoss", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.StorageLocations.AddRange(hauptlager, nebenlager1, nebenlager2);

        await context.SaveChangesAsync();

        // Create products
        var products = new List<Product>
        {
            new() { Name = "Laptop Dell XPS 15", Description = "High-Performance Laptop", Barcode = "4001234567890", Quantity = 5, MinQuantity = 2, Price = 1299.99m, Category = catElektronik, StorageLocation = hauptlager, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "USB-Hub 7-Port", Description = "USB 3.0 Hub mit 7 Anschlüssen", Barcode = "4001234567891", Quantity = 15, MinQuantity = 5, Price = 29.99m, Category = catElektronik, StorageLocation = hauptlager, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "HDMI Kabel 2m", Description = "HDMI 2.1 Kabel, 2 Meter", Barcode = "4001234567892", Quantity = 30, MinQuantity = 10, Price = 9.99m, Category = catElektronik, StorageLocation = nebenlager1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Drucker HP LaserJet", Description = "Laserdrucker schwarz/weiß", Barcode = "4001234567893", Quantity = 3, MinQuantity = 1, Price = 249.99m, Category = catElektronik, StorageLocation = hauptlager, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "A4 Papier 500 Blatt", Description = "Kopierpapier 80g/m²", Barcode = "4001234567894", Quantity = 100, MinQuantity = 20, Price = 5.99m, Category = catBüro, StorageLocation = nebenlager2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Kugelschreiber blau (10er)", Description = "Blaue Kugelschreiber, 10 Stück", Barcode = "4001234567895", Quantity = 50, MinQuantity = 10, Price = 3.49m, Category = catBüro, StorageLocation = nebenlager2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Ordner A4 breite Rücken", Description = "Aktenordner A4, 80mm Rückenbreite", Barcode = "4001234567896", Quantity = 40, MinQuantity = 10, Price = 2.99m, Category = catBüro, StorageLocation = nebenlager2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Hammer 500g", Description = "Schlosserhammer 500g", Barcode = "4001234567897", Quantity = 8, MinQuantity = 2, Price = 14.99m, Category = catWerkzeug, StorageLocation = nebenlager1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Schraubenzieher-Set", Description = "Schraubenzieher-Set 12-teilig", Barcode = "4001234567898", Quantity = 10, MinQuantity = 3, Price = 19.99m, Category = catWerkzeug, StorageLocation = nebenlager1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Name = "Akkuschrauber Bosch", Description = "Akkuschrauber 18V mit 2 Akkus", Barcode = "4001234567899", Quantity = 4, MinQuantity = 1, Price = 149.99m, Category = catWerkzeug, StorageLocation = hauptlager, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task EnsureAdminExistsAsync(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Administrator,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
