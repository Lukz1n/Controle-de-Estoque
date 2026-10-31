using ControleEstoque.Data;
using ControleEstoque.Forms;
using ControleEstoque.Helpers;
using ControleEstoque.Services;

namespace ControleEstoque;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var database = new Database();
            database.Initialize();
            Application.Run(new MainForm(
                new ProductService(database),
                new StockService(database),
                new MovementHistoryService(database)));
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(
                "Não foi possível iniciar o Controle de Estoque. Os detalhes técnicos foram registrados no log local.",
                "Falha ao iniciar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
