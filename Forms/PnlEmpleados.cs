using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;

namespace SistemaVentas.Forms
{
    public class PnlEmpleados : UserControl
    {
        // ── Paleta ─────────────────────────────────────────────────────────
        private readonly Color cFondo    = Color.FromArgb(245, 240, 228);
        private readonly Color cHeader   = Color.FromArgb(120, 95,  55);
        private readonly Color cOro      = Color.FromArgb(160, 120, 40);
        private readonly Color cBoton    = Color.FromArgb(100, 80,  45);
        private readonly Color cRojo     = Color.FromArgb(180, 50,  50);
        private readonly Color cVerde    = Color.FromArgb(50,  140, 80);
        private readonly Color cTexto    = Color.FromArgb(50,  40,  20);

        private DataGridView dgv;
        private TextBox txtBuscar;
        private Button btnNuevo, btnEditar, btnEliminar, btnRefrescar;
        private Label lblContador;

        public PnlEmpleados()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            CrearTablaEmpleados();
            Inicializar();
            CargarEmpleados();
        }

        // ── Crear tabla en BD si no existe ────────────────────────────────
        private void CrearTablaEmpleados()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                    CREATE TABLE IF NOT EXISTS empleados (
                        id           SERIAL PRIMARY KEY,
                        empresa_id   INT REFERENCES empresas(id),
                        dni          VARCHAR(20) UNIQUE NOT NULL,
                        nombres      VARCHAR(100) NOT NULL,
                        apellidos    VARCHAR(100) NOT NULL,
                        cargo        VARCHAR(80),
                        area         VARCHAR(80),
                        telefono     VARCHAR(20),
                        correo       VARCHAR(100),
                        fecha_ingreso DATE DEFAULT CURRENT_DATE,
                        sueldo       DECIMAL(10,2) DEFAULT 0,
                        activo       BOOLEAN DEFAULT TRUE
                    );";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void Inicializar()
        {
            // ── Cabecera ──────────────────────────────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Color.White };
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 185, 155), 1))
                    e.Graphics.DrawLine(pen, 0, 109, pnlTop.Width, 109);
            };

            var lblTitulo = new Label
            {
                Text = "👔  GESTIÓN DE EMPLEADOS",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = cBoton, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(500, 36),
                Location = new Point(20, 14), TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = "Administre el personal de la empresa",
                Font = new Font("Arial", 9), ForeColor = Color.FromArgb(130, 110, 80),
                BackColor = Color.Transparent, AutoSize = false,
                Size = new Size(400, 20), Location = new Point(22, 50)
            };

            // Buscador
            var lblBuscar = new Label { Text = "🔍", Font = new Font("Segoe UI Emoji", 11), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = true, Location = new Point(22, 76) };
            txtBuscar = new TextBox
            {
                Location = new Point(48, 74), Size = new Size(280, 28),
                Font = new Font("Arial", 10), BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 247, 240), ForeColor = cTexto
            };
            txtBuscar.TextChanged += (s, e) => CargarEmpleados(txtBuscar.Text);

            // Botones
            btnNuevo = CrearBoton("➕  Nuevo", cVerde, new Point(560, 68));
            btnEditar = CrearBoton("✏️  Editar", cBoton, new Point(690, 68));
            btnEliminar = CrearBoton("🗑  Eliminar", cRojo, new Point(820, 68));
            btnRefrescar = CrearBoton("🔄  Actualizar", Color.FromArgb(80, 80, 100), new Point(960, 68));

            btnNuevo.Click    += (s, e) => AbrirFormEmpleado(null);
            btnEditar.Click   += (s, e) => EditarSeleccionado();
            btnEliminar.Click += (s, e) => EliminarSeleccionado();
            btnRefrescar.Click += (s, e) => CargarEmpleados();

            lblContador = new Label
            {
                Text = "0 empleados", Font = new Font("Arial", 8),
                ForeColor = Color.FromArgb(130, 110, 80), BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(340, 80)
            };

            pnlTop.Controls.AddRange(new Control[] { lblTitulo, lblSub, lblBuscar, txtBuscar, lblContador, btnNuevo, btnEditar, btnEliminar, btnRefrescar });

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = cFondo,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Arial", 10),
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                RowTemplate = { Height = 44 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = cHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 200, 160);
            dgv.DefaultCellStyle.SelectionForeColor = cTexto;
            dgv.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 246, 238);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id",       HeaderText = "ID",            Width = 55  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "dni",      HeaderText = "DNI",           Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "nombres",  HeaderText = "NOMBRES",       Width = 160 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "apellidos",HeaderText = "APELLIDOS",     Width = 160 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cargo",    HeaderText = "CARGO",         Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "area",     HeaderText = "ÁREA",          Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "telefono", HeaderText = "TELÉFONO",      Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "correo",   HeaderText = "CORREO",        Width = 180 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ingreso",  HeaderText = "F. INGRESO",    Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "sueldo",   HeaderText = "SUELDO (S/)",   Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "estado",   HeaderText = "ESTADO",        Width = 90  });

            dgv.RowPostPaint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(235, 225, 205), 1))
                    e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1, e.RowBounds.Right, e.RowBounds.Bottom - 1);
            };
            dgv.RowPrePaint += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditarSeleccionado(); };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlTop);
        }

        private Button CrearBoton(string texto, Color color, Point loc)
        {
            var btn = new Button
            {
                Text = texto, Size = new Size(118, 32), Location = loc,
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void CargarEmpleados(string filtro = "")
        {
            try
            {
                dgv.Rows.Clear();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT id, dni, nombres, apellidos, cargo, area,
                                          telefono, correo, fecha_ingreso, sueldo, activo
                                   FROM empleados
                                   WHERE activo = true
                                     AND (nombres ILIKE @f OR apellidos ILIKE @f OR dni ILIKE @f OR cargo ILIKE @f)
                                   ORDER BY apellidos, nombres";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("f", $"%{filtro}%");
                        using (var dr = cmd.ExecuteReader())
                        {
                            int count = 0;
                            while (dr.Read())
                            {
                                count++;
                                dgv.Rows.Add(
                                    dr.GetInt32(0),
                                    dr.GetString(1),
                                    dr.GetString(2),
                                    dr.GetString(3),
                                    dr.IsDBNull(4) ? "" : dr.GetString(4),
                                    dr.IsDBNull(5) ? "" : dr.GetString(5),
                                    dr.IsDBNull(6) ? "" : dr.GetString(6),
                                    dr.IsDBNull(7) ? "" : dr.GetString(7),
                                    dr.IsDBNull(8) ? "" : dr.GetDateTime(8).ToString("dd/MM/yyyy"),
                                    $"S/ {(dr.IsDBNull(9) ? 0 : dr.GetDecimal(9)):N2}",
                                    dr.GetBoolean(10) ? "✔ Activo" : "✖ Inactivo"
                                );
                            }
                            lblContador.Text = $"{count} empleado{(count != 1 ? "s" : "")}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar empleados:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirFormEmpleado(int? id)
        {
            using (var frm = new FrmEmpleado(id))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    CargarEmpleados();
            }
        }

        private void EditarSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un empleado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["id"].Value);
            AbrirFormEmpleado(id);
        }

        private void EliminarSeleccionado()
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Seleccione un empleado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["id"].Value);
            string nombre = $"{dgv.SelectedRows[0].Cells["nombres"].Value} {dgv.SelectedRows[0].Cells["apellidos"].Value}";

            if (MessageBox.Show($"¿Eliminar a {nombre}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("UPDATE empleados SET activo=false WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    CargarEmpleados();
                }
                catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    // =========================================================================
    //  FORMULARIO — Crear / Editar Empleado
    // =========================================================================
    public class FrmEmpleado : Form
    {
        private readonly Color cFondo = Color.FromArgb(250, 247, 240);
        private readonly Color cOro   = Color.FromArgb(160, 120, 40);
        private readonly Color cBoton = Color.FromArgb(100, 80,  45);
        private readonly Color cTexto = Color.FromArgb(50,  40,  20);
        private readonly Color cInput = Color.FromArgb(255, 252, 245);

        private TextBox txtDni, txtNombres, txtApellidos, txtCargo, txtArea, txtTelefono, txtCorreo, txtSueldo;
        private DateTimePicker dtpIngreso;
        private Button btnGuardar, btnCancelar;

        private readonly int? _id;

        public FrmEmpleado(int? id)
        {
            _id = id;
            this.Text            = id == null ? "Nuevo Empleado" : "Editar Empleado";
            this.Size            = new Size(520, 560);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = cFondo;
            this.DialogResult    = DialogResult.Cancel;

            Inicializar();
            if (id != null) CargarDatos(id.Value);
        }

        private void Inicializar()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(100, 80, 45) };
            var lblTit = new Label { Text = _id == null ? "➕  Nuevo Empleado" : "✏️  Editar Empleado", Font = new Font("Arial", 13, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = false, Size = new Size(460, 56), Location = new Point(20, 0), TextAlign = ContentAlignment.MiddleLeft };
            pnlHeader.Controls.Add(lblTit);

            int y = 76;
            int col2 = 270;

            AddLabel("DNI / Documento *", 20, y); AddLabel("Nombres *", col2, y);
            txtDni      = AddInput(20,    y + 22, 220);
            txtNombres  = AddInput(col2,  y + 22, 210);
            y += 68;

            AddLabel("Apellidos *", 20, y); AddLabel("Cargo", col2, y);
            txtApellidos = AddInput(20,   y + 22, 220);
            txtCargo     = AddInput(col2, y + 22, 210);
            y += 68;

            AddLabel("Área", 20, y); AddLabel("Teléfono", col2, y);
            txtArea     = AddInput(20,    y + 22, 220);
            txtTelefono = AddInput(col2,  y + 22, 210);
            y += 68;

            AddLabel("Correo electrónico", 20, y);
            txtCorreo = AddInput(20, y + 22, 460);
            y += 68;

            AddLabel("Fecha de ingreso", 20, y); AddLabel("Sueldo (S/)", col2, y);
            dtpIngreso = new DateTimePicker { Location = new Point(20, y + 22), Size = new Size(220, 28), Font = new Font("Arial", 10), Format = DateTimePickerFormat.Short };
            txtSueldo = AddInput(col2, y + 22, 210);
            txtSueldo.Text = "0.00";
            this.Controls.Add(dtpIngreso);
            y += 68;

            btnGuardar = new Button
            {
                Text = "💾  GUARDAR", Size = new Size(220, 42), Location = new Point(20, y + 10),
                BackColor = cBoton, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar", Size = new Size(220, 42), Location = new Point(260, y + 10),
                BackColor = Color.FromArgb(200, 190, 170), ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 11), Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { pnlHeader, btnGuardar, btnCancelar });
            this.Height = y + 100;
        }

        private void AddLabel(string texto, int x, int y)
        {
            this.Controls.Add(new Label { Text = texto, Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = cOro, BackColor = Color.Transparent, AutoSize = false, Size = new Size(220, 16), Location = new Point(x, y), TextAlign = ContentAlignment.MiddleLeft });
        }

        private TextBox AddInput(int x, int y, int w)
        {
            var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 28), Font = new Font("Arial", 10), BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(tb);
            return tb;
        }

        private void CargarDatos(int id)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT dni, nombres, apellidos, cargo, area, telefono, correo, fecha_ingreso, sueldo FROM empleados WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", id);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtDni.Text       = dr.GetString(0);
                                txtNombres.Text   = dr.GetString(1);
                                txtApellidos.Text = dr.GetString(2);
                                txtCargo.Text     = dr.IsDBNull(3) ? "" : dr.GetString(3);
                                txtArea.Text      = dr.IsDBNull(4) ? "" : dr.GetString(4);
                                txtTelefono.Text  = dr.IsDBNull(5) ? "" : dr.GetString(5);
                                txtCorreo.Text    = dr.IsDBNull(6) ? "" : dr.GetString(6);
                                if (!dr.IsDBNull(7)) dtpIngreso.Value = dr.GetDateTime(7);
                                txtSueldo.Text    = dr.IsDBNull(8) ? "0.00" : dr.GetDecimal(8).ToString("N2");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDni.Text))      { MessageBox.Show("Ingrese el DNI.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtNombres.Text))  { MessageBox.Show("Ingrese los nombres.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtApellidos.Text)){ MessageBox.Show("Ingrese los apellidos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtSueldo.Text, out decimal sueldo)) sueldo = 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = _id == null
                        ? @"INSERT INTO empleados(empresa_id, dni, nombres, apellidos, cargo, area, telefono, correo, fecha_ingreso, sueldo)
                            VALUES(@eid, @dni, @nom, @ape, @car, @are, @tel, @cor, @fin, @sue)"
                        : @"UPDATE empleados SET dni=@dni, nombres=@nom, apellidos=@ape, cargo=@car,
                            area=@are, telefono=@tel, correo=@cor, fecha_ingreso=@fin, sueldo=@sue
                            WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (_id == null) cmd.Parameters.AddWithValue("eid", Models.Sesion.UsuarioActivo?.EmpresaId ?? 1);
                        else             cmd.Parameters.AddWithValue("id",  _id.Value);
                        cmd.Parameters.AddWithValue("dni", txtDni.Text.Trim());
                        cmd.Parameters.AddWithValue("nom", txtNombres.Text.Trim());
                        cmd.Parameters.AddWithValue("ape", txtApellidos.Text.Trim());
                        cmd.Parameters.AddWithValue("car", txtCargo.Text.Trim());
                        cmd.Parameters.AddWithValue("are", txtArea.Text.Trim());
                        cmd.Parameters.AddWithValue("tel", txtTelefono.Text.Trim());
                        cmd.Parameters.AddWithValue("cor", txtCorreo.Text.Trim());
                        cmd.Parameters.AddWithValue("fin", dtpIngreso.Value.Date);
                        cmd.Parameters.AddWithValue("sue", sueldo);
                        cmd.ExecuteNonQuery();
                    }
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error al guardar:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}