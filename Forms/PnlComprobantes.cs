// ============================================================================
//  PnlComprobantes.cs  — Reemplaza tu archivo existente por completo
// ============================================================================
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;
using SistemaVentas.Services;    // ← nuevo namespace del SunatService

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
        private readonly Color cSunat  = Color.FromArgb(0,   82,  156);

        // ── Controles ──────────────────────────────────────────────────────
        private DataGridView dgv;
        private ComboBox     cboTipo;
        private DateTimePicker dtpDesde, dtpHasta;
        private Button btnBuscar, btnEmitirBoleta, btnEmitirFactura;
        private Button btnGenerarXml, btnEnviarSunat, btnImprimir, btnAnular;
        private Label  lblContador, lblTotal, lblSunatStatus;

        public PnlComprobantes()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            CrearTablaSiNoExiste();
            Inicializar();
            CargarComprobantes();
            _ = ActualizarEstadoSunatAsync();
        }

        // ── Crear tabla en BD si no existe ────────────────────────────────
        private void CrearTablaSiNoExiste()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS comprobantes (
                        id              SERIAL PRIMARY KEY,
                        empresa_id      INT REFERENCES empresas(id),
                        sucursal_id     INT REFERENCES sucursales(id),
                        venta_id        INT REFERENCES ventas(id),
                        tipo            VARCHAR(10)  NOT NULL,
                        serie           VARCHAR(10)  NOT NULL,
                        numero          VARCHAR(20)  NOT NULL,
                        fecha_emision   TIMESTAMP    DEFAULT NOW(),
                        cliente_doc     VARCHAR(20),
                        cliente_nombre  VARCHAR(200),
                        cliente_dir     VARCHAR(200),
                        subtotal        DECIMAL(12,2) DEFAULT 0,
                        igv             DECIMAL(12,2) DEFAULT 0,
                        total           DECIMAL(12,2) DEFAULT 0,
                        estado          VARCHAR(20)  DEFAULT 'EMITIDO',
                        usuario_id      INT REFERENCES usuarios(id),
                        sunat_estado    VARCHAR(30)  DEFAULT 'PENDIENTE',
                        sunat_fecha_envio TIMESTAMP,
                        sunat_respuesta VARCHAR(500),
                        xml_filename    VARCHAR(200),
                        UNIQUE(serie, numero)
                    );", conn);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        // ═════════════════════════════════════════════════════════════════
        //  INICIALIZAR UI
        // ═════════════════════════════════════════════════════════════════
        private void Inicializar()
        {
            // ── Barra superior ────────────────────────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 135, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(200, 185, 155), 1);
                e.Graphics.DrawLine(pen, 0, 134, pnlTop.Width, 134);
            };

            // Header azul SUNAT
            var pnlSunatHeader = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(9999, 50),
                BackColor = cSunat
            };

            var lblTit = new Label
            {
                Text      = "🏛  BANDEJA DE SISTEMA FACTURADOR SUNAT",
                Font      = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                AutoSize  = false, Size = new Size(650, 50),
                Location  = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft
            };

            // Configurar URL del SFS
            var btnConfig = new Button
            {
                Text      = "⚙ Configurar SFS",
                Size      = new Size(140, 28),
                Location  = new Point(870, 11),
                BackColor = Color.FromArgb(0, 55, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 8), Cursor = Cursors.Hand
            };
            btnConfig.FlatAppearance.BorderSize = 0;
            btnConfig.Click += BtnConfig_Click;

            lblSunatStatus = new Label
            {
                Text      = "⬤  Verificando...",
                Font      = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 100),
                BackColor = Color.Transparent, AutoSize = true,
                Location  = new Point(700, 18)
            };

            pnlSunatHeader.Controls.AddRange(new Control[] { lblTit, lblSunatStatus, btnConfig });
            pnlTop.Controls.Add(pnlSunatHeader);

            // ── Fila filtros ───────────────────────────────────────────────
            int fy = 60;
            var lblTipo = new Label { Text = "Tipo:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, fy + 5) };
            cboTipo = new ComboBox { Location = new Point(55, fy), Size = new Size(110, 28), Font = new Font("Arial", 9), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(250, 247, 240) };
            cboTipo.Items.AddRange(new object[] { "TODOS", "BOLETA", "FACTURA" });
            cboTipo.SelectedIndex = 0;

            var lblDesde = new Label { Text = "Desde:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(180, fy + 5) };
            dtpDesde = new DateTimePicker { Location = new Point(232, fy), Size = new Size(125, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

            var lblHasta = new Label { Text = "Hasta:", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(368, fy + 5) };
            dtpHasta = new DateTimePicker { Location = new Point(416, fy), Size = new Size(125, 28), Font = new Font("Arial", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            btnBuscar = CrearBoton("🔍  Buscar", cBoton, new Point(554, fy), 100, 30);
            btnBuscar.Click += (s, e) => CargarComprobantes();

            lblContador = new Label { Text = "0 comprobantes", Font = new Font("Arial", 8), ForeColor = Color.FromArgb(130, 110, 80), BackColor = Color.Transparent, AutoSize = true, Location = new Point(670, fy + 4) };
            lblTotal    = new Label { Text = "Total: S/ 0.00", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(670, fy + 22) };

            // ── Fila botones acción ───────────────────────────────────────
            int ay = 100;
            btnEmitirBoleta  = CrearBoton("🧾  Nueva Boleta",  cAzul,  new Point(20,  ay), 140, 28);
            btnEmitirFactura = CrearBoton("📄  Nueva Factura", cVerde, new Point(170, ay), 140, 28);
            btnGenerarXml    = CrearBoton("⚙  Generar XML",   cSunat, new Point(320, ay), 130, 28);
            btnEnviarSunat   = CrearBoton("📤  Enviar SUNAT",  Color.FromArgb(0, 130, 70), new Point(460, ay), 140, 28);
            btnImprimir      = CrearBoton("🖨  Imprimir",      cBoton, new Point(610, ay), 110, 28);
            btnAnular        = CrearBoton("🚫  Anular",        cRojo,  new Point(730, ay), 100, 28);

            btnEmitirBoleta.Click  += (s, e) => EmitirComprobante("BOLETA");
            btnEmitirFactura.Click += (s, e) => EmitirComprobante("FACTURA");
            btnGenerarXml.Click    += BtnGenerarXml_Click;
            btnEnviarSunat.Click   += async (s, e) => await BtnEnviarSunat_ClickAsync();
            btnImprimir.Click      += (s, e) => ImprimirSeleccionado();
            btnAnular.Click        += (s, e) => AnularSeleccionado();

            pnlTop.Controls.AddRange(new Control[] {
                lblTipo, cboTipo, lblDesde, dtpDesde, lblHasta, dtpHasta, btnBuscar,
                lblContador, lblTotal,
                btnEmitirBoleta, btnEmitirFactura, btnGenerarXml, btnEnviarSunat, btnImprimir, btnAnular
            });

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = cFondo,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Arial", 9), CellBorderStyle = DataGridViewCellBorderStyle.None,
                RowTemplate = { Height = 36 }, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                MultiSelect = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = cHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(8, 0, 0, 0);
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersBorderStyle     = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles    = false;
            dgv.DefaultCellStyle.BackColor   = Color.White;
            dgv.DefaultCellStyle.ForeColor   = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 200, 160);
            dgv.DefaultCellStyle.SelectionForeColor = cTexto;
            dgv.DefaultCellStyle.Padding     = new Padding(8, 0, 8, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "nro",        HeaderText = "Nro",              Width = 55  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ruc",        HeaderText = "Nro. RUC",         Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "tipo",       HeaderText = "Tipo Doc.",        Width = 90  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "numero",     HeaderText = "Número Doc.",      Width = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",      HeaderText = "Fecha Generación", Width = 145 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha_envio",HeaderText = "Fecha Envío",      Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "situacion",  HeaderText = "Situación",        Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "observaciones", HeaderText = "Observaciones", Width = 250 });

            dgv.CellPainting += Dgv_CellPainting;
            dgv.RowPrePaint  += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.RowPostPaint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 225, 205), 1);
                e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1, e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlTop);
        }

        // ═════════════════════════════════════════════════════════════════
        //  CARGAR COMPROBANTES
        // ═════════════════════════════════════════════════════════════════
        private void CargarComprobantes()
        {
            try
            {
                dgv.Rows.Clear();
                decimal totalAcum = 0;
                string  rucEmp    = ObtenerRucEmpresa();

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string tipo = cboTipo.SelectedItem?.ToString() ?? "TODOS";
                string sql  = @"
                    SELECT c.id, c.tipo, c.serie, c.numero, c.fecha_emision,
                           c.sunat_fecha_envio, c.sunat_estado, c.sunat_respuesta,
                           c.total, c.estado
                    FROM comprobantes c
                    WHERE c.empresa_id = @eid
                      AND c.fecha_emision BETWEEN @desde AND @hasta
                      AND (@tipo = 'TODOS' OR c.tipo = @tipo)
                    ORDER BY c.fecha_emision DESC";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("eid",   Sesion.UsuarioActivo?.EmpresaId ?? 1);
                cmd.Parameters.AddWithValue("desde", dtpDesde.Value.Date);
                cmd.Parameters.AddWithValue("hasta", dtpHasta.Value.Date.AddDays(1));
                cmd.Parameters.AddWithValue("tipo",  tipo);

                using var dr = cmd.ExecuteReader();
                int count = 0;
                while (dr.Read())
                {
                    count++;
                    decimal tot        = dr.GetDecimal(8);
                    string  estadoDoc  = dr.GetString(9);
                    string  sunatEst   = dr.IsDBNull(6) ? "PENDIENTE" : dr.GetString(6);
                    string  resp       = dr.IsDBNull(7) ? ""          : dr.GetString(7);
                    string  fechaEnvio = dr.IsDBNull(5) ? ""          : dr.GetDateTime(5).ToString("dd/MM/yyyy HH:mm");

                    if (estadoDoc != "ANULADO") totalAcum += tot;

                    string situacion = estadoDoc == "ANULADO" ? "ANULADO"
                        : sunatEst == "ACEPTADO"    ? "0 - ACEPTADO"
                        : sunatEst == "RECHAZADO"   ? "ERROR"
                        : sunatEst == "ENVIADO"     ? "ENVIADO"
                        : sunatEst == "XML_GENERADO"? "XML GENERADO"
                        : "PENDIENTE";

                    dgv.Rows.Add(count, rucEmp, dr.GetString(1),
                                 $"{dr.GetString(2)}-{dr.GetString(3)}",
                                 dr.GetDateTime(4).ToString("dd/MM/yyyy HH:mm"),
                                 fechaEnvio, situacion, resp);
                    dgv.Rows[count - 1].Tag = dr.GetInt32(0);
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

        // ═════════════════════════════════════════════════════════════════
        //  EMITIR COMPROBANTE
        // ═════════════════════════════════════════════════════════════════
        private void EmitirComprobante(string tipo)
        {
            using var frm = new FrmEmitirComprobante(tipo);
            if (frm.ShowDialog(this) == DialogResult.OK)
                CargarComprobantes();
        }

        // ═════════════════════════════════════════════════════════════════
        //  GENERAR XML
        // ═════════════════════════════════════════════════════════════════
        private void BtnGenerarXml_Click(object? sender, EventArgs e)
        {
            if (!VerificarSeleccion(out int compId, out string sit)) return;
            if (sit == "ANULADO")         { Aviso("No se puede generar XML de un comprobante anulado.");   return; }
            if (sit == "0 - ACEPTADO")    { Aviso("Este comprobante ya fue aceptado por SUNAT.");          return; }

            try
            {
                var datos = SunatSfsService.ObtenerDatos(compId);
                string ruta = SunatSfsService.GenerarXml(datos);

                MessageBox.Show(
                    $"✅  XML generado correctamente.\n\n" +
                    $"Archivo: {System.IO.Path.GetFileName(ruta)}\n\n" +
                    $"Ubicación:\n{ruta}\n\n" +
                    $"Ahora puede:\n" +
                    $"• Usar '📤 Enviar SUNAT' si el SFS está corriendo\n" +
                    $"• O copiar el XML a la carpeta PARA_ENVIO del SFS manualmente",
                    "XML Generado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarComprobantes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar XML:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  ENVIAR AL SFS SUNAT (async)
        // ═════════════════════════════════════════════════════════════════
        private async Task BtnEnviarSunat_ClickAsync()
        {
            if (!VerificarSeleccion(out int compId, out string sit)) return;
            if (sit == "ANULADO")      { Aviso("No se puede enviar un comprobante anulado.");  return; }
            if (sit == "0 - ACEPTADO") { Aviso("Este comprobante ya fue aceptado por SUNAT."); return; }

            var confirmar = MessageBox.Show(
                $"¿Enviar comprobante al Sistema Facturador SUNAT?\n\n" +
                $"URL del SFS: {SunatSfsService.UrlBase}",
                "Confirmar envío", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            btnEnviarSunat.Enabled = false;
            btnEnviarSunat.Text    = "⏳  Enviando...";

            try
            {
                var datos     = SunatSfsService.ObtenerDatos(compId);
                var resultado = await SunatSfsService.EnviarAsync(datos);

                if (resultado.Exito && resultado.Codigo == "0")
                {
                    MessageBox.Show(
                        $"✅  Comprobante ACEPTADO por SUNAT.\n\n" +
                        $"Código: {resultado.Codigo}\n" +
                        $"Descripción: {resultado.Descripcion}",
                        "Aceptado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (resultado.Codigo == "SFS_OFFLINE")
                {
                    MessageBox.Show(
                        $"⚠️  SFS no disponible.\n\n{resultado.Descripcion}\n\n" +
                        $"El XML fue generado. Puede:\n" +
                        $"1. Iniciar el SFS: java -jar facturadorApp-1.4.jar server prod.yaml\n" +
                        $"2. Luego reintentar el envío",
                        "SFS Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"❌  Error SUNAT.\n\nCódigo: {resultado.Codigo}\n{resultado.Descripcion}",
                        "Rechazado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                CargarComprobantes();
                _ = ActualizarEstadoSunatAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnEnviarSunat.Enabled = true;
                btnEnviarSunat.Text    = "📤  Enviar SUNAT";
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  CONFIGURAR URL DEL SFS
        // ═════════════════════════════════════════════════════════════════
        private void BtnConfig_Click(object? sender, EventArgs e)
        {
            string url = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingrese la URL del Sistema Facturador SUNAT\n(Por defecto: http://localhost:8080)",
                "Configurar SFS", SunatSfsService.UrlBase);

            if (!string.IsNullOrWhiteSpace(url))
            {
                SunatSfsService.UrlBase = url.TrimEnd('/');
                _ = ActualizarEstadoSunatAsync();
                Aviso($"URL actualizada:\n{SunatSfsService.UrlBase}\n\nVerificando conexión...");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  VERIFICAR CONEXIÓN SFS (async, no bloquea UI)
        // ═════════════════════════════════════════════════════════════════
        private async Task ActualizarEstadoSunatAsync()
        {
            bool conectado = await SunatSfsService.VerificarConexionAsync();
            if (!IsDisposed && lblSunatStatus != null && lblSunatStatus.IsHandleCreated)
            {
                try
                {
                    Invoke(() =>
                    {
                        lblSunatStatus.Text      = conectado ? "⬤  SFS CONECTADO ✓" : "⬤  SFS Sin conexión";
                        lblSunatStatus.ForeColor = conectado
                            ? Color.FromArgb(100, 255, 150)
                            : Color.FromArgb(255, 200, 100);
                    });
                }
                catch { }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  ANULAR
        // ═════════════════════════════════════════════════════════════════
        private void AnularSeleccionado()
        {
            if (!VerificarSeleccion(out int id, out string sit)) return;
            if (sit == "ANULADO") { Aviso("Ya está anulado."); return; }

            if (MessageBox.Show("¿Anular este comprobante?\nEsta acción no se puede deshacer.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new NpgsqlCommand(
                        "UPDATE comprobantes SET estado='ANULADO', sunat_estado='ANULADO' WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.ExecuteNonQuery();
                    CargarComprobantes();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  IMPRIMIR
        // ═════════════════════════════════════════════════════════════════
        private void ImprimirSeleccionado()
        {
            if (!VerificarSeleccion(out int id, out _)) return;
            try
            {
                var d  = SunatSfsService.ObtenerDatos(id);
                var pd = new PrintDocument();
                pd.PrintPage += (s, e) => ImprimirPagina(e, d);
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width    = 680, Height = 820,
                    Text     = $"Vista previa — {d.TipoDocNombre} {d.Serie}-{d.Numero}"
                };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirPagina(PrintPageEventArgs e, ComprobanteSunat d)
        {
            var g = e.Graphics!;
            float x = 30, y = 15, lh = 18;

            g.DrawString(d.NombreEmpresa, new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y); y += 22;
            g.DrawString($"RUC: {d.RucEmpresa}", new Font("Arial", 9), Brushes.Gray, x, y); y += lh;
            g.DrawString(d.DirEmpresa, new Font("Arial", 9), Brushes.Gray, x, y); y += lh + 6;

            string titulo = d.TipoDocNombre == "BOLETA"
                ? "BOLETA DE VENTA ELECTRÓNICA" : "FACTURA ELECTRÓNICA";
            var brushTipo = d.TipoDocNombre == "BOLETA" ? Brushes.DarkBlue : Brushes.DarkGreen;
            g.DrawString(titulo, new Font("Arial", 11, FontStyle.Bold), brushTipo, x, y); y += 22;
            g.DrawString($"N° {d.Serie}-{d.Numero}", new Font("Arial", 11, FontStyle.Bold), Brushes.Black, x, y); y += lh;
            g.DrawString($"Fecha: {d.FechaEmision:dd/MM/yyyy HH:mm}", new Font("Arial", 9), Brushes.Black, x, y); y += lh + 6;

            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
            g.DrawString("DATOS DEL CLIENTE", new Font("Arial", 8, FontStyle.Bold), Brushes.Gray, x, y); y += lh;
            g.DrawString($"Doc.: {d.ClienteDoc}", new Font("Arial", 9), Brushes.Black, x, y); y += lh;
            g.DrawString($"Nombre: {d.ClienteNombre}", new Font("Arial", 9), Brushes.Black, x, y); y += lh;
            if (!string.IsNullOrEmpty(d.ClienteDir))
            { g.DrawString($"Dir.: {d.ClienteDir}", new Font("Arial", 9), Brushes.Black, x, y); y += lh; }
            y += 4;
            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;

            g.DrawString("Subtotal:",   new Font("Arial", 10), Brushes.Black, x, y);
            g.DrawString($"S/ {d.Subtotal:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
            g.DrawString("IGV (18%):", new Font("Arial", 10), Brushes.Black, x, y);
            g.DrawString($"S/ {d.Igv:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
            g.DrawLine(new Pen(Color.Black, 2), x, y, 530, y); y += 6;
            g.DrawString("TOTAL:", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y);
            g.DrawString($"S/ {d.Total:N2}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 380, y); y += 28;

            g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
            string letras = $"SON {(int)d.Total} CON {(int)((d.Total - Math.Floor(d.Total)) * 100):D2}/100 SOLES";
            g.DrawString(letras, new Font("Arial", 8, FontStyle.Italic), Brushes.Black, x, y); y += lh + 4;
            g.DrawString("Representación impresa del comprobante electrónico.", new Font("Arial", 7), Brushes.Gray, x, y); y += 14;
            g.DrawString("Consulte su comprobante en: www.sunat.gob.pe", new Font("Arial", 7), Brushes.Gray, x, y);
        }

        // ═════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════
        private Button CrearBoton(string texto, Color color, Point loc, int w, int h)
        {
            var btn = new Button
            {
                Text = texto, Size = new Size(w, h), Location = loc,
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 8, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private bool VerificarSeleccion(out int compId, out string situacion)
        {
            compId    = 0;
            situacion = "";
            if (dgv.SelectedRows.Count == 0)
            {
                Aviso("Seleccione un comprobante primero.");
                return false;
            }
            compId    = Convert.ToInt32(dgv.SelectedRows[0].Tag ?? 0);
            situacion = dgv.SelectedRows[0].Cells["situacion"].Value?.ToString() ?? "";
            return compId > 0;
        }

        private string ObtenerRucEmpresa()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT ruc FROM empresas WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                return cmd.ExecuteScalar()?.ToString() ?? "";
            }
            catch { return ""; }
        }

        private void Aviso(string msg) =>
            MessageBox.Show(msg, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // ── Colorear columnas tipo y situación ────────────────────────────
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dgv.Columns["tipo"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string t     = e.Value.ToString()!;
                Color  color = t == "BOLETA" ? cAzul : cVerde;
                using var brBg = new SolidBrush(Color.FromArgb(18, color));
                e.Graphics!.FillRectangle(brBg, e.CellBounds);
                using var br = new SolidBrush(color);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
                return;
            }

            if (e.ColumnIndex == dgv.Columns["situacion"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string st    = e.Value.ToString()!;
                Color  color = st.Contains("ACEPTADO") ? cVerde
                             : st == "ENVIADO"         ? Color.FromArgb(30, 120, 200)
                             : st == "XML GENERADO"    ? Color.FromArgb(180, 130, 0)
                             : st == "PENDIENTE"       ? Color.FromArgb(180, 130, 0)
                             : st == "ANULADO"         ? Color.Gray
                             : cRojo;
                using var br = new SolidBrush(color);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics!.DrawString(st, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
            }
        }
    }

    // =========================================================================
    //  FORMULARIO — Emitir Boleta o Factura (sin cambios respecto al original)
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
            _tipo = tipo;
            this.Text            = tipo == "BOLETA" ? "Emitir Boleta de Venta" : "Emitir Factura Electrónica";
            this.Size            = new Size(560, 540);
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
            var pnlHeader   = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = colorTipo };
            var lblTit      = new Label
            {
                Text = _tipo == "BOLETA" ? "🧾  Nueva Boleta de Venta" : "📄  Nueva Factura Electrónica",
                Font = new Font("Arial", 13, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.Transparent, AutoSize = false, Size = new Size(510, 56),
                Location = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTit);

            int y = 72;
            AddLabel("Serie / Número (auto-generado)", 20, y);
            lblSerie = new Label
            {
                Text = "---", Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = colorTipo, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(500, 26), Location = new Point(20, y + 20)
            };
            this.Controls.Add(lblSerie); y += 58;

            AddLabel("Venta de origen", 20, y);
            cboVenta = new ComboBox
            {
                Location = new Point(20, y + 20), Size = new Size(500, 28),
                Font = new Font("Arial", 10), DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = cInput
            };
            cboVenta.SelectedIndexChanged += CboVenta_Changed;
            this.Controls.Add(cboVenta); y += 60;

            AddLabel(_tipo == "BOLETA" ? "DNI del cliente (opcional)" : "RUC del cliente *", 20, y);
            txtClienteDoc = new TextBox
            {
                Location = new Point(20, y + 20), Size = new Size(240, 28),
                Font = new Font("Arial", 10), BackColor = cInput,
                ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtClienteDoc); y += 60;

            AddLabel("Nombre / Razón Social", 20, y);
            txtClienteNombre = new TextBox
            {
                Location = new Point(20, y + 20), Size = new Size(500, 28),
                Font = new Font("Arial", 10), BackColor = cInput,
                ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtClienteNombre); y += 60;

            AddLabel("Dirección del cliente", 20, y);
            txtClienteDir = new TextBox
            {
                Location = new Point(20, y + 20), Size = new Size(500, 28),
                Font = new Font("Arial", 10), BackColor = cInput,
                ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtClienteDir); y += 60;

            var pnlResumen = new Panel { Location = new Point(20, y), Size = new Size(500, 70), BackColor = Color.FromArgb(240, 230, 210) };
            lblSubtotal = new Label { Text = "Subtotal: S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 8) };
            lblIgv      = new Label { Text = "IGV (18%): S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 28) };
            lblTotal    = new Label { Text = "TOTAL: S/ 0.00", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = colorTipo, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 46) };
            pnlResumen.Controls.AddRange(new Control[] { lblSubtotal, lblIgv, lblTotal });
            this.Controls.Add(pnlResumen); y += 82;

            btnGuardar = new Button
            {
                Text = "✔  EMITIR COMPROBANTE", Size = new Size(244, 42), Location = new Point(20, y),
                BackColor = colorTipo, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar", Size = new Size(244, 42), Location = new Point(276, y),
                BackColor = Color.FromArgb(200, 190, 170), ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10), Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { pnlHeader, btnGuardar, btnCancelar });
            this.Height = y + 90;
        }

        private void AddLabel(string texto, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = texto, Font = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = cOro, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(500, 16), Location = new Point(x, y)
            });
        }

        private void CargarVentas()
        {
            try
            {
                cboVenta.Items.Clear();
                cboVenta.Items.Add("-- Seleccione venta --");
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string sql = @"SELECT v.id, v.numero_venta, v.total, COALESCE(c.nombre,'CLIENTE GENERAL')
                               FROM ventas v LEFT JOIN clientes c ON v.cliente_id = c.id
                               WHERE v.empresa_id = @eid AND v.estado = 'COMPLETADA'
                                 AND v.id NOT IN (
                                     SELECT COALESCE(venta_id,0) FROM comprobantes
                                     WHERE venta_id IS NOT NULL AND estado != 'ANULADO')
                               ORDER BY v.fecha DESC LIMIT 100";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("eid", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    cboVenta.Items.Add($"{dr.GetInt32(0)}|{dr.GetString(1)}|{dr.GetDecimal(2)}|{dr.GetString(3)}");

                cboVenta.SelectedIndex     = 0;
                cboVenta.FormattingEnabled = true;
                cboVenta.Format += (s, e) =>
                {
                    if (e.ListItem!.ToString()!.Contains("|"))
                    {
                        var p   = e.ListItem.ToString()!.Split('|');
                        e.Value = $"{p[1]} — {p[3]} (S/ {decimal.Parse(p[2]):N2})";
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
                using var cmd  = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM comprobantes WHERE tipo=@t AND empresa_id=@eid", conn);
                cmd.Parameters.AddWithValue("t",   _tipo);
                cmd.Parameters.AddWithValue("eid", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                long count     = (long)cmd.ExecuteScalar()!;
                lblSerie.Text  = $"{prefijo}-{(count + 1):D8}";
            }
            catch { lblSerie.Text = _tipo == "BOLETA" ? "B001-00000001" : "F001-00000001"; }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (cboVenta.SelectedIndex <= 0)
            { MessageBox.Show("Seleccione una venta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_tipo == "FACTURA" && string.IsNullOrWhiteSpace(txtClienteDoc.Text))
            { MessageBox.Show("Ingrese el RUC del cliente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var parts    = cboVenta.SelectedItem!.ToString()!.Split('|');
            int ventaId  = int.Parse(parts[0]);
            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;
            string[] sp  = lblSerie.Text.Split('-');
            string serie = sp[0];
            string numero= sp.Length > 1 ? sp[1] : "00000001";

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string sql = @"INSERT INTO comprobantes
                    (empresa_id, sucursal_id, venta_id, tipo, serie, numero,
                     cliente_doc, cliente_nombre, cliente_dir,
                     subtotal, igv, total, usuario_id, sunat_estado)
                    VALUES(@eid,@sid,@vid,@tipo,@ser,@num,@cdoc,@cnom,@cdir,@sub,@igv,@tot,@uid,'PENDIENTE')";
                using var cmd = new NpgsqlCommand(sql, conn);
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

                MessageBox.Show(
                    $"✅ {_tipo} emitida: {serie}-{numero}\n\n" +
                    $"Siguiente paso:\n" +
                    $"1. Click '⚙ Generar XML' para crear el XML SUNAT\n" +
                    $"2. Click '📤 Enviar SUNAT' para enviar al SFS",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
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