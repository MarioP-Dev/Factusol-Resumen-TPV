using System.Text.Json;

namespace ResumenTPV;

/// <summary>
/// Persistencia de la ruta a la base Access en LocalAppData (sobrevive a actualizaciones Velopack).
/// </summary>
public static class AppConfig
{
    public const string ConfigFileName = "resumentpv_config.json";
    public const string LegacyConfigFileName = "cierreangeles_config.json";

    /// <summary>Carpeta del ejecutable (se reemplaza en cada update de Velopack).</summary>
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

    /// <summary>Datos de usuario que deben persistir entre versiones.</summary>
    public static string DataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppUpdates.PackId);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string ConfigPath => Path.Combine(DataDirectory, ConfigFileName);

    private static void MigrateLegacyConfigIfNeeded()
    {
        if (File.Exists(ConfigPath))
        {
            return;
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(AppDirectory, ConfigFileName),
                     Path.Combine(AppDirectory, LegacyConfigFileName),
                     Path.Combine(Environment.CurrentDirectory, ConfigFileName),
                     Path.Combine(Environment.CurrentDirectory, LegacyConfigFileName),
                 })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                File.Copy(candidate, ConfigPath, overwrite: false);
            }
            catch
            {
                // Si no se puede migrar, se pedirá la ruta al usuario.
            }

            return;
        }
    }

    public static string? GetSavedDatabasePath()
    {
        MigrateLegacyConfigIfNeeded();

        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
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

        Directory.CreateDirectory(DataDirectory);
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, string> { ["database_path"] = full },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, payload + Environment.NewLine);
    }

    public static void ClearDatabasePath()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                File.Delete(ConfigPath);
            }
        }
        catch (Exception)
        {
            // Si no se puede borrar, al menos la UI limpiará el estado en memoria.
        }
    }
}
