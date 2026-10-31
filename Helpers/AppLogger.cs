namespace ControleEstoque.Helpers;

public static class AppLogger
{
    private static readonly object Sync = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ControleEstoque",
        "logs",
        "aplicativo.log");

    public static void Log(Exception exception)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 72)}{Environment.NewLine}";
            lock (Sync)
                File.AppendAllText(LogPath, entry);
        }
        catch
        {
        }
    }
}
