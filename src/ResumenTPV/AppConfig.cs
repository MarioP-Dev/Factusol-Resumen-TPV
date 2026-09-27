using System.Text.Json;

namespace ResumenTPV;

/// <summary>
/// Persistencia de la ruta a la base Access junto al ejecutable (o legado junto al proyecto).
/// </summary>
public static class AppConfig
{
    public const string ConfigFileName = "resumentpv_config.json";
    public const string LegacyConfigFileName = "cierreangeles_config.json";

    public static string AppDirectory
    {
        get
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrEmpty(baseDir)
                ? Environment.CurrentDirectory
                : baseDir;
        }
    }

    private static string ConfigPathForRead()
    {
        var primary = Path.Combine(AppDirectory, ConfigFileName);
        if (File.Exists(primary))
        {
            return primary;
        }

        var legacy = Path.Combine(AppDirectory, LegacyConfigFileName);
        return File.Exists(legacy) ? legacy : primary;
    }

    private static string ConfigPathForWrite() => Path.Combine(AppDirectory, ConfigFileName);

    public static string? GetSavedDatabasePath()
    {
        var path = ConfigPathForRead();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("database_path", out var prop)
                && prop.ValueKind == JsonValueKind.String)
            {
                var raw = prop.GetString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(raw.Trim()));
                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Config ilegible: se ignora y se pide al usuario.
        }

        return null;
    }

    public static void SetDatabasePath(string databasePath)
    {
        var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(databasePath.Trim()));
        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"No existe o no es un fichero: {full}", full);
        }

        var payload = JsonSerializer.Serialize(
            new Dictionary<string, string> { ["database_path"] = full },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPathForWrite(), payload + Environment.NewLine);
    }
}
