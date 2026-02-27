using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlHistorialCompras : UserControl
    {
        private DateTimePicker dtpDesde, dtpHasta;
        private TextBox txtBuscar;
        private DataGridView gridCompras, gridDetalle;
        private Label lblTotalMostrado;
        private readonly Color colorVerde  = Color.FromArgb(30, 120, 60);
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);

        public PnlHistorialCompras()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarCompras();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label
            {
                Text = "📋  HISTORIAL DE COMPRAS",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorVerde, Location = new Point(20, 15), AutoSize = true
            };
            this.Controls.Add(lbl);

            // Filtros
            var pnlFiltros = new Panel { Location = new Point(15, 50), Size = new Size(940, 50), BackColor = Color.Transparent };

            pnlFiltros.Controls.Add(new Label { Text = "Desde:", Location = new Point(0, 12), AutoSize = true });
            dtpDesde = new DateTimePicker
            {
                Location = new Point(55, 8), Size = new Size(140, 28),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30)
            };
            pnlFiltros.Controls.Add(dtpDesde);

            pnlFiltros.Controls.Add(new Label { Text = "Hasta:", Location = new Point(210, 12), AutoSize = true });
            dtpHasta = new DateTimePicker
            {
                Location = new Point(260, 8), Size = new Size(140, 28),
                Format = DateTimePickerFormat.Short
            };
            pnlFiltros.Controls.Add(dtpHasta);

            pnlFiltros.Controls.Add(new Label { Text = "Buscar:", Location = new Point(420, 12), AutoSize = true });
            txtBuscar = new TextBox { Location = new Point(475, 8), Size = new Size(200, 28) };
            pnlFiltros.Controls.Add(txtBuscar);

            var btnBuscar = new Button
            {
                Text = "🔍 Buscar", Location = new Point(685, 6), Size = new Size(100, 32),
                BackColor = colorVerde, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarCompras();
            pnlFiltros.Controls.Add(btnBuscar);

            this.Controls.Add(pnlFiltros);

            // Grid compras
            gridCompras = CrearGrid(new Point(15, 110), new Size(940, 250));
            gridCompras.Columns.Add("num_compra", "N° Compra");
            gridCompras.Columns.Add("proveedor",  "Proveedor");
            gridCompras.Columns.Add("fecha",      "Fecha");
            gridCompras.Columns.Add("total",      "Total");
            gridCompras.Columns.Add("tipo_pago",  "Pago");
            gridCompras.Columns.Add("estado",     "Estado");
            gridCompras.Columns.Add("usuario",    "Registrado por");
            gridCompras.SelectionChanged += GridCompras_SelectionChanged;
            this.Controls.Add(gridCompras);

            lblTotalMostrado = new Label
            {
                Location = new Point(15, 368), AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = colorVerde
            };
            this.Controls.Add(lblTotalMostrado);

            // Grid detalle
            var lblDet = new Label
            {
                Text = "Detalle de la Compra Seleccionada:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(15, 380), AutoSize = true, ForeColor = colorVerde
            };
            this.Controls.Add(lblDet);

            gridDetalle = CrearGrid(new Point(15, 405), new Size(940, 160));
            gridDetalle.Columns.Add("producto",  "Producto");
            gridDetalle.Columns.Add("cantidad",  "Cant.");
            gridDetalle.Columns.Add("precio",    "P. Compra");
            gridDetalle.Columns.Add("subtotal",  "Subtotal");
            this.Controls.Add(gridDetalle);
        }

        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView
            {
                Location = loc, Size = size,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorVerde;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9, FontStyle.Bold);
            return g;
        }

        private void CargarCompras()
        {
            gridCompras.Rows.Clear();
            decimal totalGeneral = 0;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Crear tablas si no existen
                    string crearTablas = @"
                    CREATE TABLE IF NOT EXISTS compras (
                        id SERIAL PRIMARY KEY, numero_compra VARCHAR(20) UNIQUE NOT NULL,
                        empresa_id INT REFERENCES empresas(id), sucursal_id INT REFERENCES sucursales(id),
                        proveedor VARCHAR(200), usuario_id INT REFERENCES usuarios(id),
                        fecha TIMESTAMP DEFAULT NOW(), subtotal DECIMAL(12,2) DEFAULT 0,
                        igv DECIMAL(12,2) DEFAULT 0, total DECIMAL(12,2) DEFAULT 0,
                        tipo_pago VARCHAR(20) DEFAULT 'EFECTIVO', estado VARCHAR(20) DEFAULT 'COMPLETADA',
                        observacion VARCHAR(300)
                    );
                    CREATE TABLE IF NOT EXISTS detalle_compras (
                        id SERIAL PRIMARY KEY, compra_id INT REFERENCES compras(id),
                        producto_id INT REFERENCES productos(id), cantidad INT NOT NULL,
                        precio_unitario DECIMAL(12,2) NOT NULL, subtotal DECIMAL(12,2) NOT NULL
                    );";
                    using (var cmd = new NpgsqlCommand(crearTablas, conn))
                        cmd.ExecuteNonQuery();

                    string sql = @"SELECT c.numero_compra, COALESCE(c.proveedor,'Sin proveedor'),
                                          c.fecha, c.total, c.tipo_pago, c.estado, u.nombre
                                   FROM compras c
                                   LEFT JOIN usuarios u ON c.usuario_id = u.id
                                   WHERE c.empresa_id = @eid
                                     AND DATE(c.fecha) BETWEEN @desde AND @hasta
                                     AND (@buscar = '' OR c.numero_compra ILIKE @buscar
                                          OR COALESCE(c.proveedor,'') ILIKE @buscar)
                                   ORDER BY c.fecha DESC";

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
                                gridCompras.Rows.Add(
                                    dr.GetString(0), dr.GetString(1),
                                    dr.GetDateTime(2).ToString("dd/MM/yyyy HH:mm"),
                                    "S/ " + tot.ToString("N2"),
                                    dr.GetString(4), dr.GetString(5), dr.GetString(6));
                            }
                        }
                    }
                }
            }
            catch { }
            lblTotalMostrado.Text = $"Total comprado: S/ {totalGeneral:N2}  |  {gridCompras.Rows.Count} compras";
        }

        private void GridCompras_SelectionChanged(object sender, EventArgs e)
        {
            gridDetalle.Rows.Clear();
            if (gridCompras.SelectedRows.Count == 0) return;
            string numCompra = gridCompras.SelectedRows[0].Cells["num_compra"].Value?.ToString();
            if (string.IsNullOrEmpty(numCompra)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT p.nombre, dc.cantidad, dc.precio_unitario, dc.subtotal
                                   FROM detalle_compras dc
                                   JOIN productos p ON dc.producto_id = p.id
                                   JOIN compras c ON dc.compra_id = c.id
                                   WHERE c.numero_compra = @num";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("num", numCompra);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                gridDetalle.Rows.Add(
                                    dr.GetString(0), dr.GetInt32(1),
                                    "S/ " + dr.GetDecimal(2).ToString("N2"),
                                    "S/ " + dr.GetDecimal(3).ToString("N2"));
                    }
                }
            }
            catch { }
        }
    }
}