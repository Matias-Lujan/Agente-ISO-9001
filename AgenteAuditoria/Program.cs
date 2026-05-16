using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using AgenteAuditoria.Agents;
using AgenteAuditoria.Executors;
using AgenteAuditoria.Models;

// ── Configuración ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var apiKey = config["Gemini:ApiKey"]
    ?? throw new InvalidOperationException(
        "API key no encontrada. " +
        "Correr: dotnet user-secrets set 'Gemini:ApiKey' 'TU_KEY'");

IChatClient geminiClient = new GeminiChatClient(apiKey: apiKey, model: "gemini-2.5-flash");
var agente   = AgentFactory.CreateFindingsClassificationAgent(geminiClient);
var executor = new FindingsClassificationExecutor(agente);

// ── Input humano: el auditor indica la etapa ──────────────────────────────────
// La IA no deduce la etapa — un proyecto puede estar en cierre
// con documentación de kick-off.
Console.WriteLine("=================================================");
Console.WriteLine(" AGENTE DE CLASIFICACIÓN DE HALLAZGOS ISO 9001");
Console.WriteLine(" PR 11-13 — BDT Global");
Console.WriteLine("=================================================");
Console.WriteLine(" 1=Planificación  2=Análisis y Diseño  3=Desarrollo");
Console.WriteLine(" 4=Testing        5=Seguimiento        6=Implementación");
Console.Write("\n Etapa actual del proyecto (1-6): ");
if (!int.TryParse(Console.ReadLine(), out var etapaActual) || etapaActual < 1 || etapaActual > 6)
{
    Console.WriteLine("[ERROR] Etapa inválida.");
    return;
}
Console.WriteLine($" Etapa: {FindingsClassificationExecutor.NombreEtapa(etapaActual)}\n");

// ── Simular hallazgos de ComplianceValidation ─────────────────────────────────
// En producción los genera el agente ComplianceValidation a partir
// del DocumentosExtraidos que recibe del workflow.
var deCompliance = new HallazgosPreliminares(
    AuditoriaId:  30052,
    AgenteOrigen: AgenteOrigen.ComplianceValidation,
    Hallazgos: new List<HallazgoPreliminar>
    {
        new(
            ArtefactoEsperadoId: 101,
            Descripcion: "Clockify sin horas registradas en período con tareas completadas en Trello",
            Justificacion: "El Tailoring indica Clockify como Mandatorio. " +
                "Se registraron 0 horas del 01/04 al 09/04 pero Trello " +
                "muestra 5 tarjetas completadas en ese período.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 102,
            Descripcion: "FR 25 Liberación de Software sin formulario para el paquete v1.0.0.13",
            Justificacion: "El paquete v1.0.0.13 existe en el repositorio " +
                "pero no hay FR 25 asociado. Mandatorio en tipo A y B.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 103,
            Descripcion: "Cronograma MS Project sin actualizar en más de 3 semanas",
            Justificacion: "Última versión: 22/03/2024. Sin nuevas versiones desde entonces. " +
                "El PR 11-13 exige generar nueva versión por cada actualización.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 104,
            Descripcion: "FR 71 Control de Costos sin actualizar en proyecto activo",
            Justificacion: "Última versión: 15/03/2024. Sin versiones posteriores " +
                "en carpeta activa con proyecto en curso.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 105,
            Descripcion: "Horas en Clockify superan en 41% las estimadas sin actualización del cronograma",
            Justificacion: "847 horas registradas vs 600 estimadas. " +
                "Sin revisión del cronograma ni del FR 71.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 106,
            Descripcion: "FR 11 Minuta de Reunión ausente para el kickoff",
            Justificacion: "La carpeta Reuniones de Drive está vacía. " +
                "Sin FR 11 de kickoff ni de reuniones de avance.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 107,
            Descripcion: "Ciclos de ejecución de pruebas (FR 32) no documentados para la release v1.0.0.13",
            Justificacion: "Tailoring indica FR 32 como aplicable. " +
                "La release está cerrada pero no hay registro de ejecución de pruebas.",
            OrigenRegla: OrigenRegla.Tailoring
        ),
        new(
            ArtefactoEsperadoId: 108,
            Descripcion: "FR 48 Sign-Off sin firma del cliente en sección de aprobación",
            Justificacion: "El documento existe pero el campo 'Firma cliente' está vacío. " +
                "Solo firmado por el Líder de Proyecto.",
            OrigenRegla: OrigenRegla.Template
        ),
    }
);

