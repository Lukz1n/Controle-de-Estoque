using ControleEstoque.Data;
using ControleEstoque.Models;

namespace ControleEstoque.Services;

public sealed class MovementHistoryService
{
    private readonly Database _database;

    public MovementHistoryService(Database database)
    {
        _database = database;
    }

    public IReadOnlyList<StockMovement> Search(string productName, string movementType, DateTime? fromLocal, DateTime? untilLocal)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        var filters = new List<string>();
        var cleanSearch = productName.Trim();
        if (cleanSearch.Length > 0)
        {
            filters.Add("ProductName LIKE $pattern ESCAPE '\\'");
            var escaped = cleanSearch.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            command.Parameters.AddWithValue("$pattern", $"%{escaped}%");
        }
        if (movementType is "Entrada" or "Saida")
        {
            filters.Add("Type = $type");
            command.Parameters.AddWithValue("$type", movementType);
        }
        if (fromLocal.HasValue)
        {
            filters.Add("OccurredAtUtc >= $fromDate");
            command.Parameters.AddWithValue("$fromDate", fromLocal.Value.ToUniversalTime().ToString("O"));
        }
        if (untilLocal.HasValue)
        {
            filters.Add("OccurredAtUtc < $untilDate");
            command.Parameters.AddWithValue("$untilDate", untilLocal.Value.AddDays(1).ToUniversalTime().ToString("O"));
        }

        command.CommandText = """
            SELECT Id, ProductId, ProductName, Type, Quantity, PreviousQuantity, NewQuantity, OccurredAtUtc, Notes
            FROM StockMovements
            """ + (filters.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", filters)) + " ORDER BY OccurredAtUtc DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        var movements = new List<StockMovement>();
        while (reader.Read())
        {
            movements.Add(new StockMovement
            {
                Id = reader.GetInt64(0),
                ProductId = reader.GetInt64(1),
                ProductName = reader.GetString(2),
                Type = reader.GetString(3) == "Entrada" ? MovementType.Entrada : MovementType.Saida,
                Quantity = reader.GetInt32(4),
                PreviousQuantity = reader.GetInt32(5),
                NewQuantity = reader.GetInt32(6),
                OccurredAtUtc = DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Notes = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }
        return movements;
    }
}
