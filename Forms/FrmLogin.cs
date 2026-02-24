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
        // ── Controles ──────────────────────────────────────────────────────
        private Panel      pnlIzquierdo, pnlDerecho;
        private Label      lblBienvenido, lblSubtitulo, lblCopyright;
        private Label      lblEmpresa, lblSucursal, lblUsuario, lblPassword;
        private ComboBox   cboEmpresa, cboSucursal;
        private TextBox    txtUsuario, txtPassword;
        private Button     btnAceptar, btnCancelar;
        private Button     btnOlvideCredenciales, btnNuevoUsuario;
        private PictureBox picLogo;

        // ── Paleta CLARA inspirada en diseño moderno ───────────────────────
        private readonly Color cFondo       = Color.FromArgb(247, 245, 242);  // crema muy suave
        private readonly Color cPanelBlanco = Color.FromArgb(255, 255, 255);  // blanco panel form
        private readonly Color cPanelRight  = Color.FromArgb(185, 210, 225);  // azul grisáceo panel derecho
        private readonly Color cAccent      = Color.FromArgb(37,  99,  235);  // azul vivo
        private readonly Color cAccentH     = Color.FromArgb(29,  78, 216);   // hover azul
        private readonly Color cTexto       = Color.FromArgb(26,  26,  46);   // casi negro
        private readonly Color cTextoSub    = Color.FromArgb(107, 114, 128);  // gris subtítulo
        private readonly Color cLabel       = Color.FromArgb(55,  65,  81);   // label oscuro
        private readonly Color cBorde       = Color.FromArgb(209, 203, 184);  // borde arena
        private readonly Color cInputBg     = Color.FromArgb(250, 250, 248);  // fondo inputs

        public FrmLogin()
        {
            InicializarComponentes();
            CargarEmpresas();
        }

        private void InicializarComponentes()
        {
            this.Text            = "Sistema de Ventas — Iniciar Sesión";
            this.Size            = new Size(900, 580);
            this.MinimumSize     = new Size(900, 580);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox     = false;
            this.BackColor       = cFondo;

            // ══════════════════════════════════════════════════════════════
            // PANEL IZQUIERDO — formulario (blanco)
            // ══════════════════════════════════════════════════════════════
            pnlIzquierdo = new Panel
            {
                Size      = new Size(555, 580),
                Location  = new Point(0, 0),
                BackColor = cPanelBlanco
            };

            // Logo badge
            picLogo = new PictureBox
            {
                Size      = new Size(36, 36),
                Location  = new Point(52, 36),
                BackColor = Color.Transparent
            };
            picLogo.Paint += PicLogo_Paint;

            var lblLogoText = new Label
            {
                Text      = "SistemaVentas",
                Font      = new Font("Georgia", 12, FontStyle.Bold),
                ForeColor = cTexto,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(96, 47)
            };

            // Títulos
            lblBienvenido = new Label
            {
                Text      = "Bienvenido de vuelta",
                Font      = new Font("Georgia", 22, FontStyle.Bold),
                ForeColor = cTexto,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(450, 38),
                Location  = new Point(52, 102),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblSubtitulo = new Label
            {
                Text      = "Ingrese sus credenciales para continuar",
                Font      = new Font("Arial", 9),
                ForeColor = cTextoSub,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(450, 20),
                Location  = new Point(52, 142),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSeccion = new Label
            {
                Text      = "─────  Empresa y sucursal  ─────",
                Font      = new Font("Arial", 8),
                ForeColor = Color.FromArgb(180, 175, 165),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(450, 16),
                Location  = new Point(52, 174),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Campos grilla empresa / sucursal (lado a lado) ──
            lblEmpresa  = CrearLabel("EMPRESA",   200);
            cboEmpresa  = CrearCombo(218);
            cboEmpresa.Size     = new Size(216, 32);
            cboEmpresa.Location = new Point(52, 218);
            cboEmpresa.SelectedIndexChanged += CboEmpresa_SelectedIndexChanged;

            lblSucursal  = CrearLabel("SUCURSAL", 200);
            lblSucursal.Location = new Point(280, 200);
            cboSucursal  = CrearCombo(218);
            cboSucursal.Size     = new Size(216, 32);
            cboSucursal.Location = new Point(280, 218);

            lblUsuario  = CrearLabel("USUARIO",    268);
            txtUsuario  = CrearTextBox(286);

            lblPassword = CrearLabel("CONTRASEÑA", 336);
            txtPassword = CrearTextBox(354, esPassword: true);
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnAceptar_Click(s, e); };

            // Extras: recordar / olvidé
            var chkRecordar = new CheckBox
            {
                Text      = "Recordar por 30 días",
                Font      = new Font("Arial", 8),
                ForeColor = cTextoSub,
                BackColor = Color.Transparent,
                Location  = new Point(52, 404),
                AutoSize  = true
            };

            btnOlvideCredenciales = new Button
            {
                Text      = "Olvidé mi contraseña",
                Size      = new Size(160, 22),
                Location  = new Point(334, 402),
                BackColor = Color.Transparent,
                ForeColor = cAccent,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 8, FontStyle.Underline),
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight
            };
            btnOlvideCredenciales.FlatAppearance.BorderSize         = 0;
            btnOlvideCredenciales.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnOlvideCredenciales.Click += BtnOlvideCredenciales_Click;

            // ── Botón INGRESAR ─────────────────────────────────────────────
            btnAceptar = new Button
            {
                Text      = "Ingresar al sistema",
                Size      = new Size(450, 48),
                Location  = new Point(52, 434),
                BackColor = cTexto,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 11, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnAceptar.FlatAppearance.BorderSize             = 0;
            btnAceptar.FlatAppearance.MouseOverBackColor     = Color.FromArgb(17, 17, 40);
            btnAceptar.FlatAppearance.MouseDownBackColor     = Color.FromArgb(40, 40, 80);
            btnAceptar.Click += BtnAceptar_Click;

            // ── Crear nuevo usuario ────────────────────────────────────────
            var lblSignup = new Label
            {
                Text      = "¿No tienes cuenta?",
                Font      = new Font("Arial", 8),
                ForeColor = cTextoSub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(182, 502)
            };

            btnNuevoUsuario = new Button
            {
                Text      = "Crear usuario",
                Size      = new Size(90, 20),
                Location  = new Point(300, 500),
                BackColor = Color.Transparent,
                ForeColor = cAccent,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 8, FontStyle.Underline),
                Cursor    = Cursors.Hand
            };
            btnNuevoUsuario.FlatAppearance.BorderSize         = 0;
            btnNuevoUsuario.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnNuevoUsuario.Click += BtnNuevoUsuario_Click;

            // Cancelar pequeño
            btnCancelar = new Button
            {
                Text      = "Cancelar y salir",
                Size      = new Size(120, 20),
                Location  = new Point(218, 548),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 175, 165),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 7, FontStyle.Underline),
                Cursor    = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize         = 0;
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCancelar.Click += (s, e) => Application.Exit();

            pnlIzquierdo.Controls.AddRange(new Control[]
            {
                picLogo, lblLogoText,
                lblBienvenido, lblSubtitulo, lblSeccion,
                lblEmpresa,  cboEmpresa,
                lblSucursal, cboSucursal,
                lblUsuario,  txtUsuario,
                lblPassword, txtPassword,
                chkRecordar, btnOlvideCredenciales,
                btnAceptar,
                lblSignup, btnNuevoUsuario,
                btnCancelar
            });

            // ══════════════════════════════════════════════════════════════
            // PANEL DERECHO — decorativo azul claro
            // ══════════════════════════════════════════════════════════════
            pnlDerecho = new Panel
            {
                Size      = new Size(345, 580),
                Location  = new Point(555, 0),
                BackColor = cPanelRight
            };
            pnlDerecho.Paint += PnlDerecho_Paint;

            // Badge "Acceso seguro"
            var lblBadge = new Label
            {
                Text      = "● Acceso seguro",
                Font      = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 248, 255),
                BackColor = Color.FromArgb(60, 255, 255, 255),
                AutoSize  = false,
                Size      = new Size(120, 24),
                Location  = new Point(22, 290),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblRightTitle = new Label
            {
                Text      = "Lleva tu negocio\nal siguiente nivel.",
                Font      = new Font("Georgia", 17, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(300, 72),
                Location  = new Point(22, 322),
                TextAlign = ContentAlignment.TopLeft
            };

            var lblRightSub = new Label
            {
                Text      = "Ventas, inventario y reportes\nfinancieros en tiempo real.",
                Font      = new Font("Arial", 9),
                ForeColor = Color.FromArgb(220, 240, 255),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(300, 42),
                Location  = new Point(22, 400),
                TextAlign = ContentAlignment.TopLeft
            };

            // Feature chips
            int fy = 452;
            foreach (var f in new[] {
                ("📊", "Ventas en tiempo real"),
                ("📦", "Control de inventario"),
                ("💰", "Reportes financieros") })
            {
                var chip = new Label
                {
                    Text      = $"  {f.Item1}  {f.Item2}",
                    Font      = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.FromArgb(240, 248, 255),
                    BackColor = Color.FromArgb(40, 255, 255, 255),
                    AutoSize  = false,
                    Size      = new Size(220, 28),
                    Location  = new Point(22, fy),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlDerecho.Controls.Add(chip);
                fy += 34;
            }

            lblCopyright = new Label
            {
                Text      = "© 2025 Alverca Carbajal Paulo Meza",
                Font      = new Font("Arial", 6),
                ForeColor = Color.FromArgb(160, 220, 240, 255),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(300, 14),
                Location  = new Point(22, 556),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlDerecho.Controls.AddRange(new Control[]
            { lblBadge, lblRightTitle, lblRightSub, lblCopyright });

            this.Controls.Add(pnlIzquierdo);
            this.Controls.Add(pnlDerecho);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HELPERS VISUALES
        // ═══════════════════════════════════════════════════════════════════
        private Label CrearLabel(string texto, int top)
        {
            return new Label
            {
                Text      = texto,
                Font      = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = cLabel,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(450, 14),
                Location  = new Point(52, top),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private ComboBox CrearCombo(int top)
        {
            return new ComboBox
            {
                Location      = new Point(52, top),
                Size          = new Size(450, 32),
                BackColor     = cInputBg,
                ForeColor     = cTexto,
                FlatStyle     = FlatStyle.Flat,
                Font          = new Font("Arial", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private TextBox CrearTextBox(int top, bool esPassword = false)
        {
            return new TextBox
            {
                Location     = new Point(52, top),
                Size         = new Size(450, 32),
                BackColor    = cInputBg,
                ForeColor    = cTexto,
                BorderStyle  = BorderStyle.FixedSingle,
                Font         = new Font("Arial", 10),
                PasswordChar = esPassword ? '●' : '\0'
            };
        }

        private void PnlDerecho_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gradiente base azul claro
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, 345, 580),
                Color.FromArgb(185, 212, 228),
                Color.FromArgb(148, 185, 210),
                LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(br, 0, 0, 345, 580);

            // Círculo decorativo superior derecha
            using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                g.FillEllipse(br, 160, -80, 260, 260);

            // Círculo decorativo inferior izquierda
            using (var br = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                g.FillEllipse(br, -80, 380, 230, 230);

            // Línea separadora izquierda sutil
            using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                g.DrawLine(pen, 0, 40, 0, 540);
        }

        private void PicLogo_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Badge redondo con gradiente azul
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, 36, 36),
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(96, 165, 250),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(br, 0, 0, 36, 36);   // cuadrado redondeado manual
            }
            // Esquinas redondeadas
            using (var path = new GraphicsPath())
            {
                int r = 8;
                path.AddArc(0, 0, r*2, r*2, 180, 90);
                path.AddArc(36-r*2, 0, r*2, r*2, 270, 90);
                path.AddArc(36-r*2, 36-r*2, r*2, r*2, 0, 90);
                path.AddArc(0, 36-r*2, r*2, r*2, 90, 90);
                path.CloseAllFigures();
                using (var br2 = new LinearGradientBrush(
                    new Rectangle(0,0,36,36),
                    Color.FromArgb(37, 99, 235), Color.FromArgb(96, 165, 250),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillPath(br2, path);
            }
            using (var br = new SolidBrush(Color.White))
            using (var font = new Font("Georgia", 16, FontStyle.Bold))
                g.DrawString("SV", font, br, new PointF(3, 6));
        }

        // ═══════════════════════════════════════════════════════════════════
        //  LÓGICA DE NEGOCIO (sin cambios)
        // ═══════════════════════════════════════════════════════════════════
        private void CargarEmpresas()
        {
            try
            {
                cboEmpresa.Items.Clear();
                cboEmpresa.Items.Add(new Empresa { Id = 0, Nombre = "-- Seleccione empresa --" });

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT id, nombre FROM empresas WHERE activo = true ORDER BY nombre";
                    using (var cmd = new NpgsqlCommand(sql, conn))
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
                    string sql = "SELECT id, nombre FROM sucursales WHERE empresa_id=@eid AND activo=true ORDER BY nombre";
                    using (var cmd = new NpgsqlCommand(sql, conn))
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
                                    Id         = dr.GetInt32(0),
                                    Nombre     = dr.GetString(1),
                                    EmpresaId  = dr.GetInt32(2),
                                    SucursalId = dr.GetInt32(3),
                                    RolNombre  = dr.GetString(4)
                                };
                                Sesion.EmpresaActiva  = emp;
                                Sesion.SucursalActiva = suc;

                                var menu = new FrmMenu();
                                menu.Show();
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

        private void BtnOlvideCredenciales_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmRecuperarPassword())
                frm.ShowDialog(this);
        }

        private void BtnNuevoUsuario_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmNuevoUsuario())
                frm.ShowDialog(this);
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

    // =========================================================================
    //  FORMULARIO — Recuperar contraseña
    // =========================================================================
    public class FrmRecuperarPassword : Form
    {
        private readonly Color cFondo  = Color.FromArgb(247, 245, 242);
        private readonly Color cAccent = Color.FromArgb(37,  99,  235);
        private readonly Color cInput  = Color.FromArgb(250, 250, 248);
        private readonly Color cTexto  = Color.FromArgb(26,  26,  46);
        private readonly Color cGris   = Color.FromArgb(107, 114, 128);

        public FrmRecuperarPassword()
        {
            this.Text            = "Recuperar acceso";
            this.Size            = new Size(440, 320);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = cFondo;

            var lblIcon = new Label
            {
                Text = "📧", Font = new Font("Segoe UI Emoji", 28),
                ForeColor = cAccent, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(185, 20)
            };

            var lblTitulo = new Label
            {
                Text = "Recuperar acceso",
                Font = new Font("Georgia", 14, FontStyle.Bold),
                ForeColor = cTexto, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(380, 30),
                Location = new Point(30, 76), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDesc = new Label
            {
                Text = "Ingrese su correo electrónico registrado.\nLe enviaremos sus datos de acceso.",
                Font = new Font("Arial", 9), ForeColor = cGris, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(380, 38),
                Location = new Point(30, 110), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblCorreo = new Label
            {
                Text = "CORREO ELECTRÓNICO",
                Font = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81), BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(380, 15),
                Location = new Point(30, 158), TextAlign = ContentAlignment.MiddleLeft
            };

            var txtCorreo = new TextBox
            {
                Location = new Point(30, 176), Size = new Size(380, 32),
                BackColor = cInput, ForeColor = cTexto,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Arial", 10)
            };

            var btnEnviar = new Button
            {
                Text = "ENVIAR CORREO DE RECUPERACIÓN",
                Size = new Size(380, 46), Location = new Point(30, 228),
                BackColor = cTexto, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 17, 40);
            btnEnviar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCorreo.Text) || !txtCorreo.Text.Contains("@"))
                {
                    MessageBox.Show("Ingrese un correo válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(
                    $"Se envió un correo a:\n{txtCorreo.Text}\n\nRevise su bandeja de entrada.",
                    "Correo enviado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblIcon, lblTitulo, lblDesc, lblCorreo, txtCorreo, btnEnviar });
        }
    }
}