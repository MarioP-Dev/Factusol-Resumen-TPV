using Velopack;
using Velopack.Sources;

namespace ResumenTPV;

/// <summary>
/// Comprobación e instalación de actualizaciones desde GitHub Releases (Velopack).
/// </summary>
public static class AppUpdates
{
    public const string PackId = "ResumenTPV";
    public const string GitHubRepoUrl = "https://github.com/MarioP-Dev/Factusol-Resumen-TPV";

    public static string DisplayVersion
    {
        get
        {
            try
            {
                var mgr = CreateManager();
                if (mgr.IsInstalled && mgr.CurrentVersion is not null)
                {
                    return mgr.CurrentVersion.ToString();
                }
            }
            catch
            {
                // Desarrollo / sin instalación Velopack.
            }

            return typeof(AppUpdates).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }
    }

    public static bool IsInstalled
    {
        get
        {
            try
            {
                return CreateManager().IsInstalled;
            }
            catch
            {
                return false;
            }
        }
    }

    public static UpdateManager CreateManager() =>
        new(new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false));

    /// <summary>
    /// Comprueba si hay actualización. Devuelve null si no hay, o el UpdateInfo si hay.
    /// Si la app no está instalada vía Velopack (p. ej. F5 en Visual Studio), devuelve null.
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        var mgr = CreateManager();
        if (!mgr.IsInstalled)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
    }

    public static async Task DownloadAndApplyAsync(
        UpdateInfo update,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var mgr = CreateManager();
        if (!mgr.IsInstalled)
        {
            throw new InvalidOperationException(
                "Esta copia no está instalada con el instalador de ResumenTPV. Instale ResumenTPV-win-Setup.exe desde GitHub Releases.");
        }

        await mgr.DownloadUpdatesAsync(update, progress, cancellationToken).ConfigureAwait(false);
        mgr.ApplyUpdatesAndRestart(update.TargetFullRelease);
    }
}
