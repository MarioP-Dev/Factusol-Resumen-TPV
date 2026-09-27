using System.Data;
using System.Globalization;
using ScottPlot.WinForms;

namespace ResumenTPV;

public sealed class MainForm : Form
{
    private readonly Label _dbPathLabel = new();
    private readonly DateTimePicker _datePicker = new();
    private readonly Button _loadButton = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripProgressBar _statusProgress = new();
    private StatusStrip? _statusStrip;
    private readonly ToolStripMenuItem _closeDbMenuItem = new();
    private readonly ToolStripMenuItem _updateMenuItem = new();

    private readonly Label _kpiValesValue = CreateKpiValue();
    private readonly Label _kpiCobradoValue = CreateKpiValue();
    private readonly Label _kpiArticulosValue = CreateKpiValue();
    private readonly Label _kpiImporteValue = CreateKpiValue();

    private readonly DataGridView _valesGrid = CreateGrid();
    private readonly DataGridView _articulosGrid = CreateGrid();
    private readonly Label _valesTitle = CreateSectionTitle("Vales creados");
    private readonly Label _pagosTitle = CreateSectionTitle("Cobros por método de pago");
    private readonly Label _articulosTitle = CreateSectionTitle("Artículos vendidos");

    private readonly FormsPlot _pagosChart = ChartPlots.CreatePlotControl();

    private string? _databasePath;
    private bool _loading;
    private bool _updating;

    public MainForm()
    {
        Text = $"ResumenTPV {AppUpdates.DisplayVersion} — FactuSol / TPVSol";
        MinimumSize = new Size(1100, 700);
        ClientSize = new Size(1280, 900);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        AppTheme.Apply(this);
        ChartPlots.ApplyTheme(_pagosChart);
        ChartPlots.Clear(_pagosChart, "Cargue datos para ver el gráfico");
        WireEvents();

        _databasePath = AppConfig.GetSavedDatabasePath();
        _datePicker.Value = DateTime.Today;
        RefreshDatabaseLabel();
        UpdateCloseDbMenuState();
        ResetKpis();

        if (string.IsNullOrWhiteSpace(_databasePath) || !File.Exists(_databasePath))
        {
            SetStatus("Elija la base Access con Archivo → Abrir FactuSol DB…");
            Shown += (_, _) => BeginSilentUpdateCheck();
        }
        else
        {
            SetStatus("Preparando carga de datos…");
            // BeginInvoke: pintar la ventana y el marquee antes de tocar ACE.
            Shown += (_, _) => BeginInvoke(BeginLoad);
        }    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // —— Cabecera compacta ——
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(16, 10, 16, 6),
        };

