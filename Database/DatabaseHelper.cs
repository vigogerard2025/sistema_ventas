using System;
using Npgsql;

namespace SistemaVentas.Database
{
    public class DatabaseHelper
    {
        private static string connectionString =
            "Host=localhost;Port=5432;Database=SistemaVentas;Username=postgres;Password=12345;";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        public static void SetConnectionString(string host, string port, string database, string user, string password)
        {
            connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        public static void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // ============================================================
                // PASO 1: Crear tablas si no existen
                // ============================================================
                string sqlTablas = @"
                CREATE TABLE IF NOT EXISTS empresas (
                    id       SERIAL PRIMARY KEY,
                    nombre   VARCHAR(100) NOT NULL,
                    ruc      VARCHAR(20),
                    direccion VARCHAR(200),
                    telefono VARCHAR(20),
                    activo   BOOLEAN DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS sucursales (
                    id         SERIAL PRIMARY KEY,
                    empresa_id INT REFERENCES empresas(id),
                    nombre     VARCHAR(100) NOT NULL,
                    direccion  VARCHAR(200),
                    activo     BOOLEAN DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS roles (
                    id     SERIAL PRIMARY KEY,
                    nombre VARCHAR(50) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS usuarios (
                    id            SERIAL PRIMARY KEY,
                    empresa_id    INT REFERENCES empresas(id),
                    sucursal_id   INT REFERENCES sucursales(id),
                    nombre        VARCHAR(100) NOT NULL,
                    usuario       VARCHAR(50) UNIQUE NOT NULL,
                    password_hash VARCHAR(256) NOT NULL,
                    rol_id        INT REFERENCES roles(id),
                    activo        BOOLEAN DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS categorias (
                    id          SERIAL PRIMARY KEY,
                    nombre      VARCHAR(100) NOT NULL,
                    descripcion VARCHAR(200)
                );

                CREATE TABLE IF NOT EXISTS productos (
                    id            SERIAL PRIMARY KEY,
                    codigo        VARCHAR(50) UNIQUE NOT NULL,
                    nombre        VARCHAR(200) NOT NULL,
                    descripcion   VARCHAR(500),
                    categoria_id  INT REFERENCES categorias(id),
                    precio_compra DECIMAL(12,2) DEFAULT 0,
                    precio_venta  DECIMAL(12,2) DEFAULT 0,
                    stock         INT DEFAULT 0,
                    stock_minimo  INT DEFAULT 5,
                    activo        BOOLEAN DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS clientes (
                    id        SERIAL PRIMARY KEY,
                    documento VARCHAR(20),
                    nombre    VARCHAR(200) NOT NULL,
                    direccion VARCHAR(200),
                    telefono  VARCHAR(20),
                    email     VARCHAR(100),
                    activo    BOOLEAN DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS ventas (
                    id           SERIAL PRIMARY KEY,
                    numero_venta VARCHAR(20) UNIQUE NOT NULL,
                    empresa_id   INT REFERENCES empresas(id),
                    sucursal_id  INT REFERENCES sucursales(id),
                    cliente_id   INT REFERENCES clientes(id),
                    usuario_id   INT REFERENCES usuarios(id),
                    fecha        TIMESTAMP DEFAULT NOW(),
                    subtotal     DECIMAL(12,2) DEFAULT 0,
                    igv          DECIMAL(12,2) DEFAULT 0,
                    total        DECIMAL(12,2) DEFAULT 0,
                    tipo_pago    VARCHAR(20) DEFAULT 'EFECTIVO',
                    estado       VARCHAR(20) DEFAULT 'COMPLETADA',
                    observacion  VARCHAR(300)
                );

                CREATE TABLE IF NOT EXISTS detalle_ventas (
                    id              SERIAL PRIMARY KEY,
                    venta_id        INT REFERENCES ventas(id),
                    producto_id     INT REFERENCES productos(id),
                    cantidad        INT NOT NULL,
                    precio_unitario DECIMAL(12,2) NOT NULL,
                    descuento       DECIMAL(12,2) DEFAULT 0,
                    subtotal        DECIMAL(12,2) NOT NULL
                );
                ";

                using (var cmd = new NpgsqlCommand(sqlTablas, conn))
                    cmd.ExecuteNonQuery();

                // ============================================================
                // PASO 2: Agregar restricciones UNIQUE solo si no existen aun
                // ============================================================
                string sqlConstraints = @"
                DO $$
                BEGIN
                    -- UNIQUE en empresas.ruc
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'uq_empresas_ruc'
                    ) THEN
                        -- Eliminar duplicados antes de crear el indice
                        DELETE FROM empresas
                        WHERE id NOT IN (
                            SELECT MIN(id) FROM empresas GROUP BY ruc
                        );
                        ALTER TABLE empresas ADD CONSTRAINT uq_empresas_ruc UNIQUE (ruc);
                    END IF;

                    -- UNIQUE en sucursales (empresa_id, nombre)
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'uq_sucursal_empresa_nombre'
                    ) THEN
                        -- Eliminar duplicados antes de crear el indice
                        DELETE FROM sucursales
                        WHERE id NOT IN (
                            SELECT MIN(id) FROM sucursales GROUP BY empresa_id, nombre
                        );
                        ALTER TABLE sucursales ADD CONSTRAINT uq_sucursal_empresa_nombre UNIQUE (empresa_id, nombre);
                    END IF;

                    -- UNIQUE en roles.nombre
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'uq_roles_nombre'
                    ) THEN
                        -- Eliminar duplicados antes de crear el indice
                        DELETE FROM roles
                        WHERE id NOT IN (
                            SELECT MIN(id) FROM roles GROUP BY nombre
                        );
                        ALTER TABLE roles ADD CONSTRAINT uq_roles_nombre UNIQUE (nombre);
                    END IF;
                END
                $$;
                ";

                using (var cmd = new NpgsqlCommand(sqlConstraints, conn))
                    cmd.ExecuteNonQuery();

                // ============================================================
                // PASO 3: Insertar datos iniciales sin duplicar
                // ============================================================
                string sqlDatos = @"
                -- Roles
                INSERT INTO roles(nombre)
                VALUES ('ADMINISTRADOR'), ('VENDEDOR'), ('ALMACEN')
                ON CONFLICT (nombre) DO NOTHING;

                -- Empresa principal
                INSERT INTO empresas(nombre, ruc)
                VALUES ('ALVERCA CARBAJAL PAULO MEZA', '20000000001')
                ON CONFLICT (ruc) DO NOTHING;

                -- Sucursal principal (usa el id de la empresa recien insertada)
                INSERT INTO sucursales(empresa_id, nombre)
                SELECT id, 'PRINCIPAL'
                FROM empresas
                WHERE ruc = '20000000001'
                ON CONFLICT (empresa_id, nombre) DO NOTHING;

                -- Usuario administrador por defecto (password: admin)
                INSERT INTO usuarios(empresa_id, sucursal_id, nombre, usuario, password_hash, rol_id)
                SELECT
                    e.id,
                    s.id,
                    'Administrador',
                    'admin',
                    '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918',
                    r.id
                FROM empresas  e
                JOIN sucursales s ON s.empresa_id = e.id AND s.nombre = 'PRINCIPAL'
                JOIN roles      r ON r.nombre = 'ADMINISTRADOR'
                WHERE e.ruc = '20000000001'
                ON CONFLICT (usuario) DO NOTHING;
                ";

                using (var cmd = new NpgsqlCommand(sqlDatos, conn))
                    cmd.ExecuteNonQuery();
            }
        }
    }
}