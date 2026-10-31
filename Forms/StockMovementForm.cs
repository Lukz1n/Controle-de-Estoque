using ControleEstoque.Helpers;
using ControleEstoque.Models;
using ControleEstoque.Services;

namespace ControleEstoque.Forms;

public sealed class StockMovementForm : Form
{
    private readonly StockService _stock;
    private readonly Product _product;
    private readonly MovementType _type;
    private readonly TextBox _quantity = UiTheme.CreateTextBox();
    private readonly TextBox _notes = UiTheme.CreateTextBox();
    private readonly Label _currentQuantity;

    public StockMovementForm(StockService stock, Product product, MovementType type)
    {
        _stock = stock;
        _product = product;
        _type = type;
        Text = type == MovementType.Entrada ? "Adicionar unidades" : "Registrar saída";
        MinimumSize = new Size(450, 390);
        ClientSize = new Size(520, 420);
        UiTheme.StyleForm(this);
        _currentQuantity = UiTheme.CreateLabel($"Disponível agora: {product.Quantity:N0} unidade(s)", 10, true, UiTheme.Accent);
        BuildLayout();
        Shown += (_, _) => _quantity.Focus();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 18),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiTheme.Background, Margin = new Padding(0) };
        heading.Controls.Add(UiTheme.CreateLabel(_type == MovementType.Entrada ? "ADICIONAR AO ESTOQUE" : "REGISTRAR SAÍDA", 8, true, UiTheme.Accent));
        var productName = UiTheme.CreateLabel(_product.Name, 15, true);
        productName.AutoEllipsis = true;
        heading.Controls.Add(productName);
        root.Controls.Add(heading, 0, 0);
        var stockCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.SoftAccent, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 3, 0, 5) };
        _currentQuantity.Dock = DockStyle.Fill;
        _currentQuantity.TextAlign = ContentAlignment.MiddleLeft;
        stockCard.Controls.Add(_currentQuantity);
        root.Controls.Add(stockCard, 0, 1);
        AddField(root, _type == MovementType.Entrada ? "Quantidade a adicionar" : "Quantidade a retirar", _quantity, 2);
        AddField(root, "Motivo ou observação (opcional)", _notes, 3);
        root.Controls.Add(UiTheme.CreateLabel("A movimentação será registrada no histórico.", 9, false, UiTheme.Muted), 0, 4);
        _quantity.Text = "1";
        _quantity.MaxLength = 10;
        _notes.MaxLength = 1000;
        _notes.Multiline = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Height = 52;
        _quantity.KeyPress += IntegerKeyPress;
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        var save = UiTheme.CreateButton(_type == MovementType.Entrada ? "Confirmar entrada" : "Confirmar saída", true);
        var cancel = UiTheme.CreateButton("Cancelar");
        save.Click += SaveMovement;
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 5);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(TableLayoutPanel root, string label, Control input, int row)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var caption = UiTheme.CreateLabel(label, 9, true, UiTheme.Muted);
        caption.Location = new Point(0, 0);
        input.Location = new Point(0, 20);
        input.Width = 460;
        input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(caption);
        panel.Controls.Add(input);
        root.Controls.Add(panel, 0, row);
    }

    private void SaveMovement(object? sender, EventArgs e)
    {
        if (!int.TryParse(_quantity.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show(this, "Informe uma quantidade inteira maior que zero.", "Verifique a quantidade", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _quantity.Focus();
            return;
        }

        try
        {
            _stock.ChangeStock(_product.Id, _type, quantity, _notes.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(this, exception.Message, "Não foi possível registrar a movimentação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void IntegerKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            e.Handled = true;
    }
}
