#!/usr/bin/env python3
"""
Descarga en lib/ las dependencias Maven de UCanAccess (sin ucanload.jar, no publicado
en Central con ese artefacto). Uso: CI o clon limpio, antes de empaquetar.

Coloque ucanload.jar en lib/ a partir del zip oficial de UCanAccess (SourceForge)
o súbelo con el repositorio.
"""
from __future__ import annotations

import base64
import os
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LIB = ROOT / "lib"

ARTIFACTS: list[tuple[str, str]] = [
    (
        "ucanaccess-5.0.1.jar",
        "https://repo1.maven.org/maven2/net/sf/ucanaccess/ucanaccess/5.0.1/ucanaccess-5.0.1.jar",
    ),
    (
        "jackcess-3.0.1.jar",
        "https://repo1.maven.org/maven2/com/healthmarketscience/jackcess/jackcess/3.0.1/jackcess-3.0.1.jar",
    ),
    (
        "commons-lang3-3.8.1.jar",
        "https://repo1.maven.org/maven2/org/apache/commons/commons-lang3/3.8.1/commons-lang3-3.8.1.jar",
    ),
    (
        "hsqldb-2.5.0.jar",
        "https://repo1.maven.org/maven2/org/hsqldb/hsqldb/2.5.0/hsqldb-2.5.0.jar",
    ),
    (
        "commons-logging-1.2.jar",
        "https://repo1.maven.org/maven2/commons-logging/commons-logging/1.2/commons-logging-1.2.jar",
    ),
]


def main() -> None:
    LIB.mkdir(parents=True, exist_ok=True)
    for name, url in ARTIFACTS:
        dest = LIB / name
        if dest.is_file() and dest.stat().st_size > 0:
            print(f"OK (ya existe) {name}")
            continue
        print(f"Descargando {name}…")
        req = urllib.request.Request(url, headers={"User-Agent": "resumentpv-fetch-jars/1.0"})
        with urllib.request.urlopen(req, timeout=120) as r:  # noqa: S310
            data = r.read()
        if len(data) < 1000:
            raise RuntimeError(f"Respuesta inesperada al descargar {name!r} ({len(data)} bytes).")
        dest.write_bytes(data)
        print(f"  {len(data)} bytes")

    ucan = LIB / "ucanload.jar"
    if ucan.is_file() and ucan.stat().st_size > 0:
        print("OK (ya existe) ucanload.jar")
        return
    b64 = Path(__file__).resolve().parent / "ucanload.jar.b64"
    if b64.is_file():
        raw = b64.read_text().strip().split()
        data = base64.standard_b64decode("".join(raw).encode("ascii"))
        ucan.write_bytes(data)
        print(f"ucanload.jar extraído de {b64.name} ({len(data)} bytes).")
        return
    print(
        "Aviso: Falta lib/ucanload.jar. Cópielo del paquete binario de UCanAccess 5.x "
        "o añada scripts/ucanload.jar.b64 (vista en el repo) para CI.",
    )
    if os.environ.get("CI") == "true":
        raise SystemExit(1)


if __name__ == "__main__":
    main()
