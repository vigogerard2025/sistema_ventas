using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;

namespace SistemaVentas.Forms
{
    // =========================================================================
    //  FORMULARIO — Solicitar nuevo usuario (guarda en BD)
    // =========================================================================
    public class FrmNuevoUsuario : Form
    {
        private readonly Color cFondo = Color.FromArgb(22, 22, 32);
        private readonly Color cOro   = Color.FromArgb(212, 175, 95);
        private readonly Color cInput = Color.FromArgb(46, 46, 62);
        private readonly Color cTexto = Color.FromArgb(225, 220, 210);
        private readonly Color cGris  = Color.FromArgb(130, 125, 115);
        private readonly Color cRojo  = Color.FromArgb(200, 70, 70);

        private TextBox txtNombre, txtUsuario, txtCorreo, txtPassword;
        private ComboBox cboEmpresa;
        private Button btnEnviar, btnCancelar;

        public FrmNuevoUsuario()
        {
            this.Text            = "Solicitar nuevo usuario";
            this.Size            = new Size(480, 540);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = cFondo;

            // ── Encabezado ────────────────────────────────────────────────
            var lblIcon = new Label
            {
                Text = "👤", Font = new Font("Segoe UI Emoji", 26),
                ForeColor = cOro, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(210, 18)
            };

            var lblTitulo = new Label
            {
                Text = "Solicitar nuevo usuario",
                Font = new Font("Georgia", 14, FontStyle.Bold),
                ForeColor = cTexto, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(420, 30),
                Location = new Point(30, 70), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDesc = new Label
            {
                Text = "Complete el formulario. El administrador\nrecibirá su solicitud y activará su cuenta.",
                Font = new Font("Arial", 9), ForeColor = cGris, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(420, 36),
                Location = new Point(30, 104), TextAlign = ContentAlignment.MiddleCenter
            };

            var sep = new Panel { Size = new Size(420, 1), Location = new Point(30, 144), BackColor = Color.FromArgb(60, 60, 75) };

            // ── Campos ────────────────────────────────────────────────────
            int y = 158;

            AddLabel("EMPRESA", y);        y += 18;
            cboEmpresa = new ComboBox
            {
                Location = new Point(30, y), Size = new Size(420, 32),
                BackColor = cInput, ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            CargarEmpresas();
            this.Controls.Add(cboEmpresa);
            y += 44;

            AddLabel("NOMBRE COMPLETO", y); y += 18;
            txtNombre  = AddTextBox(y); y += 44;

            AddLabel("USUARIO DESEADO", y);  y += 18;
            txtUsuario = AddTextBox(y); y += 44;

            AddLabel("CORREO ELECTRÓNICO", y); y += 18;
            txtCorreo  = AddTextBox(y); y += 44;

            AddLabel("CONTRASEÑA", y); y += 18;
            txtPassword = AddTextBox(y, esPassword: true); y += 52;

            // ── Botones ───────────────────────────────────────────────────
            btnEnviar = new Button
            {
                Text = "ENVIAR SOLICITUD",
                Size = new Size(420, 44), Location = new Point(30, y),
                BackColor = cOro, ForeColor = Color.FromArgb(20, 16, 6),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 210, 130);
            btnEnviar.Click += BtnEnviar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar", Size = new Size(120, 22),
                Location = new Point(180, y + 50),
                BackColor = Color.Transparent, ForeColor = cGris,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 8, FontStyle.Underline), Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblIcon, lblTitulo, lblDesc, sep, btnEnviar, btnCancelar });
            this.Height = y + 110;
        }

        private void AddLabel(string texto, int top)
        {
            this.Controls.Add(new Label
            {
                Text = texto, Font = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = cOro, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(420, 15),
                Location = new Point(30, top), TextAlign = ContentAlignment.MiddleLeft
            });
        }

        private TextBox AddTextBox(int top, bool esPassword = false)
        {
            var tb = new TextBox
            {
                Location = new Point(30, top), Size = new Size(420, 32),
                BackColor = cInput, ForeColor = cTexto,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 10),
                PasswordChar = esPassword ? '●' : '\0'
            };
            this.Controls.Add(tb);
            return tb;
        }

        private void CargarEmpresas()
        {
            try
            {
                cboEmpresa.Items.Clear();
                cboEmpresa.Items.Add("-- Seleccione empresa --");
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nombre FROM empresas WHERE activo=true ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboEmpresa.Items.Add($"{dr.GetInt32(0)}|{dr.GetString(1)}");
                }
                cboEmpresa.SelectedIndex = 0;
                // Mostrar solo nombre en el combo
                cboEmpresa.Format += (s, e) => {
                    if (e.ListItem.ToString().Contains("|"))
                        e.Value = e.ListItem.ToString().Split('|')[1];
                };
                cboEmpresa.FormattingEnabled = true;
            }
            catch { }
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            // Validaciones
            if (cboEmpresa.SelectedIndex == 0)
            { MessageBox.Show("Seleccione una empresa.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("Ingrese su nombre completo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            { MessageBox.Show("Ingrese el usuario deseado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtCorreo.Text) || !txtCorreo.Text.Contains("@"))
            { MessageBox.Show("Ingrese un correo válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text.Length < 4)
            { MessageBox.Show("La contraseña debe tener al menos 4 caracteres.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int empresaId = int.Parse(cboEmpresa.SelectedItem.ToString().Split('|')[0]);

            // Hash de contraseña
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(txtPassword.Text));
                var sb = new System.Text.StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                hash = sb.ToString();
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Crear tabla si no existe
                    string crearTabla = @"
                    CREATE TABLE IF NOT EXISTS solicitudes_usuario (
                        id          SERIAL PRIMARY KEY,
                        empresa_id  INT REFERENCES empresas(id),
                        nombre      VARCHAR(100) NOT NULL,
                        usuario     VARCHAR(50)  NOT NULL,
                        correo      VARCHAR(100) NOT NULL,
                        password_hash VARCHAR(256) NOT NULL,
                        estado      VARCHAR(20)  DEFAULT 'PENDIENTE',
                        fecha       TIMESTAMP    DEFAULT NOW()
                    );";
                    using (var cmd = new NpgsqlCommand(crearTabla, conn))
                        cmd.ExecuteNonQuery();

                    // Verificar que el usuario no exista ya
                    string checkSql = "SELECT COUNT(*) FROM usuarios WHERE usuario = @usr";
                    using (var cmd = new NpgsqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("usr", txtUsuario.Text.Trim());
                        long count = (long)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Ese nombre de usuario ya existe. Elija otro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // Insertar solicitud
                    string sql = @"INSERT INTO solicitudes_usuario(empresa_id, nombre, usuario, correo, password_hash)
                                   VALUES(@eid, @nom, @usr, @cor, @pwd)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", empresaId);
                        cmd.Parameters.AddWithValue("nom", txtNombre.Text.Trim());
                        cmd.Parameters.AddWithValue("usr", txtUsuario.Text.Trim());
                        cmd.Parameters.AddWithValue("cor", txtCorreo.Text.Trim());
                        cmd.Parameters.AddWithValue("pwd", hash);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "✅ Solicitud enviada correctamente.\n\nEl administrador revisará su solicitud\ny activará su cuenta pronto.",
                    "Solicitud enviada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar solicitud:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}