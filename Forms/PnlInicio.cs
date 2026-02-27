using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlInicio : UserControl
    {
        private readonly Color colorDorado  = Color.FromArgb(120, 95, 55);
        private readonly Color colorVerde   = Color.FromArgb(46, 125, 50);
        private readonly Color colorAzul    = Color.FromArgb(21, 101, 192);
        private readonly Color colorNaranja = Color.FromArgb(230, 81, 0);
        private readonly Color colorRojo    = Color.FromArgb(183, 28, 28);
        private readonly Color colorTeal    = Color.FromArgb(0, 121, 107);
        private readonly Color colorMorado  = Color.FromArgb(106, 27, 154);

        public PnlInicio()
        {
            this.BackColor  = Color.FromArgb(245, 240, 228);
            this.Padding    = new Padding(20);
            this.AutoScroll = true;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            var lblTitulo = new Label
            {
                Text      = $"Bienvenido, {Sesion.UsuarioActivo?.Nombre}",
                Font      = new Font("Arial", 16, FontStyle.Bold),
                Location  = new Point(20, 20),
                AutoSize  = true,
                ForeColor = colorDorado
            };

            var lblFecha = new Label
            {
                Text      = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                            new System.Globalization.CultureInfo("es-PE")),
                Font      = new Font("Arial", 10),
                Location  = new Point(22, 50),
                AutoSize  = true,
                ForeColor = Color.Gray
            };

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblFecha);

            // ── Separador VENTAS ──────────────────────────────────────────
            var lblSepVentas = new Label
            {
                Text = "VENTAS",
                Font = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 140, 100),
                Location = new Point(22, 88), AutoSize = true
            };
            this.Controls.Add(lblSepVentas);

            CrearTarjeta("💰  Ventas Hoy",     ObtenerVentasHoy(),      colorVerde,   20,  105);
            CrearTarjeta("📅  Ventas del Mes", ObtenerVentasMes(),      colorAzul,    240, 105);
            CrearTarjeta("📦  Productos",      ObtenerTotalProductos(), colorNaranja, 460, 105);
            CrearTarjeta("⚠️  Stock Bajo",     ObtenerStockBajo(),      colorRojo,    680, 105);

            // ── Separador COMPRAS ─────────────────────────────────────────
            var lblSepCompras = new Label
            {
                Text = "COMPRAS",
                Font = new Font("Arial", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 140, 100),
                Location = new Point(22, 228), AutoSize = true
            };
            this.Controls.Add(lblSepCompras);

            CrearTarjeta("📥  Compras Hoy",     ObtenerComprasHoy(),  colorTeal,   20,  245);
            CrearTarjeta("📅  Compras del Mes", ObtenerComprasMes(),  colorMorado, 240, 245);
            CrearTarjeta("👥  Clientes",         ObtenerTotalClientes(), colorDorado, 460, 245);

            // ── Últimas ventas ────────────────────────────────────────────
            var lblUltimas = new Label
            {
                Text      = "Últimas Ventas",
                Font      = new Font("Arial", 12, FontStyle.Bold),
                Location  = new Point(20, 375),
                AutoSize  = true,
                ForeColor = colorDorado
            };
            this.Controls.Add(lblUltimas);

            var gridVentas = new DataGridView
            {
                Location            = new Point(20, 400),
                Size                = new Size(440, 220),
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.FixedSingle,
                ReadOnly            = true,
                AllowUserToAddRows  = false,
                RowHeadersVisible   = false,
                Font                = new Font("Arial", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid(gridVentas, colorVerde);
            this.Controls.Add(gridVentas);
            CargarUltimasVentas(gridVentas);

            // ── Últimas compras ───────────────────────────────────────────
            var lblUltimasC = new Label
            {
                Text      = "Últimas Compras",
                Font      = new Font("Arial", 12, FontStyle.Bold),
                Location  = new Point(480, 375),
                AutoSize  = true,
                ForeColor = colorTeal
            };
            this.Controls.Add(lblUltimasC);

            var gridCompras = new DataGridView
            {
                Location            = new Point(480, 400),
                Size                = new Size(440, 220),
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.FixedSingle,
                ReadOnly            = true,
                AllowUserToAddRows  = false,
                RowHeadersVisible   = false,
                Font                = new Font("Arial", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid(gridCompras, colorTeal);
            this.Controls.Add(gridCompras);
            CargarUltimasCompras(gridCompras);
        }

        private void EstiloGrid(DataGridView g, Color color)
        {
            g.ColumnHeadersDefaultCellStyle.BackColor = color;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
        }

        private void CrearTarjeta(string titulo, string valor, Color color, int x, int y)
        {
            var pnl = new Panel { Size = new Size(200, 110), Location = new Point(x, y), BackColor = color };
            pnl.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(200, 255, 200),
                Location = new Point(10, 10), AutoSize = true
            });
            pnl.Controls.Add(new Label
            {
                Text = valor, Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(10, 45), AutoSize = true
            });
            this.Controls.Add(pnl);
        }

        // ── Métricas de ventas ────────────────────────────────────────────
        private string ObtenerVentasHoy()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COALESCE(SUM(total),0) FROM ventas WHERE DATE(fecha)=CURRENT_DATE AND empresa_id=@eid", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        return "S/ " + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2");
                    }
                }
            }
            catch { return "S/ 0.00"; }
        }

        private string ObtenerVentasMes()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COALESCE(SUM(total),0) FROM ventas WHERE DATE_TRUNC('month',fecha)=DATE_TRUNC('month',CURRENT_DATE) AND empresa_id=@eid", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        return "S/ " + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2");
                    }
                }
            }
            catch { return "S/ 0.00"; }
        }

        private string ObtenerTotalProductos()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM productos WHERE activo=true", conn))
                        return cmd.ExecuteScalar().ToString();
                }
            }
            catch { return "0"; }
        }

        private string ObtenerStockBajo()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM productos WHERE stock <= stock_minimo AND activo=true", conn))
                        return cmd.ExecuteScalar().ToString();
                }
            }
            catch { return "0"; }
        }

        private string ObtenerTotalClientes()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM clientes WHERE activo=true", conn))
                        return cmd.ExecuteScalar().ToString();
                }
            }
            catch { return "0"; }
        }

        // ── Métricas de compras ───────────────────────────────────────────
        private string ObtenerComprasHoy()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    // Verificar que la tabla existe primero
                    using (var cmd = new NpgsqlCommand(
                        @"SELECT COALESCE(SUM(total),0) FROM compras
                          WHERE DATE(fecha)=CURRENT_DATE AND empresa_id=@eid", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        return "S/ " + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2");
                    }
                }
            }
            catch { return "S/ 0.00"; }
        }

        private string ObtenerComprasMes()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        @"SELECT COALESCE(SUM(total),0) FROM compras
                          WHERE DATE_TRUNC('month',fecha)=DATE_TRUNC('month',CURRENT_DATE)
                            AND empresa_id=@eid", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        return "S/ " + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2");
                    }
                }
            }
            catch { return "S/ 0.00"; }
        }

        // ── Grids de últimos registros ────────────────────────────────────
        private void CargarUltimasVentas(DataGridView grid)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT v.numero_venta, COALESCE(c.nombre,'General') as cliente,
                                          v.fecha, v.total, v.estado
                                   FROM ventas v LEFT JOIN clientes c ON v.cliente_id=c.id
                                   WHERE v.empresa_id=@eid
                                   ORDER BY v.fecha DESC LIMIT 10";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        var dt = new System.Data.DataTable();
                        new NpgsqlDataAdapter(cmd).Fill(dt);
                        dt.Columns["numero_venta"].ColumnName = "N° Venta";
                        dt.Columns["cliente"].ColumnName      = "Cliente";
                        dt.Columns["fecha"].ColumnName        = "Fecha";
                        dt.Columns["total"].ColumnName        = "Total";
                        dt.Columns["estado"].ColumnName       = "Estado";
                        grid.DataSource = dt;
                    }
                }
            }
            catch { }
        }

        private void CargarUltimasCompras(DataGridView grid)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT c.numero_compra, COALESCE(c.proveedor,'Sin proveedor') as proveedor,
                                          c.fecha, c.total, c.estado
                                   FROM compras c
                                   WHERE c.empresa_id=@eid
                                   ORDER BY c.fecha DESC LIMIT 10";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        var dt = new System.Data.DataTable();
                        new NpgsqlDataAdapter(cmd).Fill(dt);
                        dt.Columns["numero_compra"].ColumnName = "N° Compra";
                        dt.Columns["proveedor"].ColumnName     = "Proveedor";
                        dt.Columns["fecha"].ColumnName         = "Fecha";
                        dt.Columns["total"].ColumnName         = "Total";
                        dt.Columns["estado"].ColumnName        = "Estado";
                        grid.DataSource = dt;
                    }
                }
            }
            catch { }
        }
    }
}