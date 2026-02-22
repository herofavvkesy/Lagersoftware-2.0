namespace Lagersoftware.Shared.DTOs;

public class SyncRequestDto
{
    public DateTime LastSyncAt { get; set; }
    public List<ProductDto> ChangedProducts { get; set; } = new();
    public List<CategoryDto> ChangedCategories { get; set; } = new();
    public List<StorageLocationDto> ChangedStorageLocations { get; set; } = new();
    public List<StockMovementDto> NewMovements { get; set; } = new();
}

public class SyncResponseDto
{
    public DateTime SyncedAt { get; set; }
    public List<ProductDto> UpdatedProducts { get; set; } = new();
    public List<CategoryDto> UpdatedCategories { get; set; } = new();
    public List<StorageLocationDto> UpdatedStorageLocations { get; set; } = new();
    public List<StockMovementDto> NewMovements { get; set; } = new();
    public int ConflictsResolved { get; set; }
}

public class DashboardDto
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalStorageLocations { get; set; }
    public int TotalMovementsToday { get; set; }
    public int TotalMovementsThisWeek { get; set; }
    public List<StockMovementDto> RecentMovements { get; set; } = new();
}

public class SpeicherDatenDto
{
    public DateTime ExportedAt { get; set; }
    public List<ProductDto> Products { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<StorageLocationDto> StorageLocations { get; set; } = new();
    public List<StockMovementDto> StockMovements { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();
}
