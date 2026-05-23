// ============================================================================
//  Nodos del workflow de auditoría — 6 nodos (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Grafo:
//
//    [1] ResolutorContextoNode        Executor<IniciarAuditoriaWorkflowInput,
//                                              ContextoAuditoria>      sin LLM
//    [2] DocumentAnalysisNode         Executor<ContextoAuditoria,
//                                              DocumentosExtraidos>    con LLM
//    [3] ComplianceValidationNode     Executor<DocumentosExtraidos,
//                                              HallazgosPreliminares>  con LLM
//    [4] ConsistencyVerificationNode  Executor<DocumentosExtraidos,
//                                              HallazgosPreliminares>  con LLM
//    [5] FindingsClassificationNode   Executor (3 entradas cacheadas)  con LLM
//    [6] ConsolidadorResultadoNode    Executor<ResultadoClasificacion
//                                              ConContexto,
//                                              AuditoriaResultado>     sin LLM
//
//  Aristas:
//    [1] -> [2]                                  (AddEdge)
//    [2] -> [3]  y  [2] -> [4]                   (AddFanOutEdge)
//    [2] -> [5]                                  (AddEdge: DocumentosExtraidos)
//    [3] -> [5]  y  [4] -> [5]                   (AddFanInBarrierEdge)
//    [5] -> [6]                                  (AddEdge)
//    salida desde [6]                            (WithOutputFrom)
//
//  Carril de DocumentosExtraidos: [2] -> [5] -> [6]. Un solo carril. No pasa
//  por los validadores. El LLM de [5] no lo toca: lo conserva el executor C#.
//
//  PATRONES DE EXECUTOR (los dos válidos, cada uno para su caso):
//   - Executor<TIn,TOut> + HandleAsync  -> nodo de UN input. Usan [1][2][3][4][6].
//   - Executor + [MessageHandler]       -> nodo de VARIOS inputs. Usa [5].
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using ISOAuditAgent.API.Agents.FindingsClassification;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Agents.ComplianceValidation;

namespace ISOAuditAgent.API.Agents.Orchestrator;

// ============================================================================
//  [1] ResolutorContextoNode — nodo determinista inicial, sin LLM
// ============================================================================

public sealed class ResolutorContextoNode
    : Executor<IniciarAuditoriaWorkflowInput, ContextoAuditoria>
{
    private readonly ResolutorContextoService _servicio;

    public ResolutorContextoNode(ResolutorContextoService servicio)
        : base("ResolutorContexto")
    {
        _servicio = servicio;
    }

    public override async ValueTask<ContextoAuditoria> HandleAsync(
        IniciarAuditoriaWorkflowInput message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        return await _servicio.ResolverAsync(message, ct);
    }
}

