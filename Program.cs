using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CCL.InventoryManagement.API.Services;
using CCL.InventoryManagement.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configurar el servidor para escuchar en los puertos indicados
builder.WebHost.UseUrls("http://localhost:5068", "https://localhost:7271");

// 🔹 Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
    );
});

// 🔹 Configurar PostgreSQL con Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔹 Cargar configuración de JWT desde appsettings.json
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new Exception("🚨 ERROR: La configuración de JWT en appsettings.json es inválida o está incompleta.");
}

// 🔹 Registrar servicios y controladores
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 🔹 Configurar Swagger con Autenticación JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CCL Inventory API",
        Version = "v1",
        Description = "API para gestionar el inventario de productos de CCL"
    });

    // Configurar autenticación en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce tu token en este formato: 'Bearer {tu_token}'."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 🔹 Registrar el servicio de JWT
builder.Services.AddSingleton<JwtService>();

// 🔹 Configurar autenticación con JWT
var key = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Uso del contenedor de dependencias para obtener el logger
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError($"🔴 Error en autenticación JWT: {context.Exception.Message}");

                context.Response.StatusCode = 401;
                return context.Response.WriteAsync($"🚨 Error en autenticación JWT: {context.Exception.Message}");
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var username = context.Principal?.Identity?.Name;
                logger.LogInformation($"✅ Token JWT válido. Usuario autenticado: {username}");
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// 🔹 Aplicar Migraciones Automáticas y Cargar Datos Iniciales
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();  // Aplica las migraciones
}

// 🔹 Manejo Global de Excepciones (Middleware)
var loggerApp = app.Logger;
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            loggerApp.LogError($"❌ Error en la API: {exceptionHandlerPathFeature.Error.Message}");
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("🚨 Error interno en el servidor. Contacta al soporte.");
    });
});

// 🔹 Configurar Swagger (disponible en desarrollo)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CCL Inventory API v1");
        options.RoutePrefix = string.Empty; // Swagger en http://localhost:5068/
    });
}

app.UseHttpsRedirection();

// 🔹 Habilitar CORS antes de autenticación
app.UseCors("AllowAll");

// 🔹 Habilitar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Mapear controladores
app.MapControllers();

// 🔹 Iniciar la aplicación
app.Run();
