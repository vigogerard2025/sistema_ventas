using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlHistorialVentas : UserControl
    {
        private DateTimePicker dtpDesde, dtpHasta;
        private TextBox txtBuscar;
        private DataGridView gridVentas, gridDetalle;
        private Label lblTotalMostrado;
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);

        public PnlHistorialVentas()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarVentas();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label { Text = "📋  HISTORIAL DE VENTAS", Font = new Font("Arial", 14, FontStyle.Bold),
                                   ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lbl);

            // Filtros
            var pnlFiltros = new Panel { Location = new Point(15, 50), Size = new Size(940, 50), BackColor = Color.Transparent };

            pnlFiltros.Controls.Add(new Label { Text = "Desde:", Location = new Point(0, 12), AutoSize = true });
            dtpDesde = new DateTimePicker { Location = new Point(55, 8), Size = new Size(140, 28), Format = DateTimePickerFormat.Short,
                                             Value = DateTime.Today.AddDays(-30) };
            pnlFiltros.Controls.Add(dtpDesde);

            pnlFiltros.Controls.Add(new Label { Text = "Hasta:", Location = new Point(210, 12), AutoSize = true });
            dtpHasta = new DateTimePicker { Location = new Point(260, 8), Size = new Size(140, 28), Format = DateTimePickerFormat.Short };
            pnlFiltros.Controls.Add(dtpHasta);

            pnlFiltros.Controls.Add(new Label { Text = "Buscar:", Location = new Point(420, 12), AutoSize = true });
            txtBuscar = new TextBox { Location = new Point(475, 8), Size = new Size(200, 28) };
            pnlFiltros.Controls.Add(txtBuscar);

            var btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(685, 6), Size = new Size(100, 32),
                                          BackColor = colorDorado, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarVentas();
            pnlFiltros.Controls.Add(btnBuscar);

            this.Controls.Add(pnlFiltros);

            // Grid ventas
            gridVentas = CrearGrid(new Point(15, 110), new Size(940, 250));
            gridVentas.Columns.Add("num_venta",    "N° Venta");
            gridVentas.Columns.Add("cliente",      "Cliente");
            gridVentas.Columns.Add("fecha",        "Fecha");
            gridVentas.Columns.Add("total",        "Total");
            gridVentas.Columns.Add("tipo_pago",    "Pago");
            gridVentas.Columns.Add("estado",       "Estado");
            gridVentas.Columns.Add("usuario",      "Vendedor");
            gridVentas.SelectionChanged += GridVentas_SelectionChanged;
            this.Controls.Add(gridVentas);

            lblTotalMostrado = new Label { Location = new Point(15, 368), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = colorDorado };
            this.Controls.Add(lblTotalMostrado);

            // Grid detalle
            var lblDet = new Label { Text = "Detalle de la Venta Seleccionada:", Font = new Font("Arial", 10, FontStyle.Bold),
                                      Location = new Point(15, 380), AutoSize = true, ForeColor = colorDorado };
            this.Controls.Add(lblDet);

            gridDetalle = CrearGrid(new Point(15, 405), new Size(940, 160));
            gridDetalle.Columns.Add("producto",    "Producto");
            gridDetalle.Columns.Add("cantidad",    "Cant.");
            gridDetalle.Columns.Add("precio",      "P. Unit.");
            gridDetalle.Columns.Add("subtotal",    "Subtotal");
            this.Controls.Add(gridDetalle);
        }

        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView
            {
                Location = loc, Size = size, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            return g;
        }

        private void CargarVentas()
        {
            gridVentas.Rows.Clear();
            decimal totalGeneral = 0;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT v.numero_venta, COALESCE(c.nombre,'General'), v.fecha,
                                          v.total, v.tipo_pago, v.estado, u.nombre
                                   FROM ventas v
                                   LEFT JOIN clientes c ON v.cliente_id=c.id
                                   LEFT JOIN usuarios u ON v.usuario_id=u.id
                                   WHERE v.empresa_id=@eid
                                     AND DATE(v.fecha) BETWEEN @desde AND @hasta
                                     AND (@buscar='' OR v.numero_venta ILIKE @buscar OR c.nombre ILIKE @buscar)
                                   ORDER BY v.fecha DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid",    Sesion.EmpresaActiva?.Id ?? 0);
                        cmd.Parameters.AddWithValue("desde",  dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("hasta",  dtpHasta.Value.Date);
                        cmd.Parameters.AddWithValue("buscar", "%" + txtBuscar.Text.Trim() + "%");
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                decimal tot = dr.GetDecimal(3);
                                totalGeneral += tot;
                                gridVentas.Rows.Add(dr.GetString(0), dr.GetString(1),
                                                     dr.GetDateTime(2).ToString("dd/MM/yyyy HH:mm"),
                                                     "S/ " + tot.ToString("N2"), dr.GetString(4), dr.GetString(5), dr.GetString(6));
                            }
                        }
                    }
                }
            }
            catch { }
            lblTotalMostrado.Text = $"Total mostrado: S/ {totalGeneral:N2}  |  {gridVentas.Rows.Count} ventas";
        }

        private void GridVentas_SelectionChanged(object sender, EventArgs e)
        {
            gridDetalle.Rows.Clear();
            if (gridVentas.SelectedRows.Count == 0) return;
            string numVenta = gridVentas.SelectedRows[0].Cells["num_venta"].Value?.ToString();
            if (string.IsNullOrEmpty(numVenta)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT p.nombre, dv.cantidad, dv.precio_unitario, dv.subtotal
                                   FROM detalle_ventas dv JOIN productos p ON dv.producto_id=p.id
                                   JOIN ventas v ON dv.venta_id=v.id WHERE v.numero_venta=@num";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("num", numVenta);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                gridDetalle.Rows.Add(dr.GetString(0), dr.GetInt32(1),
                                                      "S/ " + dr.GetDecimal(2).ToString("N2"),
                                                      "S/ " + dr.GetDecimal(3).ToString("N2"));
                    }
                }
            }
            catch { }
        }
    }
}
