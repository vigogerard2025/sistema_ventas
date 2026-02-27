using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class FrmLogin : Form
    {
        private Panel pnlIzquierdo, pnlDerecho;
        private Label lblBienvenido, lblSubtitulo, lblCopyright;
        private Label lblEmpresa, lblSucursal, lblUsuario, lblPassword;
        private ComboBox cboEmpresa, cboSucursal;
        private TextBox txtUsuario, txtPassword;
        private Button btnAceptar, btnCancelar;
        private PictureBox picLogo;

        private readonly Color cFondo      = Color.FromArgb(247, 245, 242);
        private readonly Color cPanelBlanco = Color.FromArgb(255, 255, 255);
        private readonly Color cAccent     = Color.FromArgb(37, 99, 235);
        private readonly Color cTexto      = Color.FromArgb(26, 26, 46);
        private readonly Color cTextoSub   = Color.FromArgb(107, 114, 128);
        private readonly Color cLabel      = Color.FromArgb(55, 65, 81);
        private readonly Color cInputBg    = Color.FromArgb(250, 250, 248);

        public FrmLogin()
        {
            InicializarComponentes();
            CargarEmpresas();
        }

        private void InicializarComponentes()
        {
            this.Text            = "Sistema de Ventas — Iniciar Sesión";
            this.Size            = new Size(900, 560);
            this.MinimumSize     = new Size(900, 560);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox     = false;
            this.BackColor       = cFondo;

            // ── PANEL IZQUIERDO ───────────────────────────────────────────
            pnlIzquierdo = new Panel
            {
                Size = new Size(555, 560), Location = new Point(0, 0), BackColor = cPanelBlanco
            };

            picLogo = new PictureBox
            {
                Size = new Size(36, 36), Location = new Point(52, 36), BackColor = Color.Transparent
            };
            picLogo.Paint += PicLogo_Paint;

            var lblLogoText = new Label
            {
                Text = "SistemaVentas", Font = new Font("Georgia", 12, FontStyle.Bold),
                ForeColor = cTexto, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(96, 47)
            };

            lblBienvenido = new Label
            {
                Text = "Bienvenido",
                Font = new Font("Georgia", 22, FontStyle.Bold),
                ForeColor = cTexto, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(450, 38),
                Location = new Point(52, 102), TextAlign = ContentAlignment.MiddleLeft
            };

            lblSubtitulo = new Label
            {
                Text = "Ingrese sus credenciales para continuar",
                Font = new Font("Arial", 9), ForeColor = cTextoSub,
                BackColor = Color.Transparent, AutoSize = false,
                Size = new Size(450, 20), Location = new Point(52, 142),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSeccion = new Label
            {
                Text = "───── Empresa:Alverca Carbajal Paulo Meza, Sucursal:Principal ─────",
                Font = new Font("Arial", 8), ForeColor = Color.FromArgb(180, 175, 165),
                BackColor = Color.Transparent, AutoSize = false,
                Size = new Size(450, 16), Location = new Point(52, 174),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Empresa / Sucursal
            lblEmpresa = CrearLabel("EMPRESA", 200);
            cboEmpresa = CrearCombo(218);
            cboEmpresa.Size = new Size(216, 32);
            cboEmpresa.Location = new Point(52, 218);
            cboEmpresa.SelectedIndexChanged += CboEmpresa_SelectedIndexChanged;

            lblSucursal = CrearLabel("SUCURSAL", 200);
            lblSucursal.Location = new Point(280, 200);
            cboSucursal = CrearCombo(218);
            cboSucursal.Size = new Size(216, 32);
            cboSucursal.Location = new Point(280, 218);

            lblUsuario = CrearLabel("USUARIO", 268);
            txtUsuario = CrearTextBox(286);

            lblPassword = CrearLabel("CONTRASEÑA", 336);
            txtPassword = CrearTextBox(354, esPassword: true);
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnAceptar_Click(s, e); };

            // Botón Ingresar
            btnAceptar = new Button
            {
                Text = "Ingresar al sistema", Size = new Size(450, 48),
                Location = new Point(52, 410), BackColor = cTexto,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnAceptar.FlatAppearance.BorderSize = 0;
            btnAceptar.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 17, 40);
            btnAceptar.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 80);
            btnAceptar.Click += BtnAceptar_Click;

            // Cancelar
            btnCancelar = new Button
            {
                Text = "Cancelar y salir", Size = new Size(120, 20),
                Location = new Point(218, 480), BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 175, 165), FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 7, FontStyle.Underline), Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCancelar.Click += (s, e) => Application.Exit();

            pnlIzquierdo.Controls.AddRange(new Control[]
            {
                picLogo, lblLogoText,
                lblBienvenido, lblSubtitulo, lblSeccion,
                lblEmpresa, cboEmpresa,
                lblSucursal, cboSucursal,
                lblUsuario, txtUsuario,
                lblPassword, txtPassword,
                btnAceptar, btnCancelar
            });

            // ── PANEL DERECHO ─────────────────────────────────────────────
            pnlDerecho = new Panel
            {
                Size = new Size(345, 560), Location = new Point(555, 0),
                BackColor = Color.FromArgb(15, 23, 42)
            };
            pnlDerecho.Paint += PnlDerecho_Paint;

            var lblBadge = new Label
            {
                Text = "● Acceso seguro",
                Font = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                BackColor = Color.FromArgb(30, 147, 197, 253),
                AutoSize = false, Size = new Size(130, 26),
                Location = new Point((345 - 130) / 2, 142),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblRightTitle = new Label
            {
                Text = "Lleva tu negocio\nal siguiente nivel.",
                Font = new Font("Georgia", 18, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(295, 82),
                Location = new Point((345 - 295) / 2, 180),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblRightSub = new Label
            {
                Text = "Ventas, compras, inventario\ny reportes en tiempo real.",
                Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(71, 85, 105), BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(275, 44),
                Location = new Point((345 - 275) / 2, 272),
                TextAlign = ContentAlignment.MiddleCenter
            };

            int chipW = 220, chipX = (345 - 220) / 2, fy = 380;
            foreach (var f in new[] {
                ("📊", "Ventas en tiempo real"),
                ("📥", "Control de compras"),
                ("📦", "Control de inventario"),
                ("💰", "Reportes financieros") })
            {
                var chip = new Label
                {
                    Text = $"  {f.Item1}  {f.Item2}",
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.FromArgb(240, 248, 255),
                    BackColor = Color.FromArgb(40, 255, 255, 255),
                    AutoSize = false, Size = new Size(chipW, 28),
                    Location = new Point(chipX, fy),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlDerecho.Controls.Add(chip);
                fy += 34;
            }

            lblCopyright = new Label
            {
                Text = "© 2025 Alverca Carbajal Paulo Meza",
                Font = new Font("Arial", 7),
                ForeColor = Color.FromArgb(71, 85, 105), BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(295, 16),
                Location = new Point((345 - 295) / 2, 530),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlDerecho.Controls.AddRange(new Control[] { lblBadge, lblRightTitle, lblRightSub, lblCopyright });

            this.Controls.Add(pnlIzquierdo);
            this.Controls.Add(pnlDerecho);
        }

        private Label CrearLabel(string texto, int top)
        {
            return new Label
            {
                Text = texto, Font = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = cLabel, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(450, 14),
                Location = new Point(52, top), TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private ComboBox CrearCombo(int top)
        {
            return new ComboBox
            {
                Location = new Point(52, top), Size = new Size(450, 32),
                BackColor = cInputBg, ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private TextBox CrearTextBox(int top, bool esPassword = false)
        {
            return new TextBox
            {
                Location = new Point(52, top), Size = new Size(450, 32),
                BackColor = cInputBg, ForeColor = cTexto,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Arial", 10),
                PasswordChar = esPassword ? '●' : '\0'
            };
        }

        private void PnlDerecho_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, 345, 560),
                Color.FromArgb(185, 212, 228), Color.FromArgb(148, 185, 210),
                LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(br, 0, 0, 345, 560);
            using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                g.FillEllipse(br, 160, -80, 260, 260);
            using (var br = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                g.FillEllipse(br, -80, 380, 230, 230);
        }

        private void PicLogo_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = new GraphicsPath())
            {
                int r = 8;
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(36 - r * 2, 0, r * 2, r * 2, 270, 90);
                path.AddArc(36 - r * 2, 36 - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0, 36 - r * 2, r * 2, r * 2, 90, 90);
                path.CloseAllFigures();
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, 36, 36),
                    Color.FromArgb(37, 99, 235), Color.FromArgb(96, 165, 250),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillPath(br, path);
            }
            using (var br = new SolidBrush(Color.White))
            using (var font = new Font("Georgia", 16, FontStyle.Bold))
                g.DrawString("SV", font, br, new PointF(3, 6));
        }

        private void CargarEmpresas()
        {
            try
            {
                cboEmpresa.Items.Clear();
                cboEmpresa.Items.Add(new Empresa { Id = 0, Nombre = "-- Seleccione empresa --" });
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, nombre FROM empresas WHERE activo = true ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboEmpresa.Items.Add(new Empresa { Id = dr.GetInt32(0), Nombre = dr.GetString(1) });
                }
                cboEmpresa.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con la base de datos:\n" + ex.Message,
                    "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboEmpresa_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboSucursal.Items.Clear();
            cboSucursal.Items.Add(new Sucursal { Id = 0, Nombre = "-- Seleccione sucursal --" });
            if (cboEmpresa.SelectedItem is Empresa emp && emp.Id > 0)
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, nombre FROM sucursales WHERE empresa_id=@eid AND activo=true ORDER BY nombre", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", emp.Id);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                cboSucursal.Items.Add(new Sucursal { Id = dr.GetInt32(0), Nombre = dr.GetString(1) });
                    }
                }
            }
            cboSucursal.SelectedIndex = 0;
        }

        private void BtnAceptar_Click(object sender, EventArgs e)
        {
            if (!(cboEmpresa.SelectedItem is Empresa emp) || emp.Id == 0)
            { MessageBox.Show("Seleccione una empresa.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboSucursal.SelectedItem is Sucursal suc) || suc.Id == 0)
            { MessageBox.Show("Seleccione una sucursal.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            { MessageBox.Show("Ingrese su usuario.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            { MessageBox.Show("Ingrese su contraseña.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string hash = SHA256Hash(txtPassword.Text);
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT u.id, u.nombre, u.empresa_id, u.sucursal_id, r.nombre as rol
                                   FROM usuarios u JOIN roles r ON u.rol_id = r.id
                                   WHERE u.usuario=@usr AND u.password_hash=@pwd
                                     AND u.empresa_id=@eid AND u.activo=true";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("usr", txtUsuario.Text.Trim());
                        cmd.Parameters.AddWithValue("pwd", hash);
                        cmd.Parameters.AddWithValue("eid", emp.Id);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                Sesion.UsuarioActivo = new Usuario
                                {
                                    Id = dr.GetInt32(0), Nombre = dr.GetString(1),
                                    EmpresaId = dr.GetInt32(2), SucursalId = dr.GetInt32(3),
                                    RolNombre = dr.GetString(4)
                                };
                                Sesion.EmpresaActiva  = emp;
                                Sesion.SucursalActiva = suc;
                                new FrmMenu().Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso Denegado",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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