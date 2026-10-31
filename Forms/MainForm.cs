using System.ComponentModel;
using ControleEstoque.Helpers;
using ControleEstoque.Models;
using ControleEstoque.Services;

namespace ControleEstoque.Forms;

public sealed class MainForm : Form
{
    private static readonly Font StockStatusFont = new("Segoe UI", 9.5F, FontStyle.Bold);
    private readonly ProductService _products;
    private readonly StockService _stock;
    private readonly MovementHistoryService _history;
    private readonly TextBox _search = UiTheme.CreateTextBox();
    private readonly DataGridView _grid = new();
    private readonly Label _noResults = UiTheme.CreateLabel("Nenhum produto encontrado.", 11, false, UiTheme.Muted);
    private readonly Label _gridCount = UiTheme.CreateLabel("", 9, false, UiTheme.Muted);
    private readonly Label _selectedLabel = UiTheme.CreateLabel("Selecione um produto para ver as ações disponíveis.", 9, false, UiTheme.Muted);
    private readonly Label _statusLabel = UiTheme.CreateLabel("Pronto.", 9, false, UiTheme.Muted);
    private readonly Label _productsValue = UiTheme.CreateLabel("0", 19, true, UiTheme.Accent);
    private readonly Label _unitsValue = UiTheme.CreateLabel("0", 19, true, UiTheme.Accent);
    private readonly Label _lowValue = UiTheme.CreateLabel("0", 19, true, UiTheme.Warning);
    private readonly ExportService _export = new();
    private readonly Button _removeOne = UiTheme.CreateButton("Retirar 1 unidade", true);
    private readonly Button _removeCustom = UiTheme.CreateButton("Retirada personalizada");
    private readonly Button _addUnits = UiTheme.CreateButton("Adicionar unidades");
    private readonly Button _editProduct = UiTheme.CreateButton("Editar produto");
    private readonly Button _deactivateProduct = UiTheme.CreateButton("Desativar produto");
    private long? _selectedProductId;
    private bool _busy;
    private bool _loadingProducts;

    public MainForm(ProductService products, StockService stock, MovementHistoryService history)
    {
        _products = products;
        _stock = stock;
        _history = history;
        Text = "Controle de Estoque";
        MinimumSize = new Size(980, 620);
        ClientSize = new Size(1180, 760);
        UiTheme.StyleForm(this);
        BuildLayout();
        LoadProducts();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildIndicators(), 0, 1);
        root.Controls.Add(BuildSearchBar(), 0, 2);
        root.Controls.Add(BuildProductGrid(), 0, 3);
        root.Controls.Add(BuildActions(), 0, 4);
        Controls.Add(root);

