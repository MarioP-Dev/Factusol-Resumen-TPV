# -*- mode: python ; coding: utf-8 -*-
# Build (raíz del repo):
#   uv sync --extra build
#   uv run pyinstaller ResumenTPV.spec
#
# Salida: dist/ResumenTPV/ (onedir). En macOS también se genera ResumenTPV.app.
# Véase PORTABLE.md.

from __future__ import annotations

import platform
from pathlib import Path

from PyInstaller.utils.hooks import collect_all

root = Path(SPECPATH).resolve()

pl_datas, pl_binaries, pl_hidden = collect_all("polars")

a = Analysis(
    [str(root / "ui.py")],
    pathex=[str(root)],
    binaries=pl_binaries,
    datas=pl_datas
    + [
        (str(root / "lib"), "lib"),
        (str(root / "datos"), "datos"),
    ],
    hiddenimports=pl_hidden,
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)
exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="ResumenTPV",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=None,
)
coll = COLLECT(
    exe,
    a.binaries,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name="ResumenTPV",
)

if platform.system() == "Darwin":
    app = BUNDLE(
        coll,
        name="ResumenTPV.app",
        icon=None,
        bundle_identifier=None,
    )
