using System.Data;
using System.Globalization;

namespace ResumenTPV;

public sealed class MainForm : Form
{
    private readonly TextBox _dbPathBox = new();
    private readonly TextBox _dateBox = new();
    private readonly Button _loadButton = new();
    private readonly ProgressBar _progress = new();
    private readonly ToolStripStatusLabel _statusLabel = new();

    private readonly DataGridView _valesGrid = CreateGrid();
    private readonly DataGridView _pagosGrid = CreateGrid();
    private readonly DataGridView _articulosGrid = CreateGrid();
    private readonly Label _valesTitle = CreateSectionTitle("Vales creados");
    private readonly Label _pagosTitle = CreateSectionTitle("Ventas por método de pago");
    private readonly Label _articulosTitle = CreateSectionTitle("Artículos vendidos");

    private bool _loading;

    public MainForm()
    {
        Text = "ResumenTPV — FactuSol / TPVSol";
        MinimumSize = new Size(900, 520);
        ClientSize = new Size(1150, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        WireEvents();

        var saved = AppConfig.GetSavedDatabasePath();
        _dbPathBox.Text = saved ?? string.Empty;
        _dateBox.Text = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(_dbPathBox.Text) || !File.Exists(_dbPathBox.Text))
        {
            SetStatus("Elija la base Access con «Examinar…» y «Usar y guardar».");
        }
        else
        {
            SetStatus("Listo. Pulse «Cargar datos» o Enter para actualizar.");
            Shown += (_, _) => BeginLoad();
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(16, 14, 16, 8),
        };

        header.Controls.Add(new Label
        {
            Text = "ResumenTPV",
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            AutoSize = true,
        });
        header.Controls.Add(new Label
        {
            Text = "Vales creados, ventas por método de pago y artículos vendidos (FactuSol / TPVSol · Access vía ACE OLEDB).",
            ForeColor = Color.Gray,
            AutoSize = true,
            Padding = new Padding(0, 2, 0, 6),
        });

        var dbRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(0, 0, 0, 6),
        };
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        dbRow.Controls.Add(new Label
        {
            Text = "Base de datos (.accdb / .mdb):",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 8, 0),
        }, 0, 0);

        _dbPathBox.Dock = DockStyle.Fill;
        dbRow.Controls.Add(_dbPathBox, 1, 0);

        var browseButton = new Button { Text = "Examinar…", AutoSize = true };
        browseButton.Click += (_, _) => BrowseDatabase();
        dbRow.Controls.Add(browseButton, 2, 0);

        var saveDbButton = new Button { Text = "Usar y guardar", AutoSize = true };
        saveDbButton.Click += (_, _) => ApplyDatabasePath();
        dbRow.Controls.Add(saveDbButton, 3, 0);
        header.Controls.Add(dbRow);

        var controlRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 4),
        };
        controlRow.Controls.Add(new Label
        {
            Text = "Fecha (AAAA-MM-DD):",
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
        });
        _dateBox.Width = 120;
        controlRow.Controls.Add(_dateBox);
        _loadButton.Text = "Cargar datos";
        _loadButton.AutoSize = true;
        controlRow.Controls.Add(_loadButton);
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 0;
        _progress.Width = 200;
        _progress.Height = 22;
        controlRow.Controls.Add(_progress);
        header.Controls.Add(controlRow);

        root.Controls.Add(header, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 0, 10, 4),
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
        content.Controls.Add(CreateSection(_valesTitle, _valesGrid), 0, 0);
        content.Controls.Add(CreateSection(_pagosTitle, _pagosGrid), 0, 1);
        content.Controls.Add(CreateSection(_articulosTitle, _articulosGrid), 0, 2);
        root.Controls.Add(content, 0, 1);

        var statusStrip = new StatusStrip();
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.Add(_statusLabel);
        root.Controls.Add(statusStrip, 0, 2);

        Controls.Add(root);
    }

    private static Control CreateSection(Label title, DataGridView grid)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        title.Dock = DockStyle.Fill;
        grid.Dock = DockStyle.Fill;
        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(grid, 0, 1);
        return panel;
    }

    private static Label CreateSectionTitle(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        AutoSize = true,
        Padding = new Padding(0, 0, 0, 4),
    };

    private static DataGridView CreateGrid() => new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private void WireEvents()
    {
        _loadButton.Click += (_, _) => BeginLoad();
        _dateBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BeginLoad();
            }
        };
    }

    private void BrowseDatabase()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Seleccionar base de datos Access",
            Filter = "Access (*.accdb;*.mdb)|*.accdb;*.mdb|Todos (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _dbPathBox.Text = dlg.FileName;
        ApplyDatabasePath();
    }

    private bool ApplyDatabasePath()
    {
        try
        {
            AppConfig.SetDatabasePath(_dbPathBox.Text);
            _dbPathBox.Text = AppConfig.GetSavedDatabasePath() ?? _dbPathBox.Text;
            SetStatus($"Ruta de base guardada. Se usará: {_dbPathBox.Text}");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No se pudo guardar la base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void BeginLoad()
    {
        if (_loading)
        {
            return;
        }

        var dbPath = _dbPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
        {
            MessageBox.Show(
                this,
                $"No se encuentra el fichero de base de datos:\n{dbPath}\n\nCompruebe la ruta o use «Examinar…» y «Usar y guardar».",
                "Base de datos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!DateOnly.TryParseExact(
                _dateBox.Text.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var day))
        {
            MessageBox.Show(
                this,
                "La fecha no es válida. Use el formato AAAA-MM-DD (ej. 2026-04-26).",
                "Fecha no válida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _loading = true;
        SetLoadingUi(true);

        _ = Task.Run(() =>
        {
            try
            {
                var vales = ReportQueries.GetValesCreated(dbPath, day);
                var pagos = ReportQueries.GetSalesByPaymentMethod(dbPath, day);
                var articulos = ReportQueries.GetSoldItems(dbPath, day);
                BeginInvoke(() => OnLoadSuccess(day, vales, pagos, articulos));
            }
            catch (Exception ex)
            {
                BeginInvoke(() => OnLoadError(ex));
            }
        });
    }

    private void OnLoadSuccess(DateOnly day, DataTable vales, DataTable pagos, DataTable articulos)
    {
        BindGrid(_valesGrid, _valesTitle, "Vales creados", vales);
        BindGrid(_pagosGrid, _pagosTitle, "Ventas por método de pago", pagos);
        BindGrid(_articulosGrid, _articulosTitle, "Artículos vendidos", articulos);
        SetLoadingUi(false);
        _loading = false;
        SetStatus(
            $"Listo. {day:yyyy-MM-dd} — {vales.Rows.Count} vales, {pagos.Rows.Count} filas (métodos de pago), {articulos.Rows.Count} filas (artículos)");
    }

    private void OnLoadError(Exception ex)
    {
        SetLoadingUi(false);
        _loading = false;
        SetStatus("Error al cargar. Revise el mensaje o vuelva a intentar.");
        MessageBox.Show(this, ex.Message, "Error al cargar datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void BindGrid(DataGridView grid, Label title, string baseTitle, DataTable table)
    {
        grid.DataSource = table;
        title.Text = $"{baseTitle} ({table.Rows.Count})";
    }

    private void SetLoadingUi(bool loading)
    {
        _loadButton.Enabled = !loading;
        _progress.MarqueeAnimationSpeed = loading ? 30 : 0;
        if (loading)
        {
            SetStatus("Cargando…");
        }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
