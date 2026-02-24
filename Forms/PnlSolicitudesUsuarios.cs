using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Npgsql;
using SistemaVentas.Database;
using SistemaVentas.Models;

namespace SistemaVentas.Forms
{
    // =========================================================================
    //  PANEL ADMINISTRADOR — Gestión de solicitudes de nuevos usuarios
    // =========================================================================
    public class PnlSolicitudesUsuarios : UserControl
    {
        // ── Paleta ─────────────────────────────────────────────────────────
        private readonly Color cFondo    = Color.FromArgb(18,  18,  24);
        private readonly Color cTarjeta  = Color.FromArgb(28,  28,  40);
        private readonly Color cOro      = Color.FromArgb(212, 175,  95);
        private readonly Color cOroClaro = Color.FromArgb(240, 210, 130);
        private readonly Color cTexto    = Color.FromArgb(225, 220, 210);
        private readonly Color cGris     = Color.FromArgb(130, 125, 115);
        private readonly Color cVerde    = Color.FromArgb(72,  180, 120);
        private readonly Color cRojo     = Color.FromArgb(200,  70,  70);
        private readonly Color cAmarillo = Color.FromArgb(220, 180,  60);
        private readonly Color cInput    = Color.FromArgb(40,  40,  56);

        private DataGridView dgv;
        private Label lblContador;
        private Button btnRefrescar;

        public PnlSolicitudesUsuarios()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = cFondo;
            Inicializar();
            CargarSolicitudes();
        }

