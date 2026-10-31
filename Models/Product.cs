namespace ControleEstoque.Models;

public sealed class Product
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public int Quantity { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string StockStatus => Quantity == 0 ? "Sem estoque" : Quantity <= 5 ? "Estoque baixo" : "Normal";
}
