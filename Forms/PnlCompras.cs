using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlCompras : UserControl
    {
        private ComboBox cboProducto, cboTipoPago;
        private TextBox txtProveedor, txtCantidad, txtObservacion;
        private DataGridView gridDetalle;
        private Label lblSubtotal, lblIgv, lblTotal;
        private Button btnAgregarProducto, btnGuardarCompra, btnLimpiar;

        private List<DetalleCompra> detalles = new List<DetalleCompra>();
        private readonly Color colorDorado = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton  = Color.FromArgb(100, 80, 45);
        private readonly Color colorVerde  = Color.FromArgb(30, 120, 60);

        public PnlCompras()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label
            {
                Text = "📥  NUEVA COMPRA",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true
            };
            this.Controls.Add(lbl);

            // ---- PANEL IZQUIERDO ----
            var pnlLeft = new GroupBox
            {
                Text = "Datos de la Compra", Location = new Point(15, 50),
                Size = new Size(680, 490), Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // Proveedor
            AgregarLabel(pnlLeft, "Proveedor:", 20, 25);
            txtProveedor = new TextBox
            {
                Location = new Point(100, 22), Size = new Size(320, 28),
                Font = new Font("Arial", 9), PlaceholderText = "Nombre del proveedor..."
            };
            pnlLeft.Controls.Add(txtProveedor);

            // Tipo pago
            AgregarLabel(pnlLeft, "Pago:", 440, 25);
            cboTipoPago = new ComboBox
            {
                Location = new Point(490, 22), Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9)
            };
            cboTipoPago.Items.AddRange(new object[] { "EFECTIVO", "TARJETA", "TRANSFERENCIA", "CRÉDITO" });
            cboTipoPago.SelectedIndex = 0;
            pnlLeft.Controls.Add(cboTipoPago);

            // Producto
            AgregarLabel(pnlLeft, "Producto:", 20, 65);
            cboProducto = new ComboBox
            {
                Location = new Point(100, 62), Size = new Size(370, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9)
            };
            pnlLeft.Controls.Add(cboProducto);

            AgregarLabel(pnlLeft, "Cant:", 480, 65);
            txtCantidad = new TextBox
            {
                Location = new Point(515, 62), Size = new Size(60, 28),
                Text = "1", Font = new Font("Arial", 9), TextAlign = HorizontalAlignment.Center
            };
            pnlLeft.Controls.Add(txtCantidad);

            btnAgregarProducto = new Button
            {
                Text = "➕ Agregar", Location = new Point(585, 60), Size = new Size(80, 30),
                BackColor = colorVerde, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9), Cursor = Cursors.Hand
            };
            btnAgregarProducto.FlatAppearance.BorderSize = 0;
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            pnlLeft.Controls.Add(btnAgregarProducto);

            // Grid detalle
            gridDetalle = new DataGridView
            {
                Location = new Point(10, 100), Size = new Size(655, 280),
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false, ReadOnly = false,
                RowHeadersVisible = false, Font = new Font("Arial", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridDetalle.ColumnHeadersDefaultCellStyle.BackColor = colorVerde;
            gridDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9, FontStyle.Bold);

            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColId",       HeaderText = "ID",          Width = 40,  Visible = false });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNombre",   HeaderText = "Producto",    Width = 250, ReadOnly = true });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCant",     HeaderText = "Cantidad",    Width = 70 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrecio",   HeaderText = "P. Compra",   Width = 90 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSubtotal", HeaderText = "Subtotal",    Width = 90,  ReadOnly = true });
            var btnEliminar = new DataGridViewButtonColumn
            {
                Name = "ColEliminar", HeaderText = "", Text = "❌",
                UseColumnTextForButtonValue = true, Width = 45
            };
            gridDetalle.Columns.Add(btnEliminar);

            gridDetalle.CellValueChanged += GridDetalle_CellValueChanged;
            gridDetalle.CellClick += GridDetalle_CellClick;
            pnlLeft.Controls.Add(gridDetalle);

            // Observación
            AgregarLabel(pnlLeft, "Nota:", 10, 398);
            txtObservacion = new TextBox
            {
                Location = new Point(65, 395), Size = new Size(590, 26),
                Font = new Font("Arial", 9), PlaceholderText = "Referencia, factura del proveedor..."
            };
            pnlLeft.Controls.Add(txtObservacion);

            this.Controls.Add(pnlLeft);

            // ---- PANEL DERECHO (TOTALES) ----
            var pnlRight = new GroupBox
            {
                Text = "Resumen", Location = new Point(710, 50),
                Size = new Size(250, 490), Font = new Font("Arial", 9, FontStyle.Bold)
            };

            AgregarLabelRight(pnlRight, "Subtotal:", 25);
            lblSubtotal = new Label
            {
                Text = "S/ 0.00", Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 45), AutoSize = true, ForeColor = Color.Black
            };
            pnlRight.Controls.Add(lblSubtotal);

            AgregarLabelRight(pnlRight, "IGV (18%):", 80);
            lblIgv = new Label
            {
                Text = "S/ 0.00", Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 100), AutoSize = true, ForeColor = Color.DarkBlue
            };
            pnlRight.Controls.Add(lblIgv);

            var sep = new Panel { BackColor = colorVerde, Location = new Point(5, 125), Size = new Size(235, 2) };
            pnlRight.Controls.Add(sep);

            AgregarLabelRight(pnlRight, "TOTAL:", 135);
            lblTotal = new Label
            {
                Text = "S/ 0.00", Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(10, 155), AutoSize = true, ForeColor = colorVerde
            };
            pnlRight.Controls.Add(lblTotal);

            var lblInfo = new Label
            {
                Text = "ℹ El stock se incrementará\nautomáticamente al\nguardar la compra.",
                Font = new Font("Arial", 8), ForeColor = Color.FromArgb(100, 130, 100),
                BackColor = Color.FromArgb(235, 248, 235),
                Location = new Point(8, 220), Size = new Size(230, 58),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlRight.Controls.Add(lblInfo);

            btnGuardarCompra = new Button
            {
                Text = "💾  REGISTRAR COMPRA", Location = new Point(10, 390),
                Size = new Size(225, 48), BackColor = colorVerde, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarCompra.FlatAppearance.BorderSize = 0;
            btnGuardarCompra.Click += BtnGuardarCompra_Click;
            pnlRight.Controls.Add(btnGuardarCompra);

            btnLimpiar = new Button
            {
                Text = "🗑  Limpiar", Location = new Point(10, 445),
                Size = new Size(225, 35), BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10), Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => LimpiarCompra();
            pnlRight.Controls.Add(btnLimpiar);

            this.Controls.Add(pnlRight);
        }

        private void AgregarLabel(Control parent, string texto, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = texto, Location = new Point(x, y),
                AutoSize = true, Font = new Font("Arial", 9)
            });
        }

        private void AgregarLabelRight(Control parent, string texto, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = texto, Location = new Point(10, y),
                AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold)
            });
        }

        private void CargarProductos()
        {
            try
            {
                cboProducto.Items.Clear();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, codigo, nombre, precio_compra, stock FROM productos WHERE activo=true ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboProducto.Items.Add(new Producto
                            {
                                Id = dr.GetInt32(0), Codigo = dr.GetString(1),
                                Nombre = dr.GetString(2), PrecioCompra = dr.GetDecimal(3),
                                Stock = dr.GetInt32(4)
                            });
                }
                if (cboProducto.Items.Count > 0) cboProducto.SelectedIndex = 0;
            }
            catch { }
        }

        private void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            if (!(cboProducto.SelectedItem is Producto prod)) return;
            if (!int.TryParse(txtCantidad.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar si ya está en la lista
            foreach (DataGridViewRow row in gridDetalle.Rows)
            {
                if (Convert.ToInt32(row.Cells["ColId"].Value) == prod.Id)
                {
                    int cantExistente = Convert.ToInt32(row.Cells["ColCant"].Value);
                    row.Cells["ColCant"].Value = cantExistente + cant;
                    decimal precio = decimal.Parse(row.Cells["ColPrecio"].Value.ToString());
                    row.Cells["ColSubtotal"].Value = (precio * (cantExistente + cant)).ToString("N2");
                    ActualizarTotales();
                    txtCantidad.Text = "1";
                    return;
                }
            }

            decimal subtotal = prod.PrecioCompra * cant;
            gridDetalle.Rows.Add(prod.Id, prod.Nombre, cant, prod.PrecioCompra.ToString("N2"), subtotal.ToString("N2"));
            detalles.Add(new DetalleCompra
            {
                ProductoId = prod.Id, ProductoNombre = prod.Nombre,
                Cantidad = cant, PrecioUnitario = prod.PrecioCompra, Subtotal = subtotal
            });
            ActualizarTotales();
            txtCantidad.Text = "1";
        }

        private void GridDetalle_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == gridDetalle.Columns["ColCant"].Index ||
                e.ColumnIndex == gridDetalle.Columns["ColPrecio"].Index)
            {
                if (int.TryParse(gridDetalle.Rows[e.RowIndex].Cells["ColCant"].Value?.ToString(), out int cant) && cant > 0 &&
                    decimal.TryParse(gridDetalle.Rows[e.RowIndex].Cells["ColPrecio"].Value?.ToString(), out decimal precio))
                {
                    decimal subtotal = precio * cant;
                    gridDetalle.Rows[e.RowIndex].Cells["ColSubtotal"].Value = subtotal.ToString("N2");
                }
                ActualizarTotales();
            }
        }

        private void GridDetalle_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == gridDetalle.Columns["ColEliminar"].Index)
            {
                gridDetalle.Rows.RemoveAt(e.RowIndex);
                if (e.RowIndex < detalles.Count) detalles.RemoveAt(e.RowIndex);
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            decimal subtotal = 0;
            foreach (DataGridViewRow row in gridDetalle.Rows)
                if (decimal.TryParse(row.Cells["ColSubtotal"].Value?.ToString(), out decimal s))
                    subtotal += s;

            decimal igv   = subtotal * 0.18m;
            decimal total = subtotal + igv;
            lblSubtotal.Text = "S/ " + subtotal.ToString("N2");
            lblIgv.Text      = "S/ " + igv.ToString("N2");
            lblTotal.Text    = "S/ " + total.ToString("N2");
        }

        private void BtnGuardarCompra_Click(object sender, EventArgs e)
        {
            if (gridDetalle.Rows.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotalVal = 0;
            foreach (DataGridViewRow row in gridDetalle.Rows)
                if (decimal.TryParse(row.Cells["ColSubtotal"].Value?.ToString(), out decimal s))
                    subtotalVal += s;

            decimal igvVal   = subtotalVal * 0.18m;
            decimal totalVal = subtotalVal + igvVal;
            string  numero   = "C-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Crear tabla si no existe
                    CrearTablasSiNoExisten(conn);

                    using (var tr = conn.BeginTransaction())
                    {
                        string sqlCompra = @"INSERT INTO compras
                            (numero_compra, empresa_id, sucursal_id, proveedor, usuario_id,
                             subtotal, igv, total, tipo_pago, observacion)
                            VALUES(@num,@eid,@sid,@prov,@uid,@sub,@igv,@tot,@pago,@obs)
                            RETURNING id";

                        int compraId;
                        using (var cmd = new NpgsqlCommand(sqlCompra, conn, tr))
                        {
                            cmd.Parameters.AddWithValue("num",  numero);
                            cmd.Parameters.AddWithValue("eid",  Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sid",  Sesion.SucursalActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("prov", txtProveedor.Text.Trim());
                            cmd.Parameters.AddWithValue("uid",  Sesion.UsuarioActivo?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sub",  subtotalVal);
                            cmd.Parameters.AddWithValue("igv",  igvVal);
                            cmd.Parameters.AddWithValue("tot",  totalVal);
                            cmd.Parameters.AddWithValue("pago", cboTipoPago.SelectedItem?.ToString() ?? "EFECTIVO");
                            cmd.Parameters.AddWithValue("obs",  txtObservacion.Text.Trim());
                            compraId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (DataGridViewRow row in gridDetalle.Rows)
                        {
                            int     prodId  = Convert.ToInt32(row.Cells["ColId"].Value);
                            int     cant    = Convert.ToInt32(row.Cells["ColCant"].Value);
                            decimal precio  = decimal.Parse(row.Cells["ColPrecio"].Value.ToString());
                            decimal sub     = decimal.Parse(row.Cells["ColSubtotal"].Value.ToString());

                            using (var cmd = new NpgsqlCommand(
                                @"INSERT INTO detalle_compras(compra_id,producto_id,cantidad,precio_unitario,subtotal)
                                  VALUES(@cid,@pid,@cant,@precio,@sub)", conn, tr))
                            {
                                cmd.Parameters.AddWithValue("cid",   compraId);
                                cmd.Parameters.AddWithValue("pid",   prodId);
                                cmd.Parameters.AddWithValue("cant",  cant);
                                cmd.Parameters.AddWithValue("precio",precio);
                                cmd.Parameters.AddWithValue("sub",   sub);
                                cmd.ExecuteNonQuery();
                            }

                            // Incrementar stock y actualizar precio de compra
                            using (var cmd = new NpgsqlCommand(
                                "UPDATE productos SET stock=stock+@c, precio_compra=@pc WHERE id=@pid", conn, tr))
                            {
                                cmd.Parameters.AddWithValue("c",   cant);
                                cmd.Parameters.AddWithValue("pc",  precio);
                                cmd.Parameters.AddWithValue("pid", prodId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                    }
                }

                MessageBox.Show(
                    $"✅  Compra registrada exitosamente\nN° {numero}\nTotal: S/ {totalVal:N2}\n\nEl stock fue actualizado.",
                    "Compra Registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarCompra();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar compra:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CrearTablasSiNoExisten(NpgsqlConnection conn)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS compras (
                id             SERIAL PRIMARY KEY,
                numero_compra  VARCHAR(20) UNIQUE NOT NULL,
                empresa_id     INT REFERENCES empresas(id),
                sucursal_id    INT REFERENCES sucursales(id),
                proveedor      VARCHAR(200),
                usuario_id     INT REFERENCES usuarios(id),
                fecha          TIMESTAMP DEFAULT NOW(),
                subtotal       DECIMAL(12,2) DEFAULT 0,
                igv            DECIMAL(12,2) DEFAULT 0,
                total          DECIMAL(12,2) DEFAULT 0,
                tipo_pago      VARCHAR(20) DEFAULT 'EFECTIVO',
                estado         VARCHAR(20) DEFAULT 'COMPLETADA',
                observacion    VARCHAR(300)
            );

            CREATE TABLE IF NOT EXISTS detalle_compras (
                id              SERIAL PRIMARY KEY,
                compra_id       INT REFERENCES compras(id),
                producto_id     INT REFERENCES productos(id),
                cantidad        INT NOT NULL,
                precio_unitario DECIMAL(12,2) NOT NULL,
                subtotal        DECIMAL(12,2) NOT NULL
            );";
            using (var cmd = new NpgsqlCommand(sql, conn))
                cmd.ExecuteNonQuery();
        }

        private void LimpiarCompra()
        {
            gridDetalle.Rows.Clear();
            detalles.Clear();
            txtProveedor.Clear();
            txtObservacion.Clear();
            txtCantidad.Text = "1";
            cboTipoPago.SelectedIndex = 0;
            ActualizarTotales();
        }
    }

    // ─── Modelo interno para detalle de compra ────────────────────────────
    public class DetalleCompra
    {
        public int     ProductoId      { get; set; }
        public string  ProductoNombre  { get; set; } = "";
        public int     Cantidad        { get; set; }
        public decimal PrecioUnitario  { get; set; }
        public decimal Subtotal        { get; set; }
    }
}