        var controlRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 4),
        };
        controlRow.Controls.Add(new Label
        {
            Text = "Fecha:",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
            Padding = new Padding(0, 6, 8, 0),
        });
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Width = 130;
        _datePicker.ShowUpDown = false;
        controlRow.Controls.Add(_datePicker);
        _loadButton.Text = "Cargar datos";
        _loadButton.AutoSize = true;
        controlRow.Controls.Add(_loadButton);
        header.Controls.Add(controlRow);

        _dbPathLabel.AutoSize = true;
        _dbPathLabel.Padding = new Padding(0, 2, 0, 4);
        header.Controls.Add(_dbPathLabel);
        root.Controls.Add(header, 0, 0);

        // —— KPIs ——
        var kpiRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(12, 0, 12, 8),
        };
        for (var i = 0; i < 4; i++)
        {
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        kpiRow.Controls.Add(CreateKpiCard("Vales", _kpiValesValue), 0, 0);
        kpiRow.Controls.Add(CreateKpiCard("Total cobrado", _kpiCobradoValue), 1, 0);
        kpiRow.Controls.Add(CreateKpiCard("Líneas de artículo", _kpiArticulosValue), 2, 0);
        kpiRow.Controls.Add(CreateKpiCard("Importe vendido", _kpiImporteValue), 3, 0);
        root.Controls.Add(kpiRow, 0, 1);

        // —— Contenido ——
        // Fila 1: Vales | Donut cobros
        // Fila 2: Artículos a ancho completo
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 0, 10, 4),
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        topRow.Controls.Add(CreateSection(_valesTitle, _valesGrid), 0, 0);
        topRow.Controls.Add(CreateChartSection(_pagosTitle, _pagosChart), 1, 0);

        content.Controls.Add(topRow, 0, 0);
        content.Controls.Add(CreateSection(_articulosTitle, _articulosGrid), 0, 1);
        root.Controls.Add(content, 0, 2);

        _statusStrip = new StatusStrip
        {
            SizingGrip = false,
            ShowItemToolTips = true,
        };
        _statusProgress.AutoSize = false;
        _statusProgress.Width = 160;
        _statusProgress.Alignment = ToolStripItemAlignment.Right;
        _statusProgress.Style = ProgressBarStyle.Marquee;
        _statusProgress.MarqueeAnimationSpeed = 0;
        // Visible siempre: ocultar/mostrar ToolStripProgressBar a veces no repinta el marquee.
        _statusProgress.Visible = true;
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_statusProgress);
        root.Controls.Add(_statusStrip, 0, 3);

        Controls.Add(root);
        var menu = BuildMenuStrip();
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private MenuStrip BuildMenuStrip()
    {
        var menu = new MenuStrip();

        var archivo = new ToolStripMenuItem("&Archivo");
        var abrir = new ToolStripMenuItem("Abrir FactuSol DB…")
        {
            ShortcutKeys = Keys.Control | Keys.O,
        };
        abrir.Click += (_, _) => BrowseDatabase();

        _closeDbMenuItem.Text = "Cerrar FactuSol DB actual";
        _closeDbMenuItem.Click += (_, _) => CloseDatabase();

        var salir = new ToolStripMenuItem("Salir")
        {
            ShortcutKeyDisplayString = "Alt+F4",
        };
        salir.Click += (_, _) => Close();

        archivo.DropDownItems.Add(abrir);
        archivo.DropDownItems.Add(_closeDbMenuItem);
        archivo.DropDownItems.Add(new ToolStripSeparator());
        archivo.DropDownItems.Add(salir);

        var ayuda = new ToolStripMenuItem("A&yuda");
        _updateMenuItem.Text = "Buscar actualizaciones";
        _updateMenuItem.Click += (_, _) => BeginUpdateCheck(interactive: true);
        var acerca = new ToolStripMenuItem("Acerca de ResumenTPV…");
        acerca.Click += (_, _) =>
        {
            using var dlg = new AboutForm();
            dlg.ShowDialog(this);
        };
        ayuda.DropDownItems.Add(_updateMenuItem);
        ayuda.DropDownItems.Add(new ToolStripSeparator());
        ayuda.DropDownItems.Add(acerca);

        menu.Items.Add(archivo);
        menu.Items.Add(ayuda);
        return menu;
    }

    private static Control CreateKpiCard(string title, Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(12, 10, 12, 10),
            MinimumSize = new Size(0, 72),
            Tag = "kpi",
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
        };
        stack.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            Margin = new Padding(0, 0, 0, 2),
        });
        stack.Controls.Add(valueLabel);
        card.Controls.Add(stack);
        return card;
    }

    private static Label CreateKpiValue() => new()
    {
        Text = "—",
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
        Margin = new Padding(0),
    };

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

    private static Control CreateChartSection(Label title, FormsPlot chart)
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
        chart.Dock = DockStyle.Fill;
        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(chart, 0, 1);
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
        BorderStyle = BorderStyle.FixedSingle,
    };

    private void WireEvents()
    {
        _loadButton.Click += (_, _) => BeginLoad();
        _datePicker.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BeginLoad();
            }
        };
    }

    private void BeginSilentUpdateCheck() => BeginUpdateCheck(interactive: false);

    private void BeginUpdateCheck(bool interactive)
    {
        if (_updating || _loading)
        {
            return;
        }

        _updating = true;
        if (interactive)
        {
            _updateMenuItem.Enabled = false;
            SetStatus("Buscando actualizaciones…");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var update = await AppUpdates.CheckAsync().ConfigureAwait(false);
                BeginInvoke(() => OnUpdateCheckFinished(update, interactive));
            }
            catch (Exception ex)
            {
                BeginInvoke(() => OnUpdateCheckError(ex, interactive));
            }
        });
    }

    private void OnUpdateCheckFinished(Velopack.UpdateInfo? update, bool interactive)
    {
        _updating = false;
        _updateMenuItem.Enabled = true;

        if (update is null)
        {
            if (interactive)
            {
                if (!AppUpdates.IsInstalled)
                {
                    MessageBox.Show(
                        this,
                        "Las actualizaciones automáticas solo funcionan con la app instalada vía ResumenTPV-win-Setup.exe (GitHub Releases).\n\n"
                        + "Esta copia parece ejecutarse en modo desarrollo o portable.",
                        "Actualizaciones",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        this,
                        "Ya tiene la última versión instalada.",
                        "Actualizaciones",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                SetStatus($"Versión {AppUpdates.DisplayVersion}.");
            }

            return;
        }

        var version = update.TargetFullRelease.Version?.ToString() ?? "?";
        var answer = MessageBox.Show(
            this,
            $"Hay una nueva versión disponible: {version}\n\n¿Descargar e instalar ahora? La aplicación se reiniciará.",
            "Actualización disponible",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
        {
            SetStatus($"Actualización {version} disponible. Use Ayuda → Buscar actualizaciones cuando quiera instalarla.");
            return;
        }

        BeginDownloadAndApply(update);
    }

    private void OnUpdateCheckError(Exception ex, bool interactive)
    {
        _updating = false;
        _updateMenuItem.Enabled = true;

        if (!interactive)
        {
            return;
        }

        MessageBox.Show(
            this,
            ex.Message,
            "No se pudo comprobar actualizaciones",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        SetStatus("No se pudo comprobar actualizaciones.");
    }

    private void BeginDownloadAndApply(Velopack.UpdateInfo update)
    {
        _updating = true;
        _updateMenuItem.Enabled = false;
        _loadButton.Enabled = false;
        SetStatus("Descargando actualización…");

        _ = Task.Run(async () =>
        {
            try
            {
                await AppUpdates.DownloadAndApplyAsync(
                    update,
                    progress =>
                    {
                        try
                        {
                            BeginInvoke(() => SetStatus($"Descargando actualización… {progress}%"));
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                BeginInvoke(() =>
                {
                    _updating = false;
                    _updateMenuItem.Enabled = true;
                    _loadButton.Enabled = true;
                    SetStatus("Error al actualizar.");
                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Error al actualizar",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                });
            }
        });
    }

    private void BrowseDatabase()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Abrir FactuSol DB",
            Filter = "Access (*.accdb;*.mdb)|*.accdb;*.mdb|Todos (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            AppConfig.SetDatabasePath(dlg.FileName);
            _databasePath = AppConfig.GetSavedDatabasePath() ?? dlg.FileName;
            RefreshDatabaseLabel();
            UpdateCloseDbMenuState();
            SetStatus($"Base abierta: {_databasePath}");
            BeginLoad();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No se pudo abrir la base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CloseDatabase()
    {
        if (_loading || _updating)
        {
            MessageBox.Show(
                this,
                "Espere a que termine la operación en curso.",
                "Cerrar base de datos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppConfig.ClearDatabasePath();
        _databasePath = null;
        ClearGrids();
        ResetKpis();
        ChartPlots.Clear(_pagosChart);
        RefreshDatabaseLabel();
        UpdateCloseDbMenuState();
        SetStatus("Base de datos cerrada. Use Archivo → Abrir FactuSol DB… para elegir otra.");
    }

    private void ClearGrids()
    {
        _valesGrid.DataSource = null;
        _articulosGrid.DataSource = null;
        _valesTitle.Text = "Vales creados";
        _pagosTitle.Text = "Cobros por método de pago";
        _articulosTitle.Text = "Artículos vendidos";
    }

    private void RefreshDatabaseLabel()
    {
        _dbPathLabel.Text = string.IsNullOrWhiteSpace(_databasePath)
            ? "Base de datos: (ninguna — Archivo → Abrir FactuSol DB…)"
            : $"Base de datos: {_databasePath}";
    }

    private void UpdateCloseDbMenuState()
    {
        _closeDbMenuItem.Enabled = !string.IsNullOrWhiteSpace(_databasePath);
    }

    private void BeginLoad()
    {
        if (_loading)
        {
            return;
        }

        var dbPath = _databasePath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
        {
            MessageBox.Show(
                this,
                "No hay una base de datos abierta.\n\nUse Archivo → Abrir FactuSol DB…",
                "Base de datos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var day = DateOnly.FromDateTime(_datePicker.Value.Date);

        _loading = true;
        SetLoadingUi(true);

        _ = LoadReportAsync(dbPath, day);
    }

    private async Task LoadReportAsync(string dbPath, DateOnly day)
    {
        try
        {
            // STA propio: evita que OleDb/COM congele el hilo de la UI.
            var report = await StaTask.Run(() =>
                ReportQueries.LoadDailyReport(dbPath, day, ReportStatus)).ConfigureAwait(true);

            OnLoadSuccess(day, report.Vales, report.Pagos, report.Articulos);
        }
        catch (Exception ex)
        {
            OnLoadError(ex);
        }
    }

    private void ReportStatus(string text)
    {
        try
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => SetStatus(text));
            }
            else
            {
                SetStatus(text);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void OnLoadSuccess(DateOnly day, DataTable vales, DataTable pagos, DataTable articulos)
    {
        BindGrid(_valesGrid, _valesTitle, "Vales creados", vales);
        _pagosTitle.Text = $"Cobros por método de pago ({pagos.Rows.Count})";
        BindGrid(_articulosGrid, _articulosTitle, "Artículos vendidos", articulos);
        UpdateKpis(vales, pagos, articulos);
        ChartPlots.ShowPaymentDonut(_pagosChart, pagos);
        SetLoadingUi(false);
        _loading = false;

        var totalCobrado = SumDecimal(pagos, "TOTAL_COBRADO");
        SetStatus(
            $"Listo. {day:yyyy-MM-dd} — {vales.Rows.Count} vales, cobrado {totalCobrado:N2} €, {articulos.Rows.Count} líneas de artículo");

        BeginSilentUpdateCheck();
    }

    private void OnLoadError(Exception ex)
    {
        SetLoadingUi(false);
        _loading = false;
        SetStatus("Error al cargar. Revise el mensaje o vuelva a intentar.");
        MessageBox.Show(this, ex.Message, "Error al cargar datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
        BeginSilentUpdateCheck();
    }

    private void UpdateKpis(DataTable vales, DataTable pagos, DataTable articulos)
    {
        var cobrado = SumDecimal(pagos, "TOTAL_COBRADO");
        var importe = SumDecimal(articulos, "IMPORTE_TOTAL");
        _kpiValesValue.Text = vales.Rows.Count.ToString("N0", CultureInfo.CurrentCulture);
        _kpiCobradoValue.Text = $"{cobrado:N2} €";
        _kpiArticulosValue.Text = articulos.Rows.Count.ToString("N0", CultureInfo.CurrentCulture);
        _kpiImporteValue.Text = $"{importe:N2} €";
    }

    private void ResetKpis()
    {
        _kpiValesValue.Text = "—";
        _kpiCobradoValue.Text = "—";
        _kpiArticulosValue.Text = "—";
        _kpiImporteValue.Text = "—";
    }

    private static decimal SumDecimal(DataTable table, string column)
    {
        decimal sum = 0;
        foreach (DataRow row in table.Rows)
        {
            if (row[column] is not DBNull)
            {
                sum += Convert.ToDecimal(row[column], CultureInfo.InvariantCulture);
            }
        }

        return sum;
    }

    private static void BindGrid(DataGridView grid, Label title, string baseTitle, DataTable table)
    {
        grid.DataSource = table;
        title.Text = $"{baseTitle} ({table.Rows.Count})";
    }

    private void SetLoadingUi(bool loading)
    {
        // No bloqueamos la ventana: solo evitamos dobles cargas y mostramos marquee abajo.
        _loadButton.Enabled = !loading && !_updating;
        _datePicker.Enabled = !loading;

        if (loading)
        {
            _statusProgress.Style = ProgressBarStyle.Marquee;
            _statusProgress.MarqueeAnimationSpeed = 30;
            SetStatus("Cargando datos…");
        }
        else
        {
            _statusProgress.MarqueeAnimationSpeed = 0;
            _statusProgress.Style = ProgressBarStyle.Continuous;
            _statusProgress.Value = 0;
        }

        _statusStrip?.Refresh();
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
