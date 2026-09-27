using Microsoft.Win32;
using System.Diagnostics;
using System.Text;

namespace ResumenTPV;

/// <summary>
/// Ajustes post-instalación: el desinstalador de Velopack es muy básico,
/// así que redirigimos «Agregar o quitar programas» a nuestra UI.
/// </summary>
public static class InstallIntegration
{
    private const string UninstallKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppUpdates.PackId;

    public const string UninstallUiArg = "--uninstall-ui";

    public static void RegisterCustomUninstaller()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                return;
            }

            using var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            var quoted = $"\"{exe}\" {UninstallUiArg}";
            key.SetValue("DisplayName", "ResumenTPV");
            key.SetValue("Publisher", "Mario P. Dev");
            key.SetValue("DisplayIcon", exe);
            key.SetValue("UninstallString", quoted);
            key.SetValue("QuietUninstallString", quoted);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
        catch
        {
            // No bloquear la instalación si falla el registro.
        }
    }

    public static string? FindUpdateExe()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = Directory.GetParent(baseDir)?.FullName;
        if (root is not null)
        {
            var update = Path.Combine(root, "Update.exe");
            if (File.Exists(update))
            {
                return update;
            }
        }

        var known = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppUpdates.PackId,
            "Update.exe");
        return File.Exists(known) ? known : null;
    }

    /// <summary>
    /// Lanza la UI de desinstalación fuera del pack (PowerShell en %TEMP%).
    /// Velopack mata cualquier proceso bajo la carpeta de instalación.
    /// </summary>
    public static bool LaunchExternalUninstallUi(string updateExe)
    {
        if (!File.Exists(updateExe))
        {
            return false;
        }

        try
        {
            var scriptPath = Path.Combine(
                Path.GetTempPath(),
                "ResumenTPV-uninstall-" + Guid.NewGuid().ToString("N") + ".ps1");
            File.WriteAllText(scriptPath, BuildUninstallScript(updateExe, scriptPath), Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    "-NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File \"" + scriptPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildUninstallScript(string updateExe, string scriptPath)
    {
        // Script autodisuasivo: muestra progreso, espera a Update.exe y solo entonces Aceptar.
        static string Esc(string s) => s.Replace("'", "''");

        var update = Esc(updateExe);
        var workDir = Esc(Path.GetDirectoryName(updateExe) ?? "");
        var self = Esc(scriptPath);

        return $$"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$updateExe = '{{update}}'
$workDir = '{{workDir}}'
$scriptPath = '{{self}}'

$form = New-Object System.Windows.Forms.Form
$form.Text = 'Desinstalar ResumenTPV'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.StartPosition = 'CenterScreen'
$form.ClientSize = New-Object System.Drawing.Size(440, 200)
$form.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$form.ControlBox = $false
$form.TopMost = $true

$title = New-Object System.Windows.Forms.Label
$title.Text = 'Desinstalando ResumenTPV…'
$title.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
$title.AutoSize = $true
$title.Location = New-Object System.Drawing.Point(24, 22)
$form.Controls.Add($title)

$detail = New-Object System.Windows.Forms.Label
$detail.Text = 'Espere mientras se eliminan archivos, accesos directos y el registro.'
$detail.AutoSize = $true
$detail.MaximumSize = New-Object System.Drawing.Size(390, 0)
$detail.Location = New-Object System.Drawing.Point(24, 58)
$form.Controls.Add($detail)

$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Style = 'Marquee'
$progress.MarqueeAnimationSpeed = 28
$progress.Location = New-Object System.Drawing.Point(24, 110)
$progress.Size = New-Object System.Drawing.Size(390, 22)
$form.Controls.Add($progress)

$accept = New-Object System.Windows.Forms.Button
$accept.Text = 'Aceptar'
$accept.AutoSize = $true
$accept.Enabled = $false
$accept.Visible = $false
$accept.Location = New-Object System.Drawing.Point(330, 150)
$accept.Add_Click({ $form.Close() })
$form.Controls.Add($accept)

$script:uninstallProc = $null
$script:pollTimer = $null
$form.Add_Shown({
  $form.Activate()
  try {
    $script:uninstallProc = Start-Process -FilePath $updateExe -ArgumentList '--uninstall','--silent' -WorkingDirectory $workDir -PassThru -WindowStyle Hidden
  } catch {
    $progress.Style = 'Continuous'
    $progress.MarqueeAnimationSpeed = 0
    $progress.Value = 0
    $title.Text = 'Error al desinstalar'
    $detail.Text = $_.Exception.Message
    $accept.Visible = $true
    $accept.Enabled = $true
    $form.ControlBox = $true
    return
  }

  $script:pollTimer = New-Object System.Windows.Forms.Timer
  $script:pollTimer.Interval = 250
  $script:pollTimer.Add_Tick({
    $p = $script:uninstallProc
    if ($null -eq $p) { return }
    if (-not $p.HasExited) { return }
    if ($null -ne $script:pollTimer) {
      $script:pollTimer.Stop()
      $script:pollTimer.Dispose()
      $script:pollTimer = $null
    }
    $code = $p.ExitCode
    $script:uninstallProc = $null
    $progress.Style = 'Continuous'
    $progress.MarqueeAnimationSpeed = 0
    $progress.Minimum = 0
    $progress.Maximum = 100
    if ($code -eq 0) {
      $progress.Value = 100
      $title.Text = 'Desinstalación completada'
      $detail.Text = 'ResumenTPV se ha eliminado correctamente de este equipo.'
    } else {
      $progress.Value = 0
      $title.Text = 'Desinstalación incompleta'
      $detail.Text = "La desinstalación terminó con código $code. Revise el registro o vuelva a intentarlo."
    }
    $accept.Visible = $true
    $accept.Enabled = $true
    $form.ControlBox = $true
    $form.AcceptButton = $accept
    $accept.Focus()
  })
  $script:pollTimer.Start()
})

$form.Add_FormClosed({
  Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
})

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::Run($form)
""";
    }
}
