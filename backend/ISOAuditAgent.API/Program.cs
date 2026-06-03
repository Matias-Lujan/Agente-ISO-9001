using System.Text;
using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Integrations.LLM;
using ISOAuditAgent.API.Integrations.MCP.Drive;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;


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
// 2. CORS — Permitir que el frontend Vite (puerto 5173) hable con la API
// Sin esto, el browser bloquea las requests del frontend.
// =============================================================================
const string CorsPolicyFrontend = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyFrontend, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Necesario para que el browser envíe/reciba la cookie HttpOnly del JWT.
            .AllowCredentials();
    });
});

// =============================================================================
// 3. BASE DE DATOS — MySQL con Entity Framework
// La cadena de conexion vive en appsettings.Development.json
// bajo la clave "ConnectionStrings:DefaultConnection"
// =============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada");

builder.Services.AddDbContext<ISOAuditAgentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// =============================================================================
// 4. AUTENTICACION JWT
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
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // El JWT viaja en una cookie HttpOnly (no en sessionStorage). Si no vino
        // el header Authorization, lo tomamos de la cookie 'auth_token'.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// =============================================================================
// 5. SERVICIOS Y REPOSITORIOS
// =============================================================================

// Repositorios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Servicios
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

// Modulo 2.5 — Hallazgos (solo lectura — los insertan los agentes)
builder.Services.AddScoped<IHallazgoRepository, HallazgoRepository>();
builder.Services.AddScoped<HallazgoService>();

// Modulo 2.5 — Informes
builder.Services.AddScoped<IInformeRepository, InformeRepository>();
builder.Services.AddScoped<InformeService>();

// =============================================================================
// 5.b WORKFLOW DEL ORCHESTRATOR (portado de dev)
// =============================================================================

// Repositorios del workflow (los compartidos ya se registraron arriba)
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
builder.Services.AddScoped<IArtefactoEvaluadoRepository, ArtefactoEvaluadoRepository>();
builder.Services.AddScoped<IDocumentoAnalizadoRepository, DocumentoAnalizadoRepository>();
builder.Services.AddScoped<IAuditoriaProgresoRepository, AuditoriaProgresoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Tracker de progreso (Singleton: crea su propio scope/DbContext por operación)
builder.Services.AddSingleton<IAuditoriaProgresoTracker, AuditoriaProgresoTracker>();

// Server MCP de Google Drive + cliente (config "GoogleDrive" y "Mcp:Drive")
builder.Services.AddGoogleDriveMcpServer(builder.Configuration);

// Gemini (IChatClient) + 4 AIAgent keyed (config "Gemini")
builder.Services.AddGeminiAndAgents(builder.Configuration);

// Servicio determinista del nodo inicial (dev lo registra aparte del orchestrator)
builder.Services.AddScoped<ResolutorContextoService>();

// Nodos + cola + runner + worker en background
builder.Services.AddAuditoriaOrchestrator();

// =============================================================================
// 6. OPENAPI
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

// IMPORTANTE: CORS debe ir ANTES de Authentication/Authorization
app.UseCors(CorsPolicyFrontend);

// El orden importa — primero autenticacion, luego autorizacion
app.UseAuthentication();
app.UseAuthorization();

// Server MCP de Google Drive montado en /mcp/drive
app.MapMcp("/mcp/drive");

app.MapControllers();

// En desarrollo: aplica migraciones pendientes y siembra los usuarios demo, así
// todo el equipo arranca con el mismo login funcionando (sin inserts manuales).
// Ver Data/DataSeeder.cs. En producción NO se ejecuta.
if (app.Environment.IsDevelopment())
{
    await DataSeeder.InicializarAsync(app.Services);
}

app.Run();