        private void Inicializar()
        {
            // ── Cabecera ──────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 80, BackColor = cTarjeta
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(cOro, 2))
                    e.Graphics.DrawLine(pen, 0, 78, pnlHeader.Width, 78);
            };

            var lblTitulo = new Label
            {
                Text = "👥  Solicitudes de Nuevos Usuarios",
                Font = new Font("Georgia", 16, FontStyle.Bold),
                ForeColor = cOro, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(500, 40),
                Location = new Point(30, 10), TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = "Apruebe o rechace solicitudes de acceso al sistema",
                Font = new Font("Arial", 9), ForeColor = cGris,
                BackColor = Color.Transparent, AutoSize = false,
                Size = new Size(500, 20), Location = new Point(30, 48),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblContador = new Label
            {
                Text = "0 pendientes",
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = cAmarillo, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(600, 28)
            };

            btnRefrescar = new Button
            {
                Text = "🔄  Actualizar",
                Size = new Size(130, 32), Location = new Point(700, 22),
                BackColor = cInput, ForeColor = cTexto,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9), Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(70, 65, 50);
            btnRefrescar.FlatAppearance.BorderSize  = 1;
            btnRefrescar.Click += (s, e) => CargarSolicitudes();

            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSub, lblContador, btnRefrescar });

            // ── Tabla de solicitudes ──────────────────────────────────────
            dgv = new DataGridView
            {
                Dock                       = DockStyle.Fill,
                BackgroundColor            = cFondo,
                BorderStyle                = BorderStyle.None,
                RowHeadersVisible          = false,
                AllowUserToAddRows         = false,
                AllowUserToDeleteRows      = false,
                ReadOnly                   = true,
                SelectionMode              = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                = false,
                AutoSizeRowsMode           = DataGridViewAutoSizeRowsMode.None,
                Font                       = new Font("Arial", 10),
                CellBorderStyle            = DataGridViewCellBorderStyle.None,
                GridColor                  = Color.FromArgb(40, 40, 55),
                RowTemplate                = { Height = 48 },
                ScrollBars                 = ScrollBars.Both,
                AutoSizeColumnsMode        = DataGridViewAutoSizeColumnsMode.None
            };

            // Estilo de cabecera
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(35, 35, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = cOro;
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Arial", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding    = new Padding(12, 0, 0, 0);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 35, 50);
            dgv.ColumnHeadersHeight                      = 44;
            dgv.ColumnHeadersBorderStyle                 = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles                = false;

            // Estilo de filas
            dgv.DefaultCellStyle.BackColor          = Color.FromArgb(26, 26, 36);
            dgv.DefaultCellStyle.ForeColor          = cTexto;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 212, 175, 95);
            dgv.DefaultCellStyle.SelectionForeColor = cTexto;
            dgv.DefaultCellStyle.Padding            = new Padding(12, 0, 12, 0);
            dgv.DefaultCellStyle.Font               = new Font("Arial", 10);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 212, 175, 95);

            // Columnas con ancho fijo y legible
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id",      HeaderText = "ID",      Width = 55,  ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "empresa",  HeaderText = "EMPRESA", Width = 220, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "nombre",   HeaderText = "NOMBRE",  Width = 180, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "usuario",  HeaderText = "USUARIO", Width = 130, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "correo",   HeaderText = "CORREO",  Width = 200, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "fecha",    HeaderText = "FECHA",   Width = 150, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "estado",   HeaderText = "ESTADO",  Width = 110, ReadOnly = true });

            // Columna botón APROBAR
            var colAprobar = new DataGridViewButtonColumn
            {
                Name       = "aprobar",
                HeaderText = "ACCIÓN",
                Text       = "✔  APROBAR",
                UseColumnTextForButtonValue = true,
                Width      = 120,
                FlatStyle  = FlatStyle.Flat
            };
            dgv.Columns.Add(colAprobar);

            // Columna botón RECHAZAR
            var colRechazar = new DataGridViewButtonColumn
            {
                Name       = "rechazar",
                HeaderText = "",
                Text       = "✖  RECHAZAR",
                UseColumnTextForButtonValue = true,
                Width      = 120,
                FlatStyle  = FlatStyle.Flat
            };
            dgv.Columns.Add(colRechazar);

            dgv.CellPainting  += Dgv_CellPainting;
            dgv.CellClick     += Dgv_CellClick;
            dgv.RowPrePaint   += Dgv_RowPrePaint;
            dgv.RowPostPaint  += Dgv_RowPostPaint;

            this.Controls.Add(dgv);
            this.Controls.Add(pnlHeader);
        }

        // ── Cargar solicitudes pendientes ──────────────────────────────────
        private void CargarSolicitudes()
        {
            try
            {
                // Crear tabla si no existe
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string crearTabla = @"
                    CREATE TABLE IF NOT EXISTS solicitudes_usuario (
                        id            SERIAL PRIMARY KEY,
                        empresa_id    INT REFERENCES empresas(id),
                        nombre        VARCHAR(100) NOT NULL,
                        usuario       VARCHAR(50)  NOT NULL,
                        correo        VARCHAR(100) NOT NULL,
                        password_hash VARCHAR(256) NOT NULL,
                        estado        VARCHAR(20)  DEFAULT 'PENDIENTE',
                        fecha         TIMESTAMP    DEFAULT NOW()
                    );";
                    using (var cmd = new NpgsqlCommand(crearTabla, conn))
                        cmd.ExecuteNonQuery();

                    dgv.Rows.Clear();

                    string sql = @"SELECT s.id, e.nombre as empresa, s.nombre, s.usuario,
                                          s.correo, s.fecha, s.estado
                                   FROM solicitudes_usuario s
                                   JOIN empresas e ON s.empresa_id = e.id
                                   ORDER BY
                                     CASE s.estado WHEN 'PENDIENTE' THEN 0 ELSE 1 END,
                                     s.fecha DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        int pendientes = 0;
                        while (dr.Read())
                        {
                            string estado = dr.GetString(6);
                            if (estado == "PENDIENTE") pendientes++;
                            dgv.Rows.Add(
                                dr.GetInt32(0),
                                dr.GetString(1),
                                dr.GetString(2),
                                dr.GetString(3),
                                dr.GetString(4),
                                dr.GetDateTime(5).ToString("dd/MM/yyyy HH:mm"),
                                estado
                            );
                        }
                        lblContador.Text = $"{pendientes} pendiente{(pendientes != 1 ? "s" : "")}";
                        lblContador.ForeColor = pendientes > 0 ? cAmarillo : cVerde;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar solicitudes:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Click en botones Aprobar / Rechazar ────────────────────────────
        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row    = dgv.Rows[e.RowIndex];
            int id     = Convert.ToInt32(row.Cells["id"].Value);
            string est = row.Cells["estado"].Value?.ToString();

            if (est != "PENDIENTE")
            {
                MessageBox.Show("Esta solicitud ya fue procesada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (e.ColumnIndex == dgv.Columns["aprobar"].Index)
                AprobarSolicitud(id, row);
            else if (e.ColumnIndex == dgv.Columns["rechazar"].Index)
                RechazarSolicitud(id);
        }

        private void AprobarSolicitud(int solicitudId, DataGridViewRow row)
        {
            var confirm = MessageBox.Show(
                $"¿Aprobar al usuario \"{row.Cells["usuario"].Value}\"?\n\nSe creará su cuenta y podrá ingresar al sistema.",
                "Confirmar aprobación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Obtener datos de la solicitud
                    string getSql = @"SELECT empresa_id, nombre, usuario, password_hash
                                      FROM solicitudes_usuario WHERE id = @id";
                    int    empresaId; string nombre, usuario, pwdHash;
                    using (var cmd = new NpgsqlCommand(getSql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", solicitudId);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read()) return;
                            empresaId = dr.GetInt32(0);
                            nombre    = dr.GetString(1);
                            usuario   = dr.GetString(2);
                            pwdHash   = dr.GetString(3);
                        }
                    }

                    // Obtener primera sucursal de la empresa
                    int sucursalId = 1;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id FROM sucursales WHERE empresa_id=@eid AND activo=true LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("eid", empresaId);
                        var res = cmd.ExecuteScalar();
                        if (res != null) sucursalId = Convert.ToInt32(res);
                    }

                    // Obtener rol VENDEDOR
                    int rolId = 2;
                    using (var cmd = new NpgsqlCommand("SELECT id FROM roles WHERE nombre='VENDEDOR' LIMIT 1", conn))
                    {
                        var res = cmd.ExecuteScalar();
                        if (res != null) rolId = Convert.ToInt32(res);
                    }

                    // Crear usuario
                    string insertSql = @"INSERT INTO usuarios(empresa_id, sucursal_id, nombre, usuario, password_hash, rol_id)
                                         VALUES(@eid, @sid, @nom, @usr, @pwd, @rid)
                                         ON CONFLICT (usuario) DO NOTHING";
                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("eid", empresaId);
                        cmd.Parameters.AddWithValue("sid", sucursalId);
                        cmd.Parameters.AddWithValue("nom", nombre);
                        cmd.Parameters.AddWithValue("usr", usuario);
                        cmd.Parameters.AddWithValue("pwd", pwdHash);
                        cmd.Parameters.AddWithValue("rid", rolId);
                        cmd.ExecuteNonQuery();
                    }

                    // Marcar solicitud como aprobada
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE solicitudes_usuario SET estado='APROBADA' WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", solicitudId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Usuario aprobado y creado correctamente.\nYa puede iniciar sesión.",
                                "Aprobado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarSolicitudes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al aprobar:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RechazarSolicitud(int solicitudId)
        {
            var confirm = MessageBox.Show(
                "¿Rechazar esta solicitud?\nEl usuario no podrá acceder al sistema.",
                "Confirmar rechazo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE solicitudes_usuario SET estado='RECHAZADA' WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", solicitudId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Solicitud rechazada.", "Rechazado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarSolicitudes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Pintar botones de acción con colores ───────────────────────────
        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Colorear columna ESTADO
            if (e.ColumnIndex == dgv.Columns["estado"].Index && e.Value != null)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string est = e.Value.ToString();
                Color color = est == "PENDIENTE" ? cAmarillo : est == "APROBADA" ? cVerde : cRojo;
                using (var br = new SolidBrush(Color.FromArgb(30, color)))
                    e.Graphics.FillRectangle(br, e.CellBounds);
                using (var br = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(est, new Font("Arial", 8, FontStyle.Bold), br, e.CellBounds, sf);
                e.Handled = true;
                return;
            }

            // Botón APROBAR
            if (e.ColumnIndex == dgv.Columns["aprobar"].Index)
            {
                string estado = dgv.Rows[e.RowIndex].Cells["estado"].Value?.ToString();
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                var rect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4,
                                         e.CellBounds.Width - 8, e.CellBounds.Height - 8);
                Color bg = estado == "PENDIENTE" ? cVerde : Color.FromArgb(50, 50, 65);
                using (var br = new SolidBrush(bg))
                    e.Graphics.FillRectangle(br, rect);
                using (var br = new SolidBrush(estado == "PENDIENTE" ? Color.White : cGris))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString("✔  APROBAR", new Font("Arial", 8, FontStyle.Bold), br, rect, sf);
                e.Handled = true;
                return;
            }

            // Botón RECHAZAR
            if (e.ColumnIndex == dgv.Columns["rechazar"].Index)
            {
                string estado = dgv.Rows[e.RowIndex].Cells["estado"].Value?.ToString();
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                var rect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4,
                                         e.CellBounds.Width - 8, e.CellBounds.Height - 8);
                Color bg = estado == "PENDIENTE" ? cRojo : Color.FromArgb(50, 50, 65);
                using (var br = new SolidBrush(bg))
                    e.Graphics.FillRectangle(br, rect);
                using (var br = new SolidBrush(estado == "PENDIENTE" ? Color.White : cGris))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString("✖  RECHAZAR", new Font("Arial", 8, FontStyle.Bold), br, rect, sf);
                e.Handled = true;
            }
        }

        private void Dgv_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        // ── Pintar línea separadora sutil entre filas ──────────────────────
        private void Dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(38, 38, 52), 1))
                e.Graphics.DrawLine(pen,
                    e.RowBounds.Left,
                    e.RowBounds.Bottom - 1,
                    e.RowBounds.Right,
                    e.RowBounds.Bottom - 1);
        }
    }
}