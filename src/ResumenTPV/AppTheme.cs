using Microsoft.Win32;

namespace ResumenTPV;

/// <summary>
/// Tema claro/oscuro según Windows (AppsUseLightTheme) y colores de la UI.
/// </summary>
public static class AppTheme
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsDarkMode
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public static ThemeColors Colors => IsDarkMode ? ThemeColors.Dark : ThemeColors.Light;

    public static Image? LoadBrandLogo()
    {
        var resourceName = IsDarkMode
            ? "ResumenTPV.Assets.Logo_white.png"
            : "ResumenTPV.Assets.Logo_black.png";

        try
        {
            var asm = typeof(AppTheme).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                return Image.FromStream(stream);
            }
        }
        catch
        {
            // Sin recurso: el llamador deja el control vacío.
        }

        return null;
    }

    /// <summary>
    /// Aplica colores de tema a un formulario y a todos sus controles hijos.
    /// </summary>
    public static void Apply(Control root)
    {
        var c = Colors;
        ApplyRecursive(root, c);
    }

    private static void PaintKpiBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
        {
            return;
        }

        using var pen = new Pen(Colors.Border);
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private static void ApplyRecursive(Control control, ThemeColors c)
    {
        switch (control)
        {
            case Form form:
                form.BackColor = c.Window;
                form.ForeColor = c.Text;
                break;
            case MenuStrip menu:
                ApplyToolStrip(menu, c);
                break;
            case StatusStrip status:
                ApplyToolStrip(status, c);
                break;
            case DataGridView grid:
                ApplyGrid(grid, c);
                break;
            case TextBox textBox:
                textBox.BackColor = c.Input;
                textBox.ForeColor = c.Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case Button button:
                if (IsDarkMode)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = c.Border;
                    button.BackColor = c.Button;
                    button.ForeColor = c.Text;
                    button.UseVisualStyleBackColor = false;
                }
                else
                {
                    button.FlatStyle = FlatStyle.System;
                    button.UseVisualStyleBackColor = true;
                }
                break;
            case LinkLabel link:
                link.BackColor = Color.Transparent;
                link.LinkColor = IsDarkMode
                    ? Color.FromArgb(120, 180, 255)
                    : Color.FromArgb(0, 102, 204);
                link.ActiveLinkColor = c.Selection;
                link.VisitedLinkColor = link.LinkColor;
                break;
            case Label label:
                // Separador fino (Height == 1): usar color de borde.
                if (label.Height <= 2 && label.BorderStyle != BorderStyle.None)
                {
                    label.BackColor = c.Border;
                    label.ForeColor = c.Border;
                    break;
                }

                label.ForeColor = ResolveLabelForeColor(label, c);
                label.BackColor = Color.Transparent;
                break;
            case ProgressBar:
                // ProgressBar no se recolore bien en WinForms clásico; se deja el sistema.
                break;
            case TableLayoutPanel or FlowLayoutPanel:
                control.BackColor = c.Window;
                control.ForeColor = c.Text;
                break;
            case Panel panel when Equals(panel.Tag, "kpi"):
                panel.BackColor = c.Menu;
                panel.ForeColor = c.Text;
                panel.Paint -= PaintKpiBorder;
                panel.Paint += PaintKpiBorder;
                break;
            case Panel panel:
                panel.BackColor = c.Window;
                panel.ForeColor = c.Text;
                break;
            case ScottPlot.WinForms.FormsPlot formsPlot:
                ChartPlots.ApplyTheme(formsPlot);
                break;
            default:
                control.BackColor = c.Window;
                control.ForeColor = c.Text;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child, c);
        }
    }

    private static Color ResolveLabelForeColor(Label label, ThemeColors c)
    {
        // Títulos de sección / título principal: texto principal.
        if (label.Font.Bold || label.Font.Size >= 14F)
        {
            return c.Text;
        }

        // Subtítulos / rutas / ayudas: texto secundario.
        return c.MutedText;
    }

    private static void ApplyToolStrip(ToolStrip strip, ThemeColors c)
    {
        strip.RenderMode = ToolStripRenderMode.Professional;
        strip.Renderer = new ThemeToolStripRenderer(c);
        strip.BackColor = c.Menu;
        strip.ForeColor = c.Text;
        ApplyToolStripItems(strip.Items, c);
    }

    private static void ApplyToolStripItems(ToolStripItemCollection items, ThemeColors c)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = c.Text;
            item.BackColor = c.Menu;
            if (item is ToolStripDropDownItem drop)
            {
                drop.DropDown.BackColor = c.Menu;
                drop.DropDown.ForeColor = c.Text;
                ApplyToolStripItems(drop.DropDownItems, c);
            }
        }
    }

    private static void ApplyGrid(DataGridView grid, ThemeColors c)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = c.GridBackground;
        grid.GridColor = c.GridLine;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = c.GridBackground,
            ForeColor = c.Text,
            SelectionBackColor = c.Selection,
            SelectionForeColor = c.SelectionText,
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = c.GridHeader,
            ForeColor = c.Text,
            SelectionBackColor = c.GridHeader,
            SelectionForeColor = c.Text,
        };
        grid.RowHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = c.GridHeader,
            ForeColor = c.Text,
        };
        grid.RowsDefaultCellStyle = grid.DefaultCellStyle;
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = c.GridAlternate,
            ForeColor = c.Text,
            SelectionBackColor = c.Selection,
            SelectionForeColor = c.SelectionText,
        };
    }

    private sealed class ThemeToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ThemeToolStripRenderer(ThemeColors colors)
            : base(new ThemeColorTable(colors))
        {
            RoundedEdges = false;
        }
    }

    private sealed class ThemeColorTable : ProfessionalColorTable
    {
        private readonly ThemeColors _c;

        public ThemeColorTable(ThemeColors colors) => _c = colors;

        public override Color MenuStripGradientBegin => _c.Menu;
        public override Color MenuStripGradientEnd => _c.Menu;
        public override Color MenuBorder => _c.Border;
        public override Color MenuItemBorder => _c.Border;
        public override Color MenuItemSelected => _c.Selection;
        public override Color MenuItemSelectedGradientBegin => _c.Selection;
        public override Color MenuItemSelectedGradientEnd => _c.Selection;
        public override Color MenuItemPressedGradientBegin => _c.Selection;
        public override Color MenuItemPressedGradientEnd => _c.Selection;
        public override Color ToolStripDropDownBackground => _c.Menu;
        public override Color ImageMarginGradientBegin => _c.Menu;
        public override Color ImageMarginGradientMiddle => _c.Menu;
        public override Color ImageMarginGradientEnd => _c.Menu;
        public override Color SeparatorDark => _c.Border;
        public override Color SeparatorLight => _c.Border;
        public override Color StatusStripGradientBegin => _c.Menu;
        public override Color StatusStripGradientEnd => _c.Menu;
    }
}

