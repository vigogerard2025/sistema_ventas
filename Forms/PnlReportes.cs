using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClosedXML.Excel;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlReportes : UserControl
    {
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);
        private readonly Color colorExcel  = Color.FromArgb(33, 115, 70);

        public PnlReportes()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes() ;
            
        }

        private void InicializarComponentes()
        {
            
            var lbl = new Label
            {
                Text      = "📊  REPORTES Y FINANZAS",
                Font      = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorDorado,
                Location  = new Point(20, 15),
                AutoSize  = true
            };
            this.Controls.Add(lbl);

            var tabs = new TabControl { Location = new Point(15, 55), Size = new Size(1100, 560) };
            tabs.TabPages.Add(CrearTabVentasPeriodo());
            tabs.TabPages.Add(CrearTabTopProductos());
            tabs.TabPages.Add(CrearTabResumenFinanciero());
            tabs.TabPages.Add(CrearTabComprasPeriodo());
            this.Controls.Add(tabs);
        }

        // ─── Helpers UI ───────────────────────────────────────────────────────

        /// <summary>
        /// Agrega los filtros Desde / Hasta / Tipo de Comprobante.
        /// El ComboBox de tipo filtra por la tabla comprobantes (BOLETA/FACTURA),
        /// no por ventas.tipo_comprobante (ese campo no existe en la BD).
        /// </summary>
        private (DateTimePicker dtpDesde, DateTimePicker dtpHasta, ComboBox cmbTipo)
            AgregarFiltros(TabPage tab)
        {
            tab.Controls.Add(new Label { Text = "Desde:", Location = new Point(10, 18), AutoSize = true });
            var dtpDesde = new DateTimePicker
            {
                Location = new Point(65, 15), Size = new Size(130, 28),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30)
            };
            tab.Controls.Add(dtpDesde);

            tab.Controls.Add(new Label { Text = "Hasta:", Location = new Point(210, 18), AutoSize = true });
            var dtpHasta = new DateTimePicker
            {
                Location = new Point(265, 15), Size = new Size(130, 28),
                Format = DateTimePickerFormat.Short
            };
            tab.Controls.Add(dtpHasta);

            tab.Controls.Add(new Label { Text = "Comprobante:", Location = new Point(410, 18), AutoSize = true });
            var cmbTipo = new ComboBox
            {
                Location      = new Point(500, 14), Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // "Todos" incluye ventas sin comprobante y con cualquier tipo
            cmbTipo.Items.AddRange(new object[] { "Todos", "BOLETA", "FACTURA" });
            cmbTipo.SelectedIndex = 0;
            tab.Controls.Add(cmbTipo);

            return (dtpDesde, dtpHasta, cmbTipo);
        }

        private Button CrearBotonGenerar(TabPage tab, int x = 648)
        {
            var btn = new Button
            {
                Text      = "📊 Generar",
                Location  = new Point(x, 12), Size = new Size(110, 32),
                BackColor = colorBoton, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btn);
            return btn;
        }

        private Button CrearBotonExcel(TabPage tab, int x = 768)
        {
            var btn = new Button
            {
                Text      = "📥 Exportar Excel",
                Location  = new Point(x, 12), Size = new Size(145, 32),
                BackColor = colorExcel, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Enabled   = false
            };
            btn.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btn);
            return btn;
        }

        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView
            {
                Location = loc, Size = size,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            return g;
        }

        // ─── Tab 1: Ventas por Período ────────────────────────────────────────
        private TabPage CrearTabVentasPeriodo()
        {
            var tab = new TabPage("📅  Ventas por Período");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var (dtpDesde, dtpHasta, cmbTipo) = AgregarFiltros(tab);
            var btnGen   = CrearBotonGenerar(tab);
            var btnExcel = CrearBotonExcel(tab);

            var grid = CrearGrid(new Point(5, 55), new Size(1060, 390));
            grid.Columns.Add("fecha",    "Fecha");
            grid.Columns.Add("tipo",     "Tipo Comprobante");
            grid.Columns.Add("ventas",   "N° Ventas");
            grid.Columns.Add("subtotal", "Subtotal");
            grid.Columns.Add("igv",      "IGV");
            grid.Columns.Add("total",    "Total");
            tab.Controls.Add(grid);

            var lblTotal = new Label
            {
                Location  = new Point(5, 455), AutoSize = true,
                Font      = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = colorDorado
            };
            tab.Controls.Add(lblTotal);

            btnGen.Click += (s, e) =>
            {
                grid.Rows.Clear();
                btnExcel.Enabled = false;
                decimal gran = 0;

                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string tipoSel = cmbTipo.SelectedItem?.ToString() ?? "Todos";

                        // ✅ FIX: ventas no tiene tipo_comprobante.
                        // Se hace LEFT JOIN con comprobantes para obtener el tipo.
                        // Si no hay comprobante, se muestra "SIN COMPROBANTE".
                        string filtroTipo = tipoSel == "Todos"
                            ? ""
                            : "AND c.tipo = @tipo";

                        string sql = $@"
                            SELECT DATE(v.fecha),
                                   COALESCE(c.tipo, 'SIN COMPROBANTE') AS tipo_comp,
                                   COUNT(DISTINCT v.id),
                                   SUM(v.subtotal),
                                   SUM(v.igv),
                                   SUM(v.total)
                            FROM ventas v
                            LEFT JOIN comprobantes c
                                   ON c.venta_id = v.id AND c.estado <> 'ANULADO'
                            WHERE v.empresa_id = @eid
                              AND DATE(v.fecha) BETWEEN @d AND @h
                              {filtroTipo}
                            GROUP BY DATE(v.fecha), COALESCE(c.tipo, 'SIN COMPROBANTE')
                            ORDER BY DATE(v.fecha), tipo_comp";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                            cmd.Parameters.AddWithValue("d",   dtpDesde.Value.Date);
                            cmd.Parameters.AddWithValue("h",   dtpHasta.Value.Date);
                            if (filtroTipo != "")
                                cmd.Parameters.AddWithValue("tipo", tipoSel);

                            using (var dr = cmd.ExecuteReader())
                                while (dr.Read())
                                {
                                    decimal t = dr.GetDecimal(5);
                                    gran += t;
                                    grid.Rows.Add(
                                        dr.GetDateTime(0).ToString("dd/MM/yyyy"),
                                        dr.GetString(1),
                                        dr.GetInt64(2),
                                        "S/ " + dr.GetDecimal(3).ToString("N2"),
                                        "S/ " + dr.GetDecimal(4).ToString("N2"),
                                        "S/ " + t.ToString("N2"));
                                }
                        }
                    }
                    lblTotal.Text    = $"TOTAL GENERAL: S/ {gran:N2}";
                    btnExcel.Enabled = grid.Rows.Count > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar reporte:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnExcel.Click += (s, e) =>
                ExportarGridAExcel(grid, "Ventas_Periodo",
                    new[] { "Fecha", "Tipo Comprobante", "N° Ventas", "Subtotal", "IGV", "Total" },
                    $"Ventas por Período  |  {dtpDesde.Value:dd/MM/yyyy} – {dtpHasta.Value:dd/MM/yyyy}  |  Tipo: {cmbTipo.SelectedItem}");

            return tab;
        }
        // ─── Tab 4: Compras por Período ─────────────────────────────────────────
private TabPage CrearTabComprasPeriodo()
{
    var tab = new TabPage("🛒  Compras por Período");
    tab.BackColor = Color.FromArgb(245, 240, 228);

    var (dtpDesde, dtpHasta, _) = AgregarFiltros(tab);

    var btnGen   = CrearBotonGenerar(tab);
    var btnExcel = CrearBotonExcel(tab);

    var grid = CrearGrid(new Point(5, 55), new Size(1060, 390));
    grid.Columns.Add("fecha",     "Fecha");
    grid.Columns.Add("proveedor", "Proveedor");
    grid.Columns.Add("ncompra",   "N° Compras");
    grid.Columns.Add("subtotal",  "Subtotal");
    grid.Columns.Add("igv",       "IGV");
    grid.Columns.Add("total",     "Total");

    tab.Controls.Add(grid);

    var lblTotal = new Label
    {
        Location  = new Point(5, 455),
        AutoSize  = true,
        Font      = new Font("Arial", 11, FontStyle.Bold),
        ForeColor = colorDorado
    };
    tab.Controls.Add(lblTotal);

    btnGen.Click += (s, e) =>
    {
        grid.Rows.Clear();
        btnExcel.Enabled = false;
        decimal granTotal = 0;

        try
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

            string sql = @"
    SELECT DATE(c.fecha),
           COALESCE(p.nombre, 'SIN PROVEEDOR'),
           COUNT(DISTINCT c.id),
           SUM(c.subtotal),
           SUM(c.igv),
           SUM(c.total)
    FROM compras c
    LEFT JOIN proveedores p ON c.proveedor_id = p.id
    WHERE DATE(c.fecha) BETWEEN @d AND @h
    GROUP BY DATE(c.fecha), COALESCE(p.nombre,'SIN PROVEEDOR')
    ORDER BY DATE(c.fecha) DESC";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("d", dtpDesde.Value.Date);
                    cmd.Parameters.AddWithValue("h", dtpHasta.Value.Date);

                    using (var dr = cmd.ExecuteReader())
                    {
                      while (dr.Read())
{
    decimal total = dr.GetDecimal(5);
    granTotal += total;

    grid.Rows.Add(
        dr.GetDateTime(0).ToString("dd/MM/yyyy"),
        dr.GetString(1),                         // proveedor (texto)
        dr.GetInt64(2),
        "S/ " + dr.GetDecimal(3).ToString("N2"),
        "S/ " + dr.GetDecimal(4).ToString("N2"),
        "S/ " + total.ToString("N2")
    );
}
                    }
                }
            }

            lblTotal.Text = $"TOTAL GENERAL COMPRAS: S/ {granTotal:N2}";
            btnExcel.Enabled = grid.Rows.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al generar reporte de compras:\n" + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    };

    btnExcel.Click += (s, e) =>
        ExportarGridAExcel(
            grid,
            "Compras_Periodo",
            new[] { "Fecha", "Proveedor", "N° Compras", "Subtotal", "IGV", "Total" },
            $"Compras por Período  |  {dtpDesde.Value:dd/MM/yyyy} – {dtpHasta.Value:dd/MM/yyyy}"
        );

    return tab;
}
        // ─── Tab 2: Top Productos ─────────────────────────────────────────────
        private TabPage CrearTabTopProductos()
        {
            var tab = new TabPage("🏆  Top Productos");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var (dtpDesde, dtpHasta, cmbTipo) = AgregarFiltros(tab);
            var btnGen   = CrearBotonGenerar(tab);
            var btnExcel = CrearBotonExcel(tab);

            var grid = CrearGrid(new Point(5, 55), new Size(1060, 440));
            grid.Columns.Add("ranking",  "#");
            grid.Columns.Add("producto", "Producto");
            grid.Columns.Add("cantidad", "Cant. Vendida");
            grid.Columns.Add("ingresos", "Ingresos");
            tab.Controls.Add(grid);

            btnGen.Click += (s, e) =>
            {
                grid.Rows.Clear();
                btnExcel.Enabled = false;

                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string tipoSel   = cmbTipo.SelectedItem?.ToString() ?? "Todos";
                        string filtroTipo = tipoSel == "Todos"
                            ? ""
                            : "AND c.tipo = @tipo";

                        // ✅ FIX: join con comprobantes para filtrar por tipo
                        string sql = $@"
                            SELECT ROW_NUMBER() OVER(ORDER BY SUM(dv.cantidad) DESC),
                                   p.nombre,
                                   SUM(dv.cantidad),
                                   SUM(dv.subtotal)
                            FROM detalle_ventas dv
                            JOIN productos p ON dv.producto_id = p.id
                            JOIN ventas v    ON dv.venta_id    = v.id
                            LEFT JOIN comprobantes c
                                   ON c.venta_id = v.id AND c.estado <> 'ANULADO'
                            WHERE v.empresa_id = @eid
                              AND DATE(v.fecha) BETWEEN @d AND @h
                              {filtroTipo}
                            GROUP BY p.nombre
                            ORDER BY SUM(dv.cantidad) DESC
                            LIMIT 20";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                            cmd.Parameters.AddWithValue("d",   dtpDesde.Value.Date);
                            cmd.Parameters.AddWithValue("h",   dtpHasta.Value.Date);
                            if (filtroTipo != "")
                                cmd.Parameters.AddWithValue("tipo", tipoSel);

                            using (var dr = cmd.ExecuteReader())
                                while (dr.Read())
                                    grid.Rows.Add(
                                        dr.GetInt64(0),
                                        dr.GetString(1),
                                        dr.GetInt64(2),
                                        "S/ " + dr.GetDecimal(3).ToString("N2"));
                        }
                    }
                    btnExcel.Enabled = grid.Rows.Count > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar reporte:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnExcel.Click += (s, e) =>
                ExportarGridAExcel(grid, "Top_Productos",
                    new[] { "#", "Producto", "Cant. Vendida", "Ingresos" },
                    $"Top Productos  |  {dtpDesde.Value:dd/MM/yyyy} – {dtpHasta.Value:dd/MM/yyyy}  |  Tipo: {cmbTipo.SelectedItem}");

            return tab;
        }

        // ─── Tab 3: Resumen Financiero ────────────────────────────────────────
        private TabPage CrearTabResumenFinanciero()
        {
            var tab = new TabPage("💰  Resumen Financiero");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            tab.Controls.Add(new Label { Text = "Comprobante:", Location = new Point(10, 18), AutoSize = true });
            var cmbTipo = new ComboBox
            {
                Location      = new Point(100, 14), Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTipo.Items.AddRange(new object[] { "Todos", "BOLETA", "FACTURA" });
            cmbTipo.SelectedIndex = 0;
            tab.Controls.Add(cmbTipo);

            var btnGen = new Button
            {
                Text      = "🔄 Actualizar", Location = new Point(248, 12), Size = new Size(130, 32),
                BackColor = colorBoton, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btnGen);

            var btnExcel = new Button
            {
                Text      = "📥 Exportar Excel", Location = new Point(390, 12), Size = new Size(145, 32),
                BackColor = colorExcel, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Enabled = false
            };
            btnExcel.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btnExcel);

            var pnlCards = new Panel
            {
                Location = new Point(5, 55), Size = new Size(1060, 155),
                BackColor = Color.Transparent
            };
            tab.Controls.Add(pnlCards);

            // Tabla de detalle mensual
            var grid = CrearGrid(new Point(5, 220), new Size(1060, 270));
            grid.Columns.Add("mes",      "Mes");
            grid.Columns.Add("tipo",     "Tipo");
            grid.Columns.Add("nventas",  "N° Ventas");
            grid.Columns.Add("subtotal", "Subtotal");
            grid.Columns.Add("igv",      "IGV");
            grid.Columns.Add("total",    "Total");
            tab.Controls.Add(grid);

            var lblDetalle = new Label
            {
                Text = "Detalle mensual del año actual:",
                Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = colorDorado,
                Location = new Point(5, 200), AutoSize = true
            };
            tab.Controls.Add(lblDetalle);

            decimal[] totales = new decimal[3];

            btnGen.Click += (s, e) =>
            {
                pnlCards.Controls.Clear();
                grid.Rows.Clear();
                btnExcel.Enabled = false;

                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        int    eid      = Sesion.EmpresaActiva?.Id ?? 0;
                        string tipoSel  = cmbTipo.SelectedItem?.ToString() ?? "Todos";

                        // ✅ FIX: usar LEFT JOIN con comprobantes para filtrar por tipo
                        string joinTipo  = tipoSel == "Todos"
                            ? ""
                            : "LEFT JOIN comprobantes c ON c.venta_id = v.id AND c.estado <> 'ANULADO'";
                        string whereTipo = tipoSel == "Todos"
                            ? ""
                            : "AND c.tipo = @tipo";

                        string sqlHoy = $@"SELECT COALESCE(SUM(v.total),0)
                            FROM ventas v {joinTipo}
                            WHERE DATE(v.fecha)=CURRENT_DATE AND v.empresa_id=@eid {whereTipo}";

                        string sqlMes  = $@"SELECT COALESCE(SUM(v.total),0)
                            FROM ventas v {joinTipo}
                            WHERE DATE_TRUNC('month',v.fecha)=DATE_TRUNC('month',CURRENT_DATE)
                              AND v.empresa_id=@eid {whereTipo}";

                        string sqlAnio = $@"SELECT COALESCE(SUM(v.total),0)
                            FROM ventas v {joinTipo}
                            WHERE EXTRACT(YEAR FROM v.fecha)=EXTRACT(YEAR FROM CURRENT_DATE)
                              AND v.empresa_id=@eid {whereTipo}";

                        totales[0] = EjecutarValor(conn, sqlHoy,  eid, tipoSel);
                        totales[1] = EjecutarValor(conn, sqlMes,  eid, tipoSel);
                        totales[2] = EjecutarValor(conn, sqlAnio, eid, tipoSel);

                        AgregarTarjeta(pnlCards, "💰  Ventas Hoy",     "S/ " + totales[0].ToString("N2"), Color.FromArgb(46,  125, 50),    0,  10);
                        AgregarTarjeta(pnlCards, "📅  Ventas del Mes", "S/ " + totales[1].ToString("N2"), Color.FromArgb(21,  101, 192), 240,  10);
                        AgregarTarjeta(pnlCards, "📆  Ventas del Año", "S/ " + totales[2].ToString("N2"), Color.FromArgb(120,  95,  55), 480,  10);

                        // Detalle mensual del año
                        string sqlDetalle = $@"
                            SELECT TO_CHAR(v.fecha, 'Mon YYYY'),
                                   COALESCE(c2.tipo, 'SIN COMPROBANTE'),
                                   COUNT(DISTINCT v.id),
                                   SUM(v.subtotal), SUM(v.igv), SUM(v.total)
                            FROM ventas v
                            LEFT JOIN comprobantes c2
                                   ON c2.venta_id = v.id AND c2.estado <> 'ANULADO'
                            WHERE v.empresa_id = @eid
                              AND EXTRACT(YEAR FROM v.fecha) = EXTRACT(YEAR FROM CURRENT_DATE)
                              {(tipoSel == "Todos" ? "" : "AND c2.tipo = @tipo")}
                            GROUP BY TO_CHAR(v.fecha,'Mon YYYY'),
                                     COALESCE(c2.tipo,'SIN COMPROBANTE'),
                                     DATE_TRUNC('month', v.fecha)
                            ORDER BY DATE_TRUNC('month', v.fecha)";

                        using (var cmd = new NpgsqlCommand(sqlDetalle, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", eid);
                            if (tipoSel != "Todos")
                                cmd.Parameters.AddWithValue("tipo", tipoSel);

                            using (var dr = cmd.ExecuteReader())
                                while (dr.Read())
                                    grid.Rows.Add(
                                        dr.GetString(0), dr.GetString(1), dr.GetInt64(2),
                                        "S/ " + dr.GetDecimal(3).ToString("N2"),
                                        "S/ " + dr.GetDecimal(4).ToString("N2"),
                                        "S/ " + dr.GetDecimal(5).ToString("N2"));
                        }
                    }
                    btnExcel.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al actualizar resumen:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnExcel.Click += (s, e) =>
                ExportarResumenFinanciero(grid, totales, cmbTipo.SelectedItem?.ToString() ?? "Todos");

            return tab;
        }

        // ─── Exportar a Excel ─────────────────────────────────────────────────

        private void ExportarGridAExcel(DataGridView grid, string nombreBase,
                                        string[] columnas, string titulo)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.FileName = $"{nombreBase}_{DateTime.Today:yyyyMMdd}.xlsx";
                dlg.Filter   = "Excel (*.xlsx)|*.xlsx";
                dlg.Title    = "Guardar reporte Excel";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add(nombreBase.Replace("_", " "));

                        ws.Cell(1, 1).Value = titulo;
                        ws.Range(1, 1, 1, columnas.Length).Merge();
                        EstilarTitulo(ws.Cell(1, 1));

                        for (int c = 0; c < columnas.Length; c++)
                        {
                            ws.Cell(2, c + 1).Value = columnas[c];
                            EstilarEncabezado(ws.Cell(2, c + 1));
                        }

                        int fila = 3;
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            for (int c = 0; c < columnas.Length; c++)
                            {
                                string val = row.Cells[c].Value?.ToString() ?? "";
                                if (val.StartsWith("S/ ") &&
                                    decimal.TryParse(val.Replace("S/ ", "").Replace(",", ""), out decimal num))
                                    ws.Cell(fila, c + 1).Value = num;
                                else if (long.TryParse(val, out long lng))
                                    ws.Cell(fila, c + 1).Value = lng;
                                else
                                    ws.Cell(fila, c + 1).Value = val;

                                EstilarDato(ws.Cell(fila, c + 1), fila % 2 == 0);
                            }
                            fila++;
                        }

                        AgregarFilaTotalesExcel(ws, grid, columnas, fila);
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(dlg.FileName);
                    }

                    MessageBox.Show("✅ Excel exportado correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName, UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al exportar:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AgregarFilaTotalesExcel(IXLWorksheet ws, DataGridView grid,
                                              string[] columnas, int filaTotal)
        {
            for (int c = 0; c < columnas.Length; c++)
            {
                bool esMonto  = columnas[c].Contains("Total") || columnas[c].Contains("Subtotal")
                             || columnas[c].Contains("IGV")   || columnas[c].Contains("Ingresos");
                bool esConteo = columnas[c].Contains("N°")   || columnas[c].Contains("Cant");
                int col = c + 1;

                if (esMonto || esConteo)
                {
                    ws.Cell(filaTotal, col).FormulaA1 =
                        $"=SUM({ws.Cell(3, col).Address}:{ws.Cell(filaTotal - 1, col).Address})";
                    ws.Cell(filaTotal, col).Style.Font.Bold = true;
                    ws.Cell(filaTotal, col).Style.Fill.BackgroundColor = XLColor.FromArgb(120, 95, 55);
                    ws.Cell(filaTotal, col).Style.Font.FontColor        = XLColor.White;
                    if (esMonto)
                        ws.Cell(filaTotal, col).Style.NumberFormat.Format = "#,##0.00";
                }
                else if (c == 0)
                {
                    ws.Cell(filaTotal, col).Value = "TOTAL";
                    ws.Cell(filaTotal, col).Style.Font.Bold = true;
                    ws.Cell(filaTotal, col).Style.Fill.BackgroundColor = XLColor.FromArgb(120, 95, 55);
                    ws.Cell(filaTotal, col).Style.Font.FontColor        = XLColor.White;
                }
            }
        }

        private void ExportarResumenFinanciero(DataGridView grid, decimal[] totales, string tipo)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.FileName = $"Resumen_Financiero_{DateTime.Today:yyyyMMdd}.xlsx";
                dlg.Filter   = "Excel (*.xlsx)|*.xlsx";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Resumen Financiero");
                        ws.Cell(1, 1).Value = $"Resumen Financiero  |  Tipo: {tipo}  |  {DateTime.Now:dd/MM/yyyy HH:mm}";
                        ws.Range(1, 1, 1, 6).Merge();
                        EstilarTitulo(ws.Cell(1, 1));

                        ws.Cell(3, 1).Value = "Ventas Hoy";   ws.Cell(3, 2).Value = totales[0];
                        ws.Cell(4, 1).Value = "Ventas del Mes"; ws.Cell(4, 2).Value = totales[1];
                        ws.Cell(5, 1).Value = "Ventas del Año"; ws.Cell(5, 2).Value = totales[2];

                        foreach (int r in new[] { 3, 4, 5 })
                        {
                            ws.Cell(r, 1).Style.Font.Bold = true;
                            ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 2).Style.Font.Bold = true;
                        }

                        string[] cols = { "Mes", "Tipo", "N° Ventas", "Subtotal", "IGV", "Total" };
                        ws.Cell(7, 1).Value = "Detalle Mensual del Año";
                        ws.Range(7, 1, 7, 6).Merge();
                        EstilarTitulo(ws.Cell(7, 1));

                        for (int c = 0; c < cols.Length; c++)
                        {
                            ws.Cell(8, c + 1).Value = cols[c];
                            EstilarEncabezado(ws.Cell(8, c + 1));
                        }

                        int fila = 9;
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            for (int c = 0; c < cols.Length; c++)
                            {
                                string val = row.Cells[c].Value?.ToString() ?? "";
                                if (val.StartsWith("S/ ") &&
                                    decimal.TryParse(val.Replace("S/ ", "").Replace(",", ""), out decimal num))
                                {
                                    ws.Cell(fila, c + 1).Value = num;
                                    ws.Cell(fila, c + 1).Style.NumberFormat.Format = "#,##0.00";
                                }
                                else if (long.TryParse(val, out long lng))
                                    ws.Cell(fila, c + 1).Value = lng;
                                else
                                    ws.Cell(fila, c + 1).Value = val;

                                EstilarDato(ws.Cell(fila, c + 1), fila % 2 == 0);
                            }
                            fila++;
                        }

                        ws.Columns().AdjustToContents();
                        wb.SaveAs(dlg.FileName);
                    }

                    MessageBox.Show("✅ Excel exportado correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName, UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al exportar:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ─── Estilos Excel ────────────────────────────────────────────────────

        private void EstilarTitulo(IXLCell cell)
        {
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontSize        = 13;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(120, 95, 55);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private void EstilarEncabezado(IXLCell cell)
        {
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(100, 80, 45);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
        }

        private void EstilarDato(IXLCell cell, bool alterna)
        {
            cell.Style.Fill.BackgroundColor = alterna
                ? XLColor.FromArgb(245, 240, 228)
                : XLColor.White;
            cell.Style.Border.BottomBorder  = XLBorderStyleValues.Hair;
        }

        // ─── Helpers BD ───────────────────────────────────────────────────────

        private decimal EjecutarValor(NpgsqlConnection conn, string sql, int eid, string tipo)
        {
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("eid", eid);
                if (tipo != "Todos")
                    cmd.Parameters.AddWithValue("tipo", tipo);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private void AgregarTarjeta(Control parent, string titulo, string valor,
                                     Color color, int x, int y)
        {
            var pnl = new Panel { Size = new Size(220, 120), Location = new Point(x, y), BackColor = color };
            pnl.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(200, 255, 200), Location = new Point(10, 10), AutoSize = true
            });
            pnl.Controls.Add(new Label
            {
                Text = valor, Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(10, 50), AutoSize = true
            });
            parent.Controls.Add(pnl);
        }
    }
}