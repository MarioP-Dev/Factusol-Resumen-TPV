using System.Data;
using System.Globalization;

namespace ResumenTPV;

/// <summary>Consultas del resumen diario FactuSol / TPVSol.</summary>
public static class ReportQueries
{
    public static DataTable GetValesCreated(string databasePath, DateOnly day)
    {
        var sql = $"""
            SELECT
                CODANT,
                FECANT,
                IMPANT,
                OBSANT
            FROM F_ANT
            WHERE {AccessDatabase.AccessDateRangeSql("FECANT", day)}
            ORDER BY CODANT
            """;
        return AccessDatabase.Query(databasePath, sql);
    }

    public static DataTable GetSalesByPaymentMethod(string databasePath, DateOnly day)
    {
        var sql = $"""
            SELECT
                c.CPTCOB AS METODO_PAGO,
                COUNT(*) AS NUM_COBROS,
                SUM(c.IMPCOB) AS TOTAL_COBRADO
            FROM F_COB c
            WHERE {AccessDatabase.AccessDateRangeSql("c.FECCOB", day)}
            GROUP BY
                c.CPTCOB
            ORDER BY
                c.CPTCOB
            """;

        var raw = AccessDatabase.Query(databasePath, sql);
        return NormalizePaymentMethods(raw);
    }

    public static DataTable GetSoldItems(string databasePath, DateOnly day)
    {
        var sql = $"""
            SELECT
                l.ARTLFA,
                l.DESLFA,
                l.CE1LFA AS TALLA,
                l.CE2LFA AS COLOR,
                SUM(l.CANLFA) AS CANTIDAD_TOTAL,
                SUM(l.TOTLFA) AS IMPORTE_TOTAL
            FROM F_FAC f
            INNER JOIN F_LFA l
                ON f.TIPFAC = l.TIPLFA
               AND f.CODFAC = l.CODLFA
            WHERE {AccessDatabase.AccessDateRangeSql("f.FECFAC", day)}
            GROUP BY
                l.ARTLFA,
                l.DESLFA,
                l.CE1LFA,
                l.CE2LFA,
                l.TCOLFA
            ORDER BY
                l.DESLFA,
                l.CE1LFA,
                l.CE2LFA
            """;
        return AccessDatabase.Query(databasePath, sql);
    }

    /// <summary>Agrupa conceptos CPTCOB que empiezan por VALE bajo la etiqueta VALES.</summary>
    private static DataTable NormalizePaymentMethods(DataTable raw)
    {
        var result = new DataTable();
        result.Columns.Add("METODO_PAGO_NORMALIZADO", typeof(string));
        result.Columns.Add("NUM_COBROS", typeof(long));
        result.Columns.Add("TOTAL_COBRADO", typeof(decimal));

        var agg = new Dictionary<string, (long Count, decimal Total)>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in raw.Rows)
        {
            var method = Convert.ToString(row["METODO_PAGO"], CultureInfo.InvariantCulture) ?? string.Empty;
            if (method.StartsWith("VALE", StringComparison.OrdinalIgnoreCase))
            {
                method = "VALES";
            }

            var count = Convert.ToInt64(row["NUM_COBROS"], CultureInfo.InvariantCulture);
            var total = row["TOTAL_COBRADO"] is DBNull
                ? 0m
                : Convert.ToDecimal(row["TOTAL_COBRADO"], CultureInfo.InvariantCulture);

            if (agg.TryGetValue(method, out var existing))
            {
                agg[method] = (existing.Count + count, existing.Total + total);
            }
            else
            {
                agg[method] = (count, total);
            }
        }

        foreach (var kv in agg.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Rows.Add(kv.Key, kv.Value.Count, kv.Value.Total);
        }

        return result;
    }
}
