using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    public class PnlVentas : UserControl
    {
        private ComboBox cboCliente, cboProducto, cboTipoPago, cboTipoComprobante;
        private TextBox txtCantidad, txtObservacion;
        private DataGridView gridDetalle;
        private Label lblSubtotal, lblIgv, lblTotal;
        private Button btnAgregarProducto, btnGuardarVenta, btnLimpiar;

        private List<DetalleVenta> detalles = new List<DetalleVenta>();
        private readonly Color colorDorado  = Color.FromArgb(120, 95, 55);
        private readonly Color colorBoton   = Color.FromArgb(100, 80, 45);

        public PnlVentas()
        {
            this.BackColor = Color.FromArgb(245, 240, 228);
            InicializarComponentes();
            CargarClientes();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            var lbl = new Label
            {
                Text = "🛒  NUEVA VENTA", Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = colorDorado, Location = new Point(20, 15), AutoSize = true
            };
            this.Controls.Add(lbl);

            // ── Panel izquierdo ───────────────────────────────────────────
            var pnlLeft = new GroupBox
            {
                Text = "Datos de la Venta", Location = new Point(15, 50),
                Size = new Size(680, 500), Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // Fila 1: Cliente
            AgregarLabel(pnlLeft, "Cliente:", 10, 28);
            cboCliente = new ComboBox
            {
                Location = new Point(68, 24), Size = new Size(260, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9)
            };
            pnlLeft.Controls.Add(cboCliente);

            // ── COMPROBANTE (NUEVO) ───────────────────────────────────────
            AgregarLabel(pnlLeft, "Comprobante:", 338, 28);
            cboTipoComprobante = new ComboBox
            {
                Location = new Point(430, 24), Size = new Size(120, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9)
            };
            cboTipoComprobante.Items.AddRange(new object[] { "NINGUNO", "BOLETA", "FACTURA" });
            cboTipoComprobante.SelectedIndex = 0;
            pnlLeft.Controls.Add(cboTipoComprobante);
            // ─────────────────────────────────────────────────────────────

            // Tipo de pago
            AgregarLabel(pnlLeft, "Pago:", 560, 28);
            cboTipoPago = new ComboBox
            {
                Location = new Point(594, 24), Size = new Size(75, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 8)
            };
            cboTipoPago.Items.AddRange(new object[] { "EFECTIVO", "TARJETA", "TRANSFERENCIA", "YAPE", "PLIN" });
            cboTipoPago.SelectedIndex = 0;
            pnlLeft.Controls.Add(cboTipoPago);

            // Fila 2: Producto | Cant | Agregar
            AgregarLabel(pnlLeft, "Producto:", 10, 68);
            cboProducto = new ComboBox
            {
                Location = new Point(72, 64), Size = new Size(380, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9)
            };
            pnlLeft.Controls.Add(cboProducto);

            AgregarLabel(pnlLeft, "Cant:", 462, 68);
            txtCantidad = new TextBox
            {
                Location = new Point(498, 64), Size = new Size(55, 28),
                Text = "1", Font = new Font("Arial", 9), TextAlign = HorizontalAlignment.Center
            };
            pnlLeft.Controls.Add(txtCantidad);

            btnAgregarProducto = new Button
            {
                Text = "➕ Agregar", Location = new Point(562, 62), Size = new Size(105, 30),
                BackColor = colorBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9), Cursor = Cursors.Hand
            };
            btnAgregarProducto.FlatAppearance.BorderSize = 0;
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            pnlLeft.Controls.Add(btnAgregarProducto);

            // Grid detalle
            gridDetalle = new DataGridView
            {
                Location = new Point(10, 102), Size = new Size(655, 290),
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false, ReadOnly = false, RowHeadersVisible = false,
                Font = new Font("Arial", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 24 }
            };
            gridDetalle.ColumnHeadersDefaultCellStyle.BackColor = colorDorado;
            gridDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridDetalle.ColumnHeadersDefaultCellStyle.Font      = new Font("Arial", 9, FontStyle.Bold);
            gridDetalle.EnableHeadersVisualStyles               = false;
            gridDetalle.ColumnHeadersBorderStyle                = DataGridViewHeaderBorderStyle.None;

            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColId",       HeaderText = "ID",       Width = 40,  Visible = false });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNombre",   HeaderText = "Producto", Width = 270, ReadOnly = true });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCant",     HeaderText = "Cantidad", Width = 70 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrecio",   HeaderText = "P. Unit.", Width = 80,  ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSubtotal", HeaderText = "Subtotal", Width = 90,  ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridDetalle.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "ColEliminar", HeaderText = "", Text = "❌",
                UseColumnTextForButtonValue = true, Width = 45, FlatStyle = FlatStyle.Flat
            });

            gridDetalle.CellValueChanged += GridDetalle_CellValueChanged;
            gridDetalle.CellClick        += GridDetalle_CellClick;
            pnlLeft.Controls.Add(gridDetalle);

            // Nota
            AgregarLabel(pnlLeft, "Nota:", 10, 408);
            txtObservacion = new TextBox
            {
                Location = new Point(65, 404), Size = new Size(600, 26), Font = new Font("Arial", 9)
            };
            pnlLeft.Controls.Add(txtObservacion);

            this.Controls.Add(pnlLeft);

            // ── Panel derecho ─────────────────────────────────────────────
            var pnlRight = new GroupBox
            {
                Text = "Resumen", Location = new Point(710, 50),
                Size = new Size(250, 500), Font = new Font("Arial", 9, FontStyle.Bold)
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

            pnlRight.Controls.Add(new Panel
            {
                BackColor = colorDorado, Location = new Point(5, 125), Size = new Size(235, 2)
            });

            AgregarLabelRight(pnlRight, "TOTAL:", 135);
            lblTotal = new Label
            {
                Text = "S/ 0.00", Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(10, 155), AutoSize = true, ForeColor = colorDorado
            };
            pnlRight.Controls.Add(lblTotal);

            btnGuardarVenta = new Button
            {
                Text = "💾  GUARDAR VENTA", Location = new Point(10, 390),
                Size = new Size(225, 48), BackColor = colorBoton, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 11, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardarVenta.FlatAppearance.BorderSize = 0;
            btnGuardarVenta.Click += BtnGuardarVenta_Click;
            pnlRight.Controls.Add(btnGuardarVenta);

            btnLimpiar = new Button
            {
                Text = "🗑  Limpiar", Location = new Point(10, 445),
                Size = new Size(225, 35), BackColor = Color.FromArgb(180, 0, 0), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10), Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => LimpiarVenta();
            pnlRight.Controls.Add(btnLimpiar);

            this.Controls.Add(pnlRight);
        }

        private void AgregarLabel(Control p, string t, int x, int y) =>
            p.Controls.Add(new Label { Text = t, Location = new Point(x, y), AutoSize = true, Font = new Font("Arial", 9) });

        private void AgregarLabelRight(Control p, string t, int y) =>
            p.Controls.Add(new Label { Text = t, Location = new Point(10, y), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) });

        // ══════════════════════════════════════════════════════════════════
        //  CARGAR DATOS
        // ══════════════════════════════════════════════════════════════════
        private void CargarClientes()
        {
            try
            {
                cboCliente.Items.Clear();
                cboCliente.Items.Add(new Cliente { Id = 0, Nombre = "-- Cliente General --" });
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, nombre FROM clientes WHERE activo=true ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboCliente.Items.Add(new Cliente { Id = dr.GetInt32(0), Nombre = dr.GetString(1) });
                }
                cboCliente.SelectedIndex = 0;
            }
            catch { }
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
                        "SELECT id,codigo,nombre,precio_venta,stock FROM productos WHERE activo=true AND stock>0 ORDER BY nombre", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cboProducto.Items.Add(new Producto
                            {
                                Id = dr.GetInt32(0), Codigo = dr.GetString(1),
                                Nombre = dr.GetString(2), PrecioVenta = dr.GetDecimal(3),
                                Stock = dr.GetInt32(4)
                            });
                }
                if (cboProducto.Items.Count > 0) cboProducto.SelectedIndex = 0;
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        //  AGREGAR PRODUCTO
        // ══════════════════════════════════════════════════════════════════
        private void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            if (!(cboProducto.SelectedItem is Producto prod)) return;
            if (!int.TryParse(txtCantidad.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cant > prod.Stock)
            {
                MessageBox.Show($"Stock disponible: {prod.Stock}", "Stock insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotal = prod.PrecioVenta * cant;
            detalles.Add(new DetalleVenta
            {
                ProductoId = prod.Id, ProductoNombre = prod.Nombre,
                Cantidad = cant, PrecioUnitario = prod.PrecioVenta, Subtotal = subtotal
            });
            gridDetalle.Rows.Add(prod.Id, prod.Nombre, cant,
                prod.PrecioVenta.ToString("N2"), subtotal.ToString("N2"));
            ActualizarTotales();
            txtCantidad.Text = "1";
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTOS GRID
        // ══════════════════════════════════════════════════════════════════
        private void GridDetalle_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != gridDetalle.Columns["ColCant"].Index) return;
            if (int.TryParse(gridDetalle.Rows[e.RowIndex].Cells["ColCant"].Value?.ToString(), out int cant) && cant > 0)
            {
                decimal precio   = decimal.Parse(gridDetalle.Rows[e.RowIndex].Cells["ColPrecio"].Value?.ToString() ?? "0");
                decimal subtotal = precio * cant;
                gridDetalle.Rows[e.RowIndex].Cells["ColSubtotal"].Value = subtotal.ToString("N2");
                if (e.RowIndex < detalles.Count) { detalles[e.RowIndex].Cantidad = cant; detalles[e.RowIndex].Subtotal = subtotal; }
            }
            ActualizarTotales();
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
            decimal sub = 0;
            foreach (DataGridViewRow row in gridDetalle.Rows)
                if (decimal.TryParse(row.Cells["ColSubtotal"].Value?.ToString(), out decimal s)) sub += s;
            decimal igv   = sub * 0.18m;
            decimal total = sub + igv;
            lblSubtotal.Text = "S/ " + sub.ToString("N2");
            lblIgv.Text      = "S/ " + igv.ToString("N2");
            lblTotal.Text    = "S/ " + total.ToString("N2");
        }

        // ══════════════════════════════════════════════════════════════════
        //  GUARDAR VENTA
        // ══════════════════════════════════════════════════════════════════
        private void BtnGuardarVenta_Click(object sender, EventArgs e)
        {
            if (gridDetalle.Rows.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tipoComp = cboTipoComprobante.SelectedItem?.ToString() ?? "NINGUNO";
            var    cliente  = cboCliente.SelectedItem as Cliente;

            // Factura requiere cliente registrado
            if (tipoComp == "FACTURA" && (cliente == null || cliente.Id == 0))
            {
                MessageBox.Show("Para emitir una Factura debe seleccionar un cliente con RUC.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboCliente.Focus(); return;
            }

            decimal subtotalVal = 0;
            foreach (DataGridViewRow row in gridDetalle.Rows)
                if (decimal.TryParse(row.Cells["ColSubtotal"].Value?.ToString(), out decimal s)) subtotalVal += s;

            decimal igvVal   = Math.Round(subtotalVal * 0.18m, 2);
            decimal totalVal = subtotalVal + igvVal;
            string  numero   = "V-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var tr = conn.BeginTransaction())
                    {
                        // 1. Insertar venta
                        int ventaId;
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO ventas(numero_venta,empresa_id,sucursal_id,cliente_id,
                                              usuario_id,subtotal,igv,total,tipo_pago,observacion)
                            VALUES(@num,@eid,@sid,@cid,@uid,@sub,@igv,@tot,@pago,@obs)
                            RETURNING id", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("num",  numero);
                            cmd.Parameters.AddWithValue("eid",  Sesion.EmpresaActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sid",  Sesion.SucursalActiva?.Id ?? 1);
                            cmd.Parameters.AddWithValue("cid",  cliente?.Id > 0 ? (object)cliente.Id : DBNull.Value);
                            cmd.Parameters.AddWithValue("uid",  Sesion.UsuarioActivo?.Id ?? 1);
                            cmd.Parameters.AddWithValue("sub",  subtotalVal);
                            cmd.Parameters.AddWithValue("igv",  igvVal);
                            cmd.Parameters.AddWithValue("tot",  totalVal);
                            cmd.Parameters.AddWithValue("pago", cboTipoPago.SelectedItem?.ToString() ?? "EFECTIVO");
                            cmd.Parameters.AddWithValue("obs",  txtObservacion.Text.Trim());
                            ventaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Insertar comprobante si NO es NINGUNO
                        //    → Esto es lo que hace que el reporte muestre BOLETA/FACTURA
                        if (tipoComp != "NINGUNO")
                        {
                            string serie   = tipoComp == "BOLETA" ? "B001" : "F001";
                            string numComp = serie + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                            using (var cmd = new NpgsqlCommand(@"
                                INSERT INTO comprobantes
                                    (venta_id, empresa_id, sucursal_id, tipo, serie, numero,
                                     fecha_emision, cliente_doc, cliente_nombre,
                                     subtotal, igv, total, estado, usuario_id)
                                VALUES
                                    (@vid, @eid, @sid, @tipo, @serie, @num,
                                     @fec, @cdoc, @cnombre,
                                     @sub, @igv, @tot, 'EMITIDO', @uid)", conn, tr))
                            {
                                cmd.Parameters.AddWithValue("vid",     ventaId);
                                cmd.Parameters.AddWithValue("eid",     Sesion.EmpresaActiva?.Id ?? 1);
                                cmd.Parameters.AddWithValue("sid",     Sesion.SucursalActiva?.Id ?? 1);
                                cmd.Parameters.AddWithValue("tipo",    tipoComp);
                                cmd.Parameters.AddWithValue("serie",   serie);
                                cmd.Parameters.AddWithValue("num",     numComp);
                                cmd.Parameters.AddWithValue("fec",     DateTime.Today);
                                cmd.Parameters.AddWithValue("cdoc",    cliente?.Id > 0 ? (object)(cliente.Documento ?? "") : "");
                                cmd.Parameters.AddWithValue("cnombre", cliente?.Id > 0 ? (object)cliente.Nombre : "CLIENTE GENERAL");
                                cmd.Parameters.AddWithValue("sub",     subtotalVal);
                                cmd.Parameters.AddWithValue("igv",     igvVal);
                                cmd.Parameters.AddWithValue("tot",     totalVal);
                                cmd.Parameters.AddWithValue("uid",     Sesion.UsuarioActivo?.Id ?? 1);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3. Detalle + stock
                        foreach (DataGridViewRow row in gridDetalle.Rows)
                        {
                            int     prodId = Convert.ToInt32(row.Cells["ColId"].Value);
                            int     cant   = Convert.ToInt32(row.Cells["ColCant"].Value);
                            decimal precio = decimal.Parse(row.Cells["ColPrecio"].Value.ToString());
                            decimal sub    = decimal.Parse(row.Cells["ColSubtotal"].Value.ToString());

                            using (var cmd = new NpgsqlCommand(@"
                                INSERT INTO detalle_ventas(venta_id,producto_id,cantidad,precio_unitario,subtotal)
                                VALUES(@vid,@pid,@c,@pr,@s)", conn, tr))
                            {
                                cmd.Parameters.AddWithValue("vid", ventaId);
                                cmd.Parameters.AddWithValue("pid", prodId);
                                cmd.Parameters.AddWithValue("c",   cant);
                                cmd.Parameters.AddWithValue("pr",  precio);
                                cmd.Parameters.AddWithValue("s",   sub);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new NpgsqlCommand(
                                "UPDATE productos SET stock=stock-@c WHERE id=@pid", conn, tr))
                            {
                                cmd.Parameters.AddWithValue("c",   cant);
                                cmd.Parameters.AddWithValue("pid", prodId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tr.Commit();
                    }
                }

                MessageBox.Show(
                    $"✅  Venta guardada exitosamente\n\n" +
                    $"N° {numero}\n" +
                    $"Comprobante: {(tipoComp == "NINGUNO" ? "Sin comprobante" : tipoComp)}\n" +
                    $"Total: S/ {totalVal:N2}",
                    "Venta Registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimpiarVenta();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar venta:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  LIMPIAR
        // ══════════════════════════════════════════════════════════════════
        private void LimpiarVenta()
        {
            gridDetalle.Rows.Clear();
            detalles.Clear();
            txtObservacion.Clear();
            txtCantidad.Text = "1";
            cboTipoComprobante.SelectedIndex = 0;
            ActualizarTotales();
        }
    }
}