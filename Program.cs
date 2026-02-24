using System;
using System.Windows.Forms;
using SistemaVentas.Database;
using SistemaVentas.Forms;

namespace SistemaVentas
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Inicializar base de datos
            try
            {
                DatabaseHelper.InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo conectar a PostgreSQL.\n\n" +
                    "Por favor verifique:\n" +
                    "1. PostgreSQL está instalado y corriendo\n" +
                    "2. Las credenciales en DatabaseHelper.cs son correctas\n\n" +
                    "Error: " + ex.Message,
                    "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FrmLogin());
        }
    }
}
