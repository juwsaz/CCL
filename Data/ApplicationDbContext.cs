using Microsoft.EntityFrameworkCore;
using CCL.InventoryManagement.API.Models;

namespace CCL.InventoryManagement.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Definir la tabla en la base de datos
        public DbSet<Producto> Productos { get; set; }
    }
}