// ============================================================================
//  [2] DocumentAnalysisNode — agente con LLM + I/O async pre y post LLM
// ----------------------------------------------------------------------------
//  EXCEPCIÓN LOCAL AL TEMPLATE METHOD DE AgenteExecutorBase.
//
//  Por qué overridea HandleAsync:
//   - PRE-LLM: necesita descargar el FR-29 (tailoring) de Drive vía MCP
//     (ITailoringSource.ObtenerAsync, async).
//   - POST-LLM: por cada artefacto Aplica+Exigible debe descargar el archivo
//     físico, calcular su hash y parsear secciones (IArtefactoFisicoChecker.
//     VerificarAsync, async).
//   Ninguna de las dos cosas encaja en ConstruirPrompt(string) ni en
//   ParsearRespuesta(string) que son sync por diseño de la base. Y no usamos
//   .Result ni .GetAwaiter().GetResult() — bloquearían el worker.
//
//   La asimetría con los otros 3 nodos LLM es real pero está acotada a este
//   nodo. El resto sigue heredando el template method de AgenteExecutorBase
//   sin override.
//
//  Dependencias adicionales por constructor:
//    ITailoringSource         (Chat D §5.3: descarga + parseo del FR-29 XLSX).
//    IArtefactoFisicoChecker  (Chat D §5.3: descarga + hash + secciones de
//                              cada artefacto).
//   Ambas son interfaces definidas en Agents/DocumentAnalysis/. Las
//   implementaciones, su registro en DI y el cableado MCP los hace Chat D.
//
//  ConstruirPrompt / ParsearRespuesta no aplican acá. Lanzan
//  NotSupportedException con mensaje explícito si alguien los llama por error
//  (no debería pasar — la base ya no los invoca porque HandleAsync overridea
//  el template completo).
// ============================================================================
public sealed class DocumentAnalysisNode
    : AgenteExecutorBase<ContextoAuditoria, DocumentosExtraidos>
{
    private readonly ITailoringSource _tailoringSource;
    private readonly IArtefactoFisicoChecker _artefactoChecker;
    private readonly ILogger<DocumentAnalysisNode> _logger;

    public DocumentAnalysisNode(
        AIAgent agente,
        ITailoringSource tailoringSource,
        IArtefactoFisicoChecker artefactoChecker,
        ILogger<DocumentAnalysisNode> logger)
        : base("DocumentAnalysis", agente)
    {
        _tailoringSource = tailoringSource;
        _artefactoChecker = artefactoChecker;
        _logger = logger;
    }

    public override async ValueTask<DocumentosExtraidos> HandleAsync(
        ContextoAuditoria message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        //-------------------TEST------------------
        _logger.LogInformation(
            "NODE DocumentAnalysis INICIO auditoria={AuditoriaId} artefactosEsperados={Count}",
            message.AuditoriaId,
            message.ArtefactosEsperados.Count
            );
        //-------------------------------------

        // 1. PRE-LLM async: descargar el FR-29 del Drive del proyecto.
        var driveFolderId = message.Integraciones.DriveFolderId;
        if (string.IsNullOrWhiteSpace(driveFolderId))
        {
            throw new InvalidOperationException(
                $"Proyecto {message.ProyectoId}: no tiene DriveFolderId en " +
                "Integraciones. DocumentAnalysis necesita una carpeta de Drive " +
                "para descargar el FR-29.");
        }

        var tailoring = await _tailoringSource
            .ObtenerAsync(driveFolderId, ct)
            .ConfigureAwait(false);

        // 2. Armar el prompt (sync, sin I/O).
        var prompt = ConstruirPromptInterno(message, tailoring);

        // 3. Llamar al LLM. La base no se usa porque HandleAsync overridea
        //    el template completo, pero el AIAgent está heredado vía Agente.
        //    La llamada al AIAgent usa la misma API validada en AgenteExecutorBase.
        var respuesta = await Agente.RunAsync(prompt, cancellationToken: ct);
        var textoLlm = respuesta.Text ?? string.Empty;

        // 3.5. Log de la respuesta cruda — D7.3, mientras estabilizamos el
        //      formato del LLM. Si el parseo falla, el log tiene el texto
        //      exacto que devolvió Gemini. Se quita en D7.6 si ya no aporta.
        _logger.LogInformation(
            "DocumentAnalysis LLM respuesta cruda ({Len} chars):\n{Texto}",
            textoLlm.Length, textoLlm);

        // 4. Parsear el JSON del LLM (tolerante a prosa / cercas / BOM).
        var llmOutput = ParsearJsonLlm(textoLlm);

        // 5. POST-LLM async: verificar artefactos físicamente y armar el contrato 3.
        var artefactos = await DriveDtos
            .ConstruirAsync(message, llmOutput, _artefactoChecker, ct)
            .ConfigureAwait(false);

        var resultado = new DocumentosExtraidos(
            message.AuditoriaId,
            message.ProyectoId,
            message.EtapaId,
            artefactos);

        // 6. Defensa en profundidad: validar invariantes del contrato 3 antes
        //    de emitir el DTO al grafo.
        InvariantsValidator.Validar(resultado);

        // ------------------TEST--------------------------
        _logger.LogInformation(
            "NODE DocumentAnalysis FIN auditoria={AuditoriaId} artefactos={Count}",
            resultado.AuditoriaId,
            resultado.Artefactos.Count
            );
        // --------------------------------------------

        return resultado;
    }

    /// <summary>
    /// Serializa el contexto + tailoring al user message del LLM. El system
    /// prompt vive en SystemPrompts.AnalizadorDocumental (lo configura
    /// Chat D §5.4 al construir el AIAgent).
    /// </summary>
    private static string ConstruirPromptInterno(
    ContextoAuditoria contexto,
    IReadOnlyList<FilaTailoring> tailoring)
    {
        var payload = new
        {
            contexto = new
            {
                artefactosEsperados = contexto.ArtefactosEsperados.Select(a => new
                {
                    artefactoEsperadoId = a.ArtefactoEsperadoId,
                    codigo = a.CodigoArtefacto,          // puede ser null
                    nombre = a.NombreArtefacto,          // siempre presente, fallback de match
                    exigibilidad = a.Exigibilidad.ToString(),
                    obligatoriedad = a.Obligatoriedad.ToString()
                })
            },
            tailoring = tailoring.Select(f => new
            {
                codigoArtefacto = f.CodigoArtefacto,     // puede ser null
                nombreArtefacto = f.NombreArtefacto,     // siempre presente, fallback de match
                aplica = f.Aplica,
                justificacionNoAplica = f.JustificacionNoAplica,
                urlReferencia = f.UrlReferencia
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Deserializa el JSON del LLM, tolerando dos casos comunes:
    ///   1. Cercas markdown: ```json ... ``` (ya estaba).
    ///   2. Prosa antes/después del JSON: "Acá tenés el JSON solicitado: { ... }"
    ///      → extraemos desde el primer '{' hasta el último '}' que cierra ese
    ///      objeto raíz.
    ///   3. BOM / chars no-ASCII al inicio antes del '{'.
    ///
    /// Si después de extraer sigue sin parsear, lanza con el texto crudo
    /// incluido en el mensaje (para diagnóstico).
    /// </summary>
    private static LlmOutput ParsearJsonLlm(string textoLlm)
    {
        var jsonLimpio = ExtraerJsonObjeto(textoLlm);

        try
        {
            var resultado = System.Text.Json.JsonSerializer.Deserialize<LlmOutput>(
                jsonLimpio,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (resultado is null)
            {
                throw new InvalidOperationException(
                    "Respuesta del LLM deserializó a null.");
            }

            if (resultado.Artefactos is null)
            {
                throw new InvalidOperationException(
                    "Respuesta del LLM no incluye la propiedad 'artefactos'.");
            }

            return resultado;
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Incluimos el JSON ya extraído + un prefijo del texto crudo para
            // diagnosticar rápido qué devolvió el LLM.
            var preview = textoLlm.Length > 500
                ? textoLlm.Substring(0, 500) + "..."
                : textoLlm;

            throw new InvalidOperationException(
                $"Respuesta del LLM no es JSON válido: {ex.Message}. " +
                $"Texto crudo (primeros 500 chars): >>>{preview}<<<", ex);
        }
    }

    /// <summary>
    /// Quita cercas markdown, BOM, y extrae el primer objeto JSON balanceado.
    /// Si no encuentra '{...}', devuelve el texto original (que va a fallar
    /// el JsonSerializer con mensaje claro).
    /// </summary>
    private static string ExtraerJsonObjeto(string textoLlm)
    {
        if (string.IsNullOrWhiteSpace(textoLlm))
            return textoLlm;

        // 1. Quitar cercas markdown si las hay.
        var s = textoLlm
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();

        // 2. Quitar BOM UTF-8 si quedó.
        if (s.Length > 0 && s[0] == '\uFEFF')
            s = s.Substring(1);

        // 3. Buscar el primer '{' y el último '}' que balancean.
        var inicio = s.IndexOf('{');
        if (inicio < 0) return s; // No hay objeto; deja que JsonSerializer falle ruidoso.

        // Recorremos contando llaves para encontrar el cierre del objeto raíz.
        int depth = 0;
        int fin = -1;
        bool dentroString = false;
        bool escape = false;

        for (int i = inicio; i < s.Length; i++)
        {
            var c = s[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && dentroString) { escape = true; continue; }
            if (c == '"') { dentroString = !dentroString; continue; }
            if (dentroString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) { fin = i; break; }
            }
        }

        if (fin < 0) return s.Substring(inicio); // Sin cierre; deja fallar al deserializer con texto desde '{'.

        return s.Substring(inicio, fin - inicio + 1);
    }

    // ConstruirPrompt y ParsearRespuesta del template method NO aplican en
    // este nodo (HandleAsync overridea el flujo entero). Las dejamos con
    // NotSupportedException explícita por si alguien las invocara
    // accidentalmente — el código actual no las llama.

    protected override string ConstruirPrompt(ContextoAuditoria input) =>
        throw new NotSupportedException(
            "DocumentAnalysisNode overridea HandleAsync por requerir I/O async " +
            "pre y post LLM. La construcción del prompt vive en " +
            "ConstruirPromptInterno y se invoca desde HandleAsync.");

    protected override DocumentosExtraidos ParsearRespuesta(
        ContextoAuditoria input, string textoLlm) =>
        throw new NotSupportedException(
            "DocumentAnalysisNode overridea HandleAsync. El parseo de la " +
            "respuesta del LLM vive en ParsearJsonLlm y se invoca desde HandleAsync.");
}

// ============================================================================
//  [3] ComplianceValidationNode — agente con LLM + determinísticos pre-LLM.
//
//  ARQUITECTURA D2 — DETERMINÍSTICOS FUERA DEL LLM:
//
//  Los 3 desvíos determinísticos los genera HallazgosDeterministicos sobre el
//  DTO de entrada, ANTES de invocar al LLM. No requieren razonamiento: son
//  chequeos directos sobre el contrato 3.
//
//  Al LLM le queda una tarea acotada: detectar incoherencias evidentes entre
//  URL declarada en tailoring y NombreArchivo encontrado, sobre artefactos
//  Exigibles + Aplica + Encontrado, siempre que tenga URL y nombre de archivo
//  para comparar.
//
//  ParsearRespuesta fusiona determinísticos primero y luego hallazgos del LLM.
// ============================================================================
public sealed class ComplianceValidationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    public ComplianceValidationNode(AIAgent agente)
        : base("ComplianceValidation", agente) { }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
    {
        Console.WriteLine($"NODE ComplianceValidation INICIO auditoria={input.AuditoriaId} artefactos={input.Artefactos.Count}"); //test

        var paraLlm = input.Artefactos
            .Where(EsCandidatoLlm)
            .ToList();

        if (paraLlm.Count == 0)
        {
            return "No hay artefactos para analizar. " +
                   "Respondé exactamente: {\"hallazgos\": []}";
        }

        var lineas = paraLlm.Select(a =>
            $"--- ARTEFACTO ID: {a.ArtefactoEsperadoId} ---\n" +
            $"  Nombre                  : {a.NombreArtefacto}\n" +
            $"  Codigo                  : {a.CodigoArtefacto ?? "(sin código formal)"}\n" +
            $"  UrlReferenciaTailoring  : {a.UrlReferencia}\n" +
            $"  NombreArchivo encontrado: {a.DocumentoEncontrado!.NombreArchivo}\n");

        return
            "Analizá los siguientes artefactos para detectar INCOHERENCIAS EVIDENTES " +
            "entre la URL declarada en el tailoring y el archivo encontrado.\n\n" +
            "ARTEFACTOS A ANALIZAR:\n\n" +
            string.Join("\n", lineas) +
            "\nSi no hay incoherencias evidentes, respondé {\"hallazgos\": []}.\n" +
            "No inventes hallazgos sobre material que no tengas información para juzgar.\n\n" +
            "El campo 'artefactoEsperadoId' debe ser uno de los IDs listados arriba.\n" +
            "El campo 'origenRegla' debe ser exactamente 'Tailoring'.\n\n" +
            "Respondé ÚNICAMENTE con este JSON, sin texto adicional ni backticks:\n" +
            "{\n" +
            "  \"hallazgos\": [\n" +
            "    {\n" +
            "      \"artefactoEsperadoId\": <int>,\n" +
            "      \"descripcion\": \"Incoherencia entre URL del tailoring y archivo encontrado\",\n" +
            "      \"justificacion\": \"El tailoring declara URL '<url>' pero se encontró '<archivo>', que no corresponde al artefacto.\",\n" +
            "      \"origenRegla\": \"Tailoring\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";
    }

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        var hallazgos = HallazgosDeterministicos.Generar(input);

        var idsExpuestos = input.Artefactos
            .Where(EsCandidatoLlm)
            .Select(a => a.ArtefactoEsperadoId)
            .ToHashSet();

        if (idsExpuestos.Count > 0)
        {
            var hallazgosLlm = ComplianceHallazgosPreliminaresParser.Parsear(
                textoLlm,
                idsExpuestos);

            hallazgos.AddRange(hallazgosLlm);
        }

        //return new HallazgosPreliminares(
        //    input.AuditoriaId,
        //    AgenteOrigen.ComplianceValidation,
        //    hallazgos);
        var resultado = new HallazgosPreliminares(
            input.AuditoriaId,
            AgenteOrigen.ComplianceValidation,
            hallazgos);

        Console.WriteLine($"NODE ComplianceValidation FIN auditoria={resultado.AuditoriaId} hallazgos={resultado.Hallazgos.Count}");

        return resultado;
    }

    private static bool EsCandidatoLlm(ArtefactoExtraido a) =>
        a.Exigibilidad == ExigibilidadArtefacto.Exigible
        && a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.Aplica
        && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado
        && !string.IsNullOrWhiteSpace(a.UrlReferencia)
        && a.DocumentoEncontrado is not null
        && !string.IsNullOrWhiteSpace(a.DocumentoEncontrado.NombreArchivo);
}

// ============================================================================
//  [4] ConsistencyVerificationNode — agente con LLM. Contrato 3 INTACTO.
//  No valida firmas ni vigencia por fecha.
// ============================================================================

public sealed class ConsistencyVerificationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    public ConsistencyVerificationNode(AIAgent agente)
        : base("ConsistencyVerification", agente) { }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
    {
        Console.WriteLine($"NODE ConsistencyVerification INICIO auditoria={input.AuditoriaId} artefactos={input.Artefactos.Count}"); //test
        // Filtrar a los artefactos analizables: Exigibles + Encontrados.
        // - PendienteEtapaFutura: no se analiza, no se ha encontrado aún.
        // - Faltante: si no está, no hay nada que verificar (es trabajo de
        //   ComplianceValidation generar el hallazgo NC, no nuestro).
        // - NoBuscado: idem.
        // Este filtrado define el conjunto de IDs que el LLM verá. ParsearRespuesta
        // debe replicar exactamente este mismo criterio para validar la respuesta.
        var analizables = input.Artefactos
            .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible
                     && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado)
            .ToList();

        // Caso especial: nada para analizar. El LLM se llama igual con prompt
        // mínimo (no podemos cortocircuitar sin overridear HandleAsync, y la
        // asimetría con los otros nodos LLM no se justifica solo por ahorrar
        // un call corto). ParsearRespuesta blinda este caso devolviendo lista
        // vacía sin importar lo que diga el LLM.
        if (analizables.Count == 0)
        {
            return "No hay artefactos para analizar. " +
                   "Respondé exactamente: {\"hallazgos\": []}";
        }

        var resumen = DocumentSummaryBuilder.Construir(analizables);

        // El prompt se LIMITA a lo que el resumen efectivamente expone. El
        // builder muestra: nombre, código, obligatoriedad, archivo, fuente y
        // SeccionesDetectadas con flag TieneContenido. NADA MÁS. Por eso el
        // prompt sólo pide detectar "secciones vacías" — pedir más sería
        // inducir alucinación porque el LLM no tiene los datos para razonar.
        //
        // TODO Chat D / futura iteración: para detectar "sección esperada por
        // el template que NO aparece en SeccionesDetectadas" haría falta que
        // el contrato 3 exponga las secciones esperadas del template, no solo
        // PathTemplateAbsoluto. Eso es un cambio de contrato fuera de Chat C.
        //
        // TODO Chat D / futura iteración: para detectar "inconsistencias entre
        // artefactos" (responsables, referencias cruzadas) haría falta que el
        // builder incluya contenido de las secciones, no solo flags. Decisión
        // pendiente con el cliente.
        return
            "Sos un auditor experto en ISO 9001 y en el procedimiento PR 11-13 de BDT Global.\n\n" +
            "Analizá los siguientes artefactos de un proyecto de desarrollo de software " +
            "y detectá UN ÚNICO tipo de problema: secciones que existen en el documento " +
            "pero están VACIAS (marcadas como VACIA en el resumen).\n\n" +
            "ARTEFACTOS A ANALIZAR:\n" +
            resumen + "\n\n" +
            "QUÉ DEBÉS DETECTAR:\n" +
            "- Únicamente secciones marcadas como VACIA en el resumen. Cada sección " +
            "vacía representa un problema potencial: la sección existe en el documento " +
            "como título pero no tiene contenido.\n\n" +
            "QUÉ NO DEBÉS HACER:\n" +
            "- NO inventes hallazgos sobre contenido que NO VES en el resumen. Solo tenés\n" +
            "  títulos de secciones y un flag de tener contenido o no. NO podés juzgar:\n" +
            "  * calidad del contenido;\n" +
            "  * consistencia entre documentos (no ves contenido);\n" +
            "  * responsables o datos puntuales (no ves contenido);\n" +
            "  * si falta una sección que el template esperaría (no ves el template);\n" +
            "  * fechas o vigencia (consultas_cliente.md consulta 3);\n" +
            "  * firmas (consultas_cliente.md consulta 2 — BDT no usa firmas);\n" +
            "  * cruce con Trello o Clockify (lectura_dominio_bdt.md §3.7 — prohibido).\n" +
            "- NO emitas hallazgos sobre artefactos que no aparezcan en el resumen.\n" +
            "- Si NO hay secciones VACIAS, respondé con lista vacía.\n\n" +
            "El campo 'artefactoEsperadoId' debe ser uno de los IDs listados arriba. " +
            "El campo 'origenRegla' debe ser exactamente 'Template' (las secciones del " +
            "template son la fuente de la regla).\n\n" +
            "Respondé ÚNICAMENTE con este JSON (sin texto antes ni después, sin backticks):\n" +
            "{\n" +
            "  \"hallazgos\": [\n" +
            "    {\n" +
            "      \"artefactoEsperadoId\": <int>,\n" +
            "      \"descripcion\": \"Sección '<nombre de sección>' del documento " +
            "está vacía\",\n" +
            "      \"justificacion\": \"La sección está presente como título en el " +
            "documento pero no contiene texto. Según el template, esta sección debe " +
            "tener contenido.\",\n" +
            "      \"origenRegla\": \"Template\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";
    }

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        // Replicar el filtrado de ConstruirPrompt: los IDs expuestos al LLM
        // son los Exigible + Encontrado. Acopla los dos métodos pero el
        // criterio es trivial y vivir con la duplicación es preferible a
        // mantener estado mutable en el nodo (rompería el patrón base).
        var idsExpuestos = input.Artefactos
            .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible
                     && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado)
            .Select(a => a.ArtefactoEsperadoId)
            .ToHashSet();

        // Caso especial cortocircuito: si no había analizables, devolver vacío
        // sin importar lo que diga el LLM. Defensa contra LLM que emita
        // hallazgos espurios pese al prompt explícito que le pide vacío.
        if (idsExpuestos.Count == 0)
        {
            return new HallazgosPreliminares(
                input.AuditoriaId,
                AgenteOrigen.ConsistencyVerification,
                Array.Empty<HallazgoPreliminar>());
        }
        Console.WriteLine($"NODE ConsistencyVerification FIN auditoria={input.AuditoriaId}");//test
        return HallazgosPreliminaresParser.Parsear(textoLlm, idsExpuestos, input.AuditoriaId);
    }
}

