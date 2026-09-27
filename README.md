# ResumenTPV

Herramienta de escritorio **Windows** para consultar la base Access de **FactuSol / TPVSol** y ver en una sola pantalla, por fecha:

- **Vales creados**
- **Ventas por método de pago** (cobros)
- **Artículos vendidos** (líneas de facturación)

Lectura con **Microsoft ACE OLEDB**. Interfaz **WinForms** (.NET 8). Distribución con **Velopack** (instalador + actualizaciones OTA desde GitHub Releases).

> Rama `csharp-rewrite`: reescritura en C#. La versión Python + UCanAccess permanece en `main`.

## Requisitos (PC de tienda)

- **Windows** 10/11 (x64)
- **Microsoft Access Database Engine (ACE)** 2016 o posterior, x64 (el instalador de la app ya incluye el runtime .NET)

Descarga ACE (Microsoft): busque «Microsoft Access Database Engine Redistributable».

## Instalar (tienda)

1. Abra [Releases](https://github.com/MarioP-Dev/Factusol-Resumen-TPV/releases/latest).
2. Descargue **`ResumenTPV-win-Setup.exe`** (o el `*Setup.exe` del release).
3. Ejecute el instalador.
4. Arranque ResumenTPV, elija el `.accdb` / `.mdb` y pulse «Usar y guardar».

La configuración se guarda en `%LocalAppData%\ResumenTPV\` (no se pierde al actualizar).

### Actualizaciones OTA

Con la app instalada, al arrancar comprueba GitHub Releases. También puede usar el botón **«Buscar actualizaciones»**. Si hay versión nueva, descarga, instala y reinicia sola.

## Desarrollo

```bash
dotnet restore ResumenTPV.sln
dotnet build ResumenTPV.sln -c Release
dotnet run --project src/ResumenTPV -c Release
```

Desde macOS se puede restaurar y compilar el targeting Windows (`EnableWindowsTargeting` en `Directory.Build.props`), pero **no** se puede ejecutar ni probar OleDb/ACE.

## Publicar un release (instalador + OTA)

1. Suba la `<Version>` en `src/ResumenTPV/ResumenTPV.csproj` (p. ej. `0.3.0`).
2. Haga commit y push.
3. En GitHub: **Releases → Draft a new release**, tag `v0.3.0` (debe coincidir con la versión del csproj), publique.
4. Actions empaqueta con Velopack y sube a ese release: `ResumenTPV-win-Setup.exe`, `*.nupkg`, `releases.win.json`.

Localmente (Windows, con .NET 8 y `vpk`):

```bash
dotnet tool install -g vpk --version 1.2.158
dotnet publish src/ResumenTPV -c Release -r win-x64 --self-contained true -o publish
vpk pack --packId ResumenTPV --packVersion 0.3.0 --packDir publish --mainExe ResumenTPV.exe --runtime win-x64 --outputDir releases
```

El instalador queda en `releases/ResumenTPV-win-Setup.exe`.

## Velopack

[Velopack](https://velopack.io/) es **open source (MIT)** y gratuito. Sustituye el modelo portable por instalador + updates.
