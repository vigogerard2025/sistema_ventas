using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlConfiguracion : UserControl
    {
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);

        public PnlConfiguracion()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label { Text = "⚙️  CONFIGURACIÓN", Font = new Font("Arial", 14, FontStyle.Bold),
                                   ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lbl);

            var tabs = new TabControl { Location = new Point(15, 55), Size = new Size(900, 500) };
            tabs.TabPages.Add(CrearTabEmpresa());
            tabs.TabPages.Add(CrearTabUsuarios());
            tabs.TabPages.Add(CrearTabCategorias());
            tabs.TabPages.Add(CrearTabConexion());
            this.Controls.Add(tabs);
        }

        private TabPage CrearTabEmpresa()
        {
            var tab = new TabPage("🏢  Mi Empresa");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            TextBox txtNombre = null, txtRuc = null, txtDir = null, txtTel = null;
            int y = 20;

            txtNombre = AgregarCampoTab(tab, "Nombre:", y); y += 45;
            txtRuc    = AgregarCampoTab(tab, "RUC:",    y); y += 45;
            txtDir    = AgregarCampoTab(tab, "Dirección:", y); y += 45;
            txtTel    = AgregarCampoTab(tab, "Teléfono:", y); y += 55;

            // Cargar datos actuales
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT nombre,ruc,direccion,telefono FROM empresas WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Sesion.EmpresaActiva?.Id ?? 1);
                        using (var dr = cmd.ExecuteReader())
                            if (dr.Read())
                            {
                                txtNombre.Text = dr.GetString(0);
                                txtRuc.Text    = dr.IsDBNull(1) ? "" : dr.GetString(1);
                                txtDir.Text    = dr.IsDBNull(2) ? "" : dr.GetString(2);
                                txtTel.Text    = dr.IsDBNull(3) ? "" : dr.GetString(3);
                            }
                    }
                }
            }
            catch { }

            var btnSave = new Button { Text = "💾 Guardar Empresa", Location = new Point(20, y), Size = new Size(200, 36),
                                        BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "UPDATE empresas SET nombre=@n,ruc=@r,direccion=@d,telefono=@t WHERE id=@id";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("n",  txtNombre.Text);
                            cmd.Parameters.AddWithValue("r",  txtRuc.Text);
                            cmd.Parameters.AddWithValue("d",  txtDir.Text);
                            cmd.Parameters.AddWithValue("t",  txtTel.Text);
                            cmd.Parameters.AddWithValue("id", Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("✅  Empresa actualizada.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            tab.Controls.Add(btnSave);
            return tab;
        }

        private TabPage CrearTabUsuarios()
        {
            var tab = new TabPage("👤  Usuarios");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = new DataGridView { Location = new Point(5, 5), Size = new Size(860, 250),
                BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9), BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.Columns.Add("uid",    "ID");    grid.Columns["uid"].Visible = false;
            grid.Columns.Add("usuario","Usuario");
            grid.Columns.Add("nombre", "Nombre");
            grid.Columns.Add("rol",    "Rol");
            grid.Columns.Add("activo", "Activo");
            tab.Controls.Add(grid);

            // Nuevo usuario
            var grpNuevo = new GroupBox { Text = "Nuevo Usuario", Location = new Point(5, 265), Size = new Size(860, 180), Font = new Font("Arial", 9, FontStyle.Bold) };
            var txtUser = AgregarCampoTab(grpNuevo, "Usuario:", 20);
            var txtPass = AgregarCampoTabPassword(grpNuevo, "Contraseña:", 65);
            var txtNom  = AgregarCampoTab(grpNuevo, "Nombre:", 110);

            var btnCrear = new Button { Text = "➕ Crear Usuario", Location = new Point(550, 30), Size = new Size(150, 36),
                                         BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCrear.FlatAppearance.BorderSize = 0;
            btnCrear.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text)) return;
                string hash = SHA256Hash(txtPass.Text);
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "INSERT INTO usuarios(empresa_id,sucursal_id,nombre,usuario,password_hash,rol_id) VALUES(@eid,@sid,@nom,@usr,@pwd,2)";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sid", Sesion.SucursalActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("nom", txtNom.Text);
                            cmd.Parameters.AddWithValue("usr", txtUser.Text.Trim());
                            cmd.Parameters.AddWithValue("pwd", hash);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("✅  Usuario creado.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtUser.Clear(); txtPass.Clear(); txtNom.Clear();
                    CargarUsuariosGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            grpNuevo.Controls.Add(btnCrear);
            tab.Controls.Add(grpNuevo);

            CargarUsuariosGrid(grid);
            return tab;
        }

        private void CargarUsuariosGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT u.id,u.usuario,u.nombre,r.nombre,u.activo FROM usuarios u JOIN roles r ON u.rol_id=r.id WHERE u.empresa_id=@eid";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                grid.Rows.Add(dr.GetInt32(0), dr.GetString(1), dr.GetString(2), dr.GetString(3), dr.GetBoolean(4) ? "✅" : "❌");
                    }
                }
            }
            catch { }
        }

        private TabPage CrearTabCategorias()
        {
            var tab = new TabPage("🏷️  Categorías");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = new DataGridView { Location = new Point(5, 5), Size = new Size(500, 400),
                BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9), BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.Columns.Add("cid",    "ID");   grid.Columns["cid"].Visible = false;
            grid.Columns.Add("nombre", "Categoría");
            tab.Controls.Add(grid);

            var txtCat = new TextBox { Location = new Point(520, 10), Size = new Size(250, 30), PlaceholderText = "Nueva categoría..." };
            tab.Controls.Add(txtCat);

            var btnAdd = new Button { Text = "➕ Agregar", Location = new Point(520, 50), Size = new Size(130, 35),
                                       BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCat.Text)) return;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("INSERT INTO categorias(nombre) VALUES(@n)", conn))
                    { cmd.Parameters.AddWithValue("n", txtCat.Text.Trim()); cmd.ExecuteNonQuery(); }
                }
                txtCat.Clear();
                CargarCategoriasGrid(grid);
            };
            tab.Controls.Add(btnAdd);
            CargarCategoriasGrid(grid);
            return tab;
        }

        private void CargarCategoriasGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, nombre FROM categorias ORDER BY nombre", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        grid.Rows.Add(dr.GetInt32(0), dr.GetString(1));
            }
        }

        private TabPage CrearTabConexion()
        {
            var tab = new TabPage("🔌  Conexión BD");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            int y = 20;
            var txtHost = AgregarCampoTab(tab, "Host:",     y, "localhost"); y += 45;
            var txtPort = AgregarCampoTab(tab, "Puerto:",   y, "5432");      y += 45;
            var txtDb   = AgregarCampoTab(tab, "Base Datos:",y, "SistemaVentas"); y += 45;
            var txtUser = AgregarCampoTab(tab, "Usuario BD:",y, "postgres"); y += 45;
            var txtPass = AgregarCampoTabPassword(tab, "Contraseña BD:", y); y += 55;

            var btnTest = new Button { Text = "🔍 Probar Conexión", Location = new Point(20, y), Size = new Size(180, 36),
                                        BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += (s, e) =>
            {
                DatabaseHelper.SetConnectionString(txtHost.Text, txtPort.Text, txtDb.Text, txtUser.Text, txtPass.Text);
                bool ok = DatabaseHelper.TestConnection();
                MessageBox.Show(ok ? "✅  Conexión exitosa!" : "❌  No se pudo conectar.", ok ? "Éxito" : "Error",
                                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            };
            tab.Controls.Add(btnTest);
            return tab;
        }

        private TextBox AgregarCampoTab(Control parent, string label, int y, string defVal = "")
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
            var t = new TextBox { Location = new Point(150, y), Size = new Size(300, 28), Font = new Font("Arial", 9), Text = defVal };
            parent.Controls.Add(t); return t;
        }

        private TextBox AgregarCampoTabPassword(Control parent, string label, int y)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
            var t = new TextBox { Location = new Point(150, y), Size = new Size(300, 28), Font = new Font("Arial", 9), PasswordChar = '●' };
            parent.Controls.Add(t); return t;
        }

        private string SHA256Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
