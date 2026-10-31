namespace ControleEstoque.Models;

public enum MovementType
{
    Entrada,
    Saida
}

public sealed class StockMovement
{
    public long Id { get; init; }
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public MovementType Type { get; init; }
    public int Quantity { get; init; }
    public int PreviousQuantity { get; init; }
    public int NewQuantity { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? Notes { get; init; }
}
