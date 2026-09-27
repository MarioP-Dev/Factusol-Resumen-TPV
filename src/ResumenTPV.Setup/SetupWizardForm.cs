using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ResumenTPV.Setup;

public sealed class SetupWizardForm : Form
{
    private enum Step
    {
        Welcome,
        License,
        Options,
        Install,
        Done,
    }

    private Step _step = Step.Welcome;
    private bool _createDesktopShortcut = true;
    private bool _launchAfterInstall = true;
    private bool _installRunning;

    private readonly Panel _header = new();
    private readonly PictureBox _logo = new();
    private readonly Label _headerTitle = new();
    private readonly Label _headerSubtitle = new();
    private readonly Panel _body = new();
    private readonly Panel _footer = new();
    private readonly Button _backButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _cancelButton = new();

    // Welcome
    private readonly Label _welcomeText = new();

    // License
    private readonly TextBox _licenseBox = new();
    private readonly CheckBox _acceptLicense = new();

    // Options
    private readonly CheckBox _desktopCheck = new();
    private readonly CheckBox _startMenuInfo = new();
    private readonly Label _optionsHint = new();

    // Install / Done
    private readonly Label _statusTitle = new();
    private readonly Label _statusDetail = new();
    private readonly ProgressBar _progress = new();
    private readonly CheckBox _launchCheck = new();

