using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;

namespace SistemaVentas.Forms
{
    public class PnlProductos : UserControl
    {
        // ── Paleta ─────────────────────────────────────────────────────────
        private readonly Color cFondo   = Color.FromArgb(245, 240, 228);
        private readonly Color cHeader  = Color.FromArgb(120, 95,  55);
        private readonly Color cOro     = Color.FromArgb(160, 120, 40);
        private readonly Color cBoton   = Color.FromArgb(100, 80,  45);
        private readonly Color cVerde   = Color.FromArgb(21,  101, 192);
        private readonly Color cRojo    = Color.FromArgb(183, 28,  28);
        private readonly Color cTexto   = Color.FromArgb(50,  40,  20);

        // Campos formulario
        private TextBox txtCodigo, txtNombre, txtPresentacion, txtMarca;
        private TextBox txtDescripcion, txtPrecioCompra, txtPrecioVenta;
        private TextBox txtStock, txtStockMin, txtBuscar;
        private ComboBox cboCategoria;
        private Button btnNuevo, btnGuardar, btnEliminar, btnBuscar;
        private DataGridView dgv;
        private Label lblContador;
        private int idSeleccionado = 0;

        public PnlProductos()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            InicializarComponentes();
            CargarCategorias();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            // ── Cabecera ──────────────────────────────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 185, 155), 1))
                    e.Graphics.DrawLine(pen, 0, 55, pnlTop.Width, 55);
            };

