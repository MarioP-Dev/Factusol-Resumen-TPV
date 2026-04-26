from __future__ import annotations

import threading
import tkinter as tk
import tkinter.font as tkfont
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from tkinter import filedialog, messagebox, ttk
from typing import Final, TypedDict

import polars as pl

from main import (
    get_db_path,
    get_today_sold_items,
    get_today_sales_by_payment_method,
    get_vales_created_today,
    set_database_path,
)

_MAX_COL_PX: Final[int] = 420
_MIN_COL_PX: Final[int] = 64
_HEADER_EXTRA_PX: Final[int] = 28
_CELL_EXTRA_PX: Final[int] = 20


class _LoadingState(TypedDict):
    loading: bool


@dataclass(frozen=True, slots=True)
class DataPanel:
    """Bloque en pantalla: título, árbol, mensaje vacío; el título incluye el recuento de filas."""

    base_title: str
    body: ttk.Frame
    title_label: ttk.Label
    tree: ttk.Treeview
    empty_label: ttk.Label


def _cell_display(value: object) -> str:
    if value is None:
        return ""
    if isinstance(value, float) and value != value:  # NaN
        return ""
    return str(value)


def parse_date(value: str) -> date:
    """Valida y parsea una fecha en formato YYYY-MM-DD."""
    s = value.strip()
    if not s:
        raise ValueError("La fecha no puede estar vacía. Use el formato AAAA-MM-DD (ej. 2026-04-26).")
    try:
        return date.fromisoformat(s)
    except ValueError as exc:
        raise ValueError("La fecha no es válida. Use el formato AAAA-MM-DD (ej. 2026-04-26).") from exc


def _set_column_widths(tree: ttk.Treeview, df: pl.DataFrame) -> None:
    if len(df.columns) == 0:
        return
    font = tkfont.nametofont("TkDefaultFont")
    for col in df.columns:
        ch = str(col)
        max_px = font.measure(ch) + _HEADER_EXTRA_PX
        col_series = df[col]
        for val in col_series.head(40).to_list():
            t = _cell_display(val)
            max_px = max(max_px, font.measure(t) + _CELL_EXTRA_PX)
        w = int(min(_MAX_COL_PX, max(_MIN_COL_PX, max_px)))
        tree.column(col, width=w, minwidth=60, stretch=True, anchor="w")


def fill_table(
    panel: DataPanel,
    df: pl.DataFrame,
    no_columns_message: str = "No hay columnas: consulta sin esquema o resultado vacío.",
    empty_rows_message: str = "Sin datos para la fecha seleccionada",
) -> int:
    """
    Rellena el Treeview. Acepta DataFrames sin filas o sin columnas.
    Devuelve el número de filas insertadas.
    """
    tree = panel.tree
    el = panel.empty_label

    tree.delete(*tree.get_children())
    el.grid_remove()
    el.config(text="")

    cols = list(df.columns)
    if not cols:
        tree["columns"] = ()
        tree["show"] = "headings"
        el.config(text=no_columns_message)
        el.grid()
        panel.title_label.config(text=f"{panel.base_title} (0)")
        return 0

    tree["columns"] = tuple(cols)
    tree["show"] = "headings"
    for c in cols:
        tree.heading(c, text=str(c))
        tree.column(c, width=_MIN_COL_PX, anchor="w")
    _set_column_widths(tree, df)

    n = 0
    for row in df.iter_rows(named=True):
        r = {k: row[k] for k in cols}
        values: list[str] = [_cell_display(r[c]) for c in cols]
        tree.insert("", "end", values=values)
        n += 1

    if n == 0:
        el.config(text=empty_rows_message)
        el.grid()

    panel.title_label.config(text=f"{panel.base_title} ({n})")
    return n


def set_loading(
    root: tk.Misc,
    loading: bool,
    *,
    load_button: ttk.Button,
    progress: ttk.Progressbar,
    status_var: tk.StringVar,
) -> None:
    if loading:
        load_button.state(["disabled"])
        status_var.set("Cargando…")
        progress["mode"] = "indeterminate"
        progress.start(10)
    else:
        load_button.state(["!disabled"])
        try:
            progress.stop()
        except tk.TclError:
            pass
    root.update_idletasks()


