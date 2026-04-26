import json
from datetime import date, timedelta
from pathlib import Path

import polars as pl

from app_paths import app_dir, bundle_dir
from query_runner import run_query

# Raíz lógica del proyecto (desarrollo); con PyInstaller ver app_paths / bundle_dir.
ROOT = Path(__file__).resolve().parent

CONFIG_FILENAME = "resumentpv_config.json"
LEGACY_CONFIG_FILENAME = "cierreangeles_config.json"


def _config_path_for_read() -> Path:
    """Fichero de configuración: nuevo nombre, o legado si aún no se ha migrado."""
    ad = app_dir()
    primary = ad / CONFIG_FILENAME
    legacy = ad / LEGACY_CONFIG_FILENAME
    if primary.is_file():
        return primary
    if legacy.is_file():
        return legacy
    return primary


def _config_path_for_write() -> Path:
    return app_dir() / CONFIG_FILENAME


DEFAULT_DB_PATH = bundle_dir() / "datos" / "0022026.accdb"

# Compat: ruta por defecto empaquetada. Use get_db_path() para la ruta activa (incl. configuración).
DB_PATH = DEFAULT_DB_PATH


def _read_config() -> dict[str, object]:
    path = _config_path_for_read()
    if not path.is_file():
        return {}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}
    return data if isinstance(data, dict) else {}


def get_db_path() -> Path:
    """Ruta al .accdb/.mdb: configuración guardada, o por defecto bajo el proyecto."""
    data = _read_config()
    raw = data.get("database_path")
    if isinstance(raw, str) and raw.strip():
        p = Path(raw).expanduser().resolve()
        if p.is_file():
            return p
    return DEFAULT_DB_PATH


def set_database_path(path: Path) -> None:
    """
    Fija y persiste la base de datos. Escribe resumentpv_config.json
    bajo `app_dir()` (junto al .exe con PyInstaller, o la raíz del proyecto al desarrollar).
    """
    p = path.expanduser().resolve()
    if not p.is_file():
        raise FileNotFoundError(f"No existe o no es un fichero: {p}")
    out = _config_path_for_write()
    out.write_text(
        json.dumps({"database_path": str(p)}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def access_date_range_sql(column: str, day: date) -> str:
    next_day = day + timedelta(days=1)
    return (
        f"{column} >= #{day:%Y-%m-%d}# "
        f"AND {column} < #{next_day:%Y-%m-%d}#"
    )


def get_columns(table: str) -> list[str]:
    df = run_query(str(get_db_path()), f"SELECT * FROM {table} WHERE 1=0")
    return df.columns


def find_first_existing_column(table: str, candidates: list[str]) -> str:
    columns = set(get_columns(table))

    for candidate in candidates:
        if candidate in columns:
            return candidate

    raise ValueError(
        f"No se encontró ninguna columna válida en {table}. "
        f"Candidatas: {candidates}. Columnas reales: {sorted(columns)}"
    )


def get_vales_created_today(today: date) -> pl.DataFrame:
    query = f"""
        SELECT
            CODANT,
            FECANT,
            IMPANT,
            OBSANT
        FROM F_ANT
        WHERE {access_date_range_sql("FECANT", today)}
        ORDER BY CODANT
    """

    return run_query(str(get_db_path()), query)



def get_today_sales_by_payment_method(today: date) -> pl.DataFrame:
    query = f"""
        SELECT
            c.CPTCOB AS METODO_PAGO,
            COUNT(*) AS NUM_COBROS,
            SUM(c.IMPCOB) AS TOTAL_COBRADO
        FROM F_COB c
        WHERE {access_date_range_sql("c.FECCOB", today)}
        GROUP BY
            c.CPTCOB
        ORDER BY
            c.CPTCOB
    """

    return run_query(str(get_db_path()), query).with_columns(
        pl.when(pl.col("METODO_PAGO").str.starts_with("VALE"))
        .then(pl.lit("VALES"))
        .otherwise(pl.col("METODO_PAGO"))
        .alias("METODO_PAGO_NORMALIZADO")
    ).group_by("METODO_PAGO_NORMALIZADO").agg(
        pl.col("NUM_COBROS").sum(),
        pl.col("TOTAL_COBRADO").sum(),
    )


def get_today_sold_items(today: date) -> pl.DataFrame:
    query = f"""
        SELECT
            l.ARTLFA,
            l.DESLFA,
            l.CE1LFA AS TALLA,
            l.CE2LFA AS COLOR,
            SUM(l.CANLFA) AS CANTIDAD_TOTAL,
            SUM(l.TOTLFA) AS IMPORTE_TOTAL
        FROM F_FAC f
        INNER JOIN F_LFA l
            ON f.TIPFAC = l.TIPLFA
           AND f.CODFAC = l.CODLFA
        WHERE {access_date_range_sql("f.FECFAC", today)}
        GROUP BY
            l.ARTLFA,
            l.DESLFA,
            l.CE1LFA,
            l.CE2LFA,
            l.TCOLFA
        ORDER BY
            l.DESLFA,
            l.CE1LFA,
            l.CE2LFA
    """

    return run_query(str(get_db_path()), query)


if __name__ == "__main__":
    if not get_db_path().exists():
        raise FileNotFoundError(get_db_path())

    today = date.today()

    vales_hoy = get_vales_created_today(today)
    ventas_por_metodo_pago_hoy = get_today_sales_by_payment_method(today)
    articulos_vendidos_hoy = get_today_sold_items(today)

    print("\nVALES CREADOS HOY")
    print(vales_hoy)

    print("\nVENTAS DE HOY POR MÉTODO DE PAGO")
    print(ventas_por_metodo_pago_hoy)

    print("\nARTÍCULOS VENDIDOS HOY")
    print(articulos_vendidos_hoy)