            var lblTitulo = new Label
            {
                Text = "📦  GESTIÓN DE PRODUCTOS / SERVICIOS",
                Font = new Font("Arial", 15, FontStyle.Bold),
                ForeColor = cBoton, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(600, 36),
                Location = new Point(20, 10), TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTop.Controls.Add(lblTitulo);

            // ── Panel principal: izquierdo (form) + derecho (grid) ─────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = cFondo };

            // ══════════════════════════════════════════════════════════════
            // FORMULARIO IZQUIERDO
            // ══════════════════════════════════════════════════════════════
            var pnlForm = new Panel
            {
                Width     = 370,
                Dock      = DockStyle.Left,
                BackColor = Color.White,
                Padding   = new Padding(15)
            };
            pnlForm.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 205, 175), 1))
                    e.Graphics.DrawLine(pen, pnlForm.Width - 1, 0, pnlForm.Width - 1, pnlForm.Height);
            };

            int y = 15;

            // Código
            AddFormLabel(pnlForm, "CÓDIGO *", y);
            txtCodigo = AddFormInput(pnlForm, y + 18, 320);
            y += 58;

            // Nombre
            AddFormLabel(pnlForm, "NOMBRE DEL PRODUCTO *", y);
            txtNombre = AddFormInput(pnlForm, y + 18, 320);
            y += 58;

            // Presentación (NUEVO)
            AddFormLabel(pnlForm, "PRESENTACIÓN", y);
            txtPresentacion = AddFormInput(pnlForm, y + 18, 320);
            txtPresentacion.PlaceholderText = "Ej: CAJA, UNIDAD, BOLSA...";
            y += 58;

            // Marca (NUEVO)
            AddFormLabel(pnlForm, "MARCA", y);
            txtMarca = AddFormInput(pnlForm, y + 18, 320);
            txtMarca.Text = "SIN MARCA";
            y += 58;

            // Categoría
            AddFormLabel(pnlForm, "CATEGORÍA", y);
            cboCategoria = new ComboBox
            {
                Location      = new Point(15, y + 18),
                Size          = new Size(320, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Arial", 9),
                BackColor     = Color.FromArgb(250, 247, 240)
            };
            pnlForm.Controls.Add(cboCategoria);
            y += 58;

            // Descripción
            AddFormLabel(pnlForm, "DESCRIPCIÓN", y);
            txtDescripcion = new TextBox
            {
                Location    = new Point(15, y + 18),
                Size        = new Size(320, 50),
                Multiline   = true,
                Font        = new Font("Arial", 9),
                BackColor   = Color.FromArgb(250, 247, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlForm.Controls.Add(txtDescripcion);
            y += 78;

            // Precios en línea
            AddFormLabel(pnlForm, "P. COMPRA (S/)", y);
            var lblPV = new Label { Text = "P. VENTA (S/)", Font = new Font("Arial", 7, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = false, Size = new Size(155, 14), Location = new Point(175, y), TextAlign = ContentAlignment.MiddleLeft };
            pnlForm.Controls.Add(lblPV);

            txtPrecioCompra = new TextBox { Location = new Point(15, y + 18), Size = new Size(150, 28), Font = new Font("Arial", 10), BackColor = Color.FromArgb(250, 247, 240), BorderStyle = BorderStyle.FixedSingle, Text = "0.00", TextAlign = HorizontalAlignment.Right };
            txtPrecioVenta  = new TextBox { Location = new Point(175, y + 18), Size = new Size(160, 28), Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(255, 249, 235), BorderStyle = BorderStyle.FixedSingle, Text = "0.00", TextAlign = HorizontalAlignment.Right, ForeColor = cBoton };
            pnlForm.Controls.AddRange(new Control[] { txtPrecioCompra, txtPrecioVenta });
            y += 58;

            // Stock en línea
            AddFormLabel(pnlForm, "STOCK ACTUAL", y);
            var lblSMin = new Label { Text = "STOCK MÍNIMO", Font = new Font("Arial", 7, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = false, Size = new Size(155, 14), Location = new Point(175, y), TextAlign = ContentAlignment.MiddleLeft };
            pnlForm.Controls.Add(lblSMin);

            txtStock    = new TextBox { Location = new Point(15, y + 18), Size = new Size(150, 28), Font = new Font("Arial", 10), BackColor = Color.FromArgb(250, 247, 240), BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Right };
            txtStockMin = new TextBox { Location = new Point(175, y + 18), Size = new Size(160, 28), Font = new Font("Arial", 10), BackColor = Color.FromArgb(250, 247, 240), BorderStyle = BorderStyle.FixedSingle, Text = "5", TextAlign = HorizontalAlignment.Right };
            pnlForm.Controls.AddRange(new Control[] { txtStock, txtStockMin });
            y += 58;

            // Separador
            var sep = new Panel { Location = new Point(15, y), Size = new Size(320, 1), BackColor = Color.FromArgb(220, 205, 175) };
            pnlForm.Controls.Add(sep);
            y += 12;

            // Botones
            btnNuevo = CrearBoton("➕  NUEVO",    new Point(15,  y), 100, Color.FromArgb(55, 105, 185));
            btnGuardar = CrearBoton("💾  GUARDAR", new Point(120, y), 110, cBoton);
            btnEliminar = CrearBoton("🗑  ELIMINAR",new Point(235, y), 100, cRojo);

            btnNuevo.Click    += (s, e) => LimpiarFormulario();
            btnGuardar.Click  += BtnGuardar_Click;
            btnEliminar.Click += BtnEliminar_Click;

            pnlForm.Controls.AddRange(new Control[] { btnNuevo, btnGuardar, btnEliminar });

            // ══════════════════════════════════════════════════════════════
            // PANEL DERECHO — Tabla de productos
            // ══════════════════════════════════════════════════════════════
            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = cFondo, Padding = new Padding(8) };

            // Barra de búsqueda
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };

            txtBuscar = new TextBox
            {
                Location    = new Point(8, 10),
                Size        = new Size(380, 30),
                Font        = new Font("Arial", 10),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor   = cTexto
            };
            txtBuscar.GotFocus  += (s, e) => { if (txtBuscar.Text == "🔍  Buscar por código, nombre o marca...") { txtBuscar.Text = ""; txtBuscar.ForeColor = cTexto; } };
            txtBuscar.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) { txtBuscar.Text = "🔍  Buscar por código, nombre o marca..."; txtBuscar.ForeColor = Color.LightGray; } };
            txtBuscar.Text      = "🔍  Buscar por código, nombre o marca...";
            txtBuscar.ForeColor = Color.LightGray;
            // Búsqueda en tiempo real
            txtBuscar.TextChanged += (s, e) => {
                if (txtBuscar.Text != "🔍  Buscar por código, nombre o marca...")
                    CargarProductos(txtBuscar.Text.Trim());
            };

            btnBuscar = new Button { Text = "Buscar", Location = new Point(398, 9), Size = new Size(80, 30), BackColor = cBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarProductos(txtBuscar.Text == "🔍  Buscar por código, nombre o marca..." ? "" : txtBuscar.Text.Trim());

            lblContador = new Label { Text = "0 productos", Font = new Font("Arial", 8), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(490, 15) };

            pnlSearch.Controls.AddRange(new Control[] { txtBuscar, btnBuscar, lblContador });

            // DataGridView — igual al de la imagen
            dgv = new DataGridView
            {
                Dock                       = DockStyle.Fill,
                BackgroundColor            = Color.White,
                BorderStyle                = BorderStyle.None,
                RowHeadersVisible          = false,
                AllowUserToAddRows         = false,
                AllowUserToDeleteRows      = false,
                ReadOnly                   = true,
                SelectionMode              = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                = false,
                Font                       = new Font("Arial", 9),
                CellBorderStyle            = DataGridViewCellBorderStyle.None,
                RowTemplate                = { Height = 26 },
                AutoSizeColumnsMode        = DataGridViewAutoSizeColumnsMode.None
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor  = cHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding    = new Padding(6, 0, 0, 0);
            dgv.ColumnHeadersHeight = 34;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor          = Color.White;
            dgv.DefaultCellStyle.ForeColor          = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 100, 210);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Padding            = new Padding(6, 0, 6, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 244, 235);

            // Columnas (orden igual a la imagen)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColId",     HeaderText = "Código",        Width = 70  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNombre", HeaderText = "Nombre del Producto", Width = 240 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPres",   HeaderText = "Presentacion",  Width = 140 });
dgv.Columns.Add(new DataGridViewTextBoxColumn
{
    Name = "ColPrecio",
    HeaderText = "Precio",
    Width = 80,
    DefaultCellStyle =
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N2" // 2 decimales
    },
});            dgv.Columns.Add(new DataGridViewTextBoxColumn
{
    Name = "ColStock",
    HeaderText = "Stock",
    Width = 70,
    DefaultCellStyle =
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0" // entero sin decimales
    }
});
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColMarca",  HeaderText = "Marca",         Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColIdReal", HeaderText = "ID",            Width = 0, Visible = false });

            dgv.CellClick      += DgvCellClick;
            dgv.RowPrePaint    += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.RowPostPaint   += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(230, 220, 200), 1))
                    e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1, e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };
            dgv.CellPainting += Dgv_CellPainting;

            pnlRight.Controls.Add(dgv);
            pnlRight.Controls.Add(pnlSearch);

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlForm);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlTop);
        }

        // ── Helpers de UI ─────────────────────────────────────────────────
        private void AddFormLabel(Control parent, string texto, int y)
        {
            parent.Controls.Add(new Label
            {
                Text      = texto,
                Font      = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = cOro,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(320, 14),
                Location  = new Point(15, y),
                TextAlign = ContentAlignment.MiddleLeft
            });
        }

        private TextBox AddFormInput(Control parent, int y, int width)
        {
            var tb = new TextBox
            {
                Location    = new Point(15, y),
                Size        = new Size(width, 28),
                Font        = new Font("Arial", 10),
                BackColor   = Color.FromArgb(250, 247, 240),
                ForeColor   = cTexto,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private Button CrearBoton(string texto, Point loc, int w, Color color)
        {
            var btn = new Button
            {
                Text      = texto,
                Location  = loc,
                Size      = new Size(w, 34),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ── Cargar categorías en combo ─────────────────────────────────────
        private void CargarCategorias()
        {
            try
            {
                cboCategoria.Items.Clear();
                cboCategoria.Items.Add(new Models.Categoria { Id = 0, Nombre = "-- Sin categoría --" });
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nombre FROM categorias ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboCategoria.Items.Add(new Models.Categoria { Id = dr.GetInt32(0), Nombre = dr.GetString(1) });
                }
                cboCategoria.SelectedIndex = 0;
            }
            catch { }
        }

        // ── Cargar productos en grid ───────────────────────────────────────
       private void CargarProductos(string filtro = "")
{
    dgv.Rows.Clear();

    try
    {
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            string sql = @"
                SELECT p.codigo, p.nombre,
                       COALESCE(p.presentacion, p.nombre) as presentacion,
                       p.precio_venta, 
                       p.stock,
                       COALESCE(p.marca, 'SIN MARCA') as marca,
                       p.id
                FROM productos p
                WHERE p.activo = true
                  AND (
                       p.codigo   ILIKE @b
                    OR p.nombre   ILIKE @b
                    OR COALESCE(p.marca,'') ILIKE @b
                    OR COALESCE(p.presentacion,'') ILIKE @b
                  )
                ORDER BY p.codigo DESC";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("b", "%" + filtro + "%");

                using (var dr = cmd.ExecuteReader())
                {
                    int count = 0;

                    while (dr.Read())
                    {
                        count++;

                        int rowIdx = dgv.Rows.Add(
                            dr.GetString(0),   // Código
                            dr.GetString(1),   // Nombre
                            dr.GetString(2),   // Presentación
                            dr.GetDecimal(3),  // Precio (DECIMAL ✔)
                            dr.GetInt32(4),    // Stock (INT ✔)
                            dr.GetString(5),   // Marca
                            dr.GetInt32(6)     // ID oculto
                        );

                        int stock = dr.GetInt32(4);

                        if (stock <= 0)
                            dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(200, 50, 50);
                        else if (stock <= 5)
                            dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(200, 120, 0);
                    }

                    lblContador.Text = $"{count} producto{(count != 1 ? "s" : "")}";
                }
            }
        }
    }
    catch
    {
        // Silencioso para búsqueda en tiempo real
    }
}
        // ── Click en fila ─────────────────────────────────────────────────
        private void DgvCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int realId = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["ColIdReal"].Value);
            idSeleccionado = realId;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT codigo, nombre, descripcion, categoria_id,
                                          precio_compra, precio_venta, stock, stock_minimo,
                                          COALESCE(presentacion,''), COALESCE(marca,'SIN MARCA')
                                   FROM productos WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", realId);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtCodigo.Text       = dr.GetString(0);
                                txtNombre.Text       = dr.GetString(1);
                                txtDescripcion.Text  = dr.IsDBNull(2) ? "" : dr.GetString(2);
                                txtPrecioCompra.Text = dr.GetDecimal(4).ToString("N2");
                                txtPrecioVenta.Text  = dr.GetDecimal(5).ToString("N2");
                                txtStock.Text        = dr.GetInt32(6).ToString();
                                txtStockMin.Text     = dr.GetInt32(7).ToString();
                                txtPresentacion.Text = dr.GetString(8);
                                txtMarca.Text        = dr.GetString(9);

                                // Seleccionar categoría en combo
                                int catId = dr.IsDBNull(3) ? 0 : dr.GetInt32(3);
                                for (int i = 0; i < cboCategoria.Items.Count; i++)
                                    if ((cboCategoria.Items[i] as Models.Categoria)?.Id == catId)
                                    { cboCategoria.SelectedIndex = i; break; }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // ── Colorear fila seleccionada (azul como en la imagen) ───────────
        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            // La selección azul ya la maneja DefaultCellStyle
        }

        // ── Guardar ───────────────────────────────────────────────────────
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) || string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("Código y Nombre son obligatorios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtPrecioCompra.Text, out decimal pc)) pc = 0;
            if (!decimal.TryParse(txtPrecioVenta.Text,  out decimal pv)) pv = 0;
            if (!int.TryParse(txtStock.Text,    out int stock))   stock = 0;
            if (!int.TryParse(txtStockMin.Text, out int stockMin)) stockMin = 5;

            int catId = (cboCategoria.SelectedItem as Models.Categoria)?.Id ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = idSeleccionado == 0
                        ? @"INSERT INTO productos(codigo, nombre, descripcion, categoria_id,
                                                  precio_compra, precio_venta, stock, stock_minimo,
                                                  presentacion, marca)
                            VALUES(@cod, @nom, @des, @cat, @pc, @pv, @stk, @smin, @pres, @marca)"
                        : @"UPDATE productos SET
                                codigo=@cod, nombre=@nom, descripcion=@des, categoria_id=@cat,
                                precio_compra=@pc, precio_venta=@pv, stock=@stk, stock_minimo=@smin,
                                presentacion=@pres, marca=@marca
                            WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("cod",  txtCodigo.Text.Trim());
                        cmd.Parameters.AddWithValue("nom",  txtNombre.Text.Trim());
                        cmd.Parameters.AddWithValue("des",  txtDescripcion.Text.Trim());
                        cmd.Parameters.AddWithValue("cat",  catId > 0 ? (object)catId : DBNull.Value);
                        cmd.Parameters.AddWithValue("pc",   pc);
                        cmd.Parameters.AddWithValue("pv",   pv);
                        cmd.Parameters.AddWithValue("stk",  stock);
                        cmd.Parameters.AddWithValue("smin", stockMin);
                        cmd.Parameters.AddWithValue("pres", txtPresentacion.Text.Trim());
                        cmd.Parameters.AddWithValue("marca",txtMarca.Text.Trim());
                        if (idSeleccionado > 0)
                            cmd.Parameters.AddWithValue("id", idSeleccionado);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("✅  Producto guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarFormulario();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Eliminar ──────────────────────────────────────────────────────
        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == 0)
            { MessageBox.Show("Seleccione un producto primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("¿Eliminar este producto?\nSus datos históricos se conservarán.", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE productos SET activo=false WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", idSeleccionado);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Producto eliminado.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarFormulario();
                CargarProductos();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ── Limpiar formulario ────────────────────────────────────────────
        private void LimpiarFormulario()
        {
            idSeleccionado      = 0;
            txtCodigo.Clear();
            txtNombre.Clear();
            txtDescripcion.Clear();
            txtPresentacion.Clear();
            txtMarca.Text       = "SIN MARCA";
            txtPrecioCompra.Text = "0.00";
            txtPrecioVenta.Text  = "0.00";
            txtStock.Text        = "0";
            txtStockMin.Text     = "5";
            if (cboCategoria.Items.Count > 0) cboCategoria.SelectedIndex = 0;
        }
    }
}