// ── Simular hallazgos de ConsistencyVerification ──────────────────────────────
var deConsistency = new HallazgosPreliminares(
    AuditoriaId:  30052,
    AgenteOrigen: AgenteOrigen.ConsistencyVerification,
    Hallazgos: new List<HallazgoPreliminar>
    {
        new(
            ArtefactoEsperadoId: 109,
            Descripcion: "Trello excluido del Tailoring sin justificación, pese a estar siendo usado",
            Justificacion: "Tailoring marca Trello como 'No (justificar)' con columna vacía. " +
                "El tablero tiene 47 tarjetas activas — contradicción directa.",
            OrigenRegla: OrigenRegla.Tailoring
        ),
        new(
            ArtefactoEsperadoId: 110,
            Descripcion: "Responsable del cronograma distinto entre Tailoring y archivo .mpp",
            Justificacion: "Tailoring: responsable = Líder de Proyecto. " +
                "Archivo .mpp: autor = G. Operaciones.",
            OrigenRegla: OrigenRegla.Tailoring
        ),
        new(
            ArtefactoEsperadoId: 111,
            Descripcion: "ERS (FR 30) marcada como aplicable en Tailoring pero no encontrada en Drive",
            Justificacion: "Tailoring indica Aplica=Sí con link. " +
                "Carpeta Alcance Funcional solo contiene desktop.ini.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 112,
            Descripcion: "Fecha de entrega en cronograma no coincide con la de Trello",
            Justificacion: "Cronograma: 31/03/2024. Trello card 'Entrega final': 15/04/2024. " +
                "Sin justificación del cambio.",
            OrigenRegla: OrigenRegla.Template
        ),
        new(
            ArtefactoEsperadoId: 113,
            Descripcion: "Carpeta Drive sin subcarpeta de Ingeniería de Software",
            Justificacion: "La estructura definida en el PR 11-13 incluye " +
                "Ingeniería de Software y Reuniones — ambas ausentes.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
        new(
            ArtefactoEsperadoId: 114,
            Descripcion: "Planilla de riesgos (FR 31) sin versiones actualizadas desde el inicio",
            Justificacion: "Única versión: 01/02/2024. El proyecto lleva 10 semanas activo " +
                "con múltiples cambios de alcance.",
            OrigenRegla: OrigenRegla.Procedimiento
        ),
    }
);

// ── Ejecutar ──────────────────────────────────────────────────────────────────
await executor.ProcesarAsync(deCompliance);
var resultado = await executor.ProcesarAsync(deConsistency);

// ── Mostrar resultados ────────────────────────────────────────────────────────
if (resultado is null)
{
    Console.WriteLine("[ERROR] El agente no produjo resultado.");
    return;
}

Console.WriteLine($"\n=================================================");
Console.WriteLine($" RESULTADO — Auditoría {resultado.AuditoriaId}");
Console.WriteLine($"=================================================");
Console.WriteLine($"  NC:  {resultado.Hallazgos.Count(h => h.Tipo == TipoHallazgo.NC)}");
Console.WriteLine($"  OBS: {resultado.Hallazgos.Count(h => h.Tipo == TipoHallazgo.OBS)}");
Console.WriteLine($"  OM:  {resultado.Hallazgos.Count(h => h.Tipo == TipoHallazgo.OM)}");
Console.WriteLine($"  Total: {resultado.Hallazgos.Count}");
Console.WriteLine("=================================================\n");

foreach (var h in resultado.Hallazgos)
{
    var etiqueta = h.Tipo switch
    {
        TipoHallazgo.NC  => "[NC ]",
        TipoHallazgo.OBS => "[OBS]",
        TipoHallazgo.OM  => "[OM ]",
        _                => "[???]"
    };

    Console.WriteLine($"{etiqueta} {h.Descripcion}");
    Console.WriteLine($"       Artefacto ID: {h.ArtefactoEsperadoId}");
    Console.WriteLine($"       Agente:       {h.AgenteOrigen}");
    Console.WriteLine($"       Justificación: {h.Justificacion}");
    Console.WriteLine();
}

Console.WriteLine("✓ HallazgosClasificados listo para el ConsolidadorResultado.");