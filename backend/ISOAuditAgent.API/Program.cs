using ISOAuditAgent.API.Data;
using Microsoft.EntityFrameworkCore;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.Integrations.MCP.Drive;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;
using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Integrations.LLM;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ISOAuditAgent.API.Endpoints;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- Registro del DbContext con el provider de MySQL ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ISOAuditAgentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
// -------------------------------------------------------

// --- Registro de repositorios concretos (D2) ---
// Todos Scoped porque comparten el DbContext, que AddDbContext registra
// como Scoped por default. Un repositorio Singleton sobre un DbContext
// Scoped tiraría una excepción de validación de scopes al arrancar.
builder.Services.AddScoped<IProyectoRepository, ProyectoRepository>();
builder.Services.AddScoped<IProcedimientoRepository, ProcedimientoRepository>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IArtefactoEvaluadoRepository, ArtefactoEvaluadoRepository>();
builder.Services.AddScoped<IHallazgoRepository, HallazgoRepository>();
builder.Services.AddScoped<IDocumentoAnalizadoRepository, DocumentoAnalizadoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// -----------------------------------------------

// --- Registro de servicios deterministas del orquestador ---
// AuditoriaPersistenceService: depende del UoW + 4 repos (todos Scoped),
// vive en el ámbito de una auditoría.
builder.Services.AddScoped<IAuditoriaPersistenceService, AuditoriaPersistenceService>();

// ResolutorContextoService: clase concreta (no tiene interface). Lo consume
// ResolutorContextoNode por DI. Depende de 3 repos Scoped.
builder.Services.AddScoped<ResolutorContextoService>();
// -----------------------------------------------------------

// --- D3.1: Server MCP de Google Drive ---
// Registra GoogleDriveClient (Singleton, perezoso) + MCP server con tools.
// El montaje en /mcp/drive se hace abajo con app.MapMcp.
builder.Services.AddGoogleDriveMcpServer(builder.Configuration);
// ----------------------------------------

// --- D4: Gemini + AIAgent keyed ---
// Registra IChatClient (Singleton) + 4 AIAgent (keyed Singleton). Los nodos
// del workflow van a consumirlos en D5 vía [FromKeyedServices(...)].
builder.Services.AddGeminiAndAgents(builder.Configuration);
// ----------------------------------

// --- D5: Wiring completo del workflow (nodos + queue + runner + worker) ---
builder.Services.AddAuditoriaOrchestrator();
// --------------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- D3.1: Montaje del server MCP de Drive en /mcp/drive ---
app.MapMcp("/mcp/drive");
// -----------------------------------------------------------

// ============================================================================
//  Endpoints de smoke test — TEMPORALES (D5-mínimo)
// ----------------------------------------------------------------------------
//  Validan que la cadena de DI cablea y que MySQL responde, sin ejecutar
//  lógica de auditoría. Se eliminan en D6 cuando se agreguen los endpoints
//  reales (/api/auditorias).
// ============================================================================

// /api/_smoke/di — resuelve la cadena DI hasta los servicios deterministas.
// Si falta algún registro, falla con InvalidOperationException explícita.
app.MapGet("/api/_smoke/di", (IServiceProvider sp) =>
{
    using var scope = sp.CreateScope();
    _ = scope.ServiceProvider.GetRequiredService<IAuditoriaPersistenceService>();
    _ = scope.ServiceProvider.GetRequiredService<ResolutorContextoService>();
    return Results.Ok("DI OK");
});

// /api/_smoke/db — lee la clave 'path_carpeta_templates' sembrada por la
// migración InitialCreate. Valida conexión a MySQL + seed aplicado +
// cadena DI del ConfiguracionRepository.
app.MapGet("/api/_smoke/db", async (IServiceProvider sp, CancellationToken ct) =>
{
    using var scope = sp.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<IConfiguracionRepository>();
    var valor = await repo.ObtenerValorOrNullAsync("path_carpeta_templates", ct);
    return valor is null
        ? Results.NotFound("La clave 'path_carpeta_templates' no está en BD.")
        : Results.Ok(new { clave = "path_carpeta_templates", valor });
});

// /api/_smoke/drive-list?folderId=... — listado recursivo plano vía MCP.
// Cada archivo incluye path relativo al folder raíz (ej. "Seguimiento/foo.xlsx").
app.MapGet("/api/_smoke/drive-list",
    async (string folderId, IDriveMcpClient drive, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return Results.BadRequest("Falta el query param 'folderId'.");

        var listing = await drive.ListFilesUnderFolderAsync(folderId, ct);
        return Results.Ok(new
        {
            folderId = listing.FolderId,
            fileCount = listing.Files.Count,
            files = listing.Files.Select(f => new
            {
                f.Id,
                f.Name,
                f.MimeType,
                f.WebViewLink,
                f.Size,
                f.Path
            })
        });
    });

// /api/_smoke/drive-download?fileId=... — D3.2: descarga un archivo por id,
// calcula SHA-256 inline y devuelve solo metadata + size + hash. NO devuelve
// los bytes en la respuesta — el SHA-256 prueba que la descarga llegó completa
// sin inundar HTTP con megabytes de PDF/DOCX.
//
// El cálculo del hash es inline en el smoke. En D3.3 va a vivir en
// ContentHasher (clase dedicada con la regla SHA-256 hex 64 minúsculas que
// pide InvariantsValidator).
app.MapGet("/api/_smoke/drive-download",
    async (string fileId, IDriveMcpClient drive, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return Results.BadRequest("Falta el query param 'fileId'.");

        var content = await drive.GetFileContentAsync(fileId, ct);

        // SHA-256 hex en minúsculas, 64 chars — formato que pide
        // InvariantsValidator (Agents/DocumentAnalysis/InvariantsValidator.cs).
        var sha256 = ContentHasher.Sha256Hex(content.Bytes);

        return Results.Ok(new
        {
            id = content.Id,
            name = content.Name,
            mimeType = content.MimeType,
            sizeBytes = content.Bytes.Length,
            sizeFromMetadata = content.Size,
            sha256 = sha256
        });
    });

// /api/_smoke/parse-file?fileId=... — D3.3: descarga un archivo, elige
// parser por MIME, devuelve sha256 + lista de secciones detectadas.
//
// El dispatcher por MIME vive inline acá. En D3.5 (ArtefactoFisicoChecker)
// puede mudarse a una clase dedicada si se vuelve útil; por ahora un switch
// es suficiente para 3 formatos.
app.MapGet("/api/_smoke/parse-file",
    async (string fileId, IDriveMcpClient drive, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return Results.BadRequest("Falta el query param 'fileId'.");

        var content = await drive.GetFileContentAsync(fileId, ct);
        var sha256 = ContentHasher.Sha256Hex(content.Bytes);

        ITemplateParser? parser = content.MimeType switch
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                => new DocxTemplateParser(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                => new XlsxTemplateParser(),
            "application/pdf"
                => new PdfTemplateParser(),
            _ => null
        };

        if (parser is null)
        {
            return Results.Ok(new
            {
                name = content.Name,
                mimeType = content.MimeType,
                sha256,
                error = $"MIME type '{content.MimeType}' no soportado por D3.3."
            });
        }

        try
        {
            var secciones = parser.Parsear(content.Bytes);
            return Results.Ok(new
            {
                name = content.Name,
                mimeType = content.MimeType,
                sha256,
                secciones
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    });

// /api/_smoke/tailoring?folderId=... — D3.4: lee el FR-29 ficticio del folder
// de Drive de prueba, lo parsea y devuelve la lista de FilaTailoring.
app.MapGet("/api/_smoke/tailoring",
    async (string folderId, ITailoringSource tailoring, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return Results.BadRequest("Falta el query param 'folderId'.");

        try
        {
            var filas = await tailoring.ObtenerAsync(folderId, ct);
            return Results.Ok(filas);
        }
        catch (InvalidOperationException ex)
        {
            // Mensaje explícito: FR-29 no encontrado, no es XLSX, headers no
            // reconocidos, etc. Útil para diagnóstico durante el smoke.
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    });


// /api/_smoke/checker?folderId=...&codigo=...&nombre=...&url=... — D3.5.
// Construye un IntegracionesProyecto ficticio (solo Drive), llama al checker
// y devuelve el VerificacionFisica. Permite probar los 3 caminos:
//   - con url   → paso 1 (URL del tailoring)
//   - con codigo y archivo cuyo nombre contiene el código → paso 2
//   - solo con nombre → paso 3
//   - con códigos/nombres inexistentes → encontrado=false
app.MapGet("/api/_smoke/checker",
    async (
        string folderId,
        string? codigo,
        string nombre,
        string? url,
        IArtefactoFisicoChecker checker,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return Results.BadRequest("Falta el query param 'folderId'.");

        if (string.IsNullOrWhiteSpace(nombre))
            return Results.BadRequest("Falta el query param 'nombre'.");

        var integraciones = new ISOAuditAgent.API.Agents.Contracts.IntegracionesProyecto(
            DriveFolderId: folderId,
            TrelloBoardId: null,
            ClockifyProjectId: null);

        var resultado = await checker.VerificarAsync(
            integraciones,
            artefactoEsperadoId: 0,
            codigoArtefacto: string.IsNullOrWhiteSpace(codigo) ? null : codigo,
            nombreArtefacto: nombre,
            urlReferenciaTailoring: string.IsNullOrWhiteSpace(url) ? null : url,
            pathTemplateAbsoluto: null,
            ct: ct);

        return Results.Ok(resultado);
    });


// /api/_smoke/llm?prompt=... — D4: invoca el IChatClient registrado con un
// prompt arbitrario. Valida que:
//   - Mscc.GenerativeAI.Microsoft está bien linkeado.
//   - Gemini:ApiKey está bien cargada (vía user-secrets en dev).
//   - La conexión con Gemini funciona y responde.
// NO usa AIAgent — solo IChatClient. Si falla, el problema está abajo de MAF.
app.MapGet("/api/_smoke/llm",
    async (string prompt, IChatClient chat, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return Results.BadRequest("Falta el query param 'prompt'.");

        try
        {
            var respuesta = await chat.GetResponseAsync(prompt, cancellationToken: ct);
            return Results.Ok(new
            {
                prompt,
                respuesta = respuesta.Text,
                modelo = respuesta.ModelId
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"IChatClient falló: {ex.GetType().Name}: {ex.Message}",
                statusCode: 500);
        }
    });

// /api/_smoke/agent?nodo=<DocumentAnalysis|ComplianceValidation|ConsistencyVerification|FindingsClassification>
//                  &prompt=...
//
// D4: invoca un AIAgent específico (de los 4 keyed registrados) con un
// prompt. Valida que:
//   - El AIAgent se construyó bien con su SystemPrompt como instructions.
//   - chat.CreateAIAgent + AIAgent.RunAsync funcionan end-to-end (que es
//     justo lo que AgenteExecutorBase.HandleAsync usa).
//   - Las 4 keys del AddKeyedSingleton resuelven correctamente.
// Si /api/_smoke/llm funciona pero este falla, el problema está en MAF.
app.MapGet("/api/_smoke/agent",
    async (string nodo, string prompt, IServiceProvider sp, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(nodo))
            return Results.BadRequest("Falta el query param 'nodo'.");
        if (string.IsNullOrWhiteSpace(prompt))
            return Results.BadRequest("Falta el query param 'prompt'.");

        var keysValidas = new[] {
        "DocumentAnalysis",
        "ComplianceValidation",
        "ConsistencyVerification",
        "FindingsClassification"
    };

        if (!keysValidas.Contains(nodo))
        {
            return Results.BadRequest(
                $"'nodo' debe ser uno de: {string.Join(", ", keysValidas)}.");
        }

        try
        {
            var agente = sp.GetRequiredKeyedService<AIAgent>(nodo);
            var respuesta = await agente.RunAsync(prompt, cancellationToken: ct);

            return Results.Ok(new
            {
                nodo,
                prompt,
                respuesta = respuesta.Text
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"AIAgent '{nodo}' falló: {ex.GetType().Name}: {ex.Message}",
                statusCode: 500);
        }
    });


// /api/_smoke/wiring — D5: valida que TODA la cadena DI resuelve.
//
// Crea un scope nuevo (igual que hace el AuditoriaRunner) y resuelve uno por
// uno los 6 nodos + el persistence service + la queue + el runner. Si
// cualquier dependencia falta o tiene lifetime mal, la primera resolución
// que toque eso tira con mensaje claro.
//
// NO ejecuta ningún nodo, NO toca BD, NO llama a Gemini. Es puro chequeo de DI.
app.MapGet("/api/_smoke/wiring", (IServiceScopeFactory scopeFactory) =>
{
    using var scope = scopeFactory.CreateScope();
    var sp = scope.ServiceProvider;

    var resueltos = new List<string>();

    try
    {
        // Persistencia
        _ = sp.GetRequiredService<IAuditoriaPersistenceService>();
        resueltos.Add(nameof(IAuditoriaPersistenceService));

        // 6 nodos
        _ = sp.GetRequiredService<ResolutorContextoNode>();
        resueltos.Add(nameof(ResolutorContextoNode));

        _ = sp.GetRequiredService<DocumentAnalysisNode>();
        resueltos.Add(nameof(DocumentAnalysisNode));

        _ = sp.GetRequiredService<ComplianceValidationNode>();
        resueltos.Add(nameof(ComplianceValidationNode));

        _ = sp.GetRequiredService<ConsistencyVerificationNode>();
        resueltos.Add(nameof(ConsistencyVerificationNode));

        _ = sp.GetRequiredService<FindingsClassificationNode>();
        resueltos.Add(nameof(FindingsClassificationNode));

        _ = sp.GetRequiredService<ConsolidadorResultadoNode>();
        resueltos.Add(nameof(ConsolidadorResultadoNode));

        // Queue + Runner (Singleton, los resolvemos desde el sp también)
        _ = sp.GetRequiredService<IAuditoriaQueue>();
        resueltos.Add(nameof(IAuditoriaQueue));

        _ = sp.GetRequiredService<AuditoriaRunner>();
        resueltos.Add(nameof(AuditoriaRunner));

        return Results.Ok(new
        {
            ok = true,
            resueltos,
            total = resueltos.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"Resolución falló después de {resueltos.Count} componentes OK. " +
                    $"Último fallo: {ex.GetType().Name}: {ex.Message}",
            statusCode: 500);
    }
});


// /api/_smoke/run-workflow?auditoriaId=N — D5: encola una auditoría para
// que el worker la procese.
//
// Devuelve 202 Accepted inmediato. NO espera resultado. La validación de
// que la auditoría realmente corre exitosa es D7 — esto solo valida que la
// queue funciona y el worker recoge el item.
//
// Si auditoriaId no existe en BD, el runner va a fallar y marcar la
// auditoría como Fallida (intentando, porque tampoco existe el registro).
// Eso es esperado en D5 — el chequeo real es ver en logs que el worker
// tomó el item y empezó a procesar.
app.MapGet("/api/_smoke/run-workflow",
    async (int auditoriaId, IAuditoriaQueue cola, CancellationToken ct) =>
    {
        if (auditoriaId <= 0)
            return Results.BadRequest("'auditoriaId' debe ser > 0.");

        await cola.EncolarAsync(auditoriaId, ct);

        return Results.Accepted(value: new
        {
            auditoriaId,
            mensaje = "Auditoría encolada. El worker la va a procesar en background. " +
                      "Mirá los logs para ver progreso/errores."
        });
    });


// /api/_smoke/auditorias/{id}/resultado — D7 smoke de lectura.
//
// Devuelve metadata + artefactos_evaluados + hallazgos + documentos_analizados
// de una auditoría completada. Es solo para inspección desde Postman; no es el
// endpoint definitivo (eso sería /api/auditorias/{id}/hallazgos etc).
//
// Lee directo del DbContext con Include para traer todo en una sola query.
// Si una auditoría está EnCurso o Fallida, igual responde — los campos
// pueden venir vacíos. El smoke no asume estado Completada.
app.MapGet("/api/_smoke/auditorias/{id:int}/resultado",
    async (int id, ISOAuditAgentDbContext db, CancellationToken ct) =>
    {
        if (id <= 0)
            return Results.BadRequest("id debe ser > 0.");

        // Una sola query con todas las navegaciones.
        var auditoria = await db.Auditorias
            .AsNoTracking()
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.ArtefactoEsperado)
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.Hallazgos)
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.DocumentosAnalizados)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (auditoria is null)
            return Results.NotFound($"Auditoría {id} no encontrada.");

        // Proyección plana a DTO anónimo. NO devolvemos las entities directo
        // (evita ciclos de navegaciones y exposición de campos internos de EF).
        return Results.Ok(new
        {
            // Metadata de la auditoría
            id = auditoria.Id,
            proyectoId = auditoria.ProyectoId,
            etapaId = auditoria.EtapaId,
            usuarioId = auditoria.UsuarioId,
            estado = auditoria.Estado.ToString(),
            fechaInicioUtc = auditoria.FechaInicioUtc,
            fechaFinalizacionUtc = auditoria.FechaFinalizacionUtc,

            // Contadores rápidos para tener visión a un golpe
            contadores = new
            {
                artefactosEvaluados = auditoria.ArtefactosEvaluados.Count,
                hallazgos = auditoria.ArtefactosEvaluados.Sum(ae => ae.Hallazgos.Count),
                documentosAnalizados = auditoria.ArtefactosEvaluados.Sum(ae => ae.DocumentosAnalizados.Count)
            },

            // Detalle por artefacto evaluado, con sus hallazgos y documentos anidados.
            artefactosEvaluados = auditoria.ArtefactosEvaluados
                .OrderBy(ae => ae.Id)
                .Select(ae => new
                {
                    id = ae.Id,
                    artefactoEsperadoId = ae.ArtefactoEsperadoId,
                    artefactoEsperadoCodigo = ae.ArtefactoEsperado?.Codigo,
                    artefactoEsperadoNombre = ae.ArtefactoEsperado?.Nombre,
                    aplica = ae.Aplica.ToString(),
                    justificacionNoAplica = ae.JustificacionNoAplica,
                    resultado = ae.Resultado.ToString(),
                    observaciones = ae.Observaciones,

                    hallazgos = ae.Hallazgos
                        .OrderBy(h => h.Id)
                        .Select(h => new
                        {
                            id = h.Id,
                            tipo = h.Tipo.ToString(),
                            descripcion = h.Descripcion,
                            justificacion = h.Justificacion,
                            agenteOrigen = h.AgenteOrigen.ToString()
                        })
                        .ToList(),

                    documentosAnalizados = ae.DocumentosAnalizados
                        .OrderBy(d => d.Id)
                        .Select(d => new
                        {
                            id = d.Id,
                            nombreArchivo = d.NombreArchivo,
                            fuente = d.Fuente.ToString(),
                            urlReferencia = d.UrlReferencia,
                            hashContenido = d.HashContenido
                        })
                        .ToList()
                })
                .ToList()
        });
    });

// --- D6: Endpoints REST reales del workflow ---
app.MapAuditoriaEndpoints();
// ----------------------------------------------


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};



app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}