using System.Data;
using System.Globalization;
using ScottPlot;
using ScottPlot.WinForms;
using Color = System.Drawing.Color;

namespace ResumenTPV;

/// <summary>
/// Gráficos ScottPlot adaptados al tema de la app.
/// </summary>
public static class ChartPlots
{
    private static readonly ScottPlot.Color[] Palette =
    [
        new(0, 120, 215),
        new(16, 137, 62),
        new(232, 117, 0),
        new(180, 60, 160),
        new(0, 153, 168),
        new(200, 60, 60),
        new(100, 110, 200),
        new(120, 140, 40),
    ];

    public static FormsPlot CreatePlotControl()
    {
        var plot = new FormsPlot
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };
        ApplyTheme(plot);
        plot.Plot.Axes.Frameless();
        plot.Plot.HideGrid();
        plot.Refresh();
        return plot;
    }

    public static void ApplyTheme(FormsPlot formsPlot)
    {
        var c = AppTheme.Colors;
        var plot = formsPlot.Plot;
        plot.FigureBackground.Color = ToScott(c.Window);
        plot.DataBackground.Color = ToScott(c.Window);
        plot.Axes.Color(ToScott(c.MutedText));
        plot.Legend.BackgroundColor = ToScott(c.Menu);
        plot.Legend.FontColor = ToScott(c.Text);
        plot.Legend.OutlineColor = ToScott(c.Border);
        plot.Legend.FontSize = 21;
        formsPlot.BackColor = c.Window;
    }

    public static void ShowPaymentDonut(FormsPlot formsPlot, DataTable pagos)
    {
        ApplyTheme(formsPlot);
        var plot = formsPlot.Plot;
        plot.Clear();

        var slices = new List<PieSlice>();
        var i = 0;
        foreach (DataRow row in pagos.Rows)
        {
            var total = RowDecimal(row, "TOTAL_COBRADO");
            if (total <= 0)
            {
                continue;
            }

            var name = Convert.ToString(row["METODO_PAGO_NORMALIZADO"], CultureInfo.CurrentCulture) ?? "?";
            slices.Add(new PieSlice
            {
                Value = (double)total,
                LegendText = $"{name}  ·  {total:N2} €",
                Label = string.Empty,
                FillColor = Palette[i % Palette.Length],
            });
            i++;
        }

        if (slices.Count == 0)
        {
            plot.Axes.Frameless();
            plot.HideGrid();
            plot.Title("Sin cobros");
            plot.Axes.Title.Label.ForeColor = ToScott(AppTheme.Colors.MutedText);
            formsPlot.Refresh();
            return;
        }

        var pie = plot.Add.Pie(slices);
        // Agujero más pequeño => anillo más grueso y donut más presente visualmente.
        pie.DonutFraction = 0.35;
        pie.SliceLabelDistance = 0.45;
        pie.ExplodeFraction = 0;

        plot.Axes.Frameless();
        plot.HideGrid();
        // Leyenda a la derecha para liberar altura y agrandar el círculo.
        plot.ShowLegend(Alignment.MiddleRight);
        plot.Axes.SetLimits(-0.95, 0.95, -0.95, 0.95);
        formsPlot.Refresh();
    }

    public static void Clear(FormsPlot formsPlot, string message = "Sin datos")
    {
        ApplyTheme(formsPlot);
        formsPlot.Plot.Clear();
        formsPlot.Plot.Axes.Frameless();
        formsPlot.Plot.HideGrid();
        formsPlot.Plot.Title(message);
        formsPlot.Plot.Axes.Title.Label.ForeColor = ToScott(AppTheme.Colors.MutedText);
        formsPlot.Refresh();
    }

    private static decimal RowDecimal(DataRow row, string column)
        => row[column] is DBNull ? 0m : Convert.ToDecimal(row[column], CultureInfo.InvariantCulture);

    private static ScottPlot.Color ToScott(Color c) => new(c.R, c.G, c.B, c.A);
}
