using System.ComponentModel;
using ControleEstoque.Helpers;
using ControleEstoque.Models;
using ControleEstoque.Services;

namespace ControleEstoque.Forms;

public sealed class MovementHistoryForm : Form
{
    private readonly MovementHistoryService _history;
    private readonly TextBox _search = UiTheme.CreateTextBox();
    private readonly ComboBox _type = new();
    private readonly DateTimePicker _fromDate = new();
    private readonly DateTimePicker _untilDate = new();
    private readonly CheckBox _useDates = new();
    private readonly DataGridView _grid = new();
    private readonly Label _resultCount = UiTheme.CreateLabel("", 9, false, UiTheme.Muted);
    private readonly ExportService _export = new();
    private IReadOnlyList<StockMovement> _currentMovements = Array.Empty<StockMovement>();
    private bool _loading;

    public MovementHistoryForm(MovementHistoryService history)
    {
        _history = history;
        Text = "Histórico de movimentações";
        MinimumSize = new Size(850, 480);
        ClientSize = new Size(1050, 650);
        UiTheme.StyleForm(this);
        BuildLayout();
        LoadHistory();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiTheme.Background, Margin = new Padding(0) };
        heading.Controls.Add(UiTheme.CreateLabel("Histórico de movimentações", 19, true));
        heading.Controls.Add(UiTheme.CreateLabel("Pesquise, aplique filtros e exporte os registros encontrados.", 9, false, UiTheme.Muted));
        root.Controls.Add(heading, 0, 0);

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            BackColor = UiTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12, 10, 12, 10)
        };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filters.Controls.Add(UiTheme.CreateLabel("Produto", 9, true, UiTheme.Muted), 0, 0);
        filters.Controls.Add(UiTheme.CreateLabel("Tipo", 9, true, UiTheme.Muted), 1, 0);
        filters.Controls.Add(UiTheme.CreateLabel("Período", 9, true, UiTheme.Muted), 2, 0);
        filters.Controls.Add(UiTheme.CreateLabel("De", 9, true, UiTheme.Muted), 3, 0);
        filters.Controls.Add(UiTheme.CreateLabel("Até", 9, true, UiTheme.Muted), 4, 0);
        filters.Controls.Add(UiTheme.CreateLabel("", 9), 5, 0);
        _search.Dock = DockStyle.Fill;
        _search.PlaceholderText = "Buscar pelo nome do produto";
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Items.AddRange(new object[] { "Todos", "Entrada", "Saída" });
        _type.SelectedIndex = 0;
        _type.Dock = DockStyle.Fill;
        _useDates.Text = "Filtrar datas";
        _useDates.AutoSize = true;
        _useDates.ForeColor = UiTheme.Text;
        _useDates.Anchor = AnchorStyles.Left;
        ConfigureDatePicker(_fromDate);
        ConfigureDatePicker(_untilDate);
        _fromDate.Enabled = false;
        _untilDate.Enabled = false;
        var apply = UiTheme.CreateButton("Aplicar filtros", true);
        apply.Dock = DockStyle.Fill;
        apply.Click += (_, _) => LoadHistory();
        filters.Controls.Add(_search, 0, 1);
        filters.Controls.Add(_type, 1, 1);
        filters.Controls.Add(_useDates, 2, 1);
        filters.Controls.Add(_fromDate, 3, 1);
        filters.Controls.Add(_untilDate, 4, 1);
        filters.Controls.Add(apply, 5, 1);
        root.Controls.Add(filters, 0, 1);

        var exportBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 3, 0, 0)
        };
        var exportExcel = UiTheme.CreateButton("Exportar para Excel");
        var exportPdf = UiTheme.CreateButton("Exportar para PDF");
        exportExcel.Click += (_, _) => ExportMovements(asPdf: false);
        exportPdf.Click += (_, _) => ExportMovements(asPdf: true);
        exportBar.Controls.Add(exportExcel);
        exportBar.Controls.Add(exportPdf);
        root.Controls.Add(exportBar, 0, 2);

        var listPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiTheme.Surface, Padding = new Padding(0), BorderStyle = BorderStyle.FixedSingle };
        listPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _resultCount.TextAlign = ContentAlignment.MiddleLeft;
        _resultCount.Dock = DockStyle.Fill;
        _resultCount.AutoSize = false;
        _resultCount.Margin = new Padding(12, 0, 0, 0);
        listPanel.Controls.Add(_resultCount, 0, 0);
        ConfigureGrid();
        listPanel.Controls.Add(_grid, 0, 1);
        root.Controls.Add(listPanel, 0, 3);
        Controls.Add(root);

        _search.TextChanged += (_, _) => { if (!_loading) LoadHistory(); };
        _type.SelectedIndexChanged += (_, _) => { if (!_loading) LoadHistory(); };
        _useDates.CheckedChanged += (_, _) =>
        {
            _fromDate.Enabled = _useDates.Checked;
            _untilDate.Enabled = _useDates.Checked;
            LoadHistory();
        };
        _fromDate.ValueChanged += (_, _) => { if (_useDates.Checked) LoadHistory(); };
        _untilDate.ValueChanged += (_, _) => { if (_useDates.Checked) LoadHistory(); };
    }

    private static void ConfigureDatePicker(DateTimePicker picker)
    {
        picker.Format = DateTimePickerFormat.Short;
        picker.Dock = DockStyle.Fill;
        picker.MaxDate = DateTime.Today;
        picker.Value = DateTime.Today;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.BackgroundColor = UiTheme.Surface;
        _grid.BorderStyle = BorderStyle.None;
        _grid.GridColor = UiTheme.Border;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(242, 245, 247);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Muted;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = UiTheme.SoftAccent;
        _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        _grid.ColumnHeadersHeight = 42;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        _grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
        _grid.RowTemplate.Height = 40;
        AddTextColumn("Data e hora", "LocalTime", 155);
        AddTextColumn("Produto", "ProductName", 180, true);
        AddTextColumn("Operação", "TypeLabel", 100);
        AddTextColumn("Quantidade", "Quantity", 100);
        AddTextColumn("Antes", "PreviousQuantity", 85);
        AddTextColumn("Depois", "NewQuantity", 85);
        AddTextColumn("Observação", "Notes", 180, true);
    }

    private void AddTextColumn(string title, string property, int width, bool fill = false)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = title,
            DataPropertyName = property,
            Width = width,
            AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
            MinimumWidth = fill ? 130 : width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void LoadHistory()
    {
        try
        {
            _loading = true;
            var type = _type.SelectedItem?.ToString() switch
            {
                "Entrada" => "Entrada",
                "Saída" => "Saida",
                _ => string.Empty
            };
            var items = _history.Search(
                _search.Text,
                type,
                _useDates.Checked ? _fromDate.Value.Date : null,
                _useDates.Checked ? _untilDate.Value.Date : null);
            _currentMovements = items;
            var rows = items.Select(movement => new HistoryRow
            {
                LocalTime = movement.OccurredAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                ProductName = movement.ProductName,
                TypeLabel = movement.Type == MovementType.Entrada ? "Entrada" : "Saída",
                Quantity = movement.Quantity,
                PreviousQuantity = movement.PreviousQuantity,
                NewQuantity = movement.NewQuantity,
                Notes = movement.Notes ?? string.Empty
            }).ToList();
            _grid.DataSource = new BindingList<HistoryRow>(rows);
            _resultCount.Text = $"{rows.Count:N0} movimentação(ões) encontrada(s)";
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            _resultCount.Text = "Não foi possível carregar o histórico.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ExportMovements(bool asPdf)
    {
        using var dialog = new SaveFileDialog
        {
            Title = asPdf ? "Salvar relatório em PDF" : "Exportar histórico para Excel",
            Filter = asPdf ? "Arquivo PDF (*.pdf)|*.pdf" : "Arquivo Excel (*.xlsx)|*.xlsx",
            DefaultExt = asPdf ? "pdf" : "xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"historico-movimentacoes-{DateTime.Today:yyyy-MM-dd}.{(asPdf ? "pdf" : "xlsx")}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (asPdf)
                _export.ExportMovementsToPdf(_currentMovements, dialog.FileName, DescribeCurrentFilters());
            else
                _export.ExportMovementsToExcel(_currentMovements, dialog.FileName);
            _resultCount.Text = $"Arquivo salvo: {Path.GetFileName(dialog.FileName)} ({_currentMovements.Count:N0} movimentação(ões))";
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(this, "Não foi possível gerar o arquivo. Confira se a pasta permite gravação e tente novamente.", "Falha na exportação", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string DescribeCurrentFilters()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_search.Text))
            parts.Add($"produto contém ‘{_search.Text.Trim()}’");
        if (_type.SelectedItem?.ToString() is string itemType && (itemType == "Entrada" || itemType == "Saída"))
            parts.Add($"tipo: {itemType.ToLowerInvariant()}");
        if (_useDates.Checked)
            parts.Add($"período: {_fromDate.Value:dd/MM/yyyy} a {_untilDate.Value:dd/MM/yyyy}");
        return parts.Count == 0 ? "nenhum filtro" : string.Join("; ", parts);
    }

    private sealed class HistoryRow
    {
        public string LocalTime { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public string TypeLabel { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public int PreviousQuantity { get; init; }
        public int NewQuantity { get; init; }
        public string Notes { get; init; } = string.Empty;
    }
}
