using System.Text;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 1. CONTROLADORES
// Le decimos a .NET que busque los Controllers y que los enums
// se lean como texto ("Administrador") en lugar de numeros (0, 1, 2)
// =============================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// =============================================================================
// 2. BASE DE DATOS — MySQL con Entity Framework
// Reemplaza Supabase. La cadena de conexion vive en appsettings.Development.json
// bajo la clave "ConnectionStrings:DefaultConnection"
// =============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada");

builder.Services.AddDbContext<ISOAuditAgentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// =============================================================================
// 3. AUTENTICACION JWT
// Le decimos a .NET como validar los tokens JWT que llegan en cada request.
// Si el token es invalido o vencio, el request es rechazado automaticamente.
// =============================================================================
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey no configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar que el token fue firmado con nuestra clave secreta
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Validar que el token fue creado por nosotros
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],

            // Validar que el token es para nuestra aplicacion
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],

            // Validar que el token no este vencido
            ValidateLifetime = true,

            // Sin margen de tolerancia en la expiracion
            ClockSkew = TimeSpan.Zero
        };
    });

// Necesario para que funcione [Authorize(Roles = "Administrador")]
builder.Services.AddAuthorization();

// =============================================================================
// 4. SERVICIOS Y REPOSITORIOS
// Registramos todas las clases que se van a inyectar automaticamente.
// Scoped = se crea una instancia por cada request HTTP.
// =============================================================================

// Repositorios — acceso a la base de datos
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Servicios — logica de negocio
builder.Services.AddScoped<AuthService>();

// Modulo 2.2 — Proyectos
builder.Services.AddScoped<IProyectoRepository, ProyectoRepository>();
builder.Services.AddScoped<ProyectoService>();

// Modulo 2.3 — Auditorias
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<AuditoriaService>();

// Modulo 2.4 — Procedimientos y artefactos
builder.Services.AddScoped<IProcedimientoRepository, ProcedimientoRepository>();
builder.Services.AddScoped<ProcedimientoService>();

// Modulo 2.5 — Informes
builder.Services.AddScoped<IInformeRepository, InformeRepository>();
builder.Services.AddScoped<InformeService>();

// Sub-agente de consistencia
builder.Services.AddScoped<IDocumentSummaryBuilder, DocumentSummaryBuilder>();
builder.Services.AddScoped<ConsistencyVerificationAgentService>();

// Cliente de IA (Gemini)
builder.Services.AddHttpClient<IAiClient, GeminiClient>();

// =============================================================================
// 5. OPENAPI (documentacion automatica de los endpoints)
// =============================================================================
builder.Services.AddOpenApi();

// =============================================================================
// CONSTRUIR LA APLICACION
// =============================================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// El orden importa — primero autenticacion, luego autorizacion
app.UseAuthentication(); // Verifica el token JWT
app.UseAuthorization();  // Verifica los roles y permisos

app.MapControllers();

app.Run();