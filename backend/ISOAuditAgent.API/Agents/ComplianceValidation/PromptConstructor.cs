using System.Text;
using System.Text.Json;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

/// <summary>
/// Construye el prompt dinámico que será enviado al LLM (OpenAI/Gemini).
/// El prompt incluye contexto, reglas, datos y guardrails específicos.
/// </summary>
internal class PromptConstructor
{
    private readonly ContextoValidacion _contexto;

    public PromptConstructor(ContextoValidacion contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    /// <summary>
    /// Construye el prompt completo para análisis de inconsistencias.
    /// El tailoring dinámico tiene PRIORIDAD ABSOLUTA sobre cualquier instrucción genérica.
    /// </summary>
    public string ConstruirPrompt()
    {
        var sb = new StringBuilder();

        // === SECCIÓN 1: Instrucciones principales minimalistas ===
        sb.AppendLine("## VALIDACIÓN DE DOCUMENTOS - TAILORING DINÁMICO");
        sb.AppendLine();
        sb.AppendLine("### OBJETIVO");
        sb.AppendLine("Analizar los documentos y datos proporcionados ÚNICAMENTE de acuerdo a las reglas específicas del proyecto.");
        sb.AppendLine();

        // === SECCIÓN 2: Guardrails sobre estructura JSON ===
        sb.AppendLine("### REQUISITOS DE SALIDA");
        sb.AppendLine();
        sb.AppendLine("**Estructura JSON ESTRICTA:**");
        sb.AppendLine("Tu respuesta DEBE ser un JSON con esta estructura exacta:");
        sb.AppendLine("{");
        sb.AppendLine("  \"hallazgos\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"descripcion\": \"string\",");
        sb.AppendLine("      \"procesoId\": null,");
        sb.AppendLine("      \"evidencias\": [{\"tipo\", \"idFuente\", \"descripcion\", \"datosJSON\"}],");
        sb.AppendLine("      \"justificacion\": \"string\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("⚠️ CRÍTICO para datosJSON: DEBE SER UN STRING SERIALIZADO (JSON escapado con backslashes).");
        sb.AppendLine("❌ INCORRECTO: \"datosJSON\": {\"clave\": \"valor\"} (objeto JSON real)");
        sb.AppendLine("✅ CORRECTO: \"datosJSON\": \"{\\\"clave\\\": \\\"valor\\\"}\" (string con escape)");
        sb.AppendLine();
        sb.AppendLine("**Prohibiciones:**");
        sb.AppendLine("- NO incluyas clasificaciones (Severidad, Tipo, Riesgo, Impacto)");
        sb.AppendLine("- NO incluyas campos adicionales");
        sb.AppendLine("- NO etiquetes como 'No Conformidad' u 'Observación'");
        sb.AppendLine();

        // === SECCIÓN 3: Documentos para análisis ===
        sb.AppendLine("### DOCUMENTOS PARA ANÁLISIS");
        sb.AppendLine();
        sb.AppendLine("Recibirás los documentos y datos en la variable {{$documentos}} (formato JSON).");
        sb.AppendLine("Estos contienen toda la información que necesitas analizar.");
        sb.AppendLine();

        // === SECCIÓN 4: TAILORING DINÁMICO CON PRIORIDAD ABSOLUTA ===
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### ⚡ REGLAS DE VALIDACIÓN - AUTORIDAD ABSOLUTA");
        sb.AppendLine();
        sb.AppendLine("🔴 ATENCIÓN CRÍTICA 🔴");
        sb.AppendLine("Las siguientes REGLAS DE TAILORING tienen PRIORIDAD ABSOLUTA sobre cualquier otro criterio.");
        sb.AppendLine("Si el tailoring permite una acción (ej: 'artefacto no exigible en esta etapa'), DEBES IGNORAR cualquier estándar general");
        sb.AppendLine("de auditoría o de la norma ISO 9001 que diga lo contrario.");
        sb.AppendLine();
        sb.AppendLine("{{$reglasTailoring}}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // === SECCIÓN 5: INSTRUCCIÓN FINAL CRÍTICA ===
        sb.AppendLine("### INSTRUCCIÓN FINAL");
        sb.AppendLine("Analiza los documentos {{$documentos}} ÚNICAMENTE según las reglas de tailoring proporcionadas.");
        sb.AppendLine();
        sb.AppendLine("INSTRUCCIÓN CRÍTICA DE FORMATO: Tu respuesta debe ser ÚNICA Y EXCLUSIVAMENTE un objeto JSON válido con la propiedad hallazgos (un array). NO agregues texto introductorio ni uses bloques de código. Por cada hallazgo detectado, el objeto dentro del array DEBE tener exactamente esta estructura:");
        sb.AppendLine("- artefactoEsperadoId: (entero) El ID numérico exacto del artefacto que presenta la falla, extraído del JSON de entrada.");
        sb.AppendLine("- descripcion: (string) Qué es exactamente lo que incumple.");
        sb.AppendLine("- justificacion: (string) Por qué se considera un incumplimiento basándose en la evidencia.");
        sb.AppendLine("- origenRegla: (string) Usa obligatoriamente el valor \"Tailoring\" si incumple el estado de tailoring, o \"Procedimiento\" si es una regla general.");
        sb.AppendLine();
        sb.AppendLine("Si no hay hallazgos, devuelve {\"hallazgos\": []}.");

        return sb.ToString();
    }
}
