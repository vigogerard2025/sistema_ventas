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
                // PASO 1: Crear tablas base
                // ============================================================
                string sqlTablas = @"
                CREATE TABLE IF NOT EXISTS empresas (
                    id        SERIAL PRIMARY KEY,
                    nombre    VARCHAR(100) NOT NULL,
                    ruc       VARCHAR(20),
                    direccion VARCHAR(200),
                    telefono  VARCHAR(20),
                    activo    BOOLEAN DEFAULT TRUE
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

                CREATE TABLE IF NOT EXISTS compras (
                    id            SERIAL PRIMARY KEY,
                    numero_compra VARCHAR(20) UNIQUE NOT NULL,
                    empresa_id    INT REFERENCES empresas(id),
                    sucursal_id   INT REFERENCES sucursales(id),
                    proveedor     VARCHAR(200),
                    usuario_id    INT REFERENCES usuarios(id),
                    fecha         TIMESTAMP DEFAULT NOW(),
                    subtotal      DECIMAL(12,2) DEFAULT 0,
                    igv           DECIMAL(12,2) DEFAULT 0,
                    total         DECIMAL(12,2) DEFAULT 0,
                    tipo_pago     VARCHAR(20) DEFAULT 'EFECTIVO',
                    estado        VARCHAR(20) DEFAULT 'COMPLETADA',
                    observacion   VARCHAR(300)
                );

                CREATE TABLE IF NOT EXISTS detalle_compras (
                    id              SERIAL PRIMARY KEY,
                    compra_id       INT REFERENCES compras(id),
                    producto_id     INT REFERENCES productos(id),
                    cantidad        INT NOT NULL,
                    precio_unitario DECIMAL(12,2) NOT NULL,
                    subtotal        DECIMAL(12,2) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS comprobantes (
                    id                SERIAL PRIMARY KEY,
                    empresa_id        INT REFERENCES empresas(id),
                    sucursal_id       INT REFERENCES sucursales(id),
                    venta_id          INT REFERENCES ventas(id),
                    tipo              VARCHAR(10) NOT NULL,
                    serie             VARCHAR(10) NOT NULL,
                    numero            VARCHAR(20) NOT NULL,
                    fecha_emision     TIMESTAMP DEFAULT NOW(),
                    cliente_doc       VARCHAR(20),
                    cliente_nombre    VARCHAR(200),
                    cliente_dir       VARCHAR(200),
                    subtotal          DECIMAL(12,2) DEFAULT 0,
                    igv               DECIMAL(12,2) DEFAULT 0,
                    total             DECIMAL(12,2) DEFAULT 0,
                    estado            VARCHAR(20) DEFAULT 'EMITIDO',
                    usuario_id        INT REFERENCES usuarios(id),
                    sunat_estado      VARCHAR(30) DEFAULT 'PENDIENTE',
                    sunat_fecha_envio TIMESTAMP,
                    sunat_respuesta   VARCHAR(500),
                    xml_filename      VARCHAR(200),
                    UNIQUE(serie, numero)
                );

                CREATE TABLE IF NOT EXISTS empleados (
                    id            SERIAL PRIMARY KEY,
                    empresa_id    INT REFERENCES empresas(id),
                    dni           VARCHAR(20) UNIQUE NOT NULL,
                    nombres       VARCHAR(100) NOT NULL,
                    apellidos     VARCHAR(100) NOT NULL,
                    cargo         VARCHAR(80),
                    area          VARCHAR(80),
                    telefono      VARCHAR(20),
                    correo        VARCHAR(100),
                    fecha_ingreso DATE DEFAULT CURRENT_DATE,
                    sueldo        DECIMAL(10,2) DEFAULT 0,
                    activo        BOOLEAN DEFAULT TRUE
                );
                ";

                using (var cmd = new NpgsqlCommand(sqlTablas, conn))
                    cmd.ExecuteNonQuery();

                // ============================================================
                // PASO 2: Migraciones (añadir columnas nuevas sin perder datos)
                // ============================================================
                string sqlMigraciones = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='productos' AND column_name='presentacion') THEN
                        ALTER TABLE productos ADD COLUMN presentacion VARCHAR(200) DEFAULT '';
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='productos' AND column_name='marca') THEN
                        ALTER TABLE productos ADD COLUMN marca VARCHAR(100) DEFAULT 'SIN MARCA';
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='comprobantes' AND column_name='sunat_estado') THEN
                        ALTER TABLE comprobantes ADD COLUMN sunat_estado VARCHAR(30) DEFAULT 'PENDIENTE';
                        ALTER TABLE comprobantes ADD COLUMN sunat_fecha_envio TIMESTAMP;
                        ALTER TABLE comprobantes ADD COLUMN sunat_respuesta VARCHAR(500);
                        ALTER TABLE comprobantes ADD COLUMN xml_filename VARCHAR(200);
                    END IF;
                END
                $$;
                ";

                using (var cmd = new NpgsqlCommand(sqlMigraciones, conn))
                    cmd.ExecuteNonQuery();

                // ============================================================
                // PASO 3: Restricciones UNIQUE
                // ============================================================
                string sqlConstraints = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_empresas_ruc') THEN
                        DELETE FROM empresas WHERE id NOT IN (SELECT MIN(id) FROM empresas GROUP BY ruc);
                        ALTER TABLE empresas ADD CONSTRAINT uq_empresas_ruc UNIQUE (ruc);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_sucursal_empresa_nombre') THEN
                        DELETE FROM sucursales WHERE id NOT IN (SELECT MIN(id) FROM sucursales GROUP BY empresa_id, nombre);
                        ALTER TABLE sucursales ADD CONSTRAINT uq_sucursal_empresa_nombre UNIQUE (empresa_id, nombre);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_roles_nombre') THEN
                        DELETE FROM roles WHERE id NOT IN (SELECT MIN(id) FROM roles GROUP BY nombre);
                        ALTER TABLE roles ADD CONSTRAINT uq_roles_nombre UNIQUE (nombre);
                    END IF;
                END
                $$;
                ";

                using (var cmd = new NpgsqlCommand(sqlConstraints, conn))
                    cmd.ExecuteNonQuery();

                // ============================================================
                // PASO 4: Datos iniciales
                // ============================================================
                string sqlDatos = @"
                INSERT INTO roles(nombre)
                VALUES ('ADMINISTRADOR'), ('VENDEDOR'), ('ALMACEN')
                ON CONFLICT (nombre) DO NOTHING;

                INSERT INTO empresas(nombre, ruc)
                VALUES ('ALVERCA CARBAJAL PAULO MEZA', '20000000001')
                ON CONFLICT (ruc) DO NOTHING;

                INSERT INTO sucursales(empresa_id, nombre)
                SELECT id, 'PRINCIPAL' FROM empresas WHERE ruc = '20000000001'
                ON CONFLICT (empresa_id, nombre) DO NOTHING;

                INSERT INTO usuarios(empresa_id, sucursal_id, nombre, usuario, password_hash, rol_id)
                SELECT e.id, s.id, 'Administrador', 'admin',
                       '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', r.id
                FROM empresas e
                JOIN sucursales s ON s.empresa_id = e.id AND s.nombre = 'PRINCIPAL'
                JOIN roles r ON r.nombre = 'ADMINISTRADOR'
                WHERE e.ruc = '20000000001'
                ON CONFLICT (usuario) DO NOTHING;
                ";

                using (var cmd = new NpgsqlCommand(sqlDatos, conn))
                    cmd.ExecuteNonQuery();
            }
        }
    }
}