        _search.PlaceholderText = "Buscar produto pelo nome...";
        _search.Dock = DockStyle.Fill;
        _search.TextChanged += (_, _) => LoadProducts();
        _grid.SelectionChanged += (_, _) => { if (!_loadingProducts) UpdateSelection(); };
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelectedProduct();
        };
        _removeOne.Click += (_, _) => RemoveOne();
        _removeCustom.Click += (_, _) => OpenMovement(MovementType.Saida);
        _addUnits.Click += (_, _) => OpenMovement(MovementType.Entrada);
        _editProduct.Click += (_, _) => EditSelectedProduct();
        _deactivateProduct.Click += (_, _) => DeactivateSelectedProduct();
        UpdateSelection();
    }

    private Control BuildHeader()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Background };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        var titlePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiTheme.Background, Padding = new Padding(0, 2, 0, 0) };
        titlePanel.Controls.Add(UiTheme.CreateLabel("CONTROLE LOCAL DE ESTOQUE", 8, true, UiTheme.Accent));
        titlePanel.Controls.Add(UiTheme.CreateLabel("Controle de Estoque", 23, true));
        titlePanel.Controls.Add(UiTheme.CreateLabel("Consulte e atualize seus produtos", 9, false, UiTheme.Muted));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 19, 0, 0), BackColor = UiTheme.Background };
        var historyButton = UiTheme.CreateButton("Histórico de movimentações");
        var exportButton = UiTheme.CreateButton("Exportar estoque");
        var newProductButton = UiTheme.CreateButton("Novo produto", true);
        historyButton.Click += (_, _) => new MovementHistoryForm(_history).ShowDialog(this);
        exportButton.Click += (_, _) => ExportInventory();
        newProductButton.Click += (_, _) => CreateProduct();
        actions.Controls.Add(historyButton);
        actions.Controls.Add(exportButton);
        actions.Controls.Add(newProductButton);
        layout.Controls.Add(titlePanel, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        return layout;
    }

    private Control BuildIndicators()
    {
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = UiTheme.Background, Padding = new Padding(0, 4, 0, 4) };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        cards.Controls.Add(CreateIndicatorCard("PRODUTOS CADASTRADOS", _productsValue, UiTheme.Accent), 0, 0);
        cards.Controls.Add(CreateIndicatorCard("UNIDADES DISPONÍVEIS", _unitsValue, UiTheme.Accent), 1, 0);
        cards.Controls.Add(CreateIndicatorCard("ESTOQUE BAIXO (1–5 UN.)", _lowValue, UiTheme.Warning), 2, 0);
        return cards;
    }

    private static Control CreateIndicatorCard(string title, Label value, Color accent)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(14, 8, 14, 6), Margin = new Padding(0, 0, 14, 0), BorderStyle = BorderStyle.FixedSingle };
        var stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
        var heading = UiTheme.CreateLabel(title, 8, true, UiTheme.Muted);
        heading.Location = new Point(18, 12);
        value.Location = new Point(18, 38);
        card.Controls.Add(heading);
        card.Controls.Add(value);
        card.Controls.Add(stripe);
        return card;
    }

    private Control BuildSearchBar()
    {
        var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Background, Padding = new Padding(0, 10, 0, 10) };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var label = UiTheme.CreateLabel("PESQUISAR", 9, true, UiTheme.Muted);
        label.Anchor = AnchorStyles.Left;
        bar.Controls.Add(label, 0, 0);
        bar.Controls.Add(_search, 1, 0);
        return bar;
    }

    private Control BuildProductGrid()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiTheme.Surface, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var listHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Surface, Padding = new Padding(14, 0, 14, 0) };
        listHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        listHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        var listTitle = UiTheme.CreateLabel("Produtos", 12, true);
        listTitle.Anchor = AnchorStyles.Left;
        _gridCount.Anchor = AnchorStyles.Right;
        _gridCount.TextAlign = ContentAlignment.MiddleRight;
        _gridCount.Dock = DockStyle.Fill;
        listHeader.Controls.Add(listTitle, 0, 0);
        listHeader.Controls.Add(_gridCount, 1, 0);
        panel.Controls.Add(listHeader, 0, 0);

        var gridPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
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
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
        _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = UiTheme.SoftAccent;
        _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        _grid.ColumnHeadersHeight = 42;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        _grid.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
        _grid.RowTemplate.Height = 46;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Produto",
            DataPropertyName = nameof(Product.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Quantidade disponível",
            DataPropertyName = nameof(Product.Quantity),
            Width = 190,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0", Font = new Font("Segoe UI", 10, FontStyle.Bold) },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Situação do estoque",
            DataPropertyName = nameof(Product.StockStatus),
            Width = 210,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not Product product || e.ColumnIndex != 2 || e.CellStyle is null)
                return;
            e.CellStyle.ForeColor = product.Quantity == 0 ? UiTheme.Danger : product.Quantity <= 5 ? UiTheme.Warning : UiTheme.Accent;
            e.CellStyle.Font = StockStatusFont;
        };
        _noResults.Dock = DockStyle.Fill;
        _noResults.TextAlign = ContentAlignment.MiddleCenter;
        _noResults.Visible = false;
        gridPanel.Controls.Add(_grid);
        gridPanel.Controls.Add(_noResults);
        _noResults.BringToFront();
        panel.Controls.Add(gridPanel, 0, 1);
        return panel;
    }

    private Control BuildActions()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Background, Padding = new Padding(0, 6, 0, 0) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _selectedLabel.Dock = DockStyle.Fill;
        _selectedLabel.Margin = new Padding(0);
        root.Controls.Add(_selectedLabel, 0, 0);
        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, BackColor = UiTheme.Background, Margin = new Padding(0) };
        row.Controls.Add(_removeOne);
        row.Controls.Add(_removeCustom);
        row.Controls.Add(_addUnits);
        row.Controls.Add(_editProduct);
        row.Controls.Add(_deactivateProduct);
        root.Controls.Add(row, 0, 1);
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 2);
        return root;
    }

    private void LoadProducts()
    {
        var restoreId = _selectedProductId;
        try
        {
            _loadingProducts = true;
            var allProducts = _products.GetActiveProducts();
            var visibleProducts = _products.GetActiveProducts(_search.Text);
            _productsValue.Text = allProducts.Count.ToString("N0");
            _unitsValue.Text = allProducts.Sum(product => (long)product.Quantity).ToString("N0");
            _lowValue.Text = allProducts.Count(product => product.Quantity is > 0 and <= 5).ToString("N0");
            _grid.DataSource = new BindingList<Product>(visibleProducts.ToList());
            _gridCount.Text = $"{visibleProducts.Count:N0} exibido(s) de {allProducts.Count:N0}";
            _noResults.Visible = visibleProducts.Count == 0;
            if (restoreId.HasValue)
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.DataBoundItem is Product product && product.Id == restoreId.Value)
                    {
                        row.Selected = true;
                        _grid.CurrentCell = row.Cells[0];
                        break;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            SetStatus("Não foi possível atualizar os produtos. Consulte o log local.", UiTheme.Danger);
        }
        finally
        {
            _loadingProducts = false;
            UpdateSelection();
        }
    }

    private Product? GetSelectedProduct()
    {
        return _grid.CurrentRow?.DataBoundItem as Product;
    }

    private void UpdateSelection()
    {
        var product = GetSelectedProduct();
        _selectedProductId = product?.Id;
        var enabled = product is not null && !_busy;
        _removeOne.Enabled = enabled;
        _removeCustom.Enabled = enabled;
        _addUnits.Enabled = enabled;
        _editProduct.Enabled = enabled;
        _deactivateProduct.Enabled = enabled;
        _selectedLabel.Text = product is null
            ? "Selecione um produto para ver as ações disponíveis."
            : $"Selecionado: {product.Name} ({product.Quantity:N0} unidade(s))";
    }

    private void RemoveOne()
    {
        var product = GetSelectedProduct();
        if (product is null || _busy)
            return;
        try
        {
            SetBusy(true);
            var quantity = _stock.ChangeStock(product.Id, MovementType.Saida, 1, null);
            _selectedProductId = product.Id;
            LoadProducts();
            SetStatus($"Saída de 1 unidade registrada. Novo estoque: {quantity:N0}.", UiTheme.Accent);
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            SetStatus(exception.Message, UiTheme.Danger);
            if (exception.Message.Contains("Não há unidades", StringComparison.OrdinalIgnoreCase))
                MessageBox.Show(this, exception.Message, "Estoque sem unidades", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (!exception.Message.Contains("insuficiente", StringComparison.OrdinalIgnoreCase))
                MessageBox.Show(this, "Não foi possível registrar a saída. " + exception.Message, "Operação não realizada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
            UpdateSelection();
        }
    }

    private void OpenMovement(MovementType type)
    {
        var product = GetSelectedProduct();
        if (product is null || _busy)
            return;
        using var form = new StockMovementForm(_stock, product, type);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _selectedProductId = product.Id;
            LoadProducts();
            SetStatus(type == MovementType.Entrada ? "Entrada registrada com sucesso." : "Saída registrada com sucesso.", UiTheme.Accent);
        }
    }

    private void CreateProduct()
    {
        using var form = new ProductForm(_products);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _selectedProductId = null;
            LoadProducts();
            SetStatus("Produto cadastrado com sucesso.", UiTheme.Accent);
        }
    }

    private void ExportInventory()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar estoque atual",
            Filter = "Arquivo Excel (*.xlsx)|*.xlsx|Arquivo PDF (*.pdf)|*.pdf",
            FilterIndex = 1,
            DefaultExt = "xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"estoque-atual-{DateTime.Today:yyyy-MM-dd}.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var products = _products.GetActiveProducts();
            if (dialog.FilterIndex == 2 || Path.GetExtension(dialog.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                _export.ExportProductsToPdf(products, dialog.FileName);
            else
                _export.ExportProductsToExcel(products, dialog.FileName);
            SetStatus($"Relatório do estoque salvo em {Path.GetFileName(dialog.FileName)}.", UiTheme.Accent);
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(this, "Não foi possível exportar o estoque. Confira se a pasta permite gravação e tente novamente.", "Falha na exportação", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditSelectedProduct()
    {
        var product = GetSelectedProduct();
        if (product is null || _busy)
            return;
        using var form = new ProductForm(_products, product);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _selectedProductId = product.Id;
            LoadProducts();
            SetStatus("Dados do produto atualizados.", UiTheme.Accent);
        }
    }

    private void DeactivateSelectedProduct()
    {
        var product = GetSelectedProduct();
        if (product is null || _busy)
            return;
        var answer = MessageBox.Show(
            this,
            $"Deseja desativar \"{product.Name}\"? Ele deixará de aparecer no estoque ativo, mas o histórico será preservado.",
            "Confirmar desativação",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;
        try
        {
            _products.Deactivate(product.Id);
            _selectedProductId = null;
            LoadProducts();
            SetStatus("Produto desativado. O histórico foi preservado.", UiTheme.Accent);
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(this, "Não foi possível desativar o produto.", "Operação não realizada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateSelection();
        UseWaitCursor = busy;
    }

    private void SetStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = color;
    }
}