// ============================================================================
//  [5] FindingsClassificationNode — agente con LLM, TRES entradas cacheadas
// ----------------------------------------------------------------------------
//  Recibe tres mensajes, por aristas distintas y en momentos distintos:
//    - DocumentosExtraidos        edge directo desde DocumentAnalysis [2]
//    - HallazgosPreliminares (de ComplianceValidation)    fan-in desde [3]
//    - HallazgosPreliminares (de ConsistencyVerification) fan-in desde [4]
//
//  DISEÑO DE CACHEO — robusto ante la incógnita del barrier:
//  No se asume que AddFanInBarrierEdge agrupe los dos HallazgosPreliminares en
//  una lista. Se asume el caso conservador: el barrier entrega los mensajes
//  DE A UNO. El handler de HallazgosPreliminares cachea cada lote por separado,
//  distinguiéndolos por su AgenteOrigen. La clasificación se dispara recién
//  cuando están los TRES elementos cacheados (los 2 lotes + DocumentosExtraidos).
//  En runtime, los HallazgosPreliminares llegan como mensajes individuales.
//  El nodo cachea cada lote por AgenteOrigen hasta tener ambos.
//
//  Distinguir los lotes por AgenteOrigen NO es "validar la estructura del
//  grafo": es mecánica necesaria del cacheo. Sin distinguirlos no se puede
//  saber si llegaron dos lotes distintos o el mismo dos veces.
//
//  Estado interno: SEGURO solo si se instancia un nodo nuevo por ejecución de
//  workflow. NO registrar como Singleton. El worker arma el workflow fresco
//  por auditoría -> garantizado.
//
//  El LLM clasifica los hallazgos. NO ve ni produce DocumentosExtraidos: lo
//  conserva este executor C# y lo pega en ResultadoClasificacionConContexto.
// ============================================================================

// Declara que este executor emite ResultadoClasificacionConContexto.

[SendsMessage(typeof(ResultadoClasificacionConContexto))]
public sealed partial class FindingsClassificationNode : Executor
{
    private readonly AIAgent _agente;

    // Los tres elementos a cachear antes de poder clasificar.
    private DocumentosExtraidos? _contextoDocumentos;
    private HallazgosPreliminares? _hallazgosCompliance;
    private HallazgosPreliminares? _hallazgosConsistency;

    public FindingsClassificationNode(AIAgent agente)
        : base("FindingsClassification")
    {
        _agente = agente;
    }

    // --- Entrada A: contexto, por edge directo desde DocumentAnalysis -------
    [MessageHandler]
    private async ValueTask HandleContextoAsync(
        DocumentosExtraidos message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (_contextoDocumentos is not null)
        {
            throw new InvalidOperationException(
                "FindingsClassification recibió DocumentosExtraidos más de " +
                "una vez. Revisar el cableado del grafo.");
        }

        _contextoDocumentos = message;
        await IntentarClasificarAsync(context);

        Console.WriteLine($"NODE FindingsClassification RECIBE contexto auditoria={message.AuditoriaId} artefactos={message.Artefactos.Count}");//test
    }

    // --- Entrada B: cada lote de hallazgos, de a uno ------------------------
    [MessageHandler]
    private async ValueTask HandleHallazgosAsync(
        HallazgosPreliminares message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"NODE FindingsClassification RECIBE hallazgos auditoria={message.AuditoriaId} origen={message.AgenteOrigen} count={message.Hallazgos.Count}");//test

