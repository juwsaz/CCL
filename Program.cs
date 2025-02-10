using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CCL Inventory API",
        Version = "v1",
        Description = "API para gestionar el inventario de productos de CCL",
        Contact = new OpenApiContact
        {
            Name = "Soporte CCL",
            Email = "soporte@ccl.com",
            Url = new Uri("https://ccl.com")
        }
    });
});

var app = builder.Build();

// Habilitar Swagger solo en modo Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CCL Inventory API v1");
        options.RoutePrefix = string.Empty; // Swagger en la raíz: http://localhost:5068/
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
