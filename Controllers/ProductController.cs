using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCL.InventoryManagement.API.Data;
using System.ComponentModel.DataAnnotations;

namespace CCL.InventoryManagement.API.Controllers
{
    [Route("api/productos")]
    [ApiController]
    [Authorize]  // 🔐 Requiere autenticación con JWT
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 GET /api/productos/inventario (Consultar Inventario)
        [HttpGet("inventario")]
        public async Task<IActionResult> GetInventario()
        {
            var productos = await _context.Productos.ToListAsync();

            if (!productos.Any())
                return NotFound(new { message = "❌ No hay productos en el inventario." });

            return Ok(productos);
        }

        // 🔹 POST /api/productos/movimiento (Registrar Entrada/Salida)
        [HttpPost("movimiento")]
        public async Task<IActionResult> MovimientoProducto([FromBody] MovimientoProductoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = await _context.Productos.FindAsync(request.ProductId);
            if (producto == null)
                return NotFound(new { message = "❌ Producto no encontrado." });

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

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Movimiento registrado correctamente.", producto });
        }

        // 🔹 Endpoint de prueba para verificar la conexión sin autenticación
        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }
    }

    // 🔹 Modelo Producto
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    // 🔹 Validaciones en MovimientoProductoRequest
    public class MovimientoProductoRequest
    {
        [Required(ErrorMessage = "El ID del producto es obligatorio.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
        [RegularExpression("entrada|salida", ErrorMessage = "El tipo de movimiento debe ser 'entrada' o 'salida'.")]
        public string TipoMovimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }
    }
}
