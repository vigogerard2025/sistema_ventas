# 🏪 SISTEMA DE VENTAS - Guía de Instalación Completa

## ✅ REQUISITOS PREVIOS

1. **Visual Studio 2022 Community** (GRATIS)
   - Descargar: https://visualstudio.microsoft.com/vs/community/
   - Durante la instalación marca: **"Desarrollo de escritorio con .NET"**

2. **PostgreSQL** (GRATIS)
   - Descargar: https://www.postgresql.org/download/windows/
   - Durante la instalación **anota tu contraseña** de postgres

3. **.NET 8 SDK** (se instala automático con Visual Studio)

---

## 🚀 PASOS DE INSTALACIÓN

### PASO 1 - Configurar la contraseña de PostgreSQL

Abre el archivo:
```
Database/DatabaseHelper.cs
```

Cambia esta línea con TU contraseña:
```csharp
private static string connectionString = 
    "Host=localhost;Port=5432;Database=SistemaVentas;Username=postgres;Password=TU_PASSWORD;";
```

Por ejemplo si tu contraseña es `123456`:
```csharp
Password=123456
```

### PASO 2 - Abrir el proyecto en Visual Studio

1. Abre **Visual Studio 2022**
2. Click en **"Abrir un proyecto o solución"**
3. Navega a la carpeta del sistema
4. Selecciona `SistemaVentas.csproj`

### PASO 3 - Restaurar dependencias

En Visual Studio, ve a:
**Herramientas → Administrador de paquetes NuGet → Consola del administrador**

Escribe:
```
dotnet restore
```

### PASO 4 - Ejecutar el sistema

Presiona **F5** o click en ▶ **"Ejecutar"**

El sistema creará automáticamente todas las tablas en PostgreSQL la primera vez.

---

## 🔐 USUARIO POR DEFECTO

| Campo    | Valor         |
|----------|---------------|
| Usuario  | `admin`       |
| Contraseña | `admin`     |
| Empresa  | Mi Empresa    |
| Sucursal | Sede Principal|

---

## 📁 ESTRUCTURA DEL PROYECTO

```
SistemaVentas/
├── 📄 Program.cs              → Punto de entrada
├── 📄 SistemaVentas.csproj    → Configuración del proyecto
│
├── 📁 Database/
│   └── DatabaseHelper.cs      → Conexión PostgreSQL + creación tablas
│
├── 📁 Models/
│   └── Models.cs              → Clases (Empresa, Producto, Venta...)
│
└── 📁 Forms/
    ├── FrmLogin.cs            → 🔑 Pantalla de login (igual a tu imagen)
    ├── FrmMenu.cs             → 🏠 Menú principal con sidebar
    ├── PnlInicio.cs           → 📊 Dashboard con tarjetas resumen
    ├── PnlVentas.cs           → 🛒 Nueva venta (POS)
    ├── PnlHistorialVentas.cs  → 📋 Historial con filtros por fecha
    ├── PnlProductos.cs        → 📦 CRUD de productos + inventario
    ├── PnlClientes.cs         → 👥 CRUD de clientes
    ├── PnlReportes.cs         → 💰 Reportes financieros
    └── PnlConfiguracion.cs    → ⚙️  Empresa, usuarios, categorías
```

---

## 💡 FUNCIONALIDADES DEL SISTEMA

### 🔑 Login (FrmLogin)
- Selección de empresa y sucursal (igual que tu imagen)
- Autenticación con usuario y contraseña
- Contraseñas cifradas con SHA-256

### 🏠 Dashboard (PnlInicio)
- Ventas del día en tiempo real
- Total de productos, clientes
- Alerta de stock bajo
- Últimas 20 ventas

### 🛒 Nueva Venta (PnlVentas)
- Selección de cliente
- Búsqueda y selección de productos
- Ajuste de cantidades en tiempo real
- Cálculo automático de IGV (18%)
- Tipos de pago: Efectivo, Tarjeta, Yape, Plin...
- Descuento de stock automático al guardar

### 📋 Historial de Ventas
- Filtros por rango de fechas
- Búsqueda por número de venta o cliente
- Ver detalle de cada venta

### 📦 Productos
- CRUD completo (crear, editar, eliminar)
- Categorías
- Control de stock mínimo
- Alerta visual de stock bajo (fila roja)

### 👥 Clientes
- CRUD completo
- Búsqueda por nombre o DNI/RUC

### 📊 Reportes
- Ventas por período (por día)
- Top 20 productos más vendidos
- Resumen financiero: Ventas hoy / mes / año

### ⚙️ Configuración
- Datos de la empresa
- Gestión de usuarios
- Categorías de productos
- Configuración de conexión a BD

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

**Error: "No se pudo conectar a PostgreSQL"**
→ Verifica que PostgreSQL esté corriendo
→ Ve a Servicios de Windows (Win+R → services.msc) y busca "postgresql"

**Error: "relation does not exist"**  
→ El sistema crea las tablas automáticamente al iniciar
→ Verifica que la base de datos `SistemaVentas` exista en PostgreSQL

**Error al compilar: "Npgsql no encontrado"**  
→ Click derecho en el proyecto → "Restaurar paquetes NuGet"

---

## 📞 CREDENCIALES PARA PRUEBAS

Después de instalar puedes crear más usuarios desde:
**Configuración → Usuarios → Nuevo Usuario**

Los roles disponibles son:
- **ADMINISTRADOR** → Acceso total
- **VENDEDOR** → Solo ventas y clientes  
- **ALMACEN** → Solo productos e inventario
