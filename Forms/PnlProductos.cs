using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;

namespace SistemaVentas.Forms
{
    public class PnlProductos : UserControl
    {
        private DataGridView grid;
        private TextBox txtCodigo, txtNombre, txtDescripcion, txtPrecioCompra, txtPrecioVenta, txtStock, txtStockMin;
        private ComboBox cboCategoria;
        private Button btnNuevo, btnGuardar, btnEliminar, btnBuscar;
        private TextBox txtBuscar;
        private int idSeleccionado = 0;
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);

        public PnlProductos()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarCategorias();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label { Text = "📦  GESTIÓN DE PRODUCTOS", Font = new Font("Arial", 14, FontStyle.Bold),
                                   ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lbl);

            // --- FORMULARIO ---
            var pnlForm = new GroupBox { Text = "Datos del Producto", Location = new Point(15, 50),
                                          Size = new Size(480, 360), Font = new Font("Arial", 9, FontStyle.Bold) };

            int y = 25;
            txtCodigo       = AgregarCampo(pnlForm, "Código:",       y);      y += 40;
            txtNombre       = AgregarCampo(pnlForm, "Nombre:",       y);      y += 40;
            txtDescripcion  = AgregarCampo(pnlForm, "Descripción:",  y);      y += 40;

            pnlForm.Controls.Add(new Label { Text = "Categoría:", Location = new Point(10, y + 3), AutoSize = true });
            cboCategoria = new ComboBox { Location = new Point(120, y), Size = new Size(330, 28),
                                           DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9) };
            pnlForm.Controls.Add(cboCategoria); y += 40;

            txtPrecioCompra = AgregarCampo(pnlForm, "P. Compra:",    y, "0.00"); y += 40;
            txtPrecioVenta  = AgregarCampo(pnlForm, "P. Venta:",     y, "0.00"); y += 40;
            txtStock        = AgregarCampo(pnlForm, "Stock:",         y, "0");   y += 40;
            txtStockMin     = AgregarCampo(pnlForm, "Stock Mínimo:", y, "5");

            // Botones formulario
            var pnlBtns = new Panel { Location = new Point(15, 420), Size = new Size(480, 40) };

            btnNuevo = CrearBoton("➕ Nuevo",    0,   Color.FromArgb(21, 101, 192));
            btnGuardar = CrearBoton("💾 Guardar", 125, colorBoton);
            btnEliminar = CrearBoton("🗑 Eliminar",250, Color.FromArgb(183, 28, 28));

            btnNuevo.Click    += (s, e) => LimpiarFormulario();
            btnGuardar.Click  += BtnGuardar_Click;
            btnEliminar.Click += BtnEliminar_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnNuevo, btnGuardar, btnEliminar });

            this.Controls.Add(pnlForm);
            this.Controls.Add(pnlBtns);

            // --- GRID ---
            var pnlGrid = new GroupBox { Text = "Lista de Productos", Location = new Point(510, 50),
                                          Size = new Size(640, 450), Font = new Font("Arial", 9, FontStyle.Bold) };

            txtBuscar = new TextBox { Location = new Point(10, 22), Size = new Size(400, 28), Font = new Font("Arial", 9),
                                       PlaceholderText = "Buscar por código o nombre..." };
            btnBuscar = new Button { Text = "🔍", Location = new Point(415, 20), Size = new Size(50, 30),
                                      BackColor = colorDorado, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarProductos();

            grid = new DataGridView
            {
                Location = new Point(8, 55), Size = new Size(618, 385),
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                Font = new Font("Arial", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, MultiSelect = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            grid.CellClick += Grid_CellClick;

            grid.Columns.Add("ColId",     "ID");       grid.Columns["ColId"].Visible = false;
            grid.Columns.Add("ColCodigo", "Código");
            grid.Columns.Add("ColNombre", "Nombre");
            grid.Columns.Add("ColCateg",  "Categoría");
            grid.Columns.Add("ColCompra", "P.Compra");
            grid.Columns.Add("ColVenta",  "P.Venta");
            grid.Columns.Add("ColStock",  "Stock");

            pnlGrid.Controls.AddRange(new Control[] { txtBuscar, btnBuscar, grid });
            this.Controls.Add(pnlGrid);
        }

        private TextBox AgregarCampo(GroupBox parent, string label, int y, string defVal = "")
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(10, y + 3), AutoSize = true });
            var txt = new TextBox { Location = new Point(120, y), Size = new Size(330, 28), Font = new Font("Arial", 9), Text = defVal };
            parent.Controls.Add(txt);
            return txt;
        }

        private Button CrearBoton(string texto, int x, Color color)
        {
            var btn = new Button { Text = texto, Location = new Point(x, 0), Size = new Size(118, 35),
                                    BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Arial", 9) };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void CargarCategorias()
        {
            try
            {
                cboCategoria.Items.Clear();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nombre FROM categorias ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboCategoria.Items.Add(new Models.Categoria { Id = dr.GetInt32(0), Nombre = dr.GetString(1) });
                }
                if (cboCategoria.Items.Count > 0) cboCategoria.SelectedIndex = 0;
            }
            catch { }
        }

        private void CargarProductos()
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT p.id, p.codigo, p.nombre, COALESCE(c.nombre,'Sin categoría'),
                                          p.precio_compra, p.precio_venta, p.stock
                                   FROM productos p LEFT JOIN categorias c ON p.categoria_id=c.id
                                   WHERE p.activo=true AND (p.codigo ILIKE @b OR p.nombre ILIKE @b)
                                   ORDER BY p.nombre";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("b", "%" + txtBuscar.Text.Trim() + "%");
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                            {
                                int rowIdx = grid.Rows.Add(dr.GetInt32(0), dr.GetString(1), dr.GetString(2),
                                                            dr.GetString(3), "S/ " + dr.GetDecimal(4).ToString("N2"),
                                                            "S/ " + dr.GetDecimal(5).ToString("N2"), dr.GetInt32(6));
                                // Resaltar stock bajo
                                if (dr.GetInt32(6) <= 5) grid.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                            }
                    }
                }
            }
            catch { }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            idSeleccionado = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ColId"].Value);
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT codigo,nombre,descripcion,categoria_id,precio_compra,precio_venta,stock,stock_minimo FROM productos WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", idSeleccionado);
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
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) || string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("Código y Nombre son obligatorios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = idSeleccionado == 0
                        ? @"INSERT INTO productos(codigo,nombre,descripcion,categoria_id,precio_compra,precio_venta,stock,stock_minimo)
                            VALUES(@cod,@nom,@des,@cat,@pc,@pv,@stk,@smin)"
                        : @"UPDATE productos SET codigo=@cod,nombre=@nom,descripcion=@des,categoria_id=@cat,
                                                  precio_compra=@pc,precio_venta=@pv,stock=@stk,stock_minimo=@smin
                            WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("cod",  txtCodigo.Text.Trim());
                        cmd.Parameters.AddWithValue("nom",  txtNombre.Text.Trim());
                        cmd.Parameters.AddWithValue("des",  txtDescripcion.Text.Trim());
                        cmd.Parameters.AddWithValue("cat",  (cboCategoria.SelectedItem as Models.Categoria)?.Id ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("pc",   decimal.Parse(txtPrecioCompra.Text));
                        cmd.Parameters.AddWithValue("pv",   decimal.Parse(txtPrecioVenta.Text));
                        cmd.Parameters.AddWithValue("stk",  int.Parse(txtStock.Text));
                        cmd.Parameters.AddWithValue("smin", int.Parse(txtStockMin.Text));
                        if (idSeleccionado > 0) cmd.Parameters.AddWithValue("id", idSeleccionado);
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

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == 0) { MessageBox.Show("Seleccione un producto.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("¿Eliminar este producto?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
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

        private void LimpiarFormulario()
        {
            idSeleccionado = 0;
            txtCodigo.Clear(); txtNombre.Clear(); txtDescripcion.Clear();
            txtPrecioCompra.Text = "0.00"; txtPrecioVenta.Text = "0.00";
            txtStock.Text = "0"; txtStockMin.Text = "5";
            if (cboCategoria.Items.Count > 0) cboCategoria.SelectedIndex = 0;
        }
    }
}
