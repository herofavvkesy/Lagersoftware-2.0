namespace Lagersoftware.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public int MinQuantity { get; set; } = 0;
    public decimal Price { get; set; } = 0;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? StorageLocationId { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
