using System.Reflection;
using System.Text;

namespace ResumenTPV.Setup;

internal static class Embedded
{
    public static string ReadText(string logicalName)
    {
        var asm = typeof(Embedded).Assembly;
        using var stream = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Recurso no encontrado: {logicalName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static Image? ReadImage(string logicalName)
    {
        var asm = typeof(Embedded).Assembly;
        using var stream = asm.GetManifestResourceStream(logicalName);
        if (stream is null)
        {
            return null;
        }

        return Image.FromStream(stream);
    }

    public static bool HasVelopackSetup()
    {
        return typeof(Embedded).Assembly
            .GetManifestResourceNames()
            .Any(n => n.Equals("ResumenTPV.Setup.VelopackSetup.exe", StringComparison.Ordinal));
    }

    public static async Task ExtractVelopackSetupAsync(string destination, CancellationToken ct)
    {
        var asm = Assembly.GetExecutingAssembly();
        await using var stream = asm.GetManifestResourceStream("ResumenTPV.Setup.VelopackSetup.exe")
            ?? throw new InvalidOperationException(
                "No se incrustó el instalador Velopack. Ejecute scripts/pack.ps1 para generar el Setup completo.");
        await using var file = File.Create(destination);
        await stream.CopyToAsync(file, ct).ConfigureAwait(false);
    }
}
