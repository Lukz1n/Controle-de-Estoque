using ControleEstoque.Data;
using ControleEstoque.Models;

namespace ControleEstoque.Services;

public sealed class StockService
{
    private readonly Database _database;

    public StockService(Database database)
    {
        _database = database;
    }

    public int ChangeStock(long productId, MovementType type, int quantity, string? notes)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Informe uma quantidade inteira maior que zero.");
        if (type is not MovementType.Entrada and not MovementType.Saida)
            throw new ArgumentOutOfRangeException(nameof(type));

        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            string productName;
            int previousQuantity;
            using (var readCommand = connection.CreateCommand())
            {
                readCommand.Transaction = transaction;
                readCommand.CommandText = "SELECT Name, Quantity FROM Products WHERE Id = $id AND IsActive = 1;";
                readCommand.Parameters.AddWithValue("$id", productId);
                using var reader = readCommand.ExecuteReader();
                if (!reader.Read())
                    throw new InvalidOperationException("O produto selecionado não existe ou está inativo.");
                productName = reader.GetString(0);
                previousQuantity = reader.GetInt32(1);
            }

            if (type == MovementType.Saida && quantity > previousQuantity)
                throw new InvalidOperationException(previousQuantity == 0
                    ? "Não há unidades disponíveis para retirar."
                    : $"Estoque insuficiente. Há somente {previousQuantity} unidade(s) disponível(is).");
            if (type == MovementType.Entrada && quantity > int.MaxValue - previousQuantity)
                throw new InvalidOperationException("A quantidade informada excede o limite permitido.");

            var newQuantity = type == MovementType.Entrada
                ? previousQuantity + quantity
                : previousQuantity - quantity;
            using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = "UPDATE Products SET Quantity = $newQuantity WHERE Id = $id AND IsActive = 1 AND Quantity = $previousQuantity;";
                updateCommand.Parameters.AddWithValue("$newQuantity", newQuantity);
                updateCommand.Parameters.AddWithValue("$id", productId);
                updateCommand.Parameters.AddWithValue("$previousQuantity", previousQuantity);
                if (updateCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("O estoque foi alterado por outra operação. Atualize a tela e tente novamente.");
            }

            using (var movementCommand = connection.CreateCommand())
            {
                movementCommand.Transaction = transaction;
                movementCommand.CommandText = """
                    INSERT INTO StockMovements(ProductId, ProductName, Type, Quantity, PreviousQuantity, NewQuantity, OccurredAtUtc, Notes)
                    VALUES($productId, $productName, $type, $quantity, $previousQuantity, $newQuantity, $occurredAt, $notes);
                    """;
                movementCommand.Parameters.AddWithValue("$productId", productId);
                movementCommand.Parameters.AddWithValue("$productName", productName);
                movementCommand.Parameters.AddWithValue("$type", type == MovementType.Entrada ? "Entrada" : "Saida");
                movementCommand.Parameters.AddWithValue("$quantity", quantity);
                movementCommand.Parameters.AddWithValue("$previousQuantity", previousQuantity);
                movementCommand.Parameters.AddWithValue("$newQuantity", newQuantity);
                movementCommand.Parameters.AddWithValue("$occurredAt", DateTime.UtcNow.ToString("O"));
                movementCommand.Parameters.AddWithValue("$notes", (object?)cleanNotes ?? DBNull.Value);
                movementCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return newQuantity;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }
            throw;
        }
    }
}
