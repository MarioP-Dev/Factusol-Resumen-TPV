# Distribución portable (USB / carpeta copiable)

## Qué incluye

- **Python + Polars + UI** empaquetados con PyInstaller (carpeta `dist/`, no requiere instalar Python en el PC de destino).
- **Configuración** (`resumentpv_config.json`) y **ruta a la base** guardadas en `app_dir()`: **al lado del ejecutable**, de modo que al copiar la carpeta se mantienen los ajustes.
- **Java (JRE)** no va dentro del build: o está instalado en el sistema, o descomprima un JRE portátil **junto al programa** (véase abajo).

## Generar el paquete

```bash
cd /ruta/al/clon/resumentpv
uv sync --extra build
uv run pyinstaller ResumenTPV.spec
```

- **macOS:** en `dist/` tendrá `ResumenTPV.app` (doble clic) y la carpeta `ResumenTPV/` con el binario y `_internal/`.
- **Windows:** genere en un PC Windows con el mismo comando; el `.spec` es multiplataforma (ajuste rutas si hace falta).

## Hacerlo “100 % portable” (Java sin instalar)

1. Descargue un **JRE 17+** en formato **archive** (zip) de [Adoptium](https://adoptium.net/) u otra distribución.
2. Descomprima y renombre la carpeta del JRE a **`jre`**, con `bin/java` (Linux/macOS) o `bin/java.exe` (Windows) dentro.

### Windows / Linux (onedir)

Coloque **`jre`** en la **misma carpeta** que el `.exe` (y `resumentpv_config.json` se guardará ahí también).

```text
MiCarpetaPortable/
  ResumenTPV.exe
  _internal/
  lib/
  datos/
  jre/
    bin/
      java.exe
  resumentpv_config.json
```

### macOS (`.app` en un USB)

Coloque **`jre`** en la **carpeta que contiene** `ResumenTPV.app` (no dentro del bundle), por ejemplo:

```text
MiUSB/
  ResumenTPV.app
  jre/
    bin/
      java
  resumentpv_config.json
```

Si instaló la app solo en `/Applications`, la configuración cae junto al binario dentro del `.app` (menos ideal); para un USB use la estructura anterior.

El programa busca Java en este orden: `jre/bin/java` → `JAVA_HOME` → `PATH`.

## Copiar a un USB

Copie **toda** la carpeta generada en `dist/` (o el `.app` en macOS más `jre` si aplica). No hace falta instalar nada más que el JRE portátil opcional.

## Tamaño

- El JRE suma ~50–100 MB (según versión).
- Polars y dependencias ya van en `_internal/`.

## Desarrollo local (sin empaquetar)

Sigue valiendo un `java` en el `PATH` o `JAVA_HOME`; la carpeta `jre/` junto al proyecto solo aplica si ejecuta el binario empaquetado o copia `jre` ahí para probar el mismo flujo que el USB.
