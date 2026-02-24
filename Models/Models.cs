using System;
using System.Collections.Generic;

namespace SistemaVentas.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Ruc { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }
        public override string ToString() => Nombre;
    }

    public class Sucursal
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public bool Activo { get; set; }
        public override string ToString() => Nombre;
    }

    public class Usuario
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int SucursalId { get; set; }
        public string Nombre { get; set; }
        public string NombreUsuario { get; set; }
        public string PasswordHash { get; set; }
        public int RolId { get; set; }
        public string RolNombre { get; set; }
        public bool Activo { get; set; }
    }

    public class Producto
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
        public bool Activo { get; set; }
    }

    public class Cliente
    {
        public int Id { get; set; }
        public string Documento { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public bool Activo { get; set; }
        public override string ToString() => Nombre;
    }

    public class Venta
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; }
        public int EmpresaId { get; set; }
        public int SucursalId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public string TipoPago { get; set; }
        public string Estado { get; set; }
        public string Observacion { get; set; }
        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public override string ToString() => Nombre;
    }

    // Sesión activa del usuario
    public static class Sesion
    {
        public static Usuario UsuarioActivo { get; set; }
        public static Empresa EmpresaActiva { get; set; }
        public static Sucursal SucursalActiva { get; set; }
    }
}
