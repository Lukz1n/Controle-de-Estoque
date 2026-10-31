using Microsoft.Data.Sqlite;

namespace ControleEstoque.Data;

public sealed class Database
{
    private const string SeedKey = "InitialProductsSeeded";
    private readonly string _connectionString;

    public Database()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ControleEstoque");
        Directory.CreateDirectory(directory);
        DatabasePath = Path.Combine(directory, "estoque.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = 10
        }.ToString();
    }

    public string DatabasePath { get; }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using (var journalCommand = connection.CreateCommand())
        {
            journalCommand.CommandText = "PRAGMA journal_mode = WAL;";
            journalCommand.ExecuteNonQuery();
        }

        using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                    Code TEXT NULL,
                    Quantity INTEGER NOT NULL DEFAULT 0 CHECK(Quantity >= 0),
                    Notes TEXT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1 CHECK(IsActive IN (0, 1)),
                    CreatedAtUtc TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Code
                    ON Products(Code COLLATE NOCASE)
                    WHERE Code IS NOT NULL AND length(trim(Code)) > 0;
                CREATE INDEX IF NOT EXISTS IX_Products_Active_Name
                    ON Products(IsActive, Name COLLATE NOCASE);
                CREATE TABLE IF NOT EXISTS StockMovements (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Type TEXT NOT NULL CHECK(Type IN ('Entrada', 'Saida')),
                    Quantity INTEGER NOT NULL CHECK(Quantity > 0),
                    PreviousQuantity INTEGER NOT NULL CHECK(PreviousQuantity >= 0),
                    NewQuantity INTEGER NOT NULL CHECK(NewQuantity >= 0),
                    OccurredAtUtc TEXT NOT NULL,
                    Notes TEXT NULL,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS IX_StockMovements_OccurredAt
                    ON StockMovements(OccurredAtUtc DESC);
                CREATE INDEX IF NOT EXISTS IX_StockMovements_Product
                    ON StockMovements(ProductId, OccurredAtUtc DESC);
                CREATE TABLE IF NOT EXISTS AppSettings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
                """;
            schemaCommand.ExecuteNonQuery();
        }

        using var transaction = connection.BeginTransaction();
        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = "SELECT 1 FROM AppSettings WHERE Key = $key LIMIT 1;";
            checkCommand.Parameters.AddWithValue("$key", SeedKey);
            if (checkCommand.ExecuteScalar() is not null)
            {
                transaction.Commit();
                return;
            }
        }

        AddInitialProduct(connection, transaction, "Produto 1", 54);
        AddInitialProduct(connection, transaction, "Produto 2", 23);

        using (var markCommand = connection.CreateCommand())
        {
            markCommand.Transaction = transaction;
            markCommand.CommandText = "INSERT INTO AppSettings(Key, Value) VALUES($key, $value);";
            markCommand.Parameters.AddWithValue("$key", SeedKey);
            markCommand.Parameters.AddWithValue("$value", "true");
            markCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void AddInitialProduct(SqliteConnection connection, SqliteTransaction transaction, string name, int quantity)
    {
        var now = DateTime.UtcNow.ToString("O");
        long productId;
        using (var productCommand = connection.CreateCommand())
        {
            productCommand.Transaction = transaction;
            productCommand.CommandText = """
                INSERT INTO Products(Name, Quantity, IsActive, CreatedAtUtc)
                VALUES($name, $quantity, 1, $createdAt);
                SELECT last_insert_rowid();
                """;
            productCommand.Parameters.AddWithValue("$name", name);
            productCommand.Parameters.AddWithValue("$quantity", quantity);
            productCommand.Parameters.AddWithValue("$createdAt", now);
            productId = (long)(productCommand.ExecuteScalar() ?? throw new InvalidOperationException("Não foi possível criar o produto inicial."));
        }

        using var movementCommand = connection.CreateCommand();
        movementCommand.Transaction = transaction;
        movementCommand.CommandText = """
            INSERT INTO StockMovements(ProductId, ProductName, Type, Quantity, PreviousQuantity, NewQuantity, OccurredAtUtc, Notes)
            VALUES($productId, $productName, 'Entrada', $quantity, 0, $quantity, $occurredAt, $notes);
            """;
        movementCommand.Parameters.AddWithValue("$productId", productId);
        movementCommand.Parameters.AddWithValue("$productName", name);
        movementCommand.Parameters.AddWithValue("$quantity", quantity);
        movementCommand.Parameters.AddWithValue("$occurredAt", now);
        movementCommand.Parameters.AddWithValue("$notes", "Estoque inicial");
        movementCommand.ExecuteNonQuery();
    }
}
