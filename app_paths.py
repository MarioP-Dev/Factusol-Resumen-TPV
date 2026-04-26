"""
Rutas para ejecución normal y con PyInstaller (onefile / onefolder).

- app_dir: carpeta al lado del .exe o del intérprete (ficheros escribibles: resumentpv_config.json).
- bundle_dir: recursos del programa (JAR, datos/ embebidos), en onefile bajo _MEIPASS; en
  onefolder, junto al ejecutable; en desarrollo, raíz del proyecto.

PyInstaller: incluir recursos, por ejemplo (ajuste el entrypoint ``ui.py``)::

    # macOS / Linux
    pyinstaller --onefile --name ResumenTPV \\
      --add-data "lib:lib" --add-data "datos:datos" \\
      ui.py

    # Windows (el separador de --add-data es ``;``)
    pyinstaller --onefile --name ResumenTPV --add-data "lib;lib" --add-data "datos;datos" ui.py

Compruebe además `hiddenimports` / ``--collect-all`` según requiera Polars.
Para empaquetado y JRE portátil, véase ``PORTABLE.md``.
"""

from __future__ import annotations

import os
import platform
import shutil
import sys
import tempfile
from pathlib import Path


def is_frozen() -> bool:
    return bool(getattr(sys, "frozen", False))


def _is_dir_writable(path: Path) -> bool:
    """Comprueba escribibilidad real de directorio (no solo permisos teóricos)."""
    try:
        path.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(prefix=".rwtest-", dir=path, delete=True):
            pass
        return True
    except OSError:
        return False


def app_dir() -> Path:
    """Directorio escribible para config (junto al binario si es posible)."""
    if is_frozen():
        exe = Path(sys.executable).resolve()
        if sys.platform == "darwin":
            parts = exe.parts
            for i, seg in enumerate(parts):
                if seg.endswith(".app"):
                    bundle = Path(*parts[: i + 1])
                    parent = bundle.parent
                    # Si la app está fuera de /Applications y la carpeta es escribible, usarla.
                    if (
                        str(parent) not in ("/", "/Applications", "/System/Applications")
                        and _is_dir_writable(parent)
                    ):
                        return parent
                    break
            # App Translocation / .app en zona no escribible: usar App Support del usuario.
            fallback = Path.home() / "Library" / "Application Support" / "ResumenTPV"
            fallback.mkdir(parents=True, exist_ok=True)
            return fallback
        if _is_dir_writable(exe.parent):
            return exe.parent
        # Fallback genérico para instalaciones no escribibles.
        fallback = Path.home() / ".resumentpv"
        fallback.mkdir(parents=True, exist_ok=True)
        return fallback
    return Path(__file__).resolve().parent


def bundle_dir() -> Path:
    """
    Dónde están los JAR, datos por defecto, etc.

    - PyInstaller *onefile*: `sys._MEIPASS` (extracto temporales).
    - PyInstaller *onefolder*: misma carpeta que el .exe.
    - Desarrollo: raíz del repo (carpeta de `app_paths.py`).
    """
    if is_frozen() and (meipass := getattr(sys, "_MEIPASS", None)):
        return Path(meipass)
    if is_frozen():
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent


def java_executable() -> Path:
    """
    Ruta a ``java`` para UCanAccess. Orden:

    1. ``<app_dir>/jre/bin/java`` (JRE descomprimido junto al .exe, portable)
    2. ``JAVA_HOME/bin/java``
    3. ``java`` en el ``PATH`` del sistema

    En Windows se usa ``java.exe``.
    """
    is_win = platform.system() == "Windows"
    name = "java.exe" if is_win else "java"

    portable = app_dir() / "jre" / "bin" / name
    if portable.is_file():
        return portable

    jh = os.environ.get("JAVA_HOME")
    if jh:
        p = Path(jh) / "bin" / name
        if p.is_file():
            return p
        p = Path(jh) / name
        if p.is_file():
            return p

    w = shutil.which("java")
    if w:
        return Path(w)

    raise FileNotFoundError(
        "No se encontró Java. Opciones: instale un JRE (17+), defina JAVA_HOME, "
        "o coloque una carpeta 'jre' al lado del programa con el runtime portátil "
        "Adoptium/Eclipse (estructura jre/bin/java)."
    )
