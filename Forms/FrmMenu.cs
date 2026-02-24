using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class FrmMenu : Form
    {
        private Panel  pnlSidebar, pnlHeader, pnlContent;
        private Label  lblEmpresa, lblUsuario, lblHora;
        private System.Windows.Forms.Timer tmrHora;

        private readonly Color colorSidebar = Color.FromArgb(80,  60,  30);
        private readonly Color colorHeader  = Color.FromArgb(120, 95,  55);
        private readonly Color colorHover   = Color.FromArgb(100, 78,  42);
        private readonly Color colorActivo  = Color.FromArgb(205, 170, 110);
        private readonly Color colorFondo   = Color.FromArgb(245, 240, 228);
        private readonly Color colorSep     = Color.FromArgb(120, 100, 55);

        // Lleva la cuenta del índice de botones en el sidebar
        private int _indiceSidebar = 0;

        public FrmMenu()
        {
            InicializarComponentes();
            ActualizarInfo();
        }

        private void InicializarComponentes()
        {
            this.Text          = "Sistema de Ventas";
            this.Size          = new Size(1280, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = colorFondo;
            this.FormClosing  += (s, e) => Application.Exit();

            // ── HEADER ────────────────────────────────────────────────────
            pnlHeader = new Panel { BackColor = colorHeader, Dock = DockStyle.Top, Height = 55 };

            var lblTitulo = new Label
            {
                Text      = "SISTEMA DE VENTAS",
                Font      = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(215, 10),
                AutoSize  = true
            };

            lblEmpresa = new Label
            {
                Font      = new Font("Arial", 9),
                ForeColor = Color.FromArgb(255, 230, 180),
                Location  = new Point(215, 33),
                AutoSize  = true
            };

            lblUsuario = new Label
            {
                Font      = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Location  = new Point(970, 10),
                AutoSize  = true
            };

            lblHora = new Label
            {
                Font      = new Font("Arial", 9),
                ForeColor = Color.FromArgb(255, 230, 180),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Location  = new Point(970, 33),
                AutoSize  = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblEmpresa, lblUsuario, lblHora });

            // ── SIDEBAR ───────────────────────────────────────────────────
            pnlSidebar = new Panel
            {
                BackColor  = colorSidebar,
                Dock       = DockStyle.Left,
                Width      = 210,
                AutoScroll = true
            };

            var logo = new Label
            {
                Text      = "≡  MENÚ",
                Font      = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = colorActivo,
                Dock      = DockStyle.Top,
                Height    = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSidebar.Controls.Add(logo);

            // ══════════════════════════════════════════════════════════════
            // SECCIÓN: PRINCIPAL (todos los roles)
            // ══════════════════════════════════════════════════════════════
            AgregarBotonMenu("🏠  Inicio",         () => MostrarPanel(new PnlInicio()));

            // ── Separador: VENTAS ─────────────────────────────────────────
            AgregarSeparador("VENTAS");
            AgregarBotonMenu("🛒  Nueva Venta",    () => MostrarPanel(new PnlVentas()));
            AgregarBotonMenu("📋  Historial",      () => MostrarPanel(new PnlHistorialVentas()));
            AgregarBotonMenu("🧾  Comprobantes",   () => MostrarPanel(new PnlComprobantes()));

            // ── Separador: INVENTARIO ─────────────────────────────────────
            AgregarSeparador("INVENTARIO");
            AgregarBotonMenu("📦  Productos",      () => MostrarPanel(new PnlProductos()));

            // ── Separador: PERSONAS ───────────────────────────────────────
            AgregarSeparador("PERSONAS");
            AgregarBotonMenu("👥  Clientes",       () => MostrarPanel(new PnlClientes()));
            AgregarBotonMenu("👔  Empleados",      () => MostrarPanel(new PnlEmpleados()));

            // ── Separador: ANÁLISIS ───────────────────────────────────────
            AgregarSeparador("ANÁLISIS");
            AgregarBotonMenu("📊  Reportes",       () => MostrarPanel(new PnlReportes()));

            // ── Separador: SISTEMA ────────────────────────────────────────
            AgregarSeparador("SISTEMA");
            AgregarBotonMenu("⚙️  Configuración",  () => MostrarPanel(new PnlConfiguracion()));

            // ══════════════════════════════════════════════════════════════
            // SECCIÓN EXCLUSIVA: ADMINISTRADOR
            // ══════════════════════════════════════════════════════════════
            if (Sesion.UsuarioActivo?.RolNombre == "ADMINISTRADOR")
            {
                AgregarSeparador("ADMIN");
                AgregarBotonMenu("📨  Solicitudes",    () => MostrarPanel(new PnlSolicitudesUsuarios()));
            }

            // ── Cerrar sesión siempre al final ────────────────────────────
            AgregarSeparadorLinea();
            AgregarBotonMenu("🚪  Cerrar Sesión",  CerrarSesion);

            // ── CONTENT ───────────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = colorFondo,
                Padding   = new Padding(10)
            };

            // ── TIMER HORA ────────────────────────────────────────────────
            tmrHora = new System.Windows.Forms.Timer { Interval = 1000 };
            tmrHora.Tick += (s, e) => lblHora.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            tmrHora.Start();

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlHeader);

            MostrarPanel(new PnlInicio());
        }

        // ── Botón de menú ─────────────────────────────────────────────────
        private void AgregarBotonMenu(string texto, Action accion)
        {
            var btn = new Button
            {
                Text      = texto,
                Size      = new Size(210, 44),
                Location  = new Point(0, 50 + _indiceSidebar * 44),
                BackColor = colorSidebar,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = colorHover;
            btn.MouseLeave += (s, e) => btn.BackColor = colorSidebar;
            btn.Click      += (s, e) => accion();
            pnlSidebar.Controls.Add(btn);
            _indiceSidebar++;
        }

        // ── Separador con etiqueta de sección ─────────────────────────────
        private void AgregarSeparador(string titulo)
        {
            var lbl = new Label
            {
                Text      = titulo,
                Font      = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = colorActivo,
                BackColor = Color.FromArgb(60, 45, 20),
                AutoSize  = false,
                Size      = new Size(210, 20),
                Location  = new Point(0, 50 + _indiceSidebar * 44),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0)
            };
            pnlSidebar.Controls.Add(lbl);
            _indiceSidebar++;
        }

        // ── Línea separadora simple ───────────────────────────────────────
        private void AgregarSeparadorLinea()
        {
            var pnl = new Panel
            {
                Size      = new Size(190, 1),
                Location  = new Point(10, 50 + _indiceSidebar * 44),
                BackColor = colorSep
            };
            pnlSidebar.Controls.Add(pnl);
            _indiceSidebar++;
        }

        private void MostrarPanel(UserControl panel)
        {
            pnlContent.Controls.Clear();
            panel.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(panel);
        }

        private void ActualizarInfo()
        {
            if (Sesion.EmpresaActiva != null)
                lblEmpresa.Text = $"{Sesion.EmpresaActiva.Nombre}  |  {Sesion.SucursalActiva?.Nombre}";
            if (Sesion.UsuarioActivo != null)
                lblUsuario.Text = $"👤 {Sesion.UsuarioActivo.Nombre}  [{Sesion.UsuarioActivo.RolNombre}]";
            lblHora.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void CerrarSesion()
        {
            if (MessageBox.Show("¿Desea cerrar sesión?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Sesion.UsuarioActivo  = null;
                Sesion.EmpresaActiva  = null;
                Sesion.SucursalActiva = null;
                new FrmLogin().Show();
                this.Close();
            }
        }
    }
}