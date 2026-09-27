using Velopack;

namespace ResumenTPV;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // UI propia de desinstalación (antes de hooks Velopack / MainForm).
        // Velopack mata procesos bajo la carpeta instalada: la UI corre en PowerShell (%TEMP%).
        if (args.Any(a => string.Equals(a, InstallIntegration.UninstallUiArg, StringComparison.OrdinalIgnoreCase)))
        {
            var updateExe = InstallIntegration.FindUpdateExe();
            if (updateExe is not null && InstallIntegration.LaunchExternalUninstallUi(updateExe))
            {
                return;
            }

            // Fallback si no hay instalación Velopack o falla el script.
            ApplicationConfiguration.Initialize();
            Application.Run(new UninstallForm(updateExe));
            return;
        }

        // Debe ir lo primero en arranques normales: hooks install/update/uninstall.
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => InstallIntegration.RegisterCustomUninstaller())
            .OnAfterUpdateFastCallback(_ => InstallIntegration.RegisterCustomUninstaller())
            .Run();

        ApplicationConfiguration.Initialize();

        // Por si Velopack reescribe la clave de desinstalación después del hook.
        if (AppUpdates.IsInstalled)
        {
            InstallIntegration.RegisterCustomUninstaller();
        }

        Application.Run(new MainForm());
    }
}
