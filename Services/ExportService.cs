using System.Globalization;
using ClosedXML.Excel;
using ControleEstoque.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ControleEstoque.Services;

/// <summary>Gera arquivos de relatório sem alterar os dados do estoque.</summary>
public sealed class ExportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    static ExportService()
    {
        // A licença Community do QuestPDF é apropriada apenas para quem atende aos critérios da biblioteca.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void ExportProductsToExcel(IReadOnlyList<Product> products, string filePath)
    {
        ArgumentNullException.ThrowIfNull(products);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Estoque atual");
        SetTitle(sheet, "Relatório de estoque atual", 5);
        SetHeaders(sheet, 3, "Produto", "Código", "Quantidade", "Situação", "Observação");

        for (var index = 0; index < products.Count; index++)
        {
            var product = products[index];
            var row = index + 4;
            SetText(sheet.Cell(row, 1), product.Name);
            SetText(sheet.Cell(row, 2), product.Code ?? string.Empty);
            sheet.Cell(row, 3).Value = product.Quantity;
            SetText(sheet.Cell(row, 4), product.StockStatus);
            SetText(sheet.Cell(row, 5), product.Notes ?? string.Empty);
        }

        FormatSheet(sheet, 5, products.Count + 3);
        workbook.SaveAs(filePath);
    }

    public void ExportMovementsToExcel(IReadOnlyList<StockMovement> movements, string filePath)
    {
        ArgumentNullException.ThrowIfNull(movements);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Movimentações");
        SetTitle(sheet, "Relatório de movimentações", 7);
        SetHeaders(sheet, 3, "Data e hora", "Produto", "Operação", "Quantidade", "Estoque anterior", "Estoque posterior", "Observação");

        for (var index = 0; index < movements.Count; index++)
        {
            var movement = movements[index];
            var row = index + 4;
            sheet.Cell(row, 1).Value = movement.OccurredAtUtc.ToLocalTime();
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
            SetText(sheet.Cell(row, 2), movement.ProductName);
            SetText(sheet.Cell(row, 3), TypeLabel(movement.Type));
            sheet.Cell(row, 4).Value = movement.Quantity;
            sheet.Cell(row, 5).Value = movement.PreviousQuantity;
            sheet.Cell(row, 6).Value = movement.NewQuantity;
            SetText(sheet.Cell(row, 7), movement.Notes ?? string.Empty);
        }

        FormatSheet(sheet, 7, movements.Count + 3);
        workbook.SaveAs(filePath);
    }

    public void ExportProductsToPdf(IReadOnlyList<Product> products, string filePath)
    {
        ArgumentNullException.ThrowIfNull(products);
        Document.Create(document => document.Page(page =>
        {
            ConfigurePage(page, "Relatório de estoque atual", $"{products.Count:N0} produto(s) ativo(s)");
            page.Content().PaddingVertical(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3.2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(3.1f);
                });
                AddPdfHeader(table, "Produto", "Código", "Quantidade", "Situação", "Observação");
                foreach (var product in products)
                {
                    AddPdfCell(table, product.Name);
                    AddPdfCell(table, product.Code ?? "");
                    AddPdfCell(table, product.Quantity.ToString("N0", PtBr));
                    AddPdfCell(table, product.StockStatus);
                    AddPdfCell(table, product.Notes ?? "");
                }
            });
        })).GeneratePdf(filePath);
    }

    public void ExportMovementsToPdf(IReadOnlyList<StockMovement> movements, string filePath, string? filterDescription = null)
    {
        ArgumentNullException.ThrowIfNull(movements);
        var summary = $"{movements.Count:N0} movimentação(ões)";
        if (!string.IsNullOrWhiteSpace(filterDescription))
            summary += $" | Filtros: {filterDescription}";

        Document.Create(document => document.Page(page =>
        {
            ConfigurePage(page, "Relatório de movimentações", summary);
            page.Content().PaddingVertical(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.7f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(2.1f);
                });
                AddPdfHeader(table, "Data e hora", "Produto", "Operação", "Qtd.", "Antes", "Depois", "Observação");
                foreach (var movement in movements)
                {
                    AddPdfCell(table, movement.OccurredAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr));
                    AddPdfCell(table, movement.ProductName);
                    AddPdfCell(table, TypeLabel(movement.Type));
                    AddPdfCell(table, movement.Quantity.ToString("N0", PtBr));
                    AddPdfCell(table, movement.PreviousQuantity.ToString("N0", PtBr));
                    AddPdfCell(table, movement.NewQuantity.ToString("N0", PtBr));
                    AddPdfCell(table, movement.Notes ?? "");
                }
            });
        })).GeneratePdf(filePath);
    }

    private static void ConfigurePage(PageDescriptor page, string title, string summary)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(28);
        page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(8));
        page.Header().Column(column =>
        {
            column.Item().Text(title).FontSize(19).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(3).Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} — {summary}")
                .FontSize(8).FontColor(Colors.Grey.Darken2);
        });
        page.Footer().AlignCenter().DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(text =>
        {
            text.Span("Controle de Estoque  •  Página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
        });
    }

    private static void AddPdfHeader(TableDescriptor table, params string[] labels)
    {
        table.Header(header =>
        {
            foreach (var label in labels)
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text(label).Bold().FontColor(Colors.White);
        });
    }

    private static void AddPdfCell(TableDescriptor table, string value)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value);
    }

    private static void SetTitle(IXLWorksheet sheet, string title, int columnCount)
    {
        sheet.Range(1, 1, 1, columnCount).Merge();
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(2, 1).Value = $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Range(2, 1, 2, columnCount).Merge();
    }

    private static void SetHeaders(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(row, column + 1).Value = headers[column];
            sheet.Cell(row, column + 1).Style.Font.Bold = true;
            sheet.Cell(row, column + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F2F5");
        }
    }

    private static void FormatSheet(IXLWorksheet sheet, int columnCount, int lastRow)
    {
        sheet.SheetView.FreezeRows(3);
        sheet.Range(3, 1, lastRow, columnCount).SetAutoFilter();
        sheet.Columns(1, columnCount).AdjustToContents(1, Math.Max(lastRow, 3));
        for (var column = 1; column <= columnCount; column++)
            sheet.Column(column).Width = Math.Min(sheet.Column(column).Width, 55);
        sheet.Style.Font.FontName = "Segoe UI";
        sheet.Style.Font.FontSize = 10;
    }

    private static void SetText(IXLCell cell, string value)
    {
        cell.Value = value;
    }

    private static string TypeLabel(MovementType type) => type == MovementType.Entrada ? "Entrada" : "Saída";
}
