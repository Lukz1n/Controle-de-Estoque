using System.Drawing;
using System.Windows.Forms;

namespace ControleEstoque.Helpers;

public static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(244, 247, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(31, 45, 55);
    public static readonly Color Muted = Color.FromArgb(101, 117, 128);
    public static readonly Color Border = Color.FromArgb(222, 230, 235);
    public static readonly Color Accent = Color.FromArgb(24, 91, 108);
    public static readonly Color AccentHover = Color.FromArgb(17, 73, 88);
    public static readonly Color SoftAccent = Color.FromArgb(230, 241, 244);
    public static readonly Color Warning = Color.FromArgb(151, 94, 21);
    public static readonly Color WarningSurface = Color.FromArgb(250, 244, 232);
    public static readonly Color Danger = Color.FromArgb(163, 66, 66);
    public static readonly Color Success = Color.FromArgb(42, 118, 82);

    public static Button CreateButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(116, 42),
            Padding = new Padding(14, 2, 14, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Surface,
            ForeColor = primary ? Color.White : Text,
            Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(239, 244, 246);
        button.FlatAppearance.MouseDownBackColor = primary ? AccentHover : Color.FromArgb(229, 237, 240);
        return button;
    }

    public static Label CreateLabel(string text, float size = 10, bool bold = false, Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = color ?? Text,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            Margin = new Padding(0, 2, 0, 2)
        };
    }

    public static TextBox CreateTextBox()
    {
        return new TextBox
        {
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Surface,
            ForeColor = Text,
            Margin = new Padding(0, 4, 0, 8)
        };
    }

    public static void StyleForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 9.5F);
        form.StartPosition = FormStartPosition.CenterParent;
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.ShowIcon = false;
    }
}
