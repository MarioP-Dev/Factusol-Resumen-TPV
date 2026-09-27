using System.Diagnostics;

namespace ResumenTPV;

/// <summary>
/// Fallback de desinstalación (si no se pudo lanzar la UI externa).
/// «Aceptar» solo aparece al terminar.
/// </summary>
public sealed class UninstallForm : Form
{
    private readonly string? _updateExe;
    private readonly PictureBox _logo = new();
    private readonly Label _title = new();
    private readonly Label _detail = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _acceptButton = new();

    public UninstallForm(string? updateExe)
    {
        _updateExe = updateExe;

        Text = "Desinstalar ResumenTPV";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 260);
        Font = new Font("Segoe UI", 9F);
        ControlBox = false;
        ShowInTaskbar = true;

        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
            {
                Icon = Icon.ExtractAssociatedIcon(exe);
            }
        }
        catch
        {
            // Sin icono de proceso.
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(28, 22, 28, 18),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.Size = new Size(140, 36);
        _logo.Margin = new Padding(0, 0, 0, 14);
        _logo.Image = AppTheme.LoadBrandLogo();
        root.Controls.Add(_logo, 0, 0);

        _title.Text = "Desinstalando ResumenTPV…";
        _title.Font = new Font(Font.FontFamily, 12.5F, FontStyle.Bold);
        _title.AutoSize = true;
        _title.Margin = new Padding(0, 0, 0, 8);
        root.Controls.Add(_title, 0, 1);

        _detail.Text = "Espere mientras se eliminan archivos, accesos directos y el registro.";
        _detail.AutoSize = true;
        _detail.MaximumSize = new Size(400, 0);
        _detail.Margin = new Padding(0, 0, 0, 16);
        root.Controls.Add(_detail, 0, 2);

        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 28;
        _progress.Dock = DockStyle.Top;
        _progress.Height = 22;
        _progress.Margin = new Padding(0, 0, 0, 18);
        root.Controls.Add(_progress, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0),
        };
        _acceptButton.Text = "Aceptar";
        _acceptButton.AutoSize = true;
        _acceptButton.Padding = new Padding(18, 7, 18, 7);
        _acceptButton.Enabled = false;
        _acceptButton.Visible = false;
        _acceptButton.Click += (_, _) => Close();
        buttons.Controls.Add(_acceptButton);
        root.Controls.Add(buttons, 0, 4);

        Controls.Add(root);
        AppTheme.Apply(this);

        Shown += async (_, _) => await RunUninstallAsync();
    }

    private async Task RunUninstallAsync()
    {
        if (string.IsNullOrWhiteSpace(_updateExe) || !File.Exists(_updateExe))
        {
            Finish(
                ok: false,
                title: "No se pudo desinstalar",
                detail: "No se encontró Update.exe. Esta copia no parece una instalación de Velopack.");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _updateExe,
                Arguments = "--uninstall --silent",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_updateExe) ?? Environment.CurrentDirectory,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                Finish(ok: false, title: "Error", detail: "No se pudo iniciar el desinstalador de Velopack.");
                return;
            }

            await process.WaitForExitAsync().ConfigureAwait(true);

            if (process.ExitCode != 0)
            {
                Finish(
                    ok: false,
                    title: "Desinstalación incompleta",
                    detail: $"La desinstalación terminó con código {process.ExitCode}. Revise el registro o vuelva a intentarlo.");
                return;
            }

            Finish(
                ok: true,
                title: "Desinstalación completada",
                detail: "ResumenTPV se ha eliminado correctamente de este equipo.");
        }
        catch (Exception ex)
        {
            Finish(ok: false, title: "Error al desinstalar", detail: ex.Message);
        }
    }

    private void Finish(bool ok, string title, string detail)
    {
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.MarqueeAnimationSpeed = 0;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Value = ok ? 100 : 0;
        _title.Text = title;
        _detail.Text = detail;
        _acceptButton.Visible = true;
        _acceptButton.Enabled = true;
        ControlBox = true;
        AcceptButton = _acceptButton;
        _acceptButton.Focus();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logo.Image?.Dispose();
        base.OnFormClosed(e);
    }
}
