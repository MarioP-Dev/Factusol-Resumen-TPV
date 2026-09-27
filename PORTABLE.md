# Distribución portable (USB / carpeta copiable) — Windows

## Qué incluye

- App **.NET 8 WinForms** publicada como carpeta (`dotnet publish`, win-x64).
- Configuración (`resumentpv_config.json`) junto al `.exe`.
- **No** incluye el motor Access: en el PC de destino debe estar instalado **Microsoft Access Database Engine (ACE)** x64.

## Generar el paquete (Windows)

```bash
dotnet publish src/ResumenTPV -c Release -r win-x64 --self-contained true -o dist/ResumenTPV
```

Resultado típico:

```text
dist/ResumenTPV/
  ResumenTPV.exe
  *.dll
  resumentpv_config.json   # se crea al guardar la ruta de la base
```

## Copiar a un USB

Copie **toda** la carpeta `dist/ResumenTPV`. En el PC de la tienda:

1. Instale ACE x64 si aún no está (muchas máquinas con Office ya lo tienen).
2. Ejecute `ResumenTPV.exe`.
3. Elija el `.accdb` de FactuSol / TPVSol y pulse «Usar y guardar».

## Framework-dependent (más pequeño)

Si el PC ya tiene .NET 8 Desktop Runtime:

```bash
dotnet publish src/ResumenTPV -c Release -r win-x64 --self-contained false -o dist/ResumenTPV
```

## Nota sobre macOS

Esta app **no se ejecuta en macOS**. El desarrollo en Mac sirve para editar y compilar el targeting Windows; las pruebas reales requieren un PC o VM Windows.
