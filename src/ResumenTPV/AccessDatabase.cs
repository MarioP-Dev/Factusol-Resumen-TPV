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

    private static string? _cachedProvider;

    public static DataTable Query(string databasePath, string sql)
    {
        using var conn = OpenConnection(databasePath);
        return Query(conn, sql);
    }

    /// <summary>Abre una conexión ACE/Jet reutilizable (el llamador debe disponerla).</summary>
    public static OleDbConnection OpenConnection(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"No se encuentra la base de datos:\n{fullPath}", fullPath);
        }

        Exception? lastError = null;
        var providers = _cachedProvider is null
            ? Providers
            : new[] { _cachedProvider }.Concat(Providers.Where(p => p != _cachedProvider));

        foreach (var provider in providers)
        {
            if (provider.Contains("Jet", StringComparison.OrdinalIgnoreCase)
                && !fullPath.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var conn = CreateOpenConnection(provider, fullPath);
                _cachedProvider = provider;
                return conn;
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

    public static DataTable Query(OleDbConnection conn, string sql)
    {
        using var cmd = new OleDbCommand(sql, conn);
        using var adapter = new OleDbDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    private static OleDbConnection CreateOpenConnection(string provider, string databasePath)
    {
        var cs = new OleDbConnectionStringBuilder
        {
            Provider = provider,
            DataSource = databasePath,
            PersistSecurityInfo = false,
        };

        var conn = new OleDbConnection(cs.ConnectionString);
        conn.Open();
        return conn;
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
