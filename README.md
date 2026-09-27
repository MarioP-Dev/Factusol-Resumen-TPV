# ResumenTPV

Herramienta de escritorio **Windows** para consultar la base Access de **FactuSol / TPVSol** y ver en una sola pantalla, por fecha:

- **Vales creados**
- **Ventas por método de pago** (cobros)
- **Artículos vendidos** (líneas de facturación)

Lectura con **Microsoft ACE OLEDB** (sin Java ni ODBC externo de terceros). Interfaz **WinForms** (.NET 8).

> Rama `csharp-rewrite`: reescritura en C#. La versión Python + UCanAccess permanece en `main`.

## Requisitos (PC de tienda)

- **Windows** 10/11 (x64 recomendado)
- **.NET 8 Desktop Runtime** (o publique *self-contained*; véase abajo)
- **Microsoft Access Database Engine (ACE)** 2016 o posterior, misma arquitectura que la app (x64)

Descarga ACE (Microsoft): busque «Microsoft Access Database Engine Redistributable».

## Desarrollo

```bash
dotnet restore ResumenTPV.sln
dotnet build ResumenTPV.sln -c Release
```

Ejecutar **solo en Windows**:

```bash
dotnet run --project src/ResumenTPV -c Release
```

Desde macOS se puede restaurar y compilar el targeting Windows (`EnableWindowsTargeting` en `Directory.Build.props`), pero **no** se puede ejecutar ni probar OleDb/ACE.

## Configuración

La ruta al `.accdb` / `.mdb` se guarda en `resumentpv_config.json` junto al ejecutable. Si existía el legado `cierreangeles_config.json`, se lee hasta que se vuelva a guardar.

## Publicar portable (carpeta)

En un PC Windows (o en CI):

```bash
dotnet publish src/ResumenTPV -c Release -r win-x64 --self-contained true -o dist/ResumenTPV
```

Copie la carpeta `dist/ResumenTPV` al USB o al PC de destino. Sigue haciendo falta ACE instalado en el sistema (o empaquetarlo aparte).

## Build en GitHub Actions

Push a esta rama / `main`: job en **windows-latest** que publica el zip `ResumenTPV-Windows-x64`.
