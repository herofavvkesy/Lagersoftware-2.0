namespace Lagersoftware.Client.Core.Data;

public class PendingSync
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string OperationType { get; set; } = string.Empty; // Create, Update, Delete
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
}
