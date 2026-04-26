import io
import platform
import subprocess
import sys
from pathlib import Path

import polars as pl

from app_paths import bundle_dir, java_executable

# JAR y classpath deben resolverse respecto a los recursos empaquetados (PyInstaller).
ROOT = bundle_dir()


# JARs en la raíz del proyecto (nombres tras copiar/renombrar desde UCanAccess y dependencias).
JARS = (
    "lib/ucanaccess-5.0.1.jar",
    "lib/ucanload.jar",
    "lib/jackcess-3.0.1.jar",
    "lib/commons-lang3-3.8.1.jar",
    "lib/hsqldb-2.5.0.jar",
    "lib/commons-logging-1.2.jar",
)



def _classpath_separator() -> str:
    return ";" if platform.system() == "Windows" else ":"


def _build_classpath() -> str:
    sep = _classpath_separator()
    parts = [str(ROOT / name) for name in JARS] + [str(ROOT)]
    return sep.join(parts)


def run_query(db_path: str, query: str) -> pl.DataFrame:
    """Ejecuta una consulta SQL contra la .accdb/.mdb vía Java (UCanAccess) y devuelve un DataFrame de Polars."""
    classpath = _build_classpath()
    java_cmd = [
        str(java_executable()),
        "-cp",
        classpath,
        "AccessReader",
        str(Path(db_path).resolve()),
        query,
    ]
    result = subprocess.run(
        java_cmd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if result.returncode != 0:
        err = (result.stderr or "").strip() or "(stderr vacío)"
        print("Error al ejecutar Java (UCanAccess).", file=sys.stderr)
        print(err, file=sys.stderr)
        raise RuntimeError("Fallo del proceso Java; revisa el mensaje de stderr arriba.")

    csv_text = result.stdout
    if not csv_text or not csv_text.strip():
        return pl.DataFrame()
    try:
        return pl.read_csv(
            io.StringIO(csv_text),
            infer_schema_length=1000,
            raise_if_empty=False,
        )
    except Exception as exc:  # noqa: BLE001 - mensaje de usuario claro
        print(
            f"No se pudo interpretar la salida CSV: {type(exc).__name__}: {exc}",
            file=sys.stderr,
        )
        print("Primeros 500 caracteres de stdout:", file=sys.stderr)
        print(repr(csv_text[:500]), file=sys.stderr)
        raise



