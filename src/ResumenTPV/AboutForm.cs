namespace ResumenTPV;

/// <summary>
/// Ventana «Acerca de» con marca, versión y datos de contacto.
/// </summary>
public sealed class AboutForm : Form
{
    private const string WebsiteUrl = "https://mariopdev.com";
    private const string ContactEmail = "hola@mariopdev.com";
    private const string AuthorName = "Mario P. Dev";
    private const string AuthorRole = "Data Engineer / Fullstack";
    private const string AuthorLocation = "Asturias, España";

    public AboutForm()
    {
        Text = "Acerca de ResumenTPV";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(540, 0);
        Font = new Font("Segoe UI", 9F);
        Padding = new Padding(28, 22, 28, 20);

        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Fill,
        };

        var logo = new PictureBox
        {
            Size = new Size(300, 64),
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.Transparent,
        };
        logo.Image = AppTheme.LoadBrandLogo();
        stack.Controls.Add(logo);

        stack.Controls.Add(new Label
        {
            Text = "ResumenTPV",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
        });
        stack.Controls.Add(new Label
        {
            Text = $"Versión {AppUpdates.DisplayVersion}",
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        });
        stack.Controls.Add(new Label
        {
            Text = "Resumen diario de vales, cobros y ventas · FactuSol / TPVSol",
            AutoSize = true,
            MaximumSize = new Size(480, 0),
            Margin = new Padding(0, 0, 0, 14),
        });

        var separator = new Panel
        {
            Height = 1,
            Width = 480,
            Margin = new Padding(0, 0, 0, 14),
        };
        stack.Controls.Add(separator);

        stack.Controls.Add(new Label
        {
            Text = "Desarrollado por",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 8.5F),
            Margin = new Padding(0, 0, 0, 2),
        });
        stack.Controls.Add(new Label
        {
            Text = AuthorName,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
        });
        stack.Controls.Add(new Label
        {
            Text = $"{AuthorRole} · {AuthorLocation}",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
        });

        stack.Controls.Add(CreateInfoRow("Web", WebsiteUrl, () => OpenUrl(WebsiteUrl)));
        stack.Controls.Add(CreateInfoRow("Email", ContactEmail, () => OpenUrl($"mailto:{ContactEmail}")));
        stack.Controls.Add(CreateInfoRow("Repositorio", AppUpdates.GitHubRepoUrl, () => OpenUrl(AppUpdates.GitHubRepoUrl)));

        stack.Controls.Add(new Label
        {
            Text = $"© {DateTime.Now.Year} {AuthorName}. Todos los derechos reservados.",
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 18),
        });

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        buttons.Controls.Add(CreateActionButton("GitHub…", () => OpenUrl(AppUpdates.GitHubRepoUrl)));
        buttons.Controls.Add(CreateActionButton("Web…", () => OpenUrl(WebsiteUrl)));
        buttons.Controls.Add(CreateActionButton("Email…", () => OpenUrl($"mailto:{ContactEmail}")));

        var ok = new Button
        {
            Text = "Aceptar",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Padding = new Padding(16, 6, 16, 6),
            Margin = new Padding(8, 0, 0, 0),
        };
        AcceptButton = ok;
        CancelButton = ok;
        buttons.Controls.Add(ok);
        stack.Controls.Add(buttons);

        Controls.Add(stack);
        AppTheme.Apply(this);
        logo.BackColor = Color.Transparent;
        separator.BackColor = AppTheme.Colors.Border;
        StyleLinkLabels(this);
    }

    private Control CreateInfoRow(string caption, string value, Action onClick)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 6),
        };

        row.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            MinimumSize = new Size(88, 0),
            Padding = new Padding(0, 2, 0, 0),
            Margin = new Padding(0),
        });

        var link = new LinkLabel
        {
            Text = value,
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(0),
            MaximumSize = new Size(380, 0),
        };
        link.LinkClicked += (_, _) => onClick();
        row.Controls.Add(link);
        return row;
    }

    private Button CreateActionButton(string text, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(0, 0, 8, 0),
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No se pudo abrir el enlace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void StyleLinkLabels(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is LinkLabel link)
            {
                link.LinkColor = AppTheme.IsDarkMode
                    ? Color.FromArgb(120, 180, 255)
                    : Color.FromArgb(0, 102, 204);
                link.ActiveLinkColor = AppTheme.Colors.Selection;
                link.VisitedLinkColor = link.LinkColor;
                link.BackColor = Color.Transparent;
            }

            StyleLinkLabels(child);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (Control c in Controls)
            {
                DisposeImages(c);
            }
        }

        base.Dispose(disposing);
    }

    private static void DisposeImages(Control control)
    {
        if (control is PictureBox pb && pb.Image is not null)
        {
            pb.Image.Dispose();
            pb.Image = null;
        }

        foreach (Control child in control.Controls)
        {
            DisposeImages(child);
        }
    }
}
