# ResumenTPV

Herramienta de escritorio para **consultar la base Access** de **FactuSol / TPVSol** y ver en una sola pantalla, por fecha:

- **Vales creados**
- **Ventas por método de pago** (cobros)
- **Artículos vendidos** (líneas de facturación)

La lectura es **multiplataforma** (incl. macOS y Windows) **sin ODBC**: un **JRE** ejecuta **UCanAccess** (JDBC); el programa Java emite CSV por stdout y **Polars** lo carga en tablas (interfaz **Tkinter** en `ui.py`).

## Requisitos

- **Java** (JRE 17+ recomendado): en el `PATH`, `JAVA_HOME`, o carpeta `jre/` portátil junto al ejecutable (véase `PORTABLE.md`).
- **Python 3.13+** y dependencias (`uv sync`).

## JARs (`lib/`)

Los `.jar` de UCanAccess y dependencias van en **`lib/`** (ver `query_runner.py` para nombres exactos). Puedes obtenerlos desde [Maven Central](https://search.maven.org/) o el paquete de UCanAccess.

## Clase Java

En la raíz del proyecto, compilar `AccessReader.java` con el classpath adecuado (separador `:` o `;` según SO), como en versiones anteriores del README.

## Ejecutar la UI

```bash
uv sync
python ui.py
```

O consola: `python main.py`.

## Configuración

La ruta a la `.accdb` se guarda en **`resumentpv_config.json`** junto al proyecto o al ejecutable. Si venías de un fork antiguo, también se lee **`cierreangeles_config.json`** hasta que guardes de nuevo la ruta.

## Empaquetado portable

`uv sync --extra build` y `uv run pyinstaller ResumenTPV.spec` — detalles en **`PORTABLE.md`**.

## Build en GitHub Actions

En cada push a `main`/`master` o PR, se construye con PyInstaller en **Windows** y **macOS (ARM)** y se publica un **artefacto zip** (`.app` o carpeta `ResumenTPV` según plataforma). Requiere el script `scripts/fetch_maven_jars.py` y, para `ucanload`, el fichero `scripts/ucanload.jar.b64` en el repositorio.

Puede desactivar o ajustar el workflow en **`.github/workflows/build.yml`**.
