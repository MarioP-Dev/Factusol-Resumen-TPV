# Instalación y distribución (Velopack)

ResumenTPV se distribuye como **instalador Windows** (`Setup.exe`) con actualizaciones OTA desde GitHub Releases. Ya no se recomienda copiar una carpeta portable a USB como método principal.

## Requisitos en el PC de destino

- Windows 10/11 x64
- **Microsoft Access Database Engine (ACE)** x64 (Office suele traerlo; si no, el redistribuible de Microsoft)
- El runtime .NET 8 va **incluido** en el paquete self-contained

## Instalar en tienda

1. Descargue `ResumenTPV-win-Setup.exe` del [último release](https://github.com/MarioP-Dev/Factusol-Resumen-TPV/releases/latest).
2. Ejecute el instalador (instala en `%LocalAppData%\ResumenTPV\`).
3. Abra ResumenTPV → Examinar → elija el `.accdb` / `.mdb` → «Usar y guardar».

La ruta de la base queda en `%LocalAppData%\ResumenTPV\resumentpv_config.json` y **persiste** entre actualizaciones.

## Actualizaciones

- Automáticas al arrancar (aviso si hay versión nueva)
- Manuales: botón **«Buscar actualizaciones»**

Fuente: releases del repo público `MarioP-Dev/Factusol-Resumen-TPV` (API de GitHub, sin token).

## Generar el instalador en local

```bash
dotnet tool install -g vpk --version 1.2.158
dotnet publish src/ResumenTPV -c Release -r win-x64 --self-contained true -o publish
vpk pack --packId ResumenTPV --packVersion 0.3.0 --packDir publish --mainExe ResumenTPV.exe --packTitle ResumenTPV --runtime win-x64 --outputDir releases
```

Salida típica:

```text
releases/
  ResumenTPV-win-Setup.exe
  ResumenTPV-*-full.nupkg
  releases.win.json
```

## CI

Al publicar un GitHub Release, el workflow `.github/workflows/build.yml`:

1. Publica la app (`dotnet publish`)
2. Empaqueta con `vpk pack`
3. Sube los assets al release con `vpk upload github` (feed OTA incluido)

## Nota sobre macOS

Esta app **no se ejecuta en macOS**. El desarrollo en Mac sirve para editar código; pruebas e instalador requieren Windows.
