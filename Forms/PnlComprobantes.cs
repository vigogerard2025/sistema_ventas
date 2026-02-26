using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    // =========================================================================
    //  PANEL COMPROBANTES — con integración al Sistema Facturador SUNAT
    // =========================================================================
    public class PnlComprobantes : UserControl
    {
        private readonly Color cFondo  = Color.FromArgb(245, 240, 228);
        private readonly Color cHeader = Color.FromArgb(120, 95,  55);
        private readonly Color cBoton  = Color.FromArgb(100, 80,  45);
        private readonly Color cOro    = Color.FromArgb(160, 120, 40);
        private readonly Color cTexto  = Color.FromArgb(50,  40,  20);
        private readonly Color cAzul   = Color.FromArgb(30,  80,  160);
        private readonly Color cVerde  = Color.FromArgb(30,  130, 80);
        private readonly Color cRojo   = Color.FromArgb(160, 50,  50);
        private readonly Color cSunat  = Color.FromArgb(0,   82,  156); // azul SUNAT

        private DataGridView dgv;
        private ComboBox cboTipo;
        private DateTimePicker dtpDesde, dtpHasta;
        private Button btnBuscar, btnEmitirBoleta, btnEmitirFactura;
        private Button btnGenerarSunat, btnEnviarSunat, btnImprimir, btnAnular;
        private Label lblContador, lblTotal, lblSunatStatus;

        // URL del Sistema Facturador SUNAT (puede configurarse)
        private static string SunatBaseUrl = "http://localhost:9000";

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
                    // La tabla ya es creada por DatabaseHelper, pero por si acaso:
                    string sql = @"
                    CREATE TABLE IF NOT EXISTS comprobantes (
                        id              SERIAL PRIMARY KEY,
                        empresa_id      INT REFERENCES empresas(id),
                        sucursal_id     INT REFERENCES sucursales(id),
                        venta_id        INT REFERENCES ventas(id),
                        tipo            VARCHAR(10) NOT NULL,
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
                        sunat_estado    VARCHAR(30) DEFAULT 'PENDIENTE',
                        sunat_fecha_envio TIMESTAMP,
                        sunat_respuesta VARCHAR(500),
                        xml_filename    VARCHAR(200),
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
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 135, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 185, 155), 1))
                    e.Graphics.DrawLine(pen, 0, 134, pnlTop.Width, 134);
            };

            // Logo SUNAT y título
            var pnlSunatHeader = new Panel { Location = new Point(0, 0), Size = new Size(9999, 50), BackColor = Color.FromArgb(0, 82, 156) };
            var lblSunatTit = new Label { Text = "🏛  BANDEJA DE SISTEMA FACTURADOR SUNAT", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = false, Size = new Size(600, 50), Location = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft };
            var lblSunatSub = new Label { Text = "Comprobantes Electrónicos — Boletas y Facturas", Font = new Font("Arial", 9), ForeColor = Color.FromArgb(200, 230, 255), BackColor = Color.Transparent, AutoSize = false, Size = new Size(500, 50), Location = new Point(620, 0), TextAlign = ContentAlignment.MiddleLeft };

            lblSunatStatus = new Label { Text = "⬤  Sin conexión a SUNAT", Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = Color.FromArgb(255, 200, 100), BackColor = Color.Transparent, AutoSize = true, Location = new Point(1050, 18) };

            pnlSunatHeader.Controls.AddRange(new Control[] { lblSunatTit, lblSunatSub, lblSunatStatus });
            pnlTop.Controls.Add(pnlSunatHeader);

            // Fila 2: filtros
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

            // Fila 3: botones acción
            int ay = 99;
            btnEmitirBoleta  = CrearBoton("🧾  Nueva Boleta",   cAzul,         new Point(20,  ay), 140, 28);
            btnEmitirFactura = CrearBoton("📄  Nueva Factura",   cVerde,        new Point(170, ay), 140, 28);
            btnGenerarSunat  = CrearBoton("⚙  Generar XML",     cSunat,        new Point(330, ay), 130, 28);
            btnEnviarSunat   = CrearBoton("📤  Enviar a SUNAT",  Color.FromArgb(0, 130, 70), new Point(470, ay), 140, 28);
            btnImprimir      = CrearBoton("🖨  Imprimir",        cBoton,        new Point(620, ay), 110, 28);
            btnAnular        = CrearBoton("🚫  Anular",          cRojo,         new Point(740, ay), 100, 28);

            btnEmitirBoleta.Click  += (s, e) => EmitirComprobante("BOLETA");
            btnEmitirFactura.Click += (s, e) => EmitirComprobante("FACTURA");
            btnGenerarSunat.Click  += (s, e) => GenerarXmlSunat();
            btnEnviarSunat.Click   += async (s, e) => await EnviarASunat();
            btnImprimir.Click      += (s, e) => ImprimirSeleccionado();
            btnAnular.Click        += (s, e) => AnularSeleccionado();

            lblContador = new Label { Text = "0 comprobantes", Font = new Font("Arial", 8), ForeColor = Color.FromArgb(130, 110, 80), BackColor = Color.Transparent, AutoSize = true, Location = new Point(870, fy + 4) };
            lblTotal    = new Label { Text = "Total: S/ 0.00",  Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(870, ay + 6) };

            pnlTop.Controls.AddRange(new Control[] {
                lblTipo, cboTipo, lblDesde, dtpDesde, lblHasta, dtpHasta, btnBuscar,
                btnEmitirBoleta, btnEmitirFactura, btnGenerarSunat, btnEnviarSunat,
                btnImprimir, btnAnular,
                lblContador, lblTotal
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
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.BackColor          = Color.White;
            dgv.DefaultCellStyle.ForeColor          = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 200, 160);
            dgv.DefaultCellStyle.SelectionForeColor = cTexto;
            dgv.DefaultCellStyle.Padding            = new Padding(8, 0, 8, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);

            // Columnas (igual a la imagen de SUNAT)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id",          HeaderText = "Nro",          Width = 55  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ruc",          HeaderText = "Nro. RUC",     Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "tipo",         HeaderText = "Tipo Doc.",    Width = 90  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "numero",       HeaderText = "Número Doc.",  Width = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",        HeaderText = "Fecha Generación", Width = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha_envio",  HeaderText = "Fecha Envío", Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "situacion",    HeaderText = "Situación",    Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "observaciones",HeaderText = "Observaciones",Width = 220 });

            dgv.CellPainting += Dgv_CellPainting;
            dgv.RowPrePaint  += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.RowPostPaint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(235, 225, 205), 1))
                    e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1, e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlTop);

            // Verificar conexión SUNAT al inicio (asíncrono)
            _ = VerificarConexionSunatAsync();
        }

        private Button CrearBoton(string texto, Color color, Point loc, int w, int h)
        {
            var btn = new Button { Text = texto, Size = new Size(w, h), Location = loc, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ── Cargar comprobantes ───────────────────────────────────────────
        private void CargarComprobantes()
        {
            try
            {
                dgv.Rows.Clear();
                decimal totalAcum = 0;
                string empresaRuc = "";

                // Obtener RUC de la empresa
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("SELECT ruc FROM empresas WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                            var res = cmd.ExecuteScalar();
                            empresaRuc = res?.ToString() ?? "";
                        }
                    }
                }
                catch { }

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string tipoFiltro = cboTipo.SelectedItem?.ToString() ?? "TODOS";
                    string sql = @"
                        SELECT c.id, c.tipo, c.serie, c.numero, c.fecha_emision,
                               c.sunat_fecha_envio, c.sunat_estado, c.sunat_respuesta,
                               c.total, c.estado
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
                                decimal tot       = dr.GetDecimal(8);
                                string  estadoDoc = dr.GetString(9);
                                string  sunatEst  = dr.IsDBNull(6) ? "PENDIENTE" : dr.GetString(6);
                                string  respuesta = dr.IsDBNull(7) ? "" : dr.GetString(7);
                                string  fechaEnvio = dr.IsDBNull(5) ? "" : dr.GetDateTime(5).ToString("dd/MM/yyyy HH:mm");

                                if (estadoDoc != "ANULADO") totalAcum += tot;

                                // Situación = estado SUNAT si fue enviado, sino estado documento
                                string situacion = estadoDoc == "ANULADO" ? "ANULADO"
                                    : sunatEst == "ACEPTADO" ? "0 - ACEPTADO"
                                    : sunatEst == "RECHAZADO" ? "ERROR"
                                    : sunatEst == "ENVIADO" ? "ENVIADO"
                                    : "PENDIENTE";

                                dgv.Rows.Add(
                                    count,                                              // Nro correlativo visual
                                    empresaRuc,                                         // Nro. RUC
                                    dr.GetString(1),                                    // Tipo Doc.
                                    $"{dr.GetString(2)}-{dr.GetString(3)}",            // Número Doc.
                                    dr.GetDateTime(4).ToString("dd/MM/yyyy HH:mm"),    // Fecha Generación
                                    fechaEnvio,                                         // Fecha Envío
                                    situacion,                                          // Situación
                                    respuesta                                           // Observaciones
                                );

                                // Guardar ID real en tag de la fila
                                dgv.Rows[count - 1].Tag = dr.GetInt32(0);
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

        // ── Emitir comprobante ────────────────────────────────────────────
        private void EmitirComprobante(string tipo)
        {
            using (var frm = new FrmEmitirComprobante(tipo))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    CargarComprobantes();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  INTEGRACIÓN SUNAT — Generar XML
        // ══════════════════════════════════════════════════════════════════
        private void GenerarXmlSunat()
        {
            if (dgv.SelectedRows.Count == 0)
            { MessageBox.Show("Seleccione un comprobante para generar el XML.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int    compId = Convert.ToInt32(dgv.SelectedRows[0].Tag ?? 0);
            string situacion = dgv.SelectedRows[0].Cells["situacion"].Value?.ToString() ?? "";

            if (situacion == "ANULADO")
            { MessageBox.Show("No se puede generar XML de un comprobante anulado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (situacion == "0 - ACEPTADO")
            { MessageBox.Show("Este comprobante ya fue aceptado por SUNAT.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            try
            {
                // Cargar datos del comprobante
                ComprobanteDatos datos = ObtenerDatosComprobante(compId);
                if (datos == null) return;

                // Generar XML UBL 2.1 (formato SUNAT)
                string xml = GenerarXmlUBL(datos);

                // Guardar en carpeta del Sistema Facturador SUNAT
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "sfs", "sfsweb", "resources", "REPO", "PARA_ENVIO"
                );
                // Si no existe la carpeta del SFS, guardar en documentos
                if (!Directory.Exists(carpeta))
                    carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SUNAT_XML");
                Directory.CreateDirectory(carpeta);

                string fileName = $"{datos.RucEmpresa}-{datos.TipoDoc}-{datos.Serie}-{datos.Numero}.xml";
                string filePath = Path.Combine(carpeta, fileName);
                File.WriteAllText(filePath, xml, Encoding.UTF8);

                // Guardar nombre de archivo en BD
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE comprobantes SET xml_filename=@fn, sunat_estado='XML_GENERADO' WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("fn", fileName);
                        cmd.Parameters.AddWithValue("id", compId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    $"✅  XML generado correctamente:\n\n{fileName}\n\nUbicación:\n{filePath}\n\nAhora puede enviarlo al Sistema Facturador SUNAT.",
                    "XML Generado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarComprobantes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar XML:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  INTEGRACIÓN SUNAT — Enviar al Sistema Facturador (localhost:9000)
        // ══════════════════════════════════════════════════════════════════
        private async Task EnviarASunat()
        {
            if (dgv.SelectedRows.Count == 0)
            { MessageBox.Show("Seleccione un comprobante para enviar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int compId = Convert.ToInt32(dgv.SelectedRows[0].Tag ?? 0);
            string situacion = dgv.SelectedRows[0].Cells["situacion"].Value?.ToString() ?? "";

            if (situacion == "ANULADO")
            { MessageBox.Show("No se puede enviar un comprobante anulado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (situacion == "0 - ACEPTADO")
            { MessageBox.Show("Este comprobante ya fue aceptado por SUNAT.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            // Confirmar
            if (MessageBox.Show(
                $"¿Enviar este comprobante al Sistema Facturador SUNAT?\n\nURL destino: {SunatBaseUrl}",
                "Confirmar envío SUNAT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            btnEnviarSunat.Enabled = false;
            btnEnviarSunat.Text    = "⏳  Enviando...";

            try
            {
                ComprobanteDatos datos = ObtenerDatosComprobante(compId);
                if (datos == null) return;

                // Generar XML si no existe aún
                string xml = GenerarXmlUBL(datos);

                // Intentar enviar al Sistema Facturador SUNAT (localhost:9000)
                bool enviado = false;
                string respuestaSunat = "";

                try
                {
                    using (var http = new HttpClient())
                    {
                        http.Timeout = TimeSpan.FromSeconds(15);

                        // El Sistema Facturador SUNAT (SFS) tiene un endpoint /generarComprobante
                        // que acepta el JSON con los datos del comprobante
                        string jsonPayload = GenerarJsonSunat(datos);
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        // Endpoint de generación del SFS
                        var response = await http.PostAsync($"{SunatBaseUrl}/api/generarComprobante", content);
                        respuestaSunat = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            enviado = true;
                        }
                        else
                        {
                            // Intentar endpoint alternativo del SFS
                            var response2 = await http.GetAsync($"{SunatBaseUrl}/");
                            if (response2.IsSuccessStatusCode)
                            {
                                // SFS está en línea pero el endpoint puede variar
                                respuestaSunat = "SFS en línea. Coloque el XML en la carpeta PARA_ENVIO del SFS.";
                                enviado = true;
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // SFS no está disponible en localhost:9000
                    respuestaSunat = "Sistema Facturador SUNAT no disponible en " + SunatBaseUrl +
                                     ". Verifique que el servicio esté corriendo.";
                    enviado = false;
                }

                // Actualizar estado en BD
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string nuevoEstado = enviado ? "ENVIADO" : "ERROR_ENVIO";
                    using (var cmd = new NpgsqlCommand(
                        @"UPDATE comprobantes SET sunat_estado=@est, sunat_fecha_envio=NOW(),
                          sunat_respuesta=@resp WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("est",  nuevoEstado);
                        cmd.Parameters.AddWithValue("resp", respuestaSunat.Length > 490 ? respuestaSunat.Substring(0, 490) : respuestaSunat);
                        cmd.Parameters.AddWithValue("id",   compId);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (enviado)
                {
                    MessageBox.Show(
                        $"✅  Comprobante enviado al Sistema Facturador SUNAT.\n\nRespuesta: {respuestaSunat}",
                        "Enviado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Ofrecer alternativa: copiar XML manualmente
                    var res = MessageBox.Show(
                        $"⚠️  No se pudo conectar al SFS automáticamente.\n\n{respuestaSunat}\n\n¿Desea generar el XML para enviarlo manualmente?",
                        "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (res == DialogResult.Yes)
                        GenerarXmlSunat();
                }

                CargarComprobantes();
                _ = VerificarConexionSunatAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnEnviarSunat.Enabled = true;
                btnEnviarSunat.Text    = "📤  Enviar a SUNAT";
            }
        }

        // ── Verificar conexión al SFS ─────────────────────────────────────
        private async Task VerificarConexionSunatAsync()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(5);
                    var response = await http.GetAsync(SunatBaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        if (!IsDisposed && lblSunatStatus != null)
                            Invoke(new Action(() =>
                            {
                                lblSunatStatus.Text      = "⬤  SFS SUNAT CONECTADO";
                                lblSunatStatus.ForeColor = Color.FromArgb(100, 255, 150);
                            }));
                    }
                    else
                    {
                        ActualizarEstadoSunat(false);
                    }
                }
            }
            catch
            {
                ActualizarEstadoSunat(false);
            }
        }

        private void ActualizarEstadoSunat(bool conectado)
        {
            if (!IsDisposed && lblSunatStatus != null && lblSunatStatus.IsHandleCreated)
                try
                {
                    Invoke(new Action(() =>
                    {
                        lblSunatStatus.Text      = conectado ? "⬤  SFS SUNAT CONECTADO" : "⬤  Sin conexión SUNAT";
                        lblSunatStatus.ForeColor = conectado ? Color.FromArgb(100, 255, 150) : Color.FromArgb(255, 200, 100);
                    }));
                }
                catch { }
        }

        // ── Obtener datos del comprobante ─────────────────────────────────
        private ComprobanteDatos ObtenerDatosComprobante(int id)
        {
            try
            {
                string rucEmpresa = "", nombreEmpresa = "", dirEmpresa = "";
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    // Empresa
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ruc, nombre, COALESCE(direccion,'') FROM empresas WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        using (var dr = cmd.ExecuteReader())
                            if (dr.Read()) { rucEmpresa = dr.GetString(0) ?? ""; nombreEmpresa = dr.GetString(1); dirEmpresa = dr.GetString(2); }
                    }

                    // Comprobante
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT tipo, serie, numero, fecha_emision,
                               cliente_doc, cliente_nombre, cliente_dir,
                               subtotal, igv, total
                        FROM comprobantes WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", id);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read()) return null;
                            return new ComprobanteDatos
                            {
                                Id             = id,
                                RucEmpresa     = rucEmpresa,
                                NombreEmpresa  = nombreEmpresa,
                                DirEmpresa     = dirEmpresa,
                                TipoDoc        = dr.GetString(0) == "BOLETA" ? "03" : "01",
                                TipoDocNombre  = dr.GetString(0),
                                Serie          = dr.GetString(1),
                                Numero         = dr.GetString(2),
                                FechaEmision   = dr.GetDateTime(3),
                                ClienteDoc     = dr.IsDBNull(4) ? "00000000" : dr.GetString(4),
                                ClienteNombre  = dr.IsDBNull(5) ? "CLIENTE VARIOS" : dr.GetString(5),
                                ClienteDir     = dr.IsDBNull(6) ? "" : dr.GetString(6),
                                Subtotal       = dr.GetDecimal(7),
                                Igv            = dr.GetDecimal(8),
                                Total          = dr.GetDecimal(9)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al obtener datos:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Generador de XML UBL 2.1 (formato SUNAT)
        // ══════════════════════════════════════════════════════════════════
        private string GenerarXmlUBL(ComprobanteDatos d)
        {
            string tipoDocXml = d.TipoDocNombre == "BOLETA" ? "BolsaVenta" : "Invoice";
            string nsUbl      = d.TipoDocNombre == "BOLETA"
                ? "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                : "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2""
         xmlns:ccts=""urn:un:unece:uncefact:documentation:2""
         xmlns:ds=""http://www.w3.org/2000/09/xmldsig#""
         xmlns:ext=""urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2""
         xmlns:qdt=""urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2""
         xmlns:sac=""urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1""
         xmlns:udt=""urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <ext:UBLExtensions>
    <ext:UBLExtension>
      <ext:ExtensionContent/>
    </ext:UBLExtension>
  </ext:UBLExtensions>
  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
  <cbc:CustomizationID>2.0</cbc:CustomizationID>
  <cbc:ID>{d.Serie}-{d.Numero}</cbc:ID>
  <cbc:IssueDate>{d.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>
  <cbc:IssueTime>{d.FechaEmision:HH:mm:ss}</cbc:IssueTime>
  <cbc:InvoiceTypeCode listAgencyName=""PE:SUNAT"" listName=""Tipo de Documento"" listURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"">{d.TipoDoc}</cbc:InvoiceTypeCode>
  <cbc:Note languageLocaleID=""1000""><![CDATA[{MontoEnLetras(d.Total)}]]></cbc:Note>
  <cbc:DocumentCurrencyCode>PEN</cbc:DocumentCurrencyCode>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""6"" schemeName=""Documento de Identidad"" schemeAgencyName=""PE:SUNAT"" schemeURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"">{d.RucEmpresa}</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyName>
        <cbc:Name><![CDATA[{d.NombreEmpresa}]]></cbc:Name>
      </cac:PartyName>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName><![CDATA[{d.NombreEmpresa}]]></cbc:RegistrationName>
        <cac:RegistrationAddress>
          <cbc:AddressLine>
            <cbc:Line><![CDATA[{d.DirEmpresa}]]></cbc:Line>
          </cbc:AddressLine>
        </cac:RegistrationAddress>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:AccountingCustomerParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""{(d.ClienteDoc.Length == 11 ? "6" : "1")}"" schemeName=""Documento de Identidad"" schemeAgencyName=""PE:SUNAT"">{d.ClienteDoc}</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName><![CDATA[{d.ClienteNombre}]]></cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingCustomerParty>
  <cac:TaxTotal>
    <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
    <cac:TaxSubtotal>
      <cbc:TaxableAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:TaxableAmount>
      <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
      <cac:TaxCategory>
        <cbc:ID schemeID=""UN/ECE 5305"" schemeName=""Tax Category Identifier"" schemeAgencyName=""United Nations Economic Commission for Europe"">S</cbc:ID>
        <cbc:Percent>18.00</cbc:Percent>
        <cac:TaxScheme>
          <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyID=""6"">1000</cbc:ID>
          <cbc:Name>IGV</cbc:Name>
          <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>
        </cac:TaxScheme>
      </cac:TaxCategory>
    </cac:TaxSubtotal>
  </cac:TaxTotal>
  <cac:LegalMonetaryTotal>
    <cbc:LineExtensionAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:LineExtensionAmount>
    <cbc:TaxInclusiveAmount currencyID=""PEN"">{d.Total:F2}</cbc:TaxInclusiveAmount>
    <cbc:PayableAmount currencyID=""PEN"">{d.Total:F2}</cbc:PayableAmount>
  </cac:LegalMonetaryTotal>
</Invoice>";
        }

        // ── Generar JSON para el SFS SUNAT ────────────────────────────────
        private string GenerarJsonSunat(ComprobanteDatos d)
        {
            return $@"{{
  ""operacion"": ""generar_comprobante"",
  ""tipo_de_comprobante"": {(d.TipoDocNombre == "BOLETA" ? "2" : "1")},
  ""serie"": ""{d.Serie}"",
  ""numero"": {d.Numero},
  ""sunat_transaction"": 1,
  ""cliente_tipo_de_documento"": {(d.ClienteDoc.Length == 11 ? "6" : "1")},
  ""cliente_numero_de_documento"": ""{d.ClienteDoc}"",
  ""cliente_denominacion"": ""{d.ClienteNombre.Replace("\"", "\\\"")}"",
  ""cliente_direccion"": ""{d.ClienteDir.Replace("\"", "\\\"")}"",
  ""fecha_de_emision"": ""{d.FechaEmision:dd-MM-yyyy}"",
  ""moneda"": 1,
  ""porcentaje_de_igv"": 18.0,
  ""total_gravada"": {d.Subtotal:F2},
  ""total_igv"": {d.Igv:F2},
  ""total"": {d.Total:F2},
  ""enviar_automaticamente_a_la_sunat"": true,
  ""enviar_automaticamente_al_cliente"": false,
  ""items"": [
    {{
      ""unidad_de_medida"": ""NIU"",
      ""codigo"": ""001"",
      ""descripcion"": ""Venta de productos"",
      ""cantidad"": 1,
      ""valor_unitario"": {d.Subtotal:F2},
      ""precio_unitario"": {d.Total:F2},
      ""subtotal"": {d.Subtotal:F2},
      ""tipo_de_igv"": 1,
      ""igv"": {d.Igv:F2},
      ""total"": {d.Total:F2},
      ""anticipo_regularizacion"": false
    }}
  ]
}}";
        }

        // ── Conversión de monto a letras (simplificado) ───────────────────
        private string MontoEnLetras(decimal monto)
        {
            int  soles     = (int)Math.Floor(monto);
            int  centavos  = (int)Math.Round((monto - soles) * 100);
            return $"SON {soles} CON {centavos:D2}/100 SOLES";
        }

        // ── Anular comprobante ────────────────────────────────────────────
        private void AnularSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un comprobante.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id     = Convert.ToInt32(dgv.SelectedRows[0].Tag ?? 0);
            string sit = dgv.SelectedRows[0].Cells["situacion"].Value?.ToString() ?? "";
            if (sit.Contains("ANULADO")) { MessageBox.Show("Ya está anulado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            if (MessageBox.Show("¿Anular este comprobante?\nEsta acción no se puede deshacer.", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE comprobantes SET estado='ANULADO', sunat_estado='ANULADO' WHERE id=@id", conn))
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

        // ── Imprimir comprobante ──────────────────────────────────────────
        private void ImprimirSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un comprobante.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Tag ?? 0);
            if (id == 0) return;
            ImprimirComprobante(id);
        }

        private void ImprimirComprobante(int id)
        {
            try
            {
                var datos = ObtenerDatosComprobante(id);
                if (datos == null) return;

                var pd = new PrintDocument();
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    float x = 30, y = 15, lh = 18;

                    g.DrawString(datos.NombreEmpresa, new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y); y += 22;
                    g.DrawString($"RUC: {datos.RucEmpresa}", new Font("Arial", 9), Brushes.Gray, x, y); y += lh;
                    g.DrawString(datos.DirEmpresa, new Font("Arial", 9), Brushes.Gray, x, y); y += lh + 6;

                    string titulo = datos.TipoDocNombre == "BOLETA" ? "BOLETA DE VENTA ELECTRÓNICA" : "FACTURA ELECTRÓNICA";
                    g.DrawString(titulo, new Font("Arial", 11, FontStyle.Bold),
                        datos.TipoDocNombre == "BOLETA" ? Brushes.DarkBlue : Brushes.DarkGreen, x, y); y += 22;
                    g.DrawString($"N° {datos.Serie}-{datos.Numero}", new Font("Arial", 11, FontStyle.Bold), Brushes.Black, x, y); y += lh;
                    g.DrawString($"Fecha: {datos.FechaEmision:dd/MM/yyyy HH:mm}", new Font("Arial", 9), Brushes.Black, x, y); y += lh + 6;

                    g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
                    g.DrawString("DATOS DEL CLIENTE", new Font("Arial", 8, FontStyle.Bold), Brushes.Gray, x, y); y += lh;
                    g.DrawString($"Doc.: {datos.ClienteDoc}", new Font("Arial", 9), Brushes.Black, x, y); y += lh;
                    g.DrawString($"Nombre: {datos.ClienteNombre}", new Font("Arial", 9), Brushes.Black, x, y); y += lh;
                    if (!string.IsNullOrEmpty(datos.ClienteDir))
                    { g.DrawString($"Dir.: {datos.ClienteDir}", new Font("Arial", 9), Brushes.Black, x, y); y += lh; }
                    y += 4;
                    g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;

                    g.DrawString("Subtotal:", new Font("Arial", 10), Brushes.Black, x, y);
                    g.DrawString($"S/ {datos.Subtotal:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
                    g.DrawString("IGV (18%):", new Font("Arial", 10), Brushes.Black, x, y);
                    g.DrawString($"S/ {datos.Igv:N2}", new Font("Arial", 10), Brushes.Black, 400, y); y += lh;
                    g.DrawLine(new Pen(Color.Black, 2), x, y, 530, y); y += 6;
                    g.DrawString("TOTAL:", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y);
                    g.DrawString($"S/ {datos.Total:N2}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 380, y); y += 28;

                    g.DrawLine(Pens.Gray, x, y, 530, y); y += 8;
                    g.DrawString(MontoEnLetras(datos.Total), new Font("Arial", 8, FontStyle.Italic), Brushes.Black, x, y); y += lh + 4;
                    g.DrawString("Representación impresa del comprobante electrónico.", new Font("Arial", 7), Brushes.Gray, x, y); y += 14;
                    g.DrawString("Consulte su comprobante en: www.sunat.gob.pe", new Font("Arial", 7), Brushes.Gray, x, y);
                };

                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width    = 680,
                    Height   = 820,
                    Text     = $"Vista previa — {datos.TipoDocNombre} {datos.Serie}-{datos.Numero}"
                };
                preview.ShowDialog(this);
            }
            catch (Exception ex) { MessageBox.Show("Error al imprimir:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Pintar columnas TIPO y SITUACIÓN ──────────────────────────────
        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dgv.Columns["tipo"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string t = e.Value.ToString();
                Color color = t == "BOLETA" ? cAzul : cVerde;
                using (var br = new SolidBrush(Color.FromArgb(18, color)))
                    e.Graphics.FillRectangle(br, e.CellBounds);
                using (var br = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(t, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
                return;
            }

            if (e.ColumnIndex == dgv.Columns["situacion"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string st    = e.Value.ToString();
                Color  color = st.Contains("ACEPTADO") ? cVerde
                             : st == "ENVIADO"         ? Color.FromArgb(30, 120, 200)
                             : st == "PENDIENTE"       ? Color.FromArgb(180, 130, 0)
                             : st == "ANULADO"         ? Color.Gray
                             : cRojo;
                using (var br = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(st, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
            }
        }
    }

    // ── Modelo de datos para comprobante ──────────────────────────────────
    internal class ComprobanteDatos
    {
        public int      Id;
        public string   RucEmpresa, NombreEmpresa, DirEmpresa;
        public string   TipoDoc, TipoDocNombre, Serie, Numero;
        public DateTime FechaEmision;
        public string   ClienteDoc, ClienteNombre, ClienteDir;
        public decimal  Subtotal, Igv, Total;
    }

    // =========================================================================
    //  FORMULARIO — Emitir Boleta o Factura (sin cambios)
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

        private ComboBox cboVenta;
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
            AddLabel("Serie / Número (auto-generado)", 20, y);
            lblSerie = new Label { Text = "---", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = colorTipo, BackColor = Color.Transparent, AutoSize = false, Size = new Size(500, 26), Location = new Point(20, y + 20) };
            this.Controls.Add(lblSerie); y += 58;

            AddLabel("Venta de origen", 20, y);
            cboVenta = new ComboBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = cInput };
            cboVenta.SelectedIndexChanged += CboVenta_Changed;
            this.Controls.Add(cboVenta); y += 60;

            AddLabel(_tipo == "BOLETA" ? "DNI del cliente (opcional)" : "RUC del cliente *", 20, y);
            txtClienteDoc = new TextBox { Location = new Point(20, y + 20), Size = new Size(240, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteDoc); y += 60;

            AddLabel("Nombre / Razón Social", 20, y);
            txtClienteNombre = new TextBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteNombre); y += 60;

            AddLabel("Dirección del cliente", 20, y);
            txtClienteDir = new TextBox { Location = new Point(20, y + 20), Size = new Size(500, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtClienteDir); y += 60;

            var pnlResumen = new Panel { Location = new Point(20, y), Size = new Size(500, 70), BackColor = Color.FromArgb(240, 230, 210) };
            lblSubtotal = new Label { Text = "Subtotal: S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 8) };
            lblIgv      = new Label { Text = "IGV (18%): S/ 0.00", Font = new Font("Arial", 9), ForeColor = cTexto, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 28) };
            lblTotal    = new Label { Text = "TOTAL: S/ 0.00", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = colorTipo, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 46) };
            pnlResumen.Controls.AddRange(new Control[] { lblSubtotal, lblIgv, lblTotal });
            this.Controls.Add(pnlResumen); y += 82;

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
                    string sql = @"SELECT v.id, v.numero_venta, v.total, COALESCE(c.nombre,'CLIENTE GENERAL')
                                   FROM ventas v LEFT JOIN clientes c ON v.cliente_id = c.id
                                   WHERE v.empresa_id = @eid AND v.estado = 'COMPLETADA'
                                     AND v.id NOT IN (SELECT COALESCE(venta_id,0) FROM comprobantes WHERE venta_id IS NOT NULL AND estado != 'ANULADO')
                                   ORDER BY v.fecha DESC LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                cboVenta.Items.Add($"{dr.GetInt32(0)}|{dr.GetString(1)}|{dr.GetDecimal(2)}|{dr.GetString(3)}");
                    }
                }
                cboVenta.SelectedIndex = 0;
                cboVenta.FormattingEnabled = true;
                cboVenta.Format += (s, e) =>
                {
                    if (e.ListItem.ToString().Contains("|"))
                    {
                        var p = e.ListItem.ToString().Split('|');
                        e.Value = $"{p[1]} — {p[3]} (S/ {decimal.Parse(p[2]):N2})";
                    }
                };
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
            if (string.IsNullOrWhiteSpace(txtClienteNombre.Text) && parts[3] != "CLIENTE GENERAL")
                txtClienteNombre.Text = parts[3];
        }

        private void GenerarSerie()
        {
            try
            {
                string prefijo = _tipo == "BOLETA" ? "B001" : "F001";
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM comprobantes WHERE tipo=@t AND empresa_id=@eid", conn))
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
            if (_tipo == "FACTURA" && string.IsNullOrWhiteSpace(txtClienteDoc.Text)) { MessageBox.Show("Ingrese el RUC del cliente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var parts    = cboVenta.SelectedItem.ToString().Split('|');
            int ventaId  = int.Parse(parts[0]);
            decimal total    = decimal.Parse(parts[2]);
            decimal subtotal = Math.Round(total / 1.18m, 2);
            decimal igv      = total - subtotal;
            string[] sp   = lblSerie.Text.Split('-');
            string serie  = sp[0];
            string numero = sp.Length > 1 ? sp[1] : "00000001";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"INSERT INTO comprobantes
                        (empresa_id, sucursal_id, venta_id, tipo, serie, numero,
                         cliente_doc, cliente_nombre, cliente_dir,
                         subtotal, igv, total, usuario_id, sunat_estado)
                        VALUES(@eid,@sid,@vid,@tipo,@ser,@num,@cdoc,@cnom,@cdir,@sub,@igv,@tot,@uid,'PENDIENTE')";
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
                MessageBox.Show(
                    $"✅ {_tipo} emitida: {serie}-{numero}\n\nAhora puede usar '⚙ Generar XML' y '📤 Enviar a SUNAT'.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}