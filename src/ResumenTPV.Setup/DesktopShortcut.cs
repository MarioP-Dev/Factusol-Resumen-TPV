using System.Runtime.InteropServices;

namespace ResumenTPV.Setup;

internal static class DesktopShortcut
{
    private const string PackId = "ResumenTPV";
    private const string ShortcutName = "ResumenTPV.lnk";

    public static string InstalledExePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            PackId,
            "ResumenTPV.exe");

    public static string DesktopLinkPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ShortcutName);

    public static void CreateOrReplace()
    {
        var target = InstalledExePath;
        if (!File.Exists(target))
        {
            // Stub Velopack a veces tarda un instante; también probar current\
            var current = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                PackId,
                "current",
                "ResumenTPV.exe");
            if (File.Exists(current))
            {
                target = current;
            }
            else
            {
                throw new FileNotFoundException("No se encontró ResumenTPV.exe tras la instalación.", target);
            }
        }

        // Preferir el stub estable en la raíz del pack (sobrevive a updates).
        var rootStub = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            PackId,
            "ResumenTPV.exe");
        if (File.Exists(rootStub))
        {
            target = rootStub;
        }

        CreateShortcut(DesktopLinkPath, target, "ResumenTPV — resumen diario FactuSol/TPVSol");
    }

    public static void RemoveIfExists()
    {
        try
        {
            if (File.Exists(DesktopLinkPath))
            {
                File.Delete(DesktopLinkPath);
            }
        }
        catch
        {
            // Mejor esfuerzo.
        }
    }

    private static void CreateShortcut(string linkPath, string targetPath, string description)
    {
        // IWshRuntimeLibrary vía COM sin referencia interop.
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("No se pudo crear el acceso directo (WScript.Shell).");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("No se pudo crear WScript.Shell.");
        try
        {
            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? "";
            shortcut.Description = description;
            shortcut.IconLocation = targetPath + ",0";
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