def create_data_panel(parent: ttk.Misc, base_title: str) -> DataPanel:
    body = ttk.Frame(parent, padding=4)
    body.rowconfigure(2, weight=1)
    body.columnconfigure(0, weight=1)

    title_label = ttk.Label(
        body,
        text=base_title,
        font=("", 11, "bold"),
    )
    title_label.grid(row=0, column=0, sticky="w", pady=(0, 2))

    empty_lbl = ttk.Label(
        body,
        text="",
        foreground="gray",
        font=("", 9, "italic"),
    )
    empty_lbl.grid(row=1, column=0, sticky="w", pady=(0, 2))
    empty_lbl.grid_remove()

    table_wrap = ttk.Frame(body)
    table_wrap.grid(row=2, column=0, sticky="nsew", pady=2)
    table_wrap.rowconfigure(0, weight=1)
    table_wrap.columnconfigure(0, weight=1)

    tree = ttk.Treeview(table_wrap, show="headings", selectmode="browse")
    y_scroll = ttk.Scrollbar(table_wrap, orient=tk.VERTICAL, command=tree.yview)
    x_scroll = ttk.Scrollbar(table_wrap, orient=tk.HORIZONTAL, command=tree.xview)
    tree.configure(yscrollcommand=y_scroll.set, xscrollcommand=x_scroll.set)

    tree.grid(row=0, column=0, sticky="nsew")
    y_scroll.grid(row=0, column=1, sticky="ns")
    x_scroll.grid(row=1, column=0, sticky="ew")

    return DataPanel(
        base_title=base_title,
        body=body,
        title_label=title_label,
        tree=tree,
        empty_label=empty_lbl,
    )


def on_load_success(
    root: tk.Misc,
    dfs: tuple[pl.DataFrame, pl.DataFrame, pl.DataFrame],
    panels: tuple[DataPanel, DataPanel, DataPanel],
    target_date: date,
    load_button: ttk.Button,
    progress: ttk.Progressbar,
    status_var: tk.StringVar,
    loading_state: _LoadingState,
) -> None:
    vales_df, pagos_df, art_df = dfs
    vales_p, pagos_p, art_p = panels
    try:
        fill_table(vales_p, vales_df)
        fill_table(pagos_p, pagos_df)
        fill_table(art_p, art_df)
    except Exception as exc:  # noqa: BLE001
        on_load_error(
            root,
            exc,
            load_button,
            progress,
            status_var,
            loading_state,
        )
        return
    set_loading(root, False, load_button=load_button, progress=progress, status_var=status_var)
    loading_state["loading"] = False
    n_v, n_p, n_a = len(vales_df), len(pagos_df), len(art_df)
    status_var.set(
        f"Listo. {target_date:%Y-%m-%d} — {n_v} vales, {n_p} filas (métodos de pago), {n_a} filas (artículos)"
    )


def on_load_error(
    root: tk.Misc,
    exc: BaseException,
    load_button: ttk.Button,
    progress: ttk.Progressbar,
    status_var: tk.StringVar,
    loading_state: _LoadingState,
) -> None:
    set_loading(root, False, load_button=load_button, progress=progress, status_var=status_var)
    loading_state["loading"] = False
    status_var.set("Error al cargar. Revise el mensaje o vuelva a intentar.")
    messagebox.showerror("Error al cargar datos", str(exc))


def load_data_async(
    root: tk.Tk,
    target_date: date,
    *,
    panels: tuple[DataPanel, DataPanel, DataPanel],
    load_button: ttk.Button,
    progress: ttk.Progressbar,
    status_var: tk.StringVar,
    loading_state: _LoadingState,
) -> None:
    v_p, p_p, a_p = panels

    def worker() -> None:
        try:
            vales_df = get_vales_created_today(target_date)
            pagos_df = get_today_sales_by_payment_method(target_date)
            art_df = get_today_sold_items(target_date)
        except Exception as exc:  # noqa: BLE001
            err = exc
            root.after(
                0,
                lambda e=err: on_load_error(
                    root,
                    e,
                    load_button,
                    progress,
                    status_var,
                    loading_state,
                ),
            )
            return
        root.after(
            0,
            lambda: on_load_success(
                root,
                (vales_df, pagos_df, art_df),
                (v_p, p_p, a_p),
                target_date,
                load_button,
                progress,
                status_var,
                loading_state,
            ),
        )

    threading.Thread(target=worker, daemon=True).start()


def _apply_db_path(
    root: tk.Tk,
    path_str: str,
    *,
    status_var: tk.StringVar,
    db_path_var: tk.StringVar,
) -> bool:
    try:
        p = Path(path_str.strip()).expanduser().resolve()
    except OSError as exc:
        messagebox.showerror("Ruta no válida", str(exc), parent=root)
        return False
    try:
        set_database_path(p)
    except (OSError, FileNotFoundError) as exc:
        messagebox.showerror("No se pudo guardar la base de datos", str(exc), parent=root)
        return False
    db_path_var.set(str(get_db_path()))
    status_var.set(f"Ruta de base guardada. Se usará: {get_db_path()}")
    return True


