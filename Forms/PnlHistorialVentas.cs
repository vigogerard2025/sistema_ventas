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
            var lbl = new Label
            {
                Text = "📋  HISTORIAL DE VENTAS",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorDorado,
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lbl);

            // ── Filtros ───────────────────────────────────────────────────
            var pnlFiltros = new Panel
            {
                Location  = new Point(15, 50),
                Size      = new Size(940, 50),
                BackColor = Color.Transparent
            };

            pnlFiltros.Controls.Add(new Label { Text = "Desde:", Location = new Point(0, 12), AutoSize = true });
            dtpDesde = new DateTimePicker
            {
                Location = new Point(55, 8), Size = new Size(140, 28),
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddDays(-30)
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
                BackColor = colorDorado, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarVentas();
            pnlFiltros.Controls.Add(btnBuscar);

            this.Controls.Add(pnlFiltros);

            // ── Grid ventas ───────────────────────────────────────────────
            gridVentas = CrearGrid(new Point(15, 110), new Size(940, 250));

            // Columnas de datos
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "num_venta", HeaderText = "N° Venta",  Width = 160, ReadOnly = true });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "cliente",   HeaderText = "Cliente",   Width = 180, ReadOnly = true });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",     HeaderText = "Fecha",     Width = 130, ReadOnly = true });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "total",     HeaderText = "Total",     Width = 90,  ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "tipo_pago", HeaderText = "Pago",      Width = 80,  ReadOnly = true });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "estado",    HeaderText = "Estado",    Width = 80,  ReadOnly = true });
            gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "usuario",   HeaderText = "Vendedor",  Width = 110, ReadOnly = true });

            // ── COLUMNA ELIMINAR ──────────────────────────────────────────
            // Se agrega AQUÍ, al final de las columnas del gridVentas.
            // Es un DataGridViewButtonColumn con el texto "❌ Eliminar".
            var colEliminar = new DataGridViewButtonColumn
            {
                Name       = "colEliminar",
                HeaderText = "Acción",
                Text       = "❌ Eliminar",
                UseColumnTextForButtonValue = true,
                Width      = 95,
                FlatStyle  = FlatStyle.Flat
            };
            colEliminar.DefaultCellStyle.BackColor = Color.FromArgb(200, 60, 50);
            colEliminar.DefaultCellStyle.ForeColor = Color.White;
            colEliminar.DefaultCellStyle.Font      = new Font("Arial", 8, FontStyle.Bold);
            gridVentas.Columns.Add(colEliminar);
            // ─────────────────────────────────────────────────────────────

            gridVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;  // controlar anchos manualmente
            gridVentas.SelectionChanged   += GridVentas_SelectionChanged;

            // ── EVENTO CLICK PARA ELIMINAR ────────────────────────────────
            // Se agrega AQUÍ, justo después de crear el grid.
            gridVentas.CellClick += GridVentas_CellClick;
            // ─────────────────────────────────────────────────────────────

            this.Controls.Add(gridVentas);

            lblTotalMostrado = new Label
            {
                Location  = new Point(15, 368),
                AutoSize  = true,
                Font      = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = colorDorado
            };
            this.Controls.Add(lblTotalMostrado);

            // ── Grid detalle ──────────────────────────────────────────────
            this.Controls.Add(new Label
            {
                Text = "Detalle de la Venta Seleccionada:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(15, 380),
                AutoSize = true,
                ForeColor = colorDorado
            });

            gridDetalle = CrearGrid(new Point(15, 405), new Size(940, 160));
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "producto", HeaderText = "Producto",  Width = 300 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "cantidad", HeaderText = "Cant.",     Width = 60 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "precio",   HeaderText = "P. Unit.",  Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "subtotal", HeaderText = "Subtotal",  Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            this.Controls.Add(gridDetalle);
        }

        // ── Helper grid ───────────────────────────────────────────────────
        private DataGridView CrearGrid(Point loc, Size size)
        {
            var g = new DataGridView
            {
                Location = loc, Size = size,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false,
                Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 24 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            g.ColumnHeadersBorderStyle     = DataGridViewHeaderBorderStyle.None;
            g.EnableHeadersVisualStyles    = false;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 200, 160);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 20, 5);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);
            return g;
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARGAR VENTAS
        // ══════════════════════════════════════════════════════════════════
        private void CargarVentas()
        {
            gridVentas.Rows.Clear();
            gridDetalle.Rows.Clear();
            decimal totalGeneral = 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT v.numero_venta,
                               COALESCE(c.nombre, 'General'),
                               v.fecha,
                               v.total,
                               v.tipo_pago,
                               v.estado,
                               u.nombre
                        FROM   ventas v
                        LEFT JOIN clientes c ON v.cliente_id  = c.id
                        LEFT JOIN usuarios u ON v.usuario_id  = u.id
                        WHERE  v.empresa_id = @eid
                          AND  DATE(v.fecha) BETWEEN @desde AND @hasta
                          AND  (@buscar = '' OR v.numero_venta ILIKE @buscar OR c.nombre ILIKE @buscar)
                        ORDER  BY v.fecha DESC";

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
                                gridVentas.Rows.Add(
                                    dr.GetString(0),
                                    dr.GetString(1),
                                    dr.GetDateTime(2).ToString("dd/MM/yyyy HH:mm"),
                                    "S/ " + tot.ToString("N2"),
                                    dr.GetString(4),
                                    dr.GetString(5),
                                    dr.GetString(6)
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ventas:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            lblTotalMostrado.Text =
                $"Total mostrado: S/ {totalGeneral:N2}  |  {gridVentas.Rows.Count} ventas";
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTO CLICK EN CELDA — aquí se detecta el botón Eliminar
        // ══════════════════════════════════════════════════════════════════
        private void GridVentas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ignorar si no es la columna Eliminar o si es el encabezado
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != gridVentas.Columns["colEliminar"].Index) return;

            // ── Leer datos de la fila seleccionada ────────────────────────
            string numeroVenta = gridVentas.Rows[e.RowIndex].Cells["num_venta"].Value?.ToString();
            string cliente     = gridVentas.Rows[e.RowIndex].Cells["cliente"].Value?.ToString();
            string total       = gridVentas.Rows[e.RowIndex].Cells["total"].Value?.ToString();
            string fecha       = gridVentas.Rows[e.RowIndex].Cells["fecha"].Value?.ToString();

            // ── Confirmación con los datos visibles ───────────────────────
            var confirm = MessageBox.Show(
                $"¿Está seguro de eliminar esta venta?\n\n" +
                $"  N° Venta : {numeroVenta}\n" +
                $"  Cliente  : {cliente}\n" +
                $"  Fecha    : {fecha}\n" +
                $"  Total    : {total}\n\n" +
                "Esta acción restaurará el stock de los productos\n" +
                "y NO se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            // ── Eliminar en BD ────────────────────────────────────────────
            EliminarVenta(numeroVenta);
        }

        // ══════════════════════════════════════════════════════════════════
        //  LÓGICA DE ELIMINACIÓN
        //  Orden correcto:
        //    1. Restaurar stock de cada producto del detalle
        //    2. Borrar detalle_ventas  (hijos)
        //    3. Borrar ventas          (padre)
        // ══════════════════════════════════════════════════════════════════
        private void EliminarVenta(string numeroVenta)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var tr = conn.BeginTransaction())
                    {
                        // Paso 1 — Obtener el id interno de la venta
                        int ventaId;
                        using (var cmd = new NpgsqlCommand(
                            "SELECT id FROM ventas WHERE numero_venta = @num", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("num", numeroVenta);
                            var result = cmd.ExecuteScalar();
                            if (result == null)
                            {
                                MessageBox.Show("No se encontró la venta en la base de datos.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            ventaId = Convert.ToInt32(result);
                        }

                        // Paso 2 — Restaurar stock de cada línea del detalle
                        using (var cmd = new NpgsqlCommand(
                            "SELECT producto_id, cantidad FROM detalle_ventas WHERE venta_id = @vid", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("vid", ventaId);
                            using (var dr = cmd.ExecuteReader())
                            {
                                // Leer todo antes de ejecutar más comandos
                                var lineas = new System.Collections.Generic.List<(int prodId, int cant)>();
                                while (dr.Read())
                                    lineas.Add((dr.GetInt32(0), dr.GetInt32(1)));

                                dr.Close();

                                foreach (var (prodId, cant) in lineas)
                                {
                                    using (var upd = new NpgsqlCommand(
                                        "UPDATE productos SET stock = stock + @c WHERE id = @pid", conn, tr))
                                    {
                                        upd.Parameters.AddWithValue("c",   cant);
                                        upd.Parameters.AddWithValue("pid", prodId);
                                        upd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Paso 3 — Borrar comprobantes vinculados (FK comprobantes_venta_id_fkey)
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM comprobantes WHERE venta_id = @vid", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("vid", ventaId);
                            cmd.ExecuteNonQuery();
                        }

                        // Paso 4 — Borrar detalle_ventas (hijos)
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM detalle_ventas WHERE venta_id = @vid", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("vid", ventaId);
                            cmd.ExecuteNonQuery();
                        }

                        // Paso 5 — Borrar la venta (padre)
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM ventas WHERE id = @vid", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("vid", ventaId);
                            cmd.ExecuteNonQuery();
                        }

                        tr.Commit();
                    }
                }

                MessageBox.Show(
                    $"✅  Venta {numeroVenta} eliminada correctamente.\nEl stock fue restaurado.",
                    "Eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar el grid
                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar la venta:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SELECCIÓN DE FILA → muestra detalle
        // ══════════════════════════════════════════════════════════════════
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
                    string sql = @"
                        SELECT p.nombre, dv.cantidad, dv.precio_unitario, dv.subtotal
                        FROM   detalle_ventas dv
                        JOIN   productos p ON dv.producto_id = p.id
                        JOIN   ventas    v ON dv.venta_id    = v.id
                        WHERE  v.numero_venta = @num";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("num", numVenta);
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                gridDetalle.Rows.Add(
                                    dr.GetString(0),
                                    dr.GetInt32(1),
                                    "S/ " + dr.GetDecimal(2).ToString("N2"),
                                    "S/ " + dr.GetDecimal(3).ToString("N2")
                                );
                    }
                }
            }
            catch { }
        }
    }
}