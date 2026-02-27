// ============================================================================
//  PnlComprobantes.cs  — Sin XML/SUNAT/Anular  |  Con Eliminar + Exportar Excel
// ============================================================================
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using ClosedXML.Excel;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlComprobantes : UserControl
    {
        // ── Paleta ─────────────────────────────────────────────────────────
        private readonly Color cFondo  = Color.FromArgb(245, 240, 228);
        private readonly Color cHeader = Color.FromArgb(120, 95,  55);
        private readonly Color cBoton  = Color.FromArgb(100, 80,  45);
        private readonly Color cOro    = Color.FromArgb(160, 120, 40);
        private readonly Color cTexto  = Color.FromArgb(50,  40,  20);
        private readonly Color cAzul   = Color.FromArgb(30,  80,  160);
        private readonly Color cVerde  = Color.FromArgb(30,  130, 80);
        private readonly Color cRojo   = Color.FromArgb(160, 50,  50);
        private readonly Color cExcel  = Color.FromArgb(33,  115, 70);

        // ── Controles ──────────────────────────────────────────────────────
        private DataGridView   dgv;
        private ComboBox       cboTipo;
        private DateTimePicker dtpDesde, dtpHasta;
        private Button         btnBuscar, btnEmitirBoleta, btnEmitirFactura;
        private Button         btnImprimir, btnEliminar, btnExportarExcel;
        private Label          lblContador, lblTotal;

        public PnlComprobantes()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            CrearTablaSiNoExiste();
            Inicializar();
            CargarComprobantes();
        }

        // ── Crear tabla si no existe ───────────────────────────────────────
        private void CrearTablaSiNoExiste()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS comprobantes (
                        id             SERIAL PRIMARY KEY,
                        empresa_id     INT REFERENCES empresas(id),
                        sucursal_id    INT REFERENCES sucursales(id),
                        venta_id       INT REFERENCES ventas(id),
                        tipo           VARCHAR(10)   NOT NULL,
                        serie          VARCHAR(10)   NOT NULL,
                        numero         VARCHAR(20)   NOT NULL,
                        fecha_emision  TIMESTAMP     DEFAULT NOW(),
                        cliente_doc    VARCHAR(20),
                        cliente_nombre VARCHAR(200),
                        cliente_dir    VARCHAR(200),
                        subtotal       DECIMAL(12,2) DEFAULT 0,
                        igv            DECIMAL(12,2) DEFAULT 0,
                        total          DECIMAL(12,2) DEFAULT 0,
                        estado         VARCHAR(20)   DEFAULT 'EMITIDO',
                        usuario_id     INT REFERENCES usuarios(id),
                        sunat_estado   VARCHAR(30)   DEFAULT 'PENDIENTE',
                        UNIQUE(serie, numero)
                    );", conn);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        //  INICIALIZAR UI
        // ══════════════════════════════════════════════════════════════════
        private void Inicializar()
        {
            // ── Barra superior: 3 filas (título / filtros / botones) ─────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 126, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(200, 185, 155), 1);
                e.Graphics.DrawLine(pen, 0, 125, pnlTop.Width, 125);
            };

            // ── Fila 1: Título (y=0, h=40) ───────────────────────────────
            var pnlTitulo = new Panel { Location = new Point(0, 0), Size = new Size(9999, 40), BackColor = cHeader };
            pnlTitulo.Controls.Add(new Label
            {
                Text      = "🧾  COMPROBANTES DE VENTA",
                Font      = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                AutoSize  = false, Size = new Size(600, 40),
                Location  = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTop.Controls.Add(pnlTitulo);

            // ── Fila 2: Filtros (y=46, h=28) ─────────────────────────────
            const int FY = 46;

            pnlTop.Controls.Add(LblFiltro("Tipo:", 15, FY + 5));
            cboTipo = new ComboBox { Location = new Point(50, FY), Size = new Size(95, 28), Font = new Font("Arial", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo.Items.AddRange(new object[] { "TODOS", "BOLETA", "FACTURA" });
            cboTipo.SelectedIndex = 0;
            pnlTop.Controls.Add(cboTipo);

            pnlTop.Controls.Add(LblFiltro("Desde:", 158, FY + 5));
            dtpDesde = new DateTimePicker { Location = new Point(204, FY), Size = new Size(118, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            pnlTop.Controls.Add(dtpDesde);

            pnlTop.Controls.Add(LblFiltro("Hasta:", 334, FY + 5));
            dtpHasta = new DateTimePicker { Location = new Point(378, FY), Size = new Size(118, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlTop.Controls.Add(dtpHasta);

            btnBuscar = MkBtn("🔍 Buscar", cBoton, new Point(508, FY), 95, 28);
            btnBuscar.Click += (s, e) => CargarComprobantes();
            pnlTop.Controls.Add(btnBuscar);

            lblContador = new Label { Text = "0 comprobantes", Font = new Font("Arial", 8),               ForeColor = Color.FromArgb(130, 110, 80), BackColor = Color.Transparent, AutoSize = true, Location = new Point(616, FY + 3) };
            lblTotal    = new Label { Text = "Total: S/ 0.00",  Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro,                        BackColor = Color.Transparent, AutoSize = true, Location = new Point(616, FY + 16) };
            pnlTop.Controls.Add(lblContador);
            pnlTop.Controls.Add(lblTotal);

            // ── Fila 3: Botones (y=84, h=28) ─────────────────────────────
            const int AY = 88; const int BH = 28; const int GAP = 6;
            int bx = 15;

            btnEmitirBoleta  = MkBtn("🧾 Nueva Boleta",   cAzul,  new Point(bx, AY), 130, BH); bx += 130 + GAP;
            btnEmitirFactura = MkBtn("📄 Nueva Factura",  cVerde, new Point(bx, AY), 130, BH); bx += 130 + GAP + 16;  // pequeño separador

            btnImprimir      = MkBtn("🖨 Imprimir",       cBoton, new Point(bx, AY), 108, BH); bx += 108 + GAP;
            btnEliminar      = MkBtn("🗑 Eliminar",       cRojo,  new Point(bx, AY), 108, BH); bx += 108 + GAP;
            btnExportarExcel = MkBtn("📥 Exportar Excel", cExcel, new Point(bx, AY), 138, BH);

            btnEmitirBoleta.Click  += (s, e) => EmitirComprobante("BOLETA");
            btnEmitirFactura.Click += (s, e) => EmitirComprobante("FACTURA");
            btnImprimir.Click      += (s, e) => ImprimirSeleccionado();
            btnEliminar.Click      += (s, e) => EliminarSeleccionado();
            btnExportarExcel.Click += (s, e) => ExportarExcel();

            pnlTop.Controls.AddRange(new Control[] {
                btnEmitirBoleta, btnEmitirFactura,
                btnImprimir, btnEliminar, btnExportarExcel
            });

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = cFondo,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Arial", 9),
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                RowTemplate = { Height = 34 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                MultiSelect = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = cHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(8, 0, 0, 0);
            dgv.ColumnHeadersHeight                     = 36;
            dgv.ColumnHeadersBorderStyle                = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles               = false;
            dgv.DefaultCellStyle.BackColor              = Color.White;
            dgv.DefaultCellStyle.ForeColor              = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(220, 200, 160);
            dgv.DefaultCellStyle.SelectionForeColor     = cTexto;
            dgv.DefaultCellStyle.Padding                = new Padding(8, 0, 8, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);

            // Columnas visibles
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "nro",      HeaderText = "#",                Width = 45  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "tipo",     HeaderText = "Tipo",            Width = 85  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "numero",   HeaderText = "N° Comprobante",  Width = 160 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",    HeaderText = "Fecha Emisión",   Width = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cliente",  HeaderText = "Cliente",         Width = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "doc",      HeaderText = "RUC/DNI",         Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "subtotal", HeaderText = "Subtotal",        Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "igv",      HeaderText = "IGV",            Width = 80,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "total",    HeaderText = "Total",           Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "estado",   HeaderText = "Estado",          Width = 90  });

            // Columna oculta para guardar el ID interno
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 0, Visible = false });

            dgv.CellPainting += Dgv_CellPainting;
            dgv.RowPrePaint  += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.RowPostPaint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 225, 205), 1);
                e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1,
                                        e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlTop);
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARGAR COMPROBANTES
        // ══════════════════════════════════════════════════════════════════
        private void CargarComprobantes()
        {
            dgv.Rows.Clear();
            decimal totalAcum = 0;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string tipo = cboTipo.SelectedItem?.ToString() ?? "TODOS";
                string sql  = @"
                    SELECT c.id, c.tipo, c.serie, c.numero, c.fecha_emision,
                           COALESCE(c.cliente_nombre,'CLIENTE GENERAL'),
                           COALESCE(c.cliente_doc,''),
                           c.subtotal, c.igv, c.total, c.estado
                    FROM   comprobantes c
                    WHERE  c.empresa_id = @eid
                      AND  DATE(c.fecha_emision) BETWEEN @desde AND @hasta
                      AND  (@tipo = 'TODOS' OR c.tipo = @tipo)
                    ORDER  BY c.fecha_emision DESC";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("eid",   Sesion.EmpresaActiva?.Id ?? 1);
                cmd.Parameters.AddWithValue("desde", dtpDesde.Value.Date);
                cmd.Parameters.AddWithValue("hasta", dtpHasta.Value.Date);
                cmd.Parameters.AddWithValue("tipo",  tipo);

                using var dr = cmd.ExecuteReader();
                int count = 0;
                while (dr.Read())
                {
                    count++;
                    decimal tot    = dr.GetDecimal(9);
                    string  estado = dr.GetString(10);
                    if (estado != "ANULADO") totalAcum += tot;

                    dgv.Rows.Add(
                        count,
                        dr.GetString(1),
                        $"{dr.GetString(2)}-{dr.GetString(3)}",
                        dr.GetDateTime(4).ToString("dd/MM/yyyy HH:mm"),
                        dr.GetString(5),
                        dr.GetString(6),
                        "S/ " + dr.GetDecimal(7).ToString("N2"),
                        "S/ " + dr.GetDecimal(8).ToString("N2"),
                        "S/ " + tot.ToString("N2"),
                        estado,
                        dr.GetInt32(0)   // colId oculta
                    );
                }
                lblContador.Text = $"{count} comprobante{(count != 1 ? "s" : "")}";
                lblTotal.Text    = $"Total: S/ {totalAcum:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar comprobantes:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EMITIR
        // ══════════════════════════════════════════════════════════════════
        private void EmitirComprobante(string tipo)
        {
            using var frm = new FrmEmitirComprobante(tipo);
            if (frm.ShowDialog(this) == DialogResult.OK)
                CargarComprobantes();
        }

        // ══════════════════════════════════════════════════════════════════
        //  ELIMINAR COMPROBANTE
        // ══════════════════════════════════════════════════════════════════
        private void EliminarSeleccionado()
        {
            if (!ObtenerSeleccion(out int id, out string tipo, out string numero)) return;

            var confirm = MessageBox.Show(
                $"¿Eliminar este comprobante?\n\n" +
                $"  Tipo   : {tipo}\n" +
                $"  Número : {numero}\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "DELETE FROM comprobantes WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();

                MessageBox.Show("✅ Comprobante eliminado correctamente.",
                    "Eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarComprobantes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EXPORTAR EXCEL
        // ══════════════════════════════════════════════════════════════════
        private void ExportarExcel()
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar. Presione 🔍 Buscar primero.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                FileName = $"Comprobantes_{DateTime.Today:yyyyMMdd}.xlsx",
                Filter   = "Excel (*.xlsx)|*.xlsx",
                Title    = "Guardar reporte de comprobantes"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Comprobantes");

                // ── Título ─────────────────────────────────────────────────
                string tipoSel  = cboTipo.SelectedItem?.ToString() ?? "TODOS";
                string titulo   = $"COMPROBANTES DE VENTA  |  Tipo: {tipoSel}  |  " +
                                  $"{dtpDesde.Value:dd/MM/yyyy} – {dtpHasta.Value:dd/MM/yyyy}  |  " +
                                  $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Cell(1, 1).Value = titulo;
                var rngTit = ws.Range(1, 1, 1, 9);
                rngTit.Merge();
                rngTit.Style.Font.Bold            = true;
                rngTit.Style.Font.FontSize        = 12;
                rngTit.Style.Font.FontColor       = XLColor.White;
                rngTit.Style.Fill.BackgroundColor = XLColor.FromArgb(120, 95, 55);
                rngTit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rngTit.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 22;

                // ── Encabezados ────────────────────────────────────────────
                string[] headers = { "#", "Tipo", "N° Comprobante", "Fecha Emisión",
                                     "Cliente", "RUC/DNI", "Subtotal (S/)", "IGV (S/)", "Total (S/)" };
                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cell(2, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold            = true;
                    cell.Style.Font.FontColor       = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(100, 80, 45);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
                }
                ws.Row(2).Height = 18;

                // ── Datos ─────────────────────────────────────────────────
                int fila = 3;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    bool alterna = fila % 2 == 0;
                    var bgColor  = alterna ? XLColor.FromArgb(250, 246, 238) : XLColor.White;

                    ws.Cell(fila, 1).Value = row.Cells["nro"].Value?.ToString();
                    ws.Cell(fila, 2).Value = row.Cells["tipo"].Value?.ToString();
                    ws.Cell(fila, 3).Value = row.Cells["numero"].Value?.ToString();
                    ws.Cell(fila, 4).Value = row.Cells["fecha"].Value?.ToString();
                    ws.Cell(fila, 5).Value = row.Cells["cliente"].Value?.ToString();
                    ws.Cell(fila, 6).Value = row.Cells["doc"].Value?.ToString();

                    // Montos como números
                    ws.Cell(fila, 7).Value = ParseMonto(row.Cells["subtotal"].Value);
                    ws.Cell(fila, 8).Value = ParseMonto(row.Cells["igv"].Value);
                    ws.Cell(fila, 9).Value = ParseMonto(row.Cells["total"].Value);

                    // Formato moneda en columnas 7-9
                    for (int c = 7; c <= 9; c++)
                    {
                        ws.Cell(fila, c).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(fila, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }

                    // Fondo alternado
                    for (int c = 1; c <= 9; c++)
                    {
                        ws.Cell(fila, c).Style.Fill.BackgroundColor = bgColor;
                        ws.Cell(fila, c).Style.Border.BottomBorder  = XLBorderStyleValues.Hair;
                        ws.Cell(fila, c).Style.Font.FontName        = "Arial";
                        ws.Cell(fila, c).Style.Font.FontSize        = 9;
                    }
                    fila++;
                }

                // ── Fila TOTALES ───────────────────────────────────────────
                ws.Cell(fila, 1).Value = "TOTAL";
                ws.Cell(fila, 5).Value = $"{dgv.Rows.Count} comprobante(s)";

                // Fórmulas de suma para columnas numéricas
                ws.Cell(fila, 7).FormulaA1 = $"=SUM(G3:G{fila - 1})";
                ws.Cell(fila, 8).FormulaA1 = $"=SUM(H3:H{fila - 1})";
                ws.Cell(fila, 9).FormulaA1 = $"=SUM(I3:I{fila - 1})";

                for (int c = 1; c <= 9; c++)
                {
                    ws.Cell(fila, c).Style.Font.Bold            = true;
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XLColor.FromArgb(120, 95, 55);
                    ws.Cell(fila, c).Style.Font.FontColor       = XLColor.White;
                    ws.Cell(fila, c).Style.Font.FontName        = "Arial";
                }
                for (int c = 7; c <= 9; c++)
                {
                    ws.Cell(fila, c).Style.NumberFormat.Format  = "#,##0.00";
                    ws.Cell(fila, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                ws.Row(fila).Height = 18;

                // ── Ajustar anchos ─────────────────────────────────────────
                ws.Column(1).Width  = 6;
                ws.Column(2).Width  = 10;
                ws.Column(3).Width  = 20;
                ws.Column(4).Width  = 18;
                ws.Column(5).Width  = 30;
                ws.Column(6).Width  = 14;
                ws.Column(7).Width  = 14;
                ws.Column(8).Width  = 12;
                ws.Column(9).Width  = 14;

                // Freeze encabezados
                ws.SheetView.FreezeRows(2);

                wb.SaveAs(dlg.FileName);

                MessageBox.Show("✅  Excel exportado correctamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Abrir el archivo
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

        private decimal ParseMonto(object val)
        {
            string s = val?.ToString()?.Replace("S/ ", "").Replace(",", "") ?? "0";
            return decimal.TryParse(s, out decimal d) ? d : 0;
        }

        // ══════════════════════════════════════════════════════════════════
        //  IMPRIMIR
        // ══════════════════════════════════════════════════════════════════
        private void ImprimirSeleccionado()
        {
            if (!ObtenerSeleccion(out int id, out _, out _)) return;

            try
            {
                var datos = ObtenerDatosComprobante(id);
                var pd    = new PrintDocument();
                pd.PrintPage += (s, e) => ImprimirPagina(e, datos);
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 680, Height = 820,
                    Text  = $"Vista previa — {datos.tipo} {datos.serie}-{datos.numero}"
                };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirPagina(PrintPageEventArgs e, dynamic d)
        {
            var g = e.Graphics!;
            float x = 30, y = 15, lh = 18;

            g.DrawString(d.empresa,     new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y); y += 22;
            g.DrawString($"RUC: {d.rucEmp}", new Font("Arial", 9), Brushes.Gray, x, y); y += lh + 6;

            var brush  = (string)d.tipo == "BOLETA" ? Brushes.DarkBlue : Brushes.DarkGreen;
            string tit = (string)d.tipo == "BOLETA" ? "BOLETA DE VENTA" : "FACTURA";
            g.DrawString(tit, new Font("Arial", 11, FontStyle.Bold), brush, x, y); y += 22;
            g.DrawString($"N° {d.serie}-{d.numero}", new Font("Arial", 11, FontStyle.Bold), Brushes.Black, x, y); y += lh;
            g.DrawString($"Fecha: {d.fecha}", new Font("Arial", 9), Brushes.Black, x, y); y += lh + 6;

            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
            g.DrawString($"Cliente: {d.clienteNombre}", new Font("Arial", 9), Brushes.Black, x, y); y += lh;
            g.DrawString($"Doc.: {d.clienteDoc}", new Font("Arial", 9), Brushes.Black, x, y); y += lh + 4;
            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;

            g.DrawString("Subtotal:",  new Font("Arial", 10), Brushes.Black, x, y);
            g.DrawString($"S/ {d.subtotal:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
            g.DrawString("IGV (18%):", new Font("Arial", 10), Brushes.Black, x, y);
            g.DrawString($"S/ {d.igv:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
            g.DrawLine(new Pen(Color.Black, 2), x, y, 530, y); y += 6;
            g.DrawString("TOTAL:", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y);
            g.DrawString($"S/ {d.total:N2}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 380, y); y += 28;

            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
            g.DrawString("Representación impresa del comprobante.", new Font("Arial", 7), Brushes.Gray, x, y);
        }

        private dynamic ObtenerDatosComprobante(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            string sql = @"
                SELECT c.tipo, c.serie, c.numero, c.fecha_emision,
                       COALESCE(c.cliente_nombre,'CLIENTE GENERAL'),
                       COALESCE(c.cliente_doc,''),
                       c.subtotal, c.igv, c.total,
                       COALESCE(e.nombre,''), COALESCE(e.ruc,'')
                FROM   comprobantes c
                LEFT JOIN empresas e ON c.empresa_id = e.id
                WHERE  c.id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) throw new Exception("Comprobante no encontrado.");

            return new
            {
                tipo          = dr.GetString(0),
                serie         = dr.GetString(1),
                numero        = dr.GetString(2),
                fecha         = dr.GetDateTime(3).ToString("dd/MM/yyyy HH:mm"),
                clienteNombre = dr.GetString(4),
                clienteDoc    = dr.GetString(5),
                subtotal      = dr.GetDecimal(6),
                igv           = dr.GetDecimal(7),
                total         = dr.GetDecimal(8),
                empresa       = dr.GetString(9),
                rucEmp        = dr.GetString(10)
            };
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════
        private Label LblFiltro(string t, int x, int y) =>
            new Label { Text = t, Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(x, y) };

        private Button MkBtn(string texto, Color color, Point loc, int w, int h)
        {
            var btn = new Button
            {
                Text = texto, Size = new Size(w, h), Location = loc,
                BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 8, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private bool ObtenerSeleccion(out int id, out string tipo, out string numero)
        {
            id = 0; tipo = ""; numero = "";
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un comprobante primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            id     = Convert.ToInt32(dgv.SelectedRows[0].Cells["colId"].Value  ?? 0);
            tipo   = dgv.SelectedRows[0].Cells["tipo"].Value?.ToString()   ?? "";
            numero = dgv.SelectedRows[0].Cells["numero"].Value?.ToString() ?? "";
            return id > 0;
        }

        // ── Colorear columna Tipo y Estado ────────────────────────────────
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dgv.Columns["tipo"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string t    = e.Value.ToString()!;
                Color  col  = t == "BOLETA" ? cAzul : cVerde;
                using var bg = new SolidBrush(Color.FromArgb(20, col));
                e.Graphics!.FillRectangle(bg, e.CellBounds);
                using var br = new SolidBrush(col);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
                return;
            }

            if (e.ColumnIndex == dgv.Columns["estado"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string st   = e.Value.ToString()!;
                Color  col  = st == "EMITIDO" ? cVerde : st == "ANULADO" ? Color.Gray : cOro;
                using var br = new SolidBrush(col);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics!.DrawString(st, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
            }
        }
    }

    // =========================================================================
    //  FORMULARIO — Emitir Boleta o Factura
    // =========================================================================
    public class FrmEmitirComprobante : Form
    {
        private readonly Color cFondo = Color.FromArgb(250, 247, 240);
        private readonly Color cOro   = Color.FromArgb(160, 120, 40);
        private readonly Color cBoton = Color.FromArgb(100, 80,  45);
        private readonly Color cTexto = Color.FromArgb(50,  40,  20);
        private readonly Color cInput = Color.FromArgb(255, 252, 245);
        private readonly Color cAzul  = Color.FromArgb(30,  80,  160);
        private readonly Color cVerde = Color.FromArgb(30,  130, 80);

        private ComboBox cboVenta;
        private TextBox  txtClienteDoc, txtClienteNombre, txtClienteDir;
        private Label    lblSerie, lblSubtotal, lblIgv, lblTotal;
        private Button   btnGuardar, btnCancelar;
        private readonly string _tipo;

        public FrmEmitirComprobante(string tipo)
        {
            _tipo                = tipo;
            this.Text            = tipo == "BOLETA" ? "Emitir Boleta de Venta" : "Emitir Factura Electrónica";
            this.Size            = new Size(560, 530);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = cFondo;
            this.DialogResult    = DialogResult.Cancel;
            Inicializar();
            CargarVentas();
            GenerarSerie();
        }

        private void Inicializar()
        {
            Color colorTipo = _tipo == "BOLETA" ? cAzul : cVerde;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = colorTipo };
            var lblTit    = new Label
            {
                Text      = _tipo == "BOLETA" ? "🧾  Nueva Boleta de Venta" : "📄  Nueva Factura",
                Font      = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.Transparent, AutoSize = false, Size = new Size(510, 54),
                Location  = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTit);
            this.Controls.Add(pnlHeader);

            int y = 66;
            AddLbl("Serie / Número (auto-generado)", 20, y);
            lblSerie = new Label
            {
                Text = "---", Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = colorTipo, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(500, 24), Location = new Point(20, y + 18)
            };
            this.Controls.Add(lblSerie); y += 54;

            AddLbl("Venta de origen", 20, y);
            cboVenta = new ComboBox
            {
                Location = new Point(20, y + 18), Size = new Size(500, 28),
                Font = new Font("Arial", 10), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = cInput
            };
            cboVenta.SelectedIndexChanged += CboVenta_Changed;
            this.Controls.Add(cboVenta); y += 58;

            AddLbl(_tipo == "BOLETA" ? "DNI del cliente (opcional)" : "RUC del cliente *", 20, y);
            txtClienteDoc = MkTxt(new Point(20, y + 18), new Size(240, 28));
            this.Controls.Add(txtClienteDoc); y += 58;

            AddLbl("Nombre / Razón Social", 20, y);
            txtClienteNombre = MkTxt(new Point(20, y + 18), new Size(500, 28));
            this.Controls.Add(txtClienteNombre); y += 58;

            AddLbl("Dirección del cliente", 20, y);
            txtClienteDir = MkTxt(new Point(20, y + 18), new Size(500, 28));
            this.Controls.Add(txtClienteDir); y += 58;

            var pnlRes = new Panel { Location = new Point(20, y), Size = new Size(500, 68), BackColor = Color.FromArgb(240, 230, 210) };
            lblSubtotal = MkLblRes($"Subtotal: S/ 0.00", cTexto, new Point(10, 6));
            lblIgv      = MkLblRes("IGV (18%): S/ 0.00", cTexto, new Point(10, 26));
            lblTotal    = MkLblRes("TOTAL: S/ 0.00", colorTipo, new Point(10, 44), bold: true, size: 12);
            pnlRes.Controls.AddRange(new Control[] { lblSubtotal, lblIgv, lblTotal });
            this.Controls.Add(pnlRes); y += 80;

            btnGuardar = new Button
            {
                Text = "✔  EMITIR COMPROBANTE", Size = new Size(244, 40), Location = new Point(20, y),
                BackColor = colorTipo, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar", Size = new Size(244, 40), Location = new Point(276, y),
                BackColor = Color.FromArgb(200, 190, 170), ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10), Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });
            this.Height = y + 82;
        }

        private void AddLbl(string t, int x, int y) =>
            this.Controls.Add(new Label { Text = t, Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = false, Size = new Size(500, 16), Location = new Point(x, y) });

        private TextBox MkTxt(Point loc, Size sz) =>
            new TextBox { Location = loc, Size = sz, Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };

        private Label MkLblRes(string t, Color c, Point loc, bool bold = false, float size = 9) =>
            new Label { Text = t, Font = new Font("Arial", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = c, BackColor = Color.Transparent, AutoSize = true, Location = loc };

        private void CargarVentas()
        {
            try
            {
                cboVenta.Items.Clear();
                cboVenta.Items.Add("-- Seleccione venta --");
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string sql = @"
                    SELECT v.id, v.numero_venta, v.total, COALESCE(c.nombre,'CLIENTE GENERAL')
                    FROM   ventas v LEFT JOIN clientes c ON v.cliente_id = c.id
                    WHERE  v.empresa_id = @eid
                      AND  v.id NOT IN (
                               SELECT COALESCE(venta_id,0) FROM comprobantes
                               WHERE  venta_id IS NOT NULL AND estado != 'ANULADO')
                    ORDER  BY v.fecha DESC LIMIT 100";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    cboVenta.Items.Add($"{dr.GetInt32(0)}|{dr.GetString(1)}|{dr.GetDecimal(2)}|{dr.GetString(3)}");

                cboVenta.SelectedIndex     = 0;
                cboVenta.FormattingEnabled = true;
                cboVenta.Format += (s, e) =>
                {
                    if (e.ListItem?.ToString()?.Contains("|") == true)
                    {
                        var p   = e.ListItem.ToString()!.Split('|');
                        e.Value = $"{p[1]}  –  {p[3]}  (S/ {decimal.Parse(p[2]):N2})";
                    }
                };
            }
            catch { }
        }

        private void CboVenta_Changed(object? sender, EventArgs e)
        {
            if (cboVenta.SelectedIndex <= 0) return;
            var parts = cboVenta.SelectedItem!.ToString()!.Split('|');
            if (parts.Length < 4) return;
            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;
            lblSubtotal.Text = $"Subtotal: S/ {subtotal:N2}";
            lblIgv.Text      = $"IGV (18%): S/ {igv:N2}";
            lblTotal.Text    = $"TOTAL: S/ {total:N2}";
            if (string.IsNullOrWhiteSpace(txtClienteNombre.Text) && parts[3] != "CLIENTE GENERAL")
                txtClienteNombre.Text = parts[3];
        }

        private void GenerarSerie()
        {
            try
            {
                string prefijo = _tipo == "BOLETA" ? "B001" : "F001";
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM comprobantes WHERE tipo=@t AND empresa_id=@eid", conn);
                cmd.Parameters.AddWithValue("t",   _tipo);
                cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                long count    = (long)cmd.ExecuteScalar()!;
                lblSerie.Text = $"{prefijo}-{(count + 1):D8}";
            }
            catch { lblSerie.Text = _tipo == "BOLETA" ? "B001-00000001" : "F001-00000001"; }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (cboVenta.SelectedIndex <= 0)
            { MessageBox.Show("Seleccione una venta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_tipo == "FACTURA" && string.IsNullOrWhiteSpace(txtClienteDoc.Text))
            { MessageBox.Show("Ingrese el RUC del cliente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var     parts    = cboVenta.SelectedItem!.ToString()!.Split('|');
            int     ventaId  = int.Parse(parts[0]);
            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;
            string[] sp      = lblSerie.Text.Split('-');
            string   serie   = sp[0];
            string   numero  = sp.Length > 1 ? sp[1] : "00000001";

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO comprobantes
                        (empresa_id, sucursal_id, venta_id, tipo, serie, numero,
                         cliente_doc, cliente_nombre, cliente_dir,
                         subtotal, igv, total, usuario_id, sunat_estado)
                    VALUES
                        (@eid,@sid,@vid,@tipo,@ser,@num,
                         @cdoc,@cnom,@cdir,
                         @sub,@igv,@tot,@uid,'PENDIENTE')", conn);
                cmd.Parameters.AddWithValue("eid",  Sesion.EmpresaActiva?.Id  ?? 1);
                cmd.Parameters.AddWithValue("sid",  Sesion.SucursalActiva?.Id ?? 1);
                cmd.Parameters.AddWithValue("vid",  ventaId);
                cmd.Parameters.AddWithValue("tipo", _tipo);
                cmd.Parameters.AddWithValue("ser",  serie);
                cmd.Parameters.AddWithValue("num",  numero);
                cmd.Parameters.AddWithValue("cdoc", txtClienteDoc.Text.Trim());
                cmd.Parameters.AddWithValue("cnom", txtClienteNombre.Text.Trim());
                cmd.Parameters.AddWithValue("cdir", txtClienteDir.Text.Trim());
                cmd.Parameters.AddWithValue("sub",  subtotal);
                cmd.Parameters.AddWithValue("igv",  igv);
                cmd.Parameters.AddWithValue("tot",  total);
                cmd.Parameters.AddWithValue("uid",  Sesion.UsuarioActivo?.Id ?? 1);
                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    $"✅  {_tipo} emitida correctamente.\n\nN° {serie}-{numero}\nTotal: S/ {total:N2}",
                    "Comprobante emitido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}