using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlReportes : UserControl
    {
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);

        public PnlReportes()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label { Text = "📊  REPORTES Y FINANZAS", Font = new Font("Arial", 14, FontStyle.Bold),
                                   ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lbl);

            // Tabs
            var tabs = new TabControl { Location = new Point(15, 55), Size = new Size(1100, 530) };
            tabs.TabPages.Add(CrearTabVentasPeriodo());
            tabs.TabPages.Add(CrearTabTopProductos());
            tabs.TabPages.Add(CrearTabResumenFinanciero());
            this.Controls.Add(tabs);
        }

        private TabPage CrearTabVentasPeriodo()
        {
            var tab = new TabPage("📅  Ventas por Período");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var dtpDesde = new DateTimePicker { Location = new Point(80, 15), Size = new Size(140, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var dtpHasta = new DateTimePicker { Location = new Point(280, 15), Size = new Size(140, 28), Format = DateTimePickerFormat.Short };

            tab.Controls.Add(new Label { Text = "Desde:", Location = new Point(10, 18), AutoSize = true });
            tab.Controls.Add(dtpDesde);
            tab.Controls.Add(new Label { Text = "Hasta:", Location = new Point(235, 18), AutoSize = true });
            tab.Controls.Add(dtpHasta);

            var btnGen = new Button { Text = "📊 Generar", Location = new Point(435, 12), Size = new Size(110, 32),
                                       BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGen.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btnGen);

            var grid = CrearGrid(new Point(5, 55), new Size(1060, 380));
            grid.Columns.Add("fecha",   "Fecha");
            grid.Columns.Add("ventas",  "N° Ventas");
            grid.Columns.Add("subtotal","Subtotal");
            grid.Columns.Add("igv",     "IGV");
            grid.Columns.Add("total",   "Total");
            tab.Controls.Add(grid);

            var lblTotal = new Label { Location = new Point(5, 445), AutoSize = true, Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = colorDorado };
            tab.Controls.Add(lblTotal);

            btnGen.Click += (s, e) =>
            {
                grid.Rows.Clear();
                decimal gran = 0;
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = @"SELECT DATE(fecha), COUNT(*), SUM(subtotal), SUM(igv), SUM(total)
                                       FROM ventas WHERE empresa_id=@eid AND DATE(fecha) BETWEEN @d AND @h
                                       GROUP BY DATE(fecha) ORDER BY DATE(fecha)";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                            cmd.Parameters.AddWithValue("d",   dtpDesde.Value.Date);
                            cmd.Parameters.AddWithValue("h",   dtpHasta.Value.Date);
                            using (var dr = cmd.ExecuteReader())
                                while (dr.Read())
                                {
                                    decimal t = dr.GetDecimal(4);
                                    gran += t;
                                    grid.Rows.Add(dr.GetDateTime(0).ToString("dd/MM/yyyy"), dr.GetInt64(1),
                                                   "S/ " + dr.GetDecimal(2).ToString("N2"),
                                                   "S/ " + dr.GetDecimal(3).ToString("N2"),
                                                   "S/ " + t.ToString("N2"));
                                }
                        }
                    }
                }
                catch { }
                lblTotal.Text = $"TOTAL GENERAL: S/ {gran:N2}";
            };

            return tab;
        }

        private TabPage CrearTabTopProductos()
        {
            var tab = new TabPage("🏆  Top Productos");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var dtpDesde = new DateTimePicker { Location = new Point(80, 15), Size = new Size(140, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var dtpHasta = new DateTimePicker { Location = new Point(280, 15), Size = new Size(140, 28), Format = DateTimePickerFormat.Short };
            tab.Controls.Add(new Label { Text = "Desde:", Location = new Point(10, 18), AutoSize = true });
            tab.Controls.Add(dtpDesde);
            tab.Controls.Add(new Label { Text = "Hasta:", Location = new Point(235, 18), AutoSize = true });
            tab.Controls.Add(dtpHasta);

            var btnGen = new Button { Text = "📊 Generar", Location = new Point(435, 12), Size = new Size(110, 32),
                                       BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGen.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btnGen);

            var grid = CrearGrid(new Point(5, 55), new Size(1060, 430));
            grid.Columns.Add("ranking",   "#");
            grid.Columns.Add("producto",  "Producto");
            grid.Columns.Add("cantidad",  "Cant. Vendida");
            grid.Columns.Add("ingresos",  "Ingresos");
            tab.Controls.Add(grid);

            btnGen.Click += (s, e) =>
            {
                grid.Rows.Clear();
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = @"SELECT ROW_NUMBER() OVER(ORDER BY SUM(dv.cantidad) DESC),
                                              p.nombre, SUM(dv.cantidad), SUM(dv.subtotal)
                                       FROM detalle_ventas dv
                                       JOIN productos p ON dv.producto_id=p.id
                                       JOIN ventas v ON dv.venta_id=v.id
                                       WHERE v.empresa_id=@eid AND DATE(v.fecha) BETWEEN @d AND @h
                                       GROUP BY p.nombre ORDER BY SUM(dv.cantidad) DESC LIMIT 20";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("eid", Sesion.EmpresaActiva?.Id ?? 0);
                            cmd.Parameters.AddWithValue("d",   dtpDesde.Value.Date);
                            cmd.Parameters.AddWithValue("h",   dtpHasta.Value.Date);
                            using (var dr = cmd.ExecuteReader())
                                while (dr.Read())
                                    grid.Rows.Add(dr.GetInt64(0), dr.GetString(1), dr.GetInt64(2), "S/ " + dr.GetDecimal(3).ToString("N2"));
                        }
                    }
                }
                catch { }
            };

            return tab;
        }

        private TabPage CrearTabResumenFinanciero()
        {
            var tab = new TabPage("💰  Resumen Financiero");
            tab.BackColor = Color.FromArgb(245, 240, 228);

            var btnGen = new Button { Text = "🔄 Actualizar", Location = new Point(10, 12), Size = new Size(130, 32),
                                       BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGen.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btnGen);

            var pnlCards = new Panel { Location = new Point(5, 55), Size = new Size(1060, 430), BackColor = Color.Transparent };
            tab.Controls.Add(pnlCards);

            btnGen.Click += (s, e) =>
            {
                pnlCards.Controls.Clear();
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        decimal ventasHoy = ObtenerValor(conn, "SELECT COALESCE(SUM(total),0) FROM ventas WHERE DATE(fecha)=CURRENT_DATE AND empresa_id=@eid", Sesion.EmpresaActiva?.Id ?? 0);
                        decimal ventasMes = ObtenerValor(conn, "SELECT COALESCE(SUM(total),0) FROM ventas WHERE DATE_TRUNC('month',fecha)=DATE_TRUNC('month',CURRENT_DATE) AND empresa_id=@eid", Sesion.EmpresaActiva?.Id ?? 0);
                        decimal ventasAnio= ObtenerValor(conn, "SELECT COALESCE(SUM(total),0) FROM ventas WHERE EXTRACT(YEAR FROM fecha)=EXTRACT(YEAR FROM CURRENT_DATE) AND empresa_id=@eid", Sesion.EmpresaActiva?.Id ?? 0);

                        AgregarTarjeta(pnlCards, "💰  Ventas Hoy",     "S/ " + ventasHoy.ToString("N2"),  Color.FromArgb(46, 125, 50),   0,   10);
                        AgregarTarjeta(pnlCards, "📅  Ventas del Mes", "S/ " + ventasMes.ToString("N2"),  Color.FromArgb(21, 101, 192),  240, 10);
                        AgregarTarjeta(pnlCards, "📆  Ventas del Año", "S/ " + ventasAnio.ToString("N2"), Color.FromArgb(120, 95, 55),   480, 10);
                    }
                }
                catch { }
            };

            return tab;
        }

        private decimal ObtenerValor(NpgsqlConnection conn, string sql, int eid)
        {
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("eid", eid);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private void AgregarTarjeta(Control parent, string titulo, string valor, Color color, int x, int y)
        {
            var pnl = new Panel { Size = new Size(220, 120), Location = new Point(x, y), BackColor = color };
            pnl.Controls.Add(new Label { Text = titulo, Font = new Font("Arial", 10), ForeColor = Color.FromArgb(200, 255, 200), Location = new Point(10, 10), AutoSize = true });
            pnl.Controls.Add(new Label { Text = valor,  Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 50), AutoSize = true });
            parent.Controls.Add(pnl);
        }

        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView { Location = loc, Size = size, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            return g;
        }
    }
}
