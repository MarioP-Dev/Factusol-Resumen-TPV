using Velopack;

namespace ResumenTPV;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Debe ir lo primero: hooks de instalación / actualización / desinstalación.
        VelopackApp.Build().Run();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
