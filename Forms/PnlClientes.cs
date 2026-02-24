using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;

namespace SistemaVentas.Forms
{
    public class PnlClientes : UserControl
    {
        private DataGridView grid;
        private TextBox txtDocumento, txtNombre, txtDireccion, txtTelefono, txtEmail, txtBuscar;
        private Button btnNuevo, btnGuardar, btnEliminar;
        private int idSeleccionado = 0;
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);

        public PnlClientes()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarClientes();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label { Text = "👥  GESTIÓN DE CLIENTES", Font = new Font("Arial", 14, FontStyle.Bold),
                                   ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lbl);

            // Formulario
            var pnlForm = new GroupBox { Text = "Datos del Cliente", Location = new Point(15, 50), Size = new Size(460, 310), Font = new Font("Arial", 9, FontStyle.Bold) };

            int y = 25;
            txtDocumento = AgregarCampo(pnlForm, "DNI/RUC:", y); y += 40;
            txtNombre    = AgregarCampo(pnlForm, "Nombre:",  y); y += 40;
            txtDireccion = AgregarCampo(pnlForm, "Dirección:",y); y += 40;
            txtTelefono  = AgregarCampo(pnlForm, "Teléfono:", y); y += 40;
            txtEmail     = AgregarCampo(pnlForm, "Email:",    y);

            var pnlBtns = new Panel { Location = new Point(15, 370), Size = new Size(460, 40) };
            btnNuevo    = CrearBoton("➕ Nuevo",    0,   Color.FromArgb(21, 101, 192));
            btnGuardar  = CrearBoton("💾 Guardar",  120, colorBoton);
            btnEliminar = CrearBoton("🗑 Eliminar", 240, Color.FromArgb(183, 28, 28));
            btnNuevo.Click    += (s, e) => LimpiarFormulario();
            btnGuardar.Click  += BtnGuardar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            pnlBtns.Controls.AddRange(new Control[] { btnNuevo, btnGuardar, btnEliminar });

            this.Controls.Add(pnlForm);
            this.Controls.Add(pnlBtns);

            // Grid
            var pnlGrid = new GroupBox { Text = "Lista de Clientes", Location = new Point(490, 50), Size = new Size(660, 450), Font = new Font("Arial", 9, FontStyle.Bold) };
            txtBuscar = new TextBox { Location = new Point(10, 22), Size = new Size(400, 28), PlaceholderText = "Buscar cliente..." };
            var btnBuscar = new Button { Text = "🔍", Location = new Point(415, 20), Size = new Size(50, 30),
                                          BackColor = colorDorado, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarClientes();

            grid = new DataGridView { Location = new Point(8, 55), Size = new Size(640, 385),
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true,
                AllowUserToAddRows = false, RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, MultiSelect = false };
            grid.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            grid.CellClick += Grid_CellClick;
            grid.Columns.Add("ColId",  "ID");  grid.Columns["ColId"].Visible = false;
            grid.Columns.Add("ColDoc", "Doc.");
            grid.Columns.Add("ColNom", "Nombre");
            grid.Columns.Add("ColTel", "Teléfono");
            grid.Columns.Add("ColEmail","Email");

            pnlGrid.Controls.AddRange(new Control[] { txtBuscar, btnBuscar, grid });
            this.Controls.Add(pnlGrid);
        }

        private TextBox AgregarCampo(GroupBox p, string lbl, int y)
        {
            p.Controls.Add(new Label { Text = lbl, Location = new Point(10, y + 3), AutoSize = true });
            var t = new TextBox { Location = new Point(110, y), Size = new Size(320, 28), Font = new Font("Arial", 9) };
            p.Controls.Add(t); return t;
        }

        private Button CrearBoton(string text, int x, Color color)
        {
            var b = new Button { Text = text, Location = new Point(x, 0), Size = new Size(112, 35),
                                  BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private void CargarClientes()
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT id, documento, nombre, telefono, email FROM clientes
                                   WHERE activo=true AND (nombre ILIKE @b OR documento ILIKE @b) ORDER BY nombre";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("b", "%" + txtBuscar.Text.Trim() + "%");
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                grid.Rows.Add(dr.GetInt32(0), dr.IsDBNull(1) ? "" : dr.GetString(1), dr.GetString(2),
                                               dr.IsDBNull(3) ? "" : dr.GetString(3), dr.IsDBNull(4) ? "" : dr.GetString(4));
                    }
                }
            }
            catch { }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            idSeleccionado = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ColId"].Value);
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT documento,nombre,direccion,telefono,email FROM clientes WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", idSeleccionado);
                    using (var dr = cmd.ExecuteReader())
                        if (dr.Read())
                        {
                            txtDocumento.Text = dr.IsDBNull(0) ? "" : dr.GetString(0);
                            txtNombre.Text    = dr.GetString(1);
                            txtDireccion.Text = dr.IsDBNull(2) ? "" : dr.GetString(2);
                            txtTelefono.Text  = dr.IsDBNull(3) ? "" : dr.GetString(3);
                            txtEmail.Text     = dr.IsDBNull(4) ? "" : dr.GetString(4);
                        }
                }
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("El nombre es obligatorio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = idSeleccionado == 0
                        ? "INSERT INTO clientes(documento,nombre,direccion,telefono,email) VALUES(@doc,@nom,@dir,@tel,@eml)"
                        : "UPDATE clientes SET documento=@doc,nombre=@nom,direccion=@dir,telefono=@tel,email=@eml WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("doc", txtDocumento.Text.Trim());
                        cmd.Parameters.AddWithValue("nom", txtNombre.Text.Trim());
                        cmd.Parameters.AddWithValue("dir", txtDireccion.Text.Trim());
                        cmd.Parameters.AddWithValue("tel", txtTelefono.Text.Trim());
                        cmd.Parameters.AddWithValue("eml", txtEmail.Text.Trim());
                        if (idSeleccionado > 0) cmd.Parameters.AddWithValue("id", idSeleccionado);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("✅  Cliente guardado.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarFormulario(); CargarClientes();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == 0) return;
            if (MessageBox.Show("¿Eliminar cliente?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("UPDATE clientes SET activo=false WHERE id=@id", conn))
                { cmd.Parameters.AddWithValue("id", idSeleccionado); cmd.ExecuteNonQuery(); }
            }
            LimpiarFormulario(); CargarClientes();
        }

        private void LimpiarFormulario()
        {
            idSeleccionado = 0;
            txtDocumento.Clear(); txtNombre.Clear(); txtDireccion.Clear(); txtTelefono.Clear(); txtEmail.Clear();
        }
    }
}
