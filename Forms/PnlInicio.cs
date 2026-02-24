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

        public PnlInicio()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            this.Padding   = new Padding(20);
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            var lblTitulo = new Label
            {
                Text     = $"Bienvenido, {Sesion.UsuarioActivo?.Nombre}",
                Font     = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor= colorDorado
            };

            var lblFecha = new Label
            {
                Text     = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-PE")),
                Font     = new Font("Arial", 10),
                Location = new Point(22, 50),
                AutoSize = true,
                ForeColor= Color.Gray
            };

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblFecha);

            // Tarjetas de resumen
            CrearTarjeta("💰  Ventas Hoy",      ObtenerVentasHoy(),       colorVerde,   20,  100);
            CrearTarjeta("📦  Productos",        ObtenerTotalProductos(),  colorAzul,    240, 100);
            CrearTarjeta("👥  Clientes",         ObtenerTotalClientes(),   colorNaranja, 460, 100);
            CrearTarjeta("⚠️  Stock Bajo",       ObtenerStockBajo(),       colorRojo,    680, 100);

            // Últimas ventas
            var lblUltimas = new Label
            {
                Text     = "Últimas Ventas",
                Font     = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(20, 240),
                AutoSize = true,
                ForeColor= colorDorado
            };
            this.Controls.Add(lblUltimas);

            var grid = new DataGridView
            {
                Location          = new Point(20, 270),
                Size              = new Size(900, 300),
                BackgroundColor   = Color.White,
                BorderStyle       = BorderStyle.FixedSingle,
                ReadOnly          = true,
                AllowUserToAddRows= false,
                RowHeadersVisible = false,
                Font              = new Font("Arial", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(120, 95, 55);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);

            this.Controls.Add(grid);
            CargarUltimasVentas(grid);
        }

        private void CrearTarjeta(string titulo, string valor, Color color, int x, int y)
        {
            var pnl = new Panel
            {
                Size      = new Size(200, 110),
                Location  = new Point(x, y),
                BackColor = color
            };

            var lbl1 = new Label
            {
                Text      = titulo,
                Font      = new Font("Arial", 10),
                ForeColor = Color.FromArgb(200, 255, 200),
                Location  = new Point(10, 10),
                AutoSize  = true
            };
            var lbl2 = new Label
            {
                Text      = valor,
                Font      = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(10, 45),
                AutoSize  = true
            };

            pnl.Controls.Add(lbl1);
            pnl.Controls.Add(lbl2);
            this.Controls.Add(pnl);
        }

        private string ObtenerVentasHoy()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT COALESCE(SUM(total),0) FROM ventas WHERE DATE(fecha)=CURRENT_DATE AND empresa_id=@eid";
                    using (var cmd = new NpgsqlCommand(sql, conn))
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

        private string ObtenerStockBajo()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM productos WHERE stock <= stock_minimo AND activo=true", conn))
                        return cmd.ExecuteScalar().ToString();
                }
            }
            catch { return "0"; }
        }

        private void CargarUltimasVentas(DataGridView grid)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT v.numero_venta, c.nombre as cliente, v.fecha, 
                                          v.total, v.tipo_pago, v.estado
                                   FROM ventas v LEFT JOIN clientes c ON v.cliente_id=c.id
                                   WHERE v.empresa_id=@eid
                                   ORDER BY v.fecha DESC LIMIT 20";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                        var adapter = new NpgsqlDataAdapter(cmd);
                        var dt      = new System.Data.DataTable();
                        adapter.Fill(dt);
                        dt.Columns["numero_venta"].ColumnName = "N° Venta";
                        dt.Columns["cliente"].ColumnName      = "Cliente";
                        dt.Columns["fecha"].ColumnName        = "Fecha";
                        dt.Columns["total"].ColumnName        = "Total";
                        dt.Columns["tipo_pago"].ColumnName    = "Pago";
                        dt.Columns["estado"].ColumnName       = "Estado";
                        grid.DataSource = dt;
                    }
                }
            }
            catch { }
        }
    }
}
