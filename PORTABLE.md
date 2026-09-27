# Instalación y distribución (Velopack)

ResumenTPV se distribuye como **asistente de instalación Windows** (`ResumenTPV-win-Setup.exe`) con actualizaciones OTA desde GitHub Releases. Ya no se recomienda copiar una carpeta portable a USB como método principal.

## Requisitos en el PC de destino

- Windows 10/11 x64
- **Microsoft Access Database Engine (ACE)** x64 (Office suele traerlo; si no, el redistribuible de Microsoft)
- El runtime .NET 8 va **incluido** en el paquete self-contained

## Instalar en tienda

1. Descargue `ResumenTPV-win-Setup.exe` del [último release](https://github.com/MarioP-Dev/Factusol-Resumen-TPV/releases/latest).
2. Ejecute el asistente: bienvenida → **aceptar términos** → opciones (acceso directo en escritorio) → instalar.
3. Abra ResumenTPV → Examinar → elija el `.accdb` / `.mdb` → «Usar y guardar».

La ruta de la base queda en `%LocalAppData%\MarioP.Dev\ResumenTPV\resumentpv_config.json` (aparte de la instalación, para no interferir con Velopack).

## Actualizaciones

- Automáticas al arrancar (aviso si hay versión nueva)
- Manuales: botón **«Buscar actualizaciones»**

Fuente: releases del repo público `MarioP-Dev/Factusol-Resumen-TPV` (API de GitHub, sin token).

## Generar el instalador en local

```powershell
dotnet tool install -g vpk --version 1.2.158
powershell -ExecutionPolicy Bypass -File scripts/pack.ps1
```

El script:

1. Publica la app (`dotnet publish`)
2. Empaqueta con Velopack (`vpk pack`, atajos solo en menú Inicio)
3. Compila el **asistente** (`ResumenTPV.Setup`) incrustando el Setup interno de Velopack
4. Deja en `releases/` el `ResumenTPV-win-Setup.exe` de distribución + nupkg OTA

Salida típica:

```text
releases/
  ResumenTPV-win-Setup.exe   ← asistente (términos + opciones)
  ResumenTPV-*-full.nupkg
  releases.win.json
```

Nota: el `Setup.exe` nativo de Velopack es one-click (solo splash). Los textos `--instWelcome` / `--instLicense` aplican al MSI de Velopack, no a ese exe; por eso el asistente propio muestra términos y el checkbox de escritorio.

Desinstalación: Windows abre `ResumenTPV.exe --uninstall-ui`, que lanza una UI en `%TEMP%` (PowerShell). Así sobrevive al cierre forzado de Velopack. Muestra progreso y solo habilita «Aceptar» al terminar `Update.exe --uninstall --silent`.

## CI

Al publicar un GitHub Release, el workflow `.github/workflows/build.yml` ejecuta `scripts/pack.ps1` y sube assets con `vpk upload github`.

## Nota sobre macOS

Esta app **no se ejecuta en macOS**. El desarrollo en Mac sirve para editar código; pruebas e instalador requieren Windows.