public readonly record struct ThemeColors(
    Color Window,
    Color Menu,
    Color Text,
    Color MutedText,
    Color Input,
    Color Button,
    Color Border,
    Color GridBackground,
    Color GridAlternate,
    Color GridHeader,
    Color GridLine,
    Color Selection,
    Color SelectionText)
{
    public static ThemeColors Light { get; } = new(
        Window: Color.FromArgb(250, 250, 250),
        Menu: Color.FromArgb(245, 245, 245),
        Text: Color.FromArgb(20, 20, 20),
        MutedText: Color.FromArgb(90, 90, 90),
        Input: Color.White,
        Button: Color.FromArgb(240, 240, 240),
        Border: Color.FromArgb(200, 200, 200),
        GridBackground: Color.White,
        GridAlternate: Color.FromArgb(245, 248, 252),
        GridHeader: Color.FromArgb(235, 235, 235),
        GridLine: Color.FromArgb(210, 210, 210),
        Selection: Color.FromArgb(0, 120, 215),
        SelectionText: Color.White);

    public static ThemeColors Dark { get; } = new(
        Window: Color.FromArgb(32, 32, 32),
        Menu: Color.FromArgb(45, 45, 45),
        Text: Color.FromArgb(240, 240, 240),
        MutedText: Color.FromArgb(170, 170, 170),
        Input: Color.FromArgb(50, 50, 50),
        Button: Color.FromArgb(55, 55, 55),
        Border: Color.FromArgb(70, 70, 70),
        GridBackground: Color.FromArgb(40, 40, 40),
        GridAlternate: Color.FromArgb(48, 48, 48),
        GridHeader: Color.FromArgb(55, 55, 55),
        GridLine: Color.FromArgb(70, 70, 70),
        Selection: Color.FromArgb(0, 120, 215),
        SelectionText: Color.White);
}