    public SetupWizardForm()
    {
        Text = "Instalar ResumenTPV";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 420);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.White;

        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            {
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
            }
        }
        catch
        {
            // Sin icono.
        }

        BuildChrome();
        BuildPages();
        ShowStep(Step.Welcome);
    }

    private void BuildChrome()
    {
        _header.Dock = DockStyle.Top;
        _header.Height = 72;
        _header.Padding = new Padding(20, 12, 20, 12);
        _header.BackColor = Color.FromArgb(245, 247, 250);

        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.Size = new Size(120, 32);
        _logo.Location = new Point(20, 20);
        _logo.Image = Embedded.ReadImage("ResumenTPV.Setup.Logo_black.png");
        _header.Controls.Add(_logo);

        _headerTitle.AutoSize = true;
        _headerTitle.Font = new Font(Font.FontFamily, 12F, FontStyle.Bold);
        _headerTitle.Location = new Point(150, 14);
        _header.Controls.Add(_headerTitle);

        _headerSubtitle.AutoSize = true;
        _headerSubtitle.ForeColor = Color.FromArgb(90, 90, 90);
        _headerSubtitle.Location = new Point(150, 40);
        _header.Controls.Add(_headerSubtitle);

        _body.Dock = DockStyle.Fill;
        _body.Padding = new Padding(24, 16, 24, 8);

        _footer.Dock = DockStyle.Bottom;
        _footer.Height = 56;
        _footer.Padding = new Padding(16, 10, 16, 10);
        _footer.BackColor = Color.FromArgb(245, 247, 250);

        _cancelButton.Text = "Cancelar";
        _cancelButton.AutoSize = true;
        _cancelButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _cancelButton.Location = new Point(16, 12);
        _cancelButton.Click += (_, _) =>
        {
            if (_installRunning)
            {
                return;
            }

            if (MessageBox.Show(
                    this,
                    "¿Seguro que desea cancelar la instalación?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
        };
        _footer.Controls.Add(_cancelButton);

        _nextButton.Text = "Siguiente >";
        _nextButton.AutoSize = true;
        _nextButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _nextButton.Location = new Point(_footer.Width - 120, 12);
        _nextButton.Click += async (_, _) => await OnNextAsync();
        _footer.Controls.Add(_nextButton);

        _backButton.Text = "< Atrás";
        _backButton.AutoSize = true;
        _backButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _backButton.Location = new Point(_footer.Width - 220, 12);
        _backButton.Click += (_, _) => OnBack();
        _footer.Controls.Add(_backButton);

        _footer.Resize += (_, _) =>
        {
            _nextButton.Left = _footer.ClientSize.Width - _nextButton.Width - 16;
            _backButton.Left = _nextButton.Left - _backButton.Width - 8;
        };

        Controls.Add(_body);
        Controls.Add(_footer);
        Controls.Add(_header);
    }

    private void BuildPages()
    {
        _welcomeText.Dock = DockStyle.Fill;
        _welcomeText.AutoSize = false;
        try
        {
            _welcomeText.Text = Embedded.ReadText("ResumenTPV.Setup.WELCOME.txt").Trim()
                + "\n\nEste asistente instalará ResumenTPV en su perfil de usuario "
                + "(%LocalAppData%\\ResumenTPV) y creará un acceso en el menú Inicio.";
        }
        catch
        {
            _welcomeText.Text = "Bienvenido al instalador de ResumenTPV.";
        }

        _licenseBox.Multiline = true;
        _licenseBox.ReadOnly = true;
        _licenseBox.ScrollBars = ScrollBars.Vertical;
        _licenseBox.Dock = DockStyle.Fill;
        _licenseBox.Font = new Font("Consolas", 8.5F);
        _licenseBox.Text = StripMarkdown(Embedded.ReadText("ResumenTPV.Setup.TERMINOS.md"));

        _acceptLicense.Text = "He leído y acepto los términos y condiciones";
        _acceptLicense.AutoSize = true;
        _acceptLicense.Dock = DockStyle.Bottom;
        _acceptLicense.Padding = new Padding(0, 10, 0, 0);
        _acceptLicense.CheckedChanged += (_, _) => UpdateButtons();

        _desktopCheck.Text = "Crear un acceso directo en el escritorio";
        _desktopCheck.AutoSize = true;
        _desktopCheck.Checked = true;
        _desktopCheck.CheckedChanged += (_, _) => _createDesktopShortcut = _desktopCheck.Checked;

        _startMenuInfo.Text = "Acceso en el menú Inicio (recomendado)";
        _startMenuInfo.AutoSize = true;
        _startMenuInfo.Checked = true;
        _startMenuInfo.Enabled = false;

        _optionsHint.AutoSize = true;
        _optionsHint.MaximumSize = new Size(500, 0);
        _optionsHint.ForeColor = Color.FromArgb(90, 90, 90);
        _optionsHint.Text =
            "La instalación no requiere permisos de administrador. "
            + "Las actualizaciones automáticas se aplicarán sobre esta misma instalación.";

        _statusTitle.AutoSize = true;
        _statusTitle.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _statusDetail.AutoSize = true;
        _statusDetail.MaximumSize = new Size(500, 0);
        _statusDetail.ForeColor = Color.FromArgb(90, 90, 90);

        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 30;
        _progress.Height = 22;
        _progress.Dock = DockStyle.Top;

        _launchCheck.Text = "Abrir ResumenTPV al cerrar el asistente";
        _launchCheck.AutoSize = true;
        _launchCheck.Checked = true;
        _launchCheck.CheckedChanged += (_, _) => _launchAfterInstall = _launchCheck.Checked;
    }

    private void ShowStep(Step step)
    {
        _step = step;
        _body.Controls.Clear();

        switch (step)
        {
            case Step.Welcome:
                _headerTitle.Text = "Bienvenido";
                _headerSubtitle.Text = "Instalación de ResumenTPV";
                _body.Controls.Add(_welcomeText);
                break;

            case Step.License:
                _headerTitle.Text = "Términos y condiciones";
                _headerSubtitle.Text = "Debe aceptarlos para continuar";
                var licenseHost = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1,
                };
                licenseHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                licenseHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                licenseHost.Controls.Add(_licenseBox, 0, 0);
                licenseHost.Controls.Add(_acceptLicense, 0, 1);
                _body.Controls.Add(licenseHost);
                break;

            case Step.Options:
                _headerTitle.Text = "Opciones de instalación";
                _headerSubtitle.Text = "Elija los accesos que desea crear";
                var options = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Padding = new Padding(0, 8, 0, 0),
                };
                _desktopCheck.Margin = new Padding(0, 0, 0, 12);
                _startMenuInfo.Margin = new Padding(0, 0, 0, 20);
                options.Controls.Add(_desktopCheck);
                options.Controls.Add(_startMenuInfo);
                options.Controls.Add(_optionsHint);
                _body.Controls.Add(options);
                break;

            case Step.Install:
                _headerTitle.Text = "Instalando";
                _headerSubtitle.Text = "Espere mientras se copian los archivos";
                ShowStatusPanel(
                    "Instalando ResumenTPV…",
                    "No cierre esta ventana hasta que termine el proceso.",
                    showProgress: true,
                    showLaunch: false);
                break;

            case Step.Done:
                _headerTitle.Text = "Instalación completada";
                _headerSubtitle.Text = "ResumenTPV ya está listo en este equipo";
                ShowStatusPanel(
                    "La instalación finalizó correctamente.",
                    "Puede abrir la aplicación ahora o más tarde desde el menú Inicio.",
                    showProgress: false,
                    showLaunch: true);
                break;
        }

        UpdateButtons();
    }

    private void ShowStatusPanel(string title, string detail, bool showProgress, bool showLaunch)
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0, 12, 0, 0),
        };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _statusTitle.Text = title;
        _statusTitle.Margin = new Padding(0, 0, 0, 8);
        host.Controls.Add(_statusTitle, 0, 0);

        _statusDetail.Text = detail;
        _statusDetail.Margin = new Padding(0, 0, 0, 16);
        host.Controls.Add(_statusDetail, 0, 1);

        if (showProgress)
        {
            _progress.Style = ProgressBarStyle.Marquee;
            _progress.MarqueeAnimationSpeed = 30;
            _progress.Margin = new Padding(0, 0, 0, 12);
            host.Controls.Add(_progress, 0, 2);
        }

        if (showLaunch)
        {
            _launchCheck.Margin = new Padding(0, 8, 0, 0);
            host.Controls.Add(_launchCheck, 0, showProgress ? 3 : 2);
        }

        _body.Controls.Add(host);
    }

    private void UpdateButtons()
    {
        _backButton.Visible = _step is Step.License or Step.Options;
        _backButton.Enabled = !_installRunning;
        _cancelButton.Enabled = !_installRunning && _step != Step.Done;

        switch (_step)
        {
            case Step.Welcome:
                _nextButton.Text = "Siguiente >";
                _nextButton.Enabled = true;
                break;
            case Step.License:
                _nextButton.Text = "Siguiente >";
                _nextButton.Enabled = _acceptLicense.Checked;
                break;
            case Step.Options:
                _nextButton.Text = "Instalar";
                _nextButton.Enabled = true;
                break;
            case Step.Install:
                _nextButton.Text = "Instalar";
                _nextButton.Enabled = false;
                break;
            case Step.Done:
                _nextButton.Text = "Finalizar";
                _nextButton.Enabled = true;
                _cancelButton.Visible = false;
                break;
        }
    }

    private void OnBack()
    {
        ShowStep(_step switch
        {
            Step.License => Step.Welcome,
            Step.Options => Step.License,
            _ => _step,
        });
    }

    private async Task OnNextAsync()
    {
        switch (_step)
        {
            case Step.Welcome:
                ShowStep(Step.License);
                break;
            case Step.License:
                if (!_acceptLicense.Checked)
                {
                    return;
                }

                ShowStep(Step.Options);
                break;
            case Step.Options:
                await RunInstallAsync().ConfigureAwait(true);
                break;
            case Step.Done:
                if (_launchAfterInstall)
                {
                    TryLaunchApp();
                }

                Close();
                break;
        }
    }

    private async Task RunInstallAsync()
    {
        if (!Embedded.HasVelopackSetup())
        {
            MessageBox.Show(
                this,
                "Este asistente se compiló sin el paquete de instalación incrustado.\n"
                + "Genere el instalador con scripts\\pack.ps1.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _createDesktopShortcut = _desktopCheck.Checked;
        ShowStep(Step.Install);
        _installRunning = true;
        UpdateButtons();

        var tempDir = Path.Combine(Path.GetTempPath(), "ResumenTPV-setup-" + Guid.NewGuid().ToString("N"));
        var setupPath = Path.Combine(tempDir, "VelopackSetup.exe");

        try
        {
            Directory.CreateDirectory(tempDir);
            await Embedded.ExtractVelopackSetupAsync(setupPath, CancellationToken.None).ConfigureAwait(true);

            var psi = new ProcessStartInfo
            {
                FileName = setupPath,
                Arguments = "--silent",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempDir,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("No se pudo iniciar el instalador interno.");

            await process.WaitForExitAsync().ConfigureAwait(true);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"El instalador interno terminó con código {process.ExitCode}.");
            }

            // Esperar a que exista el stub (Velopack a veces finaliza un instante antes).
            await WaitForInstalledExeAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(true);

            if (_createDesktopShortcut)
            {
                DesktopShortcut.CreateOrReplace();
            }
            else
            {
                DesktopShortcut.RemoveIfExists();
            }

            _installRunning = false;
            ShowStep(Step.Done);
        }
        catch (Exception ex)
        {
            _installRunning = false;
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.MarqueeAnimationSpeed = 0;
            _progress.Value = 0;
            MessageBox.Show(
                this,
                "No se pudo completar la instalación:\n\n" + ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ShowStep(Step.Options);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // TEMP.
            }
        }
    }

    private static async Task WaitForInstalledExeAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (File.Exists(DesktopShortcut.InstalledExePath))
            {
                return;
            }

            await Task.Delay(200).ConfigureAwait(true);
        }
    }

    private static void TryLaunchApp()
    {
        try
        {
            var exe = DesktopShortcut.InstalledExePath;
            if (!File.Exists(exe))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
            });
        }
        catch
        {
            // El usuario puede abrirlo desde Inicio.
        }
    }

    private static string StripMarkdown(string md)
    {
        var text = md;
        text = Regex.Replace(text, @"^#+\s*", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = Regex.Replace(text, @"^\s*-\s+", "• ", RegexOptions.Multiline);
        return text.Trim();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_installRunning)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo.Image?.Dispose();
        }

        base.Dispose(disposing);
    }
}
