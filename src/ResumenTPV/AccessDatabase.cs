using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Text;

namespace ResumenTPV;

/// <summary>
/// Lectura de bases Access (.accdb / .mdb) vía Microsoft ACE OLEDB (solo Windows).
/// </summary>
public static class AccessDatabase
{
    private static readonly string[] Providers =
    [
        "Microsoft.ACE.OLEDB.16.0",
        "Microsoft.ACE.OLEDB.12.0",
        "Microsoft.Jet.OLEDB.4.0",
    ];

    public static DataTable Query(string databasePath, string sql)
    {
        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"No se encuentra la base de datos:\n{fullPath}", fullPath);
        }

        Exception? lastError = null;
        foreach (var provider in Providers)
        {
            // Jet 4.0 solo aplica a .mdb antiguos.
            if (provider.Contains("Jet", StringComparison.OrdinalIgnoreCase)
                && !fullPath.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                return QueryWithProvider(provider, fullPath, sql);
            }
            catch (Exception ex) when (ex is OleDbException or InvalidOperationException)
            {
                lastError = ex;
            }
        }

        var hint = new StringBuilder();
        hint.AppendLine("No se pudo abrir la base Access con ACE/Jet OLEDB.");
        hint.AppendLine("Instale el redistributable «Microsoft Access Database Engine» (ACE) de 32 o 64 bits");
        hint.AppendLine("coincidente con la arquitectura de ResumenTPV (x64 recomendado).");
        if (lastError is not null)
        {
            hint.AppendLine();
            hint.Append("Detalle: ");
            hint.Append(lastError.Message);
        }

        throw new InvalidOperationException(hint.ToString(), lastError);
    }

    private static DataTable QueryWithProvider(string provider, string databasePath, string sql)
    {
        var cs = new OleDbConnectionStringBuilder
        {
            Provider = provider,
            DataSource = databasePath,
            PersistSecurityInfo = false,
        };

        using var conn = new OleDbConnection(cs.ConnectionString);
        conn.Open();
        using var cmd = new OleDbCommand(sql, conn);
        using var adapter = new OleDbDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    /// <summary>Filtro Inclusive/exclusive de un día calendario en sintaxis Access (#yyyy-MM-dd#).</summary>
    public static string AccessDateRangeSql(string column, DateOnly day)
    {
        var next = day.AddDays(1);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{column} >= #{day:yyyy-MM-dd}# AND {column} < #{next:yyyy-MM-dd}#");
    }
}