        // Distinguir el lote por su AgenteOrigen y cachearlo en su slot.
        switch (message.AgenteOrigen)
        {
            case AgenteOrigen.ComplianceValidation:
                if (_hallazgosCompliance is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ComplianceValidation. Se esperaba uno solo.");
                }
                _hallazgosCompliance = message;
                break;

            case AgenteOrigen.ConsistencyVerification:
                if (_hallazgosConsistency is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ConsistencyVerification. Se esperaba uno solo.");
                }
                _hallazgosConsistency = message;
                break;

            default:
                throw new InvalidOperationException(
                    $"AgenteOrigen no contemplado en el fan-in: " +
                    $"{message.AgenteOrigen}.");
        }

        await IntentarClasificarAsync(context);
    }

    // --- Disparo: clasifica cuando están los TRES elementos -----------------
    private async ValueTask IntentarClasificarAsync(IWorkflowContext context)
    {
        Console.WriteLine($"NODE FindingsClassification ESPERA contexto={_contextoDocumentos is not null} compliance={_hallazgosCompliance is not null} consistency={_hallazgosConsistency is not null}");//test
        // Falta alguno de los tres -> esperar al próximo mensaje.
        if (_contextoDocumentos is null
            || _hallazgosCompliance is null
            || _hallazgosConsistency is null)
        {
            return;
        }

        // Aserción de invariante: los tres elementos de la misma auditoría.
        int idContexto = _contextoDocumentos.AuditoriaId;
        if (_hallazgosCompliance.AuditoriaId != idContexto
            || _hallazgosConsistency.AuditoriaId != idContexto)
        {
            throw new InvalidOperationException(
                "AuditoriaId inconsistente entre los lotes de hallazgos y el " +
                "contexto recibidos por FindingsClassification.");
        }

        var lotes = new[] { _hallazgosCompliance, _hallazgosConsistency };

        // 1. Prompt de clasificación.
        string prompt = ConstruirPrompt(lotes);

        // 2 + 3. El LLM clasifica.
        // El handler actual no propaga CancellationToken hasta este punto.
        var respuesta = await _agente.RunAsync(prompt);
        string textoLlm = respuesta.Text ?? string.Empty;

        // 4. Parsear la clasificación.
        HallazgosClasificados clasificacion = ParsearRespuesta(lotes, textoLlm);

        // 5. Combinar con el contexto conservado (no pasó por el LLM).
        var salida = new ResultadoClasificacionConContexto(
            Clasificacion: clasificacion,
            ContextoDocumentos: _contextoDocumentos);

        Console.WriteLine($"NODE FindingsClassification FIN auditoria={salida.Clasificacion.AuditoriaId} hallazgos={salida.Clasificacion.Hallazgos.Count}");//test

        await context.SendMessageAsync(salida, CancellationToken.None);

        // Higiene defensiva: liberar el estado cacheado.
        _contextoDocumentos = null;
        _hallazgosCompliance = null;
        _hallazgosConsistency = null;
    }

    private string ConstruirPrompt(IReadOnlyList<HallazgosPreliminares> hallazgos)
    {
        // Aplanamos los lotes en una lista plana con indice estable.
        // El indice es la unidad de identidad para el LLM: cada HallazgoPreliminar
        // tiene su propio indice, independiente del ArtefactoEsperadoId, porque
        // puede haber múltiples preliminares para el mismo artefacto.
        // El contrato 5 (contratos_agentes.md, invariante línea 307) establece
        // que la correspondencia 1-a-1 es por HallazgoPreliminar, no por artefacto.
        var preliminaresPlanos = hallazgos
            .SelectMany(lote => lote.Hallazgos)
            .Select((h, i) => (indice: i, hallazgo: h))
            .ToList();

        var lineas = preliminaresPlanos.Select(p =>
            $"indice: {p.indice}\n" +
            $"  artefactoEsperadoId: {p.hallazgo.ArtefactoEsperadoId}\n" +
            $"  Descripcion: {p.hallazgo.Descripcion}\n" +
            $"  Justificacion: {p.hallazgo.Justificacion}\n" +
            $"  OrigenRegla: {p.hallazgo.OrigenRegla}");

        return
            "Clasificá cada hallazgo en NC, OBS u OM según las reglas del PR 11-13.\n" +
            "Cada hallazgo viene identificado por un INDICE estable (no por " +
            "artefactoEsperadoId, porque puede haber múltiples hallazgos sobre el " +
            "mismo artefacto).\n" +
            $"Recibís {preliminaresPlanos.Count} hallazgos. Devolvé exactamente " +
            $"{preliminaresPlanos.Count} objetos, uno por cada indice.\n" +
            "No inventes, no omitas, no fusiones.\n\n" +
            string.Join("\n\n", lineas) +
            "\n\nSOLO el array JSON. Sin texto adicional. Sin backticks.\n" +
            "[{ \"indice\": <int>, \"tipo\": \"NC\"|\"OBS\"|\"OM\", " +
            "\"justificacion\": \"<regla aplicada y por qué>\" }]";
    }

    private HallazgosClasificados ParsearRespuesta(
        IReadOnlyList<HallazgosPreliminares> hallazgos, string textoLlm)
    {
        // Aplanamos los lotes en la MISMA forma que ConstruirPrompt: misma lista,
        // mismo orden, mismos índices. El indice 0..N-1 es la identidad del
        // hallazgo para el LLM y para el parser.
        //
        // El AgenteOrigen vive en el raíz del lote (contrato 4); lo "bajamos" a
        // nivel de hallazgo individual para que el parser pueda emitir
        // HallazgoClasificado con su AgenteOrigen correcto.
        var preliminaresPlanos = hallazgos
            .SelectMany(lote => lote.Hallazgos
                .Select(h => (Hallazgo: h, Origen: lote.AgenteOrigen)))
            .ToList();

        // AuditoriaId ya validado en IntentarClasificarAsync — los tres lotes
        // comparten Id. Tomamos el primero por conveniencia.
        var auditoriaId = hallazgos[0].AuditoriaId;

        return ClasificacionResponseParser.Parsear(textoLlm, preliminaresPlanos, auditoriaId);
    }

    // ------------------------------------------------------------------------
    //  MAF: registro explícito del protocolo para el nodo con múltiples entradas.
    //  FindingsClassification recibe:
    //   - DocumentosExtraidos
    //   - HallazgosPreliminares
    //
    //  También declara que emite ResultadoClasificacionConContexto.
    //  Esto evita depender del ruteo implícito por atributos.
    // ------------------------------------------------------------------------
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder
            .SendsMessage<ResultadoClasificacionConContexto>()
            .ConfigureRoutes(routes =>
            {
                routes.AddHandler<DocumentosExtraidos>(HandleContextoAsync);
                routes.AddHandler<HallazgosPreliminares>(HandleHallazgosAsync);
            });
    }
}

// ============================================================================
//  [6] ConsolidadorResultadoNode — nodo determinista final, sin LLM
// ----------------------------------------------------------------------------
//  Executor<TIn,TOut> simple: una sola entrada. Sin cache, sin estado mutable.
//  Ensambla el AuditoriaResultado. Lógica en ConsolidadorResultado.Ensamble.cs.
// ============================================================================

public sealed class ConsolidadorResultadoNode
    : Executor<ResultadoClasificacionConContexto, AuditoriaResultado>
{
    public ConsolidadorResultadoNode() : base("ConsolidadorResultado") { }

    public override ValueTask<AuditoriaResultado> HandleAsync(
        ResultadoClasificacionConContexto message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        Console.WriteLine($"NODE ConsolidadorResultado INICIO auditoria={message.Clasificacion.AuditoriaId}");//test

        var resultado = ConsolidadorEnsamble.Ensamblar(
            message.ContextoDocumentos, message.Clasificacion);
        Console.WriteLine($"NODE ConsolidadorResultado FIN auditoria={resultado.AuditoriaId}");//test
        return ValueTask.FromResult(resultado);
    }
}
