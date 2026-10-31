using ControleEstoque.Data;
using ControleEstoque.Models;
using Microsoft.Data.Sqlite;

namespace ControleEstoque.Services;

public sealed class ProductService
{
    private readonly Database _database;

    public ProductService(Database database)
    {
        _database = database;
    }

    public IReadOnlyList<Product> GetActiveProducts(string search = "")
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Code, Quantity, Notes, IsActive, CreatedAtUtc
            FROM Products
            WHERE IsActive = 1 AND ($search = '' OR Name LIKE $pattern ESCAPE '\')
            ORDER BY Name COLLATE NOCASE;
            """;
        var normalizedSearch = search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        command.Parameters.AddWithValue("$search", normalizedSearch);
        command.Parameters.AddWithValue("$pattern", $"%{normalizedSearch}%");
        using var reader = command.ExecuteReader();
        var products = new List<Product>();
        while (reader.Read())
            products.Add(ReadProduct(reader));
        return products;
    }

    public Product? GetById(long id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Code, Quantity, Notes, IsActive, CreatedAtUtc FROM Products WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProduct(reader) : null;
    }

    public void Create(string name, int initialQuantity, string? code, string? notes)
    {
        ValidateName(name);
        if (initialQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "A quantidade inicial não pode ser negativa.");

        var cleanName = name.Trim();
        var cleanCode = CleanOptional(code);
        var cleanNotes = CleanOptional(notes);
        var now = DateTime.UtcNow.ToString("O");
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            long productId;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO Products(Name, Code, Quantity, Notes, IsActive, CreatedAtUtc)
                    VALUES($name, $code, $quantity, $notes, 1, $createdAt);
                    SELECT last_insert_rowid();
                    """;
                command.Parameters.AddWithValue("$name", cleanName);
                command.Parameters.AddWithValue("$code", (object?)cleanCode ?? DBNull.Value);
                command.Parameters.AddWithValue("$quantity", initialQuantity);
                command.Parameters.AddWithValue("$notes", (object?)cleanNotes ?? DBNull.Value);
                command.Parameters.AddWithValue("$createdAt", now);
                productId = (long)(command.ExecuteScalar() ?? throw new InvalidOperationException("Não foi possível salvar o produto."));
            }

            if (initialQuantity > 0)
                InsertInitialMovement(connection, transaction, productId, cleanName, initialQuantity, now);
            transaction.Commit();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            transaction.Rollback();
            throw new InvalidOperationException("Já existe um produto cadastrado com esse código.", exception);
        }
    }

    public void Update(long id, string name, string? code, string? notes)
    {
        ValidateName(name);
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Products SET Name = $name, Code = $code, Notes = $notes WHERE Id = $id AND IsActive = 1;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$code", (object?)CleanOptional(code) ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)CleanOptional(notes) ?? DBNull.Value);
        try
        {
            if (command.ExecuteNonQuery() == 0)
                throw new InvalidOperationException("O produto não foi encontrado ou está inativo.");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Já existe um produto cadastrado com esse código.", exception);
        }
    }

    public void Deactivate(long id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = $id AND IsActive = 1;";
        command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("O produto não foi encontrado ou já está inativo.");
    }

    private static void InsertInitialMovement(SqliteConnection connection, SqliteTransaction transaction, long productId, string productName, int quantity, string occurredAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO StockMovements(ProductId, ProductName, Type, Quantity, PreviousQuantity, NewQuantity, OccurredAtUtc, Notes)
            VALUES($productId, $productName, 'Entrada', $quantity, 0, $quantity, $occurredAt, $notes);
            """;
        command.Parameters.AddWithValue("$productId", productId);
        command.Parameters.AddWithValue("$productName", productName);
        command.Parameters.AddWithValue("$quantity", quantity);
        command.Parameters.AddWithValue("$occurredAt", occurredAt);
        command.Parameters.AddWithValue("$notes", "Estoque inicial do produto");
        command.ExecuteNonQuery();
    }

    private static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Code = reader.IsDBNull(2) ? null : reader.GetString(2),
            Quantity = reader.GetInt32(3),
            Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
            IsActive = reader.GetInt32(5) == 1,
            CreatedAtUtc = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Informe o nome do produto.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("O nome do produto deve ter no máximo 200 caracteres.", nameof(name));
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
