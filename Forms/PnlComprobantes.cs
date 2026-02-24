using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
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

        private DataGridView dgv;
        private ComboBox cboTipo;
        private DateTimePicker dtpDesde, dtpHasta;
        private Button btnBuscar, btnEmitirBoleta, btnEmitirFactura, btnImprimir, btnAnular;
        private Label lblContador, lblTotal;

        public PnlComprobantes()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            CrearTablas();
            Inicializar();
            CargarComprobantes();
        }

        private void CrearTablas()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                    CREATE TABLE IF NOT EXISTS comprobantes (
                        id              SERIAL PRIMARY KEY,
                        empresa_id      INT REFERENCES empresas(id),
                        sucursal_id     INT REFERENCES sucursales(id),
                        venta_id        INT REFERENCES ventas(id),
                        tipo            VARCHAR(10) NOT NULL,  -- BOLETA / FACTURA
                        serie           VARCHAR(10) NOT NULL,
                        numero          VARCHAR(20) NOT NULL,
                        fecha_emision   TIMESTAMP DEFAULT NOW(),
                        cliente_doc     VARCHAR(20),
                        cliente_nombre  VARCHAR(200),
                        cliente_dir     VARCHAR(200),
                        subtotal        DECIMAL(12,2) DEFAULT 0,
                        igv             DECIMAL(12,2) DEFAULT 0,
                        total           DECIMAL(12,2) DEFAULT 0,
                        estado          VARCHAR(20) DEFAULT 'EMITIDO',
                        usuario_id      INT REFERENCES usuarios(id),
                        UNIQUE(serie, numero)
                    );";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void Inicializar()
        {
            // ── Cabecera ──────────────────────────────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 185, 155), 1))
                    e.Graphics.DrawLine(pen, 0, 119, pnlTop.Width, 119);
            };

            var lblTitulo = new Label
            {
                Text = "🧾  COMPROBANTES DE PAGO — SUNAT",
                Font = new Font("Arial", 15, FontStyle.Bold),
                ForeColor = cBoton, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(600, 36),
                Location = new Point(20, 12), TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = "Emisión de Boletas y Facturas Electrónicas",
                Font = new Font("Arial", 9), ForeColor = Color.FromArgb(130, 110, 80),
                BackColor = Color.Transparent, AutoSize = false,
                Size = new Size(500, 18), Location = new Point(22, 48)
            };

            // Filtros
            var lblTipo = new Label { Text = "Tipo:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(22, 78) };
            cboTipo = new ComboBox { Location = new Point(60, 74), Size = new Size(120, 28), Font = new Font("Arial", 9), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(250, 247, 240) };
            cboTipo.Items.AddRange(new object[] { "TODOS", "BOLETA", "FACTURA" });
            cboTipo.SelectedIndex = 0;

            var lblDesde = new Label { Text = "Desde:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(196, 78) };
            dtpDesde = new DateTimePicker { Location = new Point(246, 74), Size = new Size(130, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

            var lblHasta = new Label { Text = "Hasta:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(390, 78) };
            dtpHasta = new DateTimePicker { Location = new Point(438, 74), Size = new Size(130, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            btnBuscar = new Button { Text = "🔍  Buscar", Size = new Size(100, 30), Location = new Point(582, 73), BackColor = cBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarComprobantes();

            // Botones acción
            btnEmitirBoleta = CrearBoton("🧾  Emitir Boleta",   cAzul,  new Point(700, 68));
            btnEmitirFactura = CrearBoton("📄  Emitir Factura", cVerde, new Point(840, 68));
            btnImprimir      = CrearBoton("🖨  Imprimir",       cBoton, new Point(980, 68));
            btnAnular        = CrearBoton("🚫  Anular",  Color.FromArgb(160, 50, 50), new Point(1100, 68));

            btnEmitirBoleta.Click  += (s, e) => EmitirComprobante("BOLETA");
            btnEmitirFactura.Click += (s, e) => EmitirComprobante("FACTURA");
            btnImprimir.Click      += (s, e) => ImprimirSeleccionado();
            btnAnular.Click        += (s, e) => AnularSeleccionado();

            lblContador = new Label { Text = "0 comprobantes", Font = new Font("Arial", 8), ForeColor = Color.FromArgb(130, 110, 80), BackColor = Color.Transparent, AutoSize = true, Location = new Point(22, 100) };
            lblTotal    = new Label { Text = "Total: S/ 0.00",  Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(200, 100) };

            pnlTop.Controls.AddRange(new Control[] { lblTitulo, lblSub, lblTipo, cboTipo, lblDesde, dtpDesde, lblHasta, dtpHasta, btnBuscar, btnEmitirBoleta, btnEmitirFactura, btnImprimir, btnAnular, lblContador, lblTotal });

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = cFondo,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Arial", 10), CellBorderStyle = DataGridViewCellBorderStyle.None,
                RowTemplate = { Height = 44 }, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = cHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(10, 0, 0, 0);
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor          = Color.White;
            dgv.DefaultCellStyle.ForeColor          = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 200, 160);
            dgv.DefaultCellStyle.SelectionForeColor = cTexto;
            dgv.DefaultCellStyle.Padding            = new Padding(10, 0, 10, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id",        HeaderText = "ID",          Width = 55  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "tipo",       HeaderText = "TIPO",        Width = 90  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "serie",      HeaderText = "SERIE",       Width = 80  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "numero",     HeaderText = "NÚMERO",      Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",      HeaderText = "FECHA",       Width = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cliente_doc",HeaderText = "DOC. CLIENTE",Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cliente",    HeaderText = "CLIENTE",     Width = 220 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "subtotal",   HeaderText = "SUBTOTAL",    Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "igv",        HeaderText = "IGV (18%)",   Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "total",      HeaderText = "TOTAL",       Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "estado",     HeaderText = "ESTADO",      Width = 100 });

            dgv.CellPainting += Dgv_CellPainting;
            dgv.RowPrePaint  += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.RowPostPaint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(235, 225, 205), 1))
                    e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1, e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlTop);
        }

        private Button CrearBoton(string texto, Color color, Point loc)
        {
            var btn = new Button { Text = texto, Size = new Size(128, 32), Location = loc, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void CargarComprobantes()
        {
            try
            {
                dgv.Rows.Clear();
                decimal totalAcum = 0;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string tipoFiltro = cboTipo.SelectedItem?.ToString() ?? "TODOS";
                    string sql = @"SELECT c.id, c.tipo, c.serie, c.numero, c.fecha_emision,
                                          c.cliente_doc, c.cliente_nombre,
                                          c.subtotal, c.igv, c.total, c.estado
                                   FROM comprobantes c
                                   WHERE c.empresa_id = @eid
                                     AND c.fecha_emision BETWEEN @desde AND @hasta
                                     AND (@tipo = 'TODOS' OR c.tipo = @tipo)
                                   ORDER BY c.fecha_emision DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid",   Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        cmd.Parameters.AddWithValue("desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("hasta", dtpHasta.Value.Date.AddDays(1));
                        cmd.Parameters.AddWithValue("tipo",  tipoFiltro);
                        using (var dr = cmd.ExecuteReader())
                        {
                            int count = 0;
                            while (dr.Read())
                            {
                                count++;
                                decimal tot = dr.GetDecimal(9);
                                if (dr.GetString(10) != "ANULADO") totalAcum += tot;
                                dgv.Rows.Add(
                                    dr.GetInt32(0),
                                    dr.GetString(1),
                                    dr.GetString(2),
                                    dr.GetString(3),
                                    dr.GetDateTime(4).ToString("dd/MM/yyyy HH:mm"),
                                    dr.IsDBNull(5) ? "" : dr.GetString(5),
                                    dr.IsDBNull(6) ? "" : dr.GetString(6),
                                    $"S/ {dr.GetDecimal(7):N2}",
                                    $"S/ {dr.GetDecimal(8):N2}",
                                    $"S/ {tot:N2}",
                                    dr.GetString(10)
                                );
                            }
                            lblContador.Text = $"{count} comprobante{(count != 1 ? "s" : "")}";
                            lblTotal.Text    = $"Total: S/ {totalAcum:N2}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar comprobantes:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EmitirComprobante(string tipo)
        {
            using (var frm = new FrmEmitirComprobante(tipo))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    CargarComprobantes();
            }
        }

        private void AnularSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un comprobante.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id     = Convert.ToInt32(dgv.SelectedRows[0].Cells["id"].Value);
            string est = dgv.SelectedRows[0].Cells["estado"].Value?.ToString();
            if (est == "ANULADO") { MessageBox.Show("Este comprobante ya está anulado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            if (MessageBox.Show("¿Anular este comprobante?\nEsta acción no se puede deshacer.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("UPDATE comprobantes SET estado='ANULADO' WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    CargarComprobantes();
                }
                catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void ImprimirSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un comprobante.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["id"].Value);
            ImprimirComprobante(id);
        }

        private void ImprimirComprobante(int id)
        {
            try
            {
                // Cargar datos del comprobante
                string tipo = "", serie = "", numero = "", clienteDoc = "", clienteNombre = "", clienteDir = "";
                decimal subtotal = 0, igv = 0, total = 0;
                DateTime fecha = DateTime.Now;
                string empresaNombre = Sesion.EmpresaActiva?.Nombre ?? "";

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT c.tipo, c.serie, c.numero, c.fecha_emision,
                                          c.cliente_doc, c.cliente_nombre, c.cliente_dir,
                                          c.subtotal, c.igv, c.total
                                   FROM comprobantes c WHERE c.id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", id);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                tipo          = dr.GetString(0);
                                serie         = dr.GetString(1);
                                numero        = dr.GetString(2);
                                fecha         = dr.GetDateTime(3);
                                clienteDoc    = dr.IsDBNull(4) ? "" : dr.GetString(4);
                                clienteNombre = dr.IsDBNull(5) ? "" : dr.GetString(5);
                                clienteDir    = dr.IsDBNull(6) ? "" : dr.GetString(6);
                                subtotal      = dr.GetDecimal(7);
                                igv           = dr.GetDecimal(8);
                                total         = dr.GetDecimal(9);
                            }
                        }
                    }
                }

                // Vista previa de impresión
                var pd = new PrintDocument();
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    float x = 40, y = 20;
                    float lineH = 20;

                    // Cabecera empresa
                    g.DrawString(empresaNombre, new Font("Arial", 13, FontStyle.Bold), Brushes.Black, x, y); y += 28;
                    g.DrawString(Sesion.SucursalActiva?.Nombre ?? "", new Font("Arial", 9), Brushes.Gray, x, y); y += lineH;
                    y += 10;

                    // Tipo de comprobante
                    string titulo = tipo == "BOLETA" ? "BOLETA DE VENTA ELECTRÓNICA" : "FACTURA ELECTRÓNICA";
                    g.DrawString(titulo, new Font("Arial", 12, FontStyle.Bold), tipo == "BOLETA" ? Brushes.DarkBlue : Brushes.DarkGreen, x, y); y += 26;
                    g.DrawString($"N° {serie}-{numero}", new Font("Arial", 11, FontStyle.Bold), Brushes.Black, x, y); y += lineH;
                    g.DrawString($"Fecha: {fecha:dd/MM/yyyy HH:mm}", new Font("Arial", 9), Brushes.Black, x, y); y += lineH + 6;

                    // Línea separadora
                    g.DrawLine(Pens.Gray, x, y, 550, y); y += 10;

                    // Datos cliente
                    g.DrawString("DATOS DEL CLIENTE", new Font("Arial", 8, FontStyle.Bold), Brushes.Gray, x, y); y += lineH;
                    g.DrawString($"Doc.: {clienteDoc}", new Font("Arial", 9), Brushes.Black, x, y); y += lineH;
                    g.DrawString($"Nombre: {clienteNombre}", new Font("Arial", 9), Brushes.Black, x, y); y += lineH;
                    if (!string.IsNullOrEmpty(clienteDir))
                    {
                        g.DrawString($"Dirección: {clienteDir}", new Font("Arial", 9), Brushes.Black, x, y); y += lineH;
                    }
                    y += 6;
                    g.DrawLine(Pens.Gray, x, y, 550, y); y += 10;

                    // Totales
                    g.DrawString($"Subtotal:", new Font("Arial", 10), Brushes.Black, x, y);
                    g.DrawString($"S/ {subtotal:N2}", new Font("Arial", 10), Brushes.Black, 420, y); y += lineH;
                    g.DrawString($"IGV (18%):", new Font("Arial", 10), Brushes.Black, x, y);
                    g.DrawString($"S/ {igv:N2}", new Font("Arial", 10), Brushes.Black, 420, y); y += lineH;

                    g.DrawLine(new Pen(Color.Black, 2), x, y, 550, y); y += 8;
                    g.DrawString("TOTAL A PAGAR:", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y);
                    g.DrawString($"S/ {total:N2}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 400, y); y += 30;

                    // Pie
                    g.DrawLine(Pens.Gray, x, y, 550, y); y += 10;
                    g.DrawString("Representación impresa del comprobante electrónico", new Font("Arial", 7), Brushes.Gray, x, y); y += 16;
                    g.DrawString("Consulte su comprobante en www.sunat.gob.pe", new Font("Arial", 7), Brushes.Gray, x, y);
                };

                var preview = new PrintPreviewDialog { Document = pd, Width = 650, Height = 800, Text = $"Vista previa — {tipo} {serie}-{numero}" };
                preview.ShowDialog(this);
            }
            catch (Exception ex) { MessageBox.Show("Error al imprimir:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Colorear columna TIPO y ESTADO ────────────────────────────────
        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dgv.Columns["tipo"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string t = e.Value.ToString();
                Color color = t == "BOLETA" ? cAzul : cVerde;
                using (var br = new SolidBrush(Color.FromArgb(20, color)))
                    e.Graphics.FillRectangle(br, e.CellBounds);
                using (var br = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(t, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
                return;
            }

            if (e.ColumnIndex == dgv.Columns["estado"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string st = e.Value.ToString();
                Color color = st == "EMITIDO" ? cVerde : Color.FromArgb(180, 50, 50);
                using (var br = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(st, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
            }
        }
    }

    // =========================================================================
    //  FORMULARIO — Emitir Boleta o Factura
    // =========================================================================
    public class FrmEmitirComprobante : Form
    {
        private readonly Color cFondo  = Color.FromArgb(250, 247, 240);
        private readonly Color cOro    = Color.FromArgb(160, 120, 40);
        private readonly Color cBoton  = Color.FromArgb(100, 80,  45);
        private readonly Color cTexto  = Color.FromArgb(50,  40,  20);
        private readonly Color cInput  = Color.FromArgb(255, 252, 245);
        private readonly Color cAzul   = Color.FromArgb(30,  80,  160);
        private readonly Color cVerde  = Color.FromArgb(30,  130, 80);

        private ComboBox cboVenta, cboCliente;
        private TextBox txtClienteDoc, txtClienteNombre, txtClienteDir;
        private Label lblSerie, lblSubtotal, lblIgv, lblTotal;
        private Button btnGuardar, btnCancelar;

        private readonly string _tipo;

        public FrmEmitirComprobante(string tipo)
        {
            _tipo = tipo;
            this.Text            = tipo == "BOLETA" ? "Emitir Boleta de Venta" : "Emitir Factura Electrónica";
            this.Size            = new Size(560, 540);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false; this.MinimizeBox = false;
            this.BackColor       = cFondo;
            this.DialogResult    = DialogResult.Cancel;
            Inicializar();
            CargarVentas();
            GenerarSerie();
        }

        private void Inicializar()
        {
            Color colorTipo = _tipo == "BOLETA" ? cAzul : cVerde;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = colorTipo };
            var lblTit = new Label { Text = _tipo == "BOLETA" ? "🧾  Nueva Boleta de Venta" : "📄  Nueva Factura Electrónica", Font = new Font("Arial", 13, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = false, Size = new Size(510, 56), Location = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft };
            pnlHeader.Controls.Add(lblTit);

            int y = 72;

            // Serie (auto)
            AddLabel("Serie / Número (auto-generado)", 20, y);
            lblSerie = new Label { Text = "---", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = colorTipo, BackColor = Color.Transparent, AutoSize = false, Size = new Size(500, 26), Location = new Point(20, y + 20) };
            this.Controls.Add(lblSerie);
            y += 58;

            // Venta origen
            AddLabel("Venta de origen", 20, y);
            cboVenta = new ComboBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = cInput };
            cboVenta.SelectedIndexChanged += CboVenta_Changed;
            this.Controls.Add(cboVenta);
            y += 60;

            // Datos del cliente
            AddLabel(_tipo == "BOLETA" ? "DNI del cliente (opcional)" : "RUC del cliente *", 20, y);
            txtClienteDoc = new TextBox { Location = new Point(20, y + 20), Size = new Size(240, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteDoc);
            y += 60;

            AddLabel("Nombre / Razón Social", 20, y);
            txtClienteNombre = new TextBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteNombre);
            y += 60;

            AddLabel("Dirección del cliente", 20, y);
            txtClienteDir = new TextBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteDir);
            y += 60;

            // Resumen
            var pnlResumen = new Panel { Location = new Point(20, y), Size = new Size(500, 70), BackColor = Color.FromArgb(240, 230, 210) };
            pnlResumen.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(200, 185, 155))) e.Graphics.DrawRectangle(pen, 0, 0, 499, 69);
            };
            lblSubtotal = new Label { Text = "Subtotal: S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 8) };
            lblIgv      = new Label { Text = "IGV (18%): S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 28) };
            lblTotal    = new Label { Text = "TOTAL: S/ 0.00", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = colorTipo, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 46) };
            pnlResumen.Controls.AddRange(new Control[] { lblSubtotal, lblIgv, lblTotal });
            this.Controls.Add(pnlResumen);
            y += 82;

            btnGuardar = new Button { Text = "✔  EMITIR COMPROBANTE", Size = new Size(244, 42), Location = new Point(20, y), BackColor = colorTipo, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button { Text = "Cancelar", Size = new Size(244, 42), Location = new Point(276, y), BackColor = Color.FromArgb(200, 190, 170), ForeColor = cTexto, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10), Cursor = Cursors.Hand };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { pnlHeader, btnGuardar, btnCancelar });
            this.Height = y + 90;
        }

        private void AddLabel(string texto, int x, int y)
        {
            this.Controls.Add(new Label { Text = texto, Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = false, Size = new Size(500, 16), Location = new Point(x, y) });
        }

        private void CargarVentas()
        {
            try
            {
                cboVenta.Items.Clear();
                cboVenta.Items.Add("-- Seleccione venta --");
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT v.id, v.numero_venta, v.total, c.nombre
                                   FROM ventas v
                                   LEFT JOIN clientes c ON v.cliente_id = c.id
                                   WHERE v.empresa_id = @eid AND v.estado = 'COMPLETADA'
                                     AND v.id NOT IN (SELECT venta_id FROM comprobantes WHERE venta_id IS NOT NULL AND estado != 'ANULADO')
                                   ORDER BY v.fecha DESC LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                cboVenta.Items.Add($"{dr.GetInt32(0)}|{dr.GetString(1)}|{dr.GetDecimal(2)}|{(dr.IsDBNull(3) ? "CLIENTE GENERAL" : dr.GetString(3))}");
                    }
                }
                cboVenta.SelectedIndex = 0;
                cboVenta.FormattingEnabled = true;
                cboVenta.Format += (s, e) => { if (e.ListItem.ToString().Contains("|")) e.Value = e.ListItem.ToString().Split('|')[1] + " — " + e.ListItem.ToString().Split('|')[3] + " (S/ " + decimal.Parse(e.ListItem.ToString().Split('|')[2]).ToString("N2") + ")"; };
            }
            catch { }
        }

        private void CboVenta_Changed(object sender, EventArgs e)
        {
            if (cboVenta.SelectedIndex <= 0) return;
            var parts = cboVenta.SelectedItem.ToString().Split('|');
            if (parts.Length < 4) return;

            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;

            lblSubtotal.Text = $"Subtotal: S/ {subtotal:N2}";
            lblIgv.Text      = $"IGV (18%): S/ {igv:N2}";
            lblTotal.Text    = $"TOTAL: S/ {total:N2}";

            if (string.IsNullOrWhiteSpace(txtClienteNombre.Text))
                txtClienteNombre.Text = parts[3] == "CLIENTE GENERAL" ? "" : parts[3];
        }

        private void GenerarSerie()
        {
            try
            {
                string prefijo = _tipo == "BOLETA" ? "B001" : "F001";
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM comprobantes WHERE tipo=@t AND empresa_id=@eid";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("t",   _tipo);
                        cmd.Parameters.AddWithValue("eid", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        long count = (long)cmd.ExecuteScalar();
                        lblSerie.Text = $"{prefijo}-{(count + 1):D8}";
                    }
                }
            }
            catch { lblSerie.Text = _tipo == "BOLETA" ? "B001-00000001" : "F001-00000001"; }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (cboVenta.SelectedIndex <= 0) { MessageBox.Show("Seleccione una venta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_tipo == "FACTURA" && string.IsNullOrWhiteSpace(txtClienteDoc.Text)) { MessageBox.Show("Ingrese el RUC del cliente para la factura.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var parts    = cboVenta.SelectedItem.ToString().Split('|');
            int ventaId  = int.Parse(parts[0]);
            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;

            string[] serieParts = lblSerie.Text.Split('-');
            string serie  = serieParts[0];
            string numero = serieParts.Length > 1 ? serieParts[1] : "00000001";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"INSERT INTO comprobantes(empresa_id, sucursal_id, venta_id, tipo, serie, numero,
                                                             cliente_doc, cliente_nombre, cliente_dir,
                                                             subtotal, igv, total, usuario_id)
                                   VALUES(@eid, @sid, @vid, @tipo, @ser, @num,
                                          @cdoc, @cnom, @cdir,
                                          @sub, @igv, @tot, @uid)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid",  Sesion.UsuarioActivo?.EmpresaId  ?? 1);
                        cmd.Parameters.AddWithValue("sid",  Sesion.UsuarioActivo?.SucursalId ?? 1);
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
                    }
                }
                MessageBox.Show($"✅ {_tipo} emitida correctamente.\nSerie: {serie}-{numero}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error al emitir:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}