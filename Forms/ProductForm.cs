using ControleEstoque.Helpers;
using ControleEstoque.Models;
using ControleEstoque.Services;

namespace ControleEstoque.Forms;

public sealed class ProductForm : Form
{
    private readonly ProductService _products;
    private readonly Product? _product;
    private readonly TextBox _name = UiTheme.CreateTextBox();
    private readonly TextBox _code = UiTheme.CreateTextBox();
    private readonly TextBox _notes = UiTheme.CreateTextBox();
    private readonly TextBox _initialQuantity = UiTheme.CreateTextBox();

    public ProductForm(ProductService products, Product? product = null)
    {
        _products = products;
        _product = product;
        Text = product is null ? "Novo produto" : "Editar produto";
        MinimumSize = new Size(470, 410);
        ClientSize = new Size(540, product is null ? 480 : 450);
        UiTheme.StyleForm(this);
        BuildLayout();
        if (product is not null)
        {
            _name.Text = product.Name;
            _code.Text = product.Code ?? string.Empty;
            _notes.Text = product.Notes ?? string.Empty;
        }
        Shown += (_, _) => _name.Focus();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 18),
            ColumnCount = 1,
            RowCount = 7,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiTheme.Background, Margin = new Padding(0) };
        heading.Controls.Add(UiTheme.CreateLabel(_product is null ? "Novo produto" : "Editar produto", 16, true));
        heading.Controls.Add(UiTheme.CreateLabel(_product is null ? "Preencha as informações para cadastrar um item." : "Altere os dados sem modificar o saldo ou o histórico.", 9, false, UiTheme.Muted));
        root.Controls.Add(heading, 0, 0);
        AddField(root, "Nome do produto", _name, 1);
        AddField(root, "Código do produto (opcional)", _code, 2);
        AddField(root, "Observação (opcional)", _notes, 3);
        if (_product is null)
        {
            AddField(root, "Quantidade inicial", _initialQuantity, 4);
            _initialQuantity.Text = "0";
            _initialQuantity.KeyPress += IntegerKeyPress;
        }
        else
        {
            root.Controls.Add(UiTheme.CreateLabel($"Estoque atual: {_product.Quantity:N0} unidade(s). A edição não altera o estoque.", 9, false, UiTheme.Muted), 0, 4);
        }
        root.Controls.Add(UiTheme.CreateLabel("O código, quando informado, deve ser único.", 9, false, UiTheme.Muted), 0, 5);
        _name.MaxLength = 200;
        _code.MaxLength = 80;
        _notes.MaxLength = 1000;
        _notes.Multiline = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Height = 52;
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        var save = UiTheme.CreateButton("Salvar produto", true);
        var cancel = UiTheme.CreateButton("Cancelar");
        save.Click += SaveProduct;
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 6);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(TableLayoutPanel root, string label, Control input, int row)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var caption = UiTheme.CreateLabel(label, 9, true, UiTheme.Muted);
        caption.Location = new Point(0, 0);
        input.Location = new Point(0, 22);
        input.Width = 480;
        input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(caption);
        panel.Controls.Add(input);
        root.Controls.Add(panel, 0, row);
    }

    private void SaveProduct(object? sender, EventArgs e)
    {
        try
        {
            if (_product is null)
            {
                if (!int.TryParse(_initialQuantity.Text, out var quantity) || quantity < 0)
                {
                    MessageBox.Show(this, "Informe uma quantidade inicial inteira igual ou maior que zero.", "Verifique os dados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _initialQuantity.Focus();
                    return;
                }
                _products.Create(_name.Text, quantity, _code.Text, _notes.Text);
            }
            else
            {
                _products.Update(_product.Id, _name.Text, _code.Text, _notes.Text);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(this, exception.Message, "Não foi possível salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void IntegerKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            e.Handled = true;
    }
}
