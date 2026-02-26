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
        private readonly Color colorDorado  = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton   = Color.FromArgb(100, 80, 45);
        private readonly Color colorElim    = Color.FromArgb(180, 60, 50);

        public PnlConfiguracion()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label
            {
                Text = "⚙️  CONFIGURACIÓN",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true
            };
            this.Controls.Add(lbl);

            var tabs = new TabControl { Location = new Point(15, 55), Size = new Size(920, 560) };
            tabs.TabPages.Add(CrearTabEmpresa());
            tabs.TabPages.Add(CrearTabUsuarios());
            tabs.TabPages.Add(CrearTabCategorias());
            tabs.TabPages.Add(CrearTabConexion());
            tabs.TabPages.Add(CrearTabSucursal());
            tabs.TabPages.Add(CrearTabAreas());
            tabs.TabPages.Add(CrearTabParametros());
            this.Controls.Add(tabs);
        }

        // ══════════════════════════════════════════════
        //  TAB EMPRESA
        // ══════════════════════════════════════════════
        private TabPage CrearTabEmpresa()
        {
            var tab = new TabPage("🏢  Mi Empresa");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            int y = 20;
            var txtNombre = AgregarCampo(tab, "Nombre:",    y); y += 48;
            var txtRuc    = AgregarCampo(tab, "RUC:",       y); y += 48;
            var txtDir    = AgregarCampo(tab, "Dirección:", y); y += 48;
            var txtTel    = AgregarCampo(tab, "Teléfono:",  y); y += 60;

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

            tab.Controls.Add(BotonPrimario("💾 Guardar Empresa", 20, y, (s, e) =>
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("UPDATE empresas SET nombre=@n,ruc=@r,direccion=@d,telefono=@t WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("n",  txtNombre.Text);
                            cmd.Parameters.AddWithValue("r",  txtRuc.Text);
                            cmd.Parameters.AddWithValue("d",  txtDir.Text);
                            cmd.Parameters.AddWithValue("t",  txtTel.Text);
                            cmd.Parameters.AddWithValue("id", Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MsgOk("Empresa actualizada.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            return tab;
        }

        // ══════════════════════════════════════════════
        //  TAB USUARIOS  (con Eliminar)
        // ══════════════════════════════════════════════
        private TabPage CrearTabUsuarios()
        {
            var tab = new TabPage("👤  Usuarios");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = CrearGrid(new Point(5, 5), new Size(890, 230));
            grid.Columns.Add("uid",    "ID");      grid.Columns["uid"].Visible = false;
            grid.Columns.Add("usuario","Usuario");
            grid.Columns.Add("nombre", "Nombre");
            grid.Columns.Add("rol",    "Rol");
            grid.Columns.Add("activo", "Activo");
            tab.Controls.Add(grid);

            // Botón eliminar debajo del grid
            tab.Controls.Add(BotonPeligro("🗑️ Eliminar usuario seleccionado", 5, 242, (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                if (MessageBox.Show("¿Eliminar el usuario seleccionado?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int id = (int)grid.CurrentRow.Cells["uid"].Value;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("DELETE FROM usuarios WHERE id=@id", conn))
                        { cmd.Parameters.AddWithValue("id", id); cmd.ExecuteNonQuery(); }
                    }
                    CargarUsuariosGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            // Formulario nuevo usuario
            var grp = new GroupBox
            {
                Text = "Nuevo Usuario", Location = new Point(5, 285), Size = new Size(890, 200),
                Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(245, 240, 228)
            };
            int gy = 22;
            var txtUser = AgregarCampo(grp, "Usuario:",    gy); gy += 48;
            var txtPass = AgregarCampoPass(grp, "Contraseña:", gy); gy += 48;
            var txtNom  = AgregarCampo(grp, "Nombre:",     gy);

            grp.Controls.Add(BotonPrimario("➕ Crear Usuario", 660, 80, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text)) return;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO usuarios(empresa_id,sucursal_id,nombre,usuario,password_hash,rol_id) VALUES(@eid,@sid,@nom,@usr,@pwd,2)", conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sid", Sesion.SucursalActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("nom", txtNom.Text);
                            cmd.Parameters.AddWithValue("usr", txtUser.Text.Trim());
                            cmd.Parameters.AddWithValue("pwd", SHA256Hash(txtPass.Text));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MsgOk("Usuario creado.");
                    txtUser.Clear(); txtPass.Clear(); txtNom.Clear();
                    CargarUsuariosGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            tab.Controls.Add(grp);
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
                    using (var cmd = new NpgsqlCommand(
                        "SELECT u.id,u.usuario,u.nombre,r.nombre,u.activo FROM usuarios u JOIN roles r ON u.rol_id=r.id WHERE u.empresa_id=@eid", conn))
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

        // ══════════════════════════════════════════════
        //  TAB CATEGORÍAS  (con Eliminar)
        // ══════════════════════════════════════════════
        private TabPage CrearTabCategorias()
        {
            var tab = new TabPage("🏷️  Categorías");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = CrearGrid(new Point(5, 5), new Size(560, 450));
            grid.Columns.Add("cid",    "ID");         grid.Columns["cid"].Visible = false;
            grid.Columns.Add("nombre", "Categoría");
            tab.Controls.Add(grid);

            // Panel lateral derecho
            int px = 580, py = 10;
            var txtCat = new TextBox
            {
                Location = new Point(px, py), Size = new Size(280, 30),
                PlaceholderText = "Nueva categoría...", Font = new Font("Arial", 9)
            };
            tab.Controls.Add(txtCat); py += 40;

            tab.Controls.Add(BotonPrimario("➕ Agregar", px, py, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCat.Text)) return;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("INSERT INTO categorias(nombre) VALUES(@n)", conn))
                        { cmd.Parameters.AddWithValue("n", txtCat.Text.Trim()); cmd.ExecuteNonQuery(); }
                    }
                    txtCat.Clear();
                    CargarCategoriasGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            })); py += 48;

            tab.Controls.Add(BotonPeligro("🗑️ Eliminar seleccionada", px, py, (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                if (MessageBox.Show("¿Eliminar la categoría seleccionada?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int id = (int)grid.CurrentRow.Cells["cid"].Value;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("DELETE FROM categorias WHERE id=@id", conn))
                        { cmd.Parameters.AddWithValue("id", id); cmd.ExecuteNonQuery(); }
                    }
                    CargarCategoriasGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            CargarCategoriasGrid(grid);
            return tab;
        }

        private void CargarCategoriasGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id,nombre FROM categorias ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            grid.Rows.Add(dr.GetInt32(0), dr.GetString(1));
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════
        //  TAB CONEXIÓN BD
        // ══════════════════════════════════════════════
        private TabPage CrearTabConexion()
        {
            var tab = new TabPage("🔌  Conexión BD");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            int y = 20;
            var txtHost = AgregarCampo(tab, "Host:",       y, "localhost");     y += 48;
            var txtPort = AgregarCampo(tab, "Puerto:",     y, "5432");          y += 48;
            var txtDb   = AgregarCampo(tab, "Base Datos:", y, "SistemaVentas"); y += 48;
            var txtUser = AgregarCampo(tab, "Usuario BD:", y, "postgres");      y += 48;
            var txtPass = AgregarCampoPass(tab, "Contraseña BD:", y);           y += 60;

            var btnTest = new Button
            {
                Text = "🔍 Probar Conexión", Location = new Point(20, y), Size = new Size(210, 38),
                BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += (s, e) =>
            {
                DatabaseHelper.SetConnectionString(txtHost.Text, txtPort.Text, txtDb.Text, txtUser.Text, txtPass.Text);
                bool ok = DatabaseHelper.TestConnection();
                MessageBox.Show(ok ? "✅  Conexión exitosa!" : "❌  No se pudo conectar.",
                    ok ? "Éxito" : "Error", MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            };
            tab.Controls.Add(btnTest);
            return tab;
        }

        // ══════════════════════════════════════════════
        //  TAB SUCURSAL  (con Eliminar)
        // ══════════════════════════════════════════════
        private TabPage CrearTabSucursal()
        {
            var tab = new TabPage("🏬  Sucursal");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = CrearGrid(new Point(5, 5), new Size(890, 230));
            grid.Columns.Add("sid",       "ID");         grid.Columns["sid"].Visible = false;
            grid.Columns.Add("nombre",    "Nombre");
            grid.Columns.Add("direccion", "Dirección");
            grid.Columns.Add("telefono",  "Teléfono");
            grid.Columns.Add("activo",    "Activo");
            tab.Controls.Add(grid);

            // Botón eliminar debajo del grid
            tab.Controls.Add(BotonPeligro("🗑️ Eliminar sucursal seleccionada", 5, 242, (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                if (MessageBox.Show("¿Eliminar la sucursal seleccionada?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int id = (int)grid.CurrentRow.Cells["sid"].Value;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("DELETE FROM sucursales WHERE id=@id", conn))
                        { cmd.Parameters.AddWithValue("id", id); cmd.ExecuteNonQuery(); }
                    }
                    CargarSucursalesGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            // Formulario nueva sucursal
            var grp = new GroupBox
            {
                Text = "Nueva Sucursal", Location = new Point(5, 285), Size = new Size(890, 200),
                Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(245, 240, 228)
            };
            int gy = 22;
            var txtNom = AgregarCampo(grp, "Nombre:",    gy); gy += 48;
            var txtDir = AgregarCampo(grp, "Dirección:", gy); gy += 48;
            var txtTel = AgregarCampo(grp, "Teléfono:",  gy);

            grp.Controls.Add(BotonPrimario("💾 Guardar Sucursal", 660, 80, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNom.Text)) { MessageBox.Show("Ingrese el nombre."); return; }
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO sucursales(empresa_id,nombre,direccion,telefono,activo) VALUES(@eid,@n,@d,@t,true)", conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("n",   txtNom.Text.Trim());
                            cmd.Parameters.AddWithValue("d",   txtDir.Text.Trim());
                            cmd.Parameters.AddWithValue("t",   txtTel.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MsgOk("Sucursal guardada.");
                    txtNom.Clear(); txtDir.Clear(); txtTel.Clear();
                    CargarSucursalesGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            tab.Controls.Add(grp);
            CargarSucursalesGrid(grid);
            return tab;
        }

        private void CargarSucursalesGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id,nombre,COALESCE(direccion,''),COALESCE(telefono,''),activo FROM sucursales WHERE empresa_id=@eid ORDER BY nombre", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 1);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                grid.Rows.Add(dr.GetInt32(0), dr.GetString(1), dr.GetString(2), dr.GetString(3), dr.GetBoolean(4) ? "✅" : "❌");
                    }
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════
        //  TAB ÁREAS
        // ══════════════════════════════════════════════
        private TabPage CrearTabAreas()
        {
            var tab = new TabPage("🗂️  Áreas");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var grid = CrearGrid(new Point(5, 5), new Size(560, 450));
            grid.Columns.Add("aid",         "ID");            grid.Columns["aid"].Visible = false;
            grid.Columns.Add("nombre",      "Nombre");
            grid.Columns.Add("descripcion", "Descripción");
            tab.Controls.Add(grid);

            int px = 580, py = 10;
            var txtNom  = new TextBox { Location = new Point(px, py),      Size = new Size(280, 30), PlaceholderText = "Nombre del área...",      Font = new Font("Arial", 9) };
            var txtDesc = new TextBox { Location = new Point(px, py + 38), Size = new Size(280, 30), PlaceholderText = "Descripción (opcional)...", Font = new Font("Arial", 9) };
            tab.Controls.Add(txtNom);
            tab.Controls.Add(txtDesc); py += 88;

            tab.Controls.Add(BotonPrimario("➕ Agregar", px, py, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNom.Text)) return;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("INSERT INTO areas(nombre,descripcion) VALUES(@n,@d)", conn))
                        {
                            cmd.Parameters.AddWithValue("n", txtNom.Text.Trim());
                            cmd.Parameters.AddWithValue("d", txtDesc.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    txtNom.Clear(); txtDesc.Clear();
                    CargarAreasGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            })); py += 48;

            tab.Controls.Add(BotonPeligro("🗑️ Eliminar seleccionada", px, py, (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                if (MessageBox.Show("¿Eliminar el área seleccionada?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int id = (int)grid.CurrentRow.Cells["aid"].Value;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("DELETE FROM areas WHERE id=@id", conn))
                        { cmd.Parameters.AddWithValue("id", id); cmd.ExecuteNonQuery(); }
                    }
                    CargarAreasGrid(grid);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            CargarAreasGrid(grid);
            return tab;
        }

        private void CargarAreasGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id,nombre,COALESCE(descripcion,'') FROM areas ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            grid.Rows.Add(dr.GetInt32(0), dr.GetString(1), dr.GetString(2));
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════
        //  TAB PARÁMETROS
        // ══════════════════════════════════════════════
        private TabPage CrearTabParametros()
        {
            var tab = new TabPage("⚙️  Parámetros");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            int y = 15;
            tab.Controls.Add(new Label
            {
                Text = "── SUNAT / SFS ──────────────────────────────",
                Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = colorDorado,
                Location = new Point(15, y), AutoSize = true
            }); y += 28;

            var txtRutaXml  = AgregarCampo(tab, "rutaXML / Envío:",      y, @"D:\SFSPauloA\sunat_archivos\sfs\FIRMA\"); y += 42;
            var txtRutaCDR  = AgregarCampo(tab, "rutaCDR / Respuesta:",  y, @"D:\SFSPauloA\sunat_archivos\sfs\RPTA\");  y += 42;
            var txtRutaQR   = AgregarCampo(tab, "rutaQR / Repositorio:", y, @"D:\SFSPauloA\sunat_archivos\sfs\REPO\");  y += 42;
            var txtCertPath = AgregarCampo(tab, "Certificado (.pfx):",   y, @"D:\SFSPauloA\sunat_archivos\sfs\CERT\10181424422.pfx"); y += 42;
            var txtCertPass = AgregarCampoPass(tab, "Clave Certificado:", y);                                                          y += 42;
            var txtRuc      = AgregarCampo(tab, "RUC Emisor:",           y, "10181424422");                              y += 42;
            var txtEmail    = AgregarCampo(tab, "Email SUNAT:",          y, "inversionesferreteros@gmail.com");          y += 42;
            var txtUbigeo   = AgregarCampo(tab, "Ubigeo:",               y, "150101");                                   y += 55;

            void Cargar()
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(
                            "SELECT clave,valor FROM parametros WHERE clave IN ('rutaXML','rutaCDR','rutaQR','rutaCertificado','ClaveCertificado','RUC','Email','Ubigeo')", conn))
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                            {
                                string k = dr.GetString(0), v = dr.IsDBNull(1) ? "" : dr.GetString(1);
                                switch (k)
                                {
                                    case "rutaXML":          txtRutaXml.Text  = v; break;
                                    case "rutaCDR":          txtRutaCDR.Text  = v; break;
                                    case "rutaQR":           txtRutaQR.Text   = v; break;
                                    case "rutaCertificado":  txtCertPath.Text = v; break;
                                    case "ClaveCertificado": txtCertPass.Text = v; break;
                                    case "RUC":              txtRuc.Text      = v; break;
                                    case "Email":            txtEmail.Text    = v; break;
                                    case "Ubigeo":           txtUbigeo.Text   = v; break;
                                }
                            }
                    }
                }
                catch { }
            }

            var btnCargar = new Button
            {
                Text = "📂 Cargar", Location = new Point(20, y), Size = new Size(150, 38),
                BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnCargar.FlatAppearance.BorderSize = 0;
            btnCargar.Click += (s, e) => { Cargar(); MsgOk("Parámetros cargados."); };
            tab.Controls.Add(btnCargar);

            tab.Controls.Add(BotonPrimario("💾 Guardar", 185, y, (s, e) =>
            {
                var vals = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"rutaXML", txtRutaXml.Text.Trim()}, {"rutaCDR", txtRutaCDR.Text.Trim()},
                    {"rutaQR",  txtRutaQR.Text.Trim()},  {"rutaCertificado", txtCertPath.Text.Trim()},
                    {"ClaveCertificado", txtCertPass.Text.Trim()},
                    {"RUC", txtRuc.Text.Trim()}, {"Email", txtEmail.Text.Trim()}, {"Ubigeo", txtUbigeo.Text.Trim()}
                };
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        foreach (var kv in vals)
                            using (var cmd = new NpgsqlCommand(
                                "INSERT INTO parametros(clave,valor) VALUES(@k,@v) ON CONFLICT(clave) DO UPDATE SET valor=EXCLUDED.valor", conn))
                            {
                                cmd.Parameters.AddWithValue("k", kv.Key);
                                cmd.Parameters.AddWithValue("v", kv.Value);
                                cmd.ExecuteNonQuery();
                            }
                    }
                    MsgOk("Parámetros guardados.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));

            try { Cargar(); } catch { }
            return tab;
        }

        // ══════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════

        private TextBox AgregarCampo(Control parent, string label, int y, string defVal = "")
        {
            parent.Controls.Add(new Label
            {
                Text = label, Location = new Point(20, y + 4),
                AutoSize = true, Font = new Font("Arial", 9)
            });
            var t = new TextBox { Location = new Point(200, y), Size = new Size(400, 28), Font = new Font("Arial", 9), Text = defVal };
            parent.Controls.Add(t);
            return t;
        }

        private TextBox AgregarCampoPass(Control parent, string label, int y)
        {
            var t = AgregarCampo(parent, label, y);
            t.PasswordChar = '●';
            return t;
        }

        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView
            {
                Location = loc, Size = size,
                BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9), BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        private Button BotonPrimario(string texto, int x, int y, EventHandler click)
        {
            var b = new Button
            {
                Text = texto, Location = new Point(x, y), Size = new Size(210, 36),
                BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += click;
            return b;
        }

        private Button BotonPeligro(string texto, int x, int y, EventHandler click)
        {
            var b = new Button
            {
                Text = texto, Location = new Point(x, y), Size = new Size(240, 36),
                BackColor = colorElim, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += click;
            return b;
        }

        private void MsgOk(string msg) =>
            MessageBox.Show("✅  " + msg, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private string SHA256Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}