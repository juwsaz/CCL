using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CCL.InventoryManagement.API.Controllers
{
    [Route("api/productos")]
    [ApiController]
    [Authorize]  // 🔐 Requiere autenticación con JWT
    public class ProductController : ControllerBase
    {
        private static List<Producto> _productos = new List<Producto>
        {
            new Producto { Id = 1, Nombre = "Laptop", Cantidad = 10 },
            new Producto { Id = 2, Nombre = "Teclado", Cantidad = 20 }
        };

        // 🔹 GET /api/productos/inventario (Consultar Inventario)
        [HttpGet("inventario")]
        public IActionResult GetInventario()
        {
            return Ok(_productos);
        }

        // 🔹 POST /api/productos/movimiento (Registrar Entrada/Salida)
        [HttpPost("movimiento")]
        public IActionResult MovimientoProducto([FromBody] MovimientoProductoRequest request)
        {
            var producto = _productos.FirstOrDefault(p => p.Id == request.ProductId);
            if (producto == null)
            {
                return NotFound(new { message = "❌ Producto no encontrado." });
            }

            if (request.TipoMovimiento.ToLower() == "entrada")
            {
                producto.Cantidad += request.Cantidad;
            }
            else if (request.TipoMovimiento.ToLower() == "salida")
            {
                if (producto.Cantidad < request.Cantidad)
                    return BadRequest(new { message = "❌ No hay suficiente stock disponible." });

                producto.Cantidad -= request.Cantidad;
            }
            else
            {
                return BadRequest(new { message = "❌ Tipo de movimiento inválido. Usa 'entrada' o 'salida'." });
            }

            return Ok(new { message = "✅ Movimiento registrado correctamente.", producto });
        }
        // Endpoint de prueba para verificar la conexión sin autenticación (opcional)
        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }
    }

    // 🔹 Clases auxiliares para modelo de datos
    public class Producto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public int Cantidad { get; set; }
    }

    public class MovimientoProductoRequest
    {
        public int ProductId { get; set; }
        public string TipoMovimiento { get; set; }  // "entrada" o "salida"
        public int Cantidad { get; set; }
    }
}