def main() -> None:
    # Python 3.13+ exige un root existente antes de StringVar/Variable.
    root = tk.Tk()

    loading_state: _LoadingState = {"loading": False}
    date_var = tk.StringVar(master=root, value=date.today().isoformat())
    db_path_var = tk.StringVar(master=root, value=str(get_db_path()))
    status_var = tk.StringVar(
        master=root,
        value="Listo. Pulse «Cargar datos» o Enter para actualizar.",
    )

    root.title("ResumenTPV — FactuSol / TPVSol")
    root.minsize(900, 520)
    root.geometry("1150x820")

    style = ttk.Style()
    if "clam" in style.theme_names():
        style.theme_use("clam")

    header = ttk.Frame(root, padding=(16, 14, 16, 8))
    header.pack(side=tk.TOP, fill=tk.X)

    ttk.Label(header, text="ResumenTPV", font=("", 16, "bold")).pack(anchor=tk.W)
    ttk.Label(
        header,
        text="Vales creados, ventas por método de pago y artículos vendidos (FactuSol / TPVSol · Access vía UCanAccess).",
        foreground="gray",
    ).pack(anchor=tk.W, pady=(2, 4))

    db_row = ttk.Frame(header)
    db_row.pack(anchor=tk.W, fill=tk.X, pady=(0, 6))
    ttk.Label(db_row, text="Base de datos (.accdb / .mdb):").pack(side=tk.LEFT, padx=(0, 6))
    db_entry_wrap = ttk.Frame(db_row)
    db_entry_wrap.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 6))
    db_entry = ttk.Entry(db_entry_wrap, textvariable=db_path_var)
    db_entry.pack(side=tk.LEFT, fill=tk.X, expand=True)

    def apply_db() -> None:
        _apply_db_path(
            root,
            db_path_var.get(),
            status_var=status_var,
            db_path_var=db_path_var,
        )

    def browse_db() -> None:
        chosen = filedialog.askopenfilename(
            parent=root,
            title="Seleccionar base de datos Access",
            filetypes=(("Access", "*.accdb *.mdb"), ("Todos", "*.*")),
        )
        if chosen:
            db_path_var.set(chosen)
            apply_db()

    ttk.Button(db_row, text="Examinar…", command=browse_db).pack(side=tk.LEFT, padx=2)
    ttk.Button(db_row, text="Usar y guardar", command=apply_db).pack(side=tk.LEFT, padx=2)

    control = ttk.Frame(header)
    control.pack(anchor=tk.W, fill=tk.X)

    ttk.Label(control, text="Fecha (AAAA-MM-DD):").pack(side=tk.LEFT, padx=(0, 8))
    date_entry = ttk.Entry(control, textvariable=date_var, width=16)
    date_entry.pack(side=tk.LEFT, padx=(0, 8))

    load_button = ttk.Button(control, text="Cargar datos")
    load_button.pack(side=tk.LEFT, padx=(0, 8))

    progress = ttk.Progressbar(control, length=200, mode="indeterminate", maximum=100)
    progress.pack(side=tk.LEFT, padx=(0, 8))

    # Tres bloques en la misma ventana (mejor para capturar pantalla / impr. pantalla).
    content = ttk.PanedWindow(root, orient=tk.VERTICAL)
    content.pack(side=tk.TOP, fill=tk.BOTH, expand=True, padx=10, pady=(0, 4))

    vales_panel = create_data_panel(content, "Vales creados")
    pagos_panel = create_data_panel(content, "Ventas por método de pago")
    art_panel = create_data_panel(content, "Artículos vendidos")
    content.add(vales_panel.body, weight=1)
    content.add(pagos_panel.body, weight=1)
    content.add(art_panel.body, weight=1)
    all_panels: tuple[DataPanel, DataPanel, DataPanel] = (vales_panel, pagos_panel, art_panel)

    ttk.Label(
        root,
        textvariable=status_var,
        relief=tk.SUNKEN,
        anchor=tk.W,
        padding=(8, 5),
    ).pack(side=tk.BOTTOM, fill=tk.X)

    def do_load() -> None:
        if loading_state["loading"]:
            return
        if not get_db_path().is_file():
            messagebox.showerror(
                "Base de datos",
                f"No se encuentra el fichero de base de datos:\n{get_db_path()}\n\n"
                "Compruebe la ruta o use «Examinar…» y «Usar y guardar».",
                parent=root,
            )
            return
        try:
            d = parse_date(date_var.get())
        except ValueError as exc:
            messagebox.showerror("Fecha no válida", str(exc))
            return
        loading_state["loading"] = True
        set_loading(
            root,
            True,
            load_button=load_button,
            progress=progress,
            status_var=status_var,
        )
        load_data_async(
            root,
            d,
            panels=all_panels,
            load_button=load_button,
            progress=progress,
            status_var=status_var,
            loading_state=loading_state,
        )

    load_button.config(command=do_load)

    def on_return(_event: tk.Event) -> str:
        do_load()
        return "break"

    date_entry.bind("<Return>", on_return)

    if not get_db_path().is_file():
        status_var.set(
            f"La ruta de la base no existe: {get_db_path()}. Elija otra con «Examinar…» o «Usar y guardar»."
        )
    else:
        root.after_idle(do_load)

    root.mainloop()


if __name__ == "__main__":
    main()
