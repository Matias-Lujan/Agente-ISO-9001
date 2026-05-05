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
    /// </summary>
    public string ConstruirPrompt()
    {
        var sb = new StringBuilder();

        // === SECCIÓN 1: Instrucciones generales ===
        sb.AppendLine("## AGENTE DE VALIDACIÓN DE PROCESOS ISO 9001");
        sb.AppendLine();
        sb.AppendLine("### INSTRUCCIONES PRINCIPALES");
        sb.AppendLine("Tu tarea es analizar inconsistencias entre tareas planificadas en Trello y registros de tiempo en Clockify.");
        sb.AppendLine("Debes detectar ÚNICAMENTE desvíos, omisiones y anomalías.");
        sb.AppendLine();

        // === SECCIÓN 2: Guardrails críticos ===
        sb.AppendLine("### ⚠️ GUARDRAILS CRÍTICOS (LEER OBLIGATORIAMENTE)");
        sb.AppendLine("1. **PROHIBICIÓN ABSOLUTA DE CLASIFICACIONES**: No debes incluir ningún campo de clasificación.");
        sb.AppendLine("   - ❌ NO incluyas 'Severidad', 'Tipo de No Conformidad', 'Riesgo' o 'Impacto'.");
        sb.AppendLine("   - ❌ NO etiquetes hallazgos como 'No Conformidad', 'Observación' u 'Oportunidad de Mejora'.");
        sb.AppendLine("   - ✓ Tu única responsabilidad es DETECTAR las inconsistencias de forma objetiva.");
        sb.AppendLine("   - ✓ La clasificación de hallazgos es EXCLUSIVAMENTE responsabilidad del Agente de Clasificación.");
        sb.AppendLine();
        sb.AppendLine("2. **ESTRUCTURA JSON ESTRICTA**: Retorna un JSON con SOLO estos campos:");
        sb.AppendLine("   - descripcion (string): Descripción del desvío encontrado");
        sb.AppendLine("   - fuente (string): Origen de las fuentes comparadas (ej: 'Trello vs Clockify')");
        sb.AppendLine("   - idTareaRelacionada (string|null): ID de la tarea relacionada si existe");
        sb.AppendLine("   - reglaIncumplida (string|null): Descripción breve de la regla incumplida");
        sb.AppendLine("   - detallesJSON (string): JSON con información técnica del hallazgo");
        sb.AppendLine("   - NO INCLUYAS: severidad, tipo, clasificación, riesgo ni campos adicionales.");
        sb.AppendLine();
        sb.AppendLine("3. **SOLO DATOS OBJETIVOS**: Basa tus análisis en hechos concretos:");
        sb.AppendLine("   - Comparación de IDs, fechas, responsables.");
        sb.AppendLine("   - Presencia o ausencia de registros.");
        sb.AppendLine("   - Coherencia de datos entre fuentes.");
        sb.AppendLine();

        // === SECCIÓN 3: Reglas de validación ===
        sb.AppendLine("### REGLAS DE VALIDACIÓN A APLICAR");
        sb.AppendLine();
        sb.AppendLine("#### Reglas OBLIGATORIAS (siempre validar):");
        foreach (var regla in _contexto.ReglasObligatorias)
        {
            sb.AppendLine($"- **Regla #{regla.Id}**: {regla.Descripcion}");
            sb.AppendLine($"  - Criterio: {regla.CriterioEvaluacion}");
            if (!string.IsNullOrEmpty(regla.Notas))
            {
                sb.AppendLine($"  - Nota: {regla.Notas}");
            }
            sb.AppendLine();
        }

        if (_contexto.ReglasOpcionales.Count > 0)
        {
            sb.AppendLine("#### Reglas OPCIONALES (validar cuando apliquen):");
            foreach (var regla in _contexto.ReglasOpcionales)
            {
                sb.AppendLine($"- **Regla #{regla.Id}**: {regla.Descripcion}");
                sb.AppendLine($"  - Criterio: {regla.CriterioEvaluacion}");
                if (!string.IsNullOrEmpty(regla.Notas))
                {
                    sb.AppendLine($"  - Nota: {regla.Notas}");
                }
                sb.AppendLine();
            }
        }

        // === SECCIÓN 4: Datos de Trello ===
        sb.AppendLine("### DATOS DE TRELLO (Tareas Planificadas)");
        sb.AppendLine("```json");
        sb.AppendLine(JsonSerializer.Serialize(_contexto.TareasTrello, new JsonSerializerOptions { WriteIndented = false }));
        sb.AppendLine("```");
        sb.AppendLine();

        // === SECCIÓN 5: Datos de Clockify ===
        sb.AppendLine("### DATOS DE CLOCKIFY (Registros de Tiempo)");
        sb.AppendLine("```json");
        sb.AppendLine(JsonSerializer.Serialize(_contexto.RegistrosClockify, new JsonSerializerOptions { WriteIndented = false }));
        sb.AppendLine("```");
        sb.AppendLine();

        // === SECCIÓN 6: Instrucciones de análisis específico ===
        sb.AppendLine("### ANÁLISIS A REALIZAR");
        sb.AppendLine();
        sb.AppendLine("1. **Registros Huérfanos en Clockify**:");
        sb.AppendLine("   - Identifica registros (CLO-xxx) cuyo IdTarea no existe en Trello.");
        sb.AppendLine();
        sb.AppendLine("2. **Discrepancia de Responsables**:");
        sb.AppendLine("   - Compara el campo 'Usuario' en Clockify con 'Responsable' en Trello para la misma tarea.");
        sb.AppendLine("   - Si difieren, es una inconsistencia.");
        sb.AppendLine();
        sb.AppendLine("3. **Registros Fuera de Plazo**:");
        sb.AppendLine("   - Verifica que FechaInicio del registro <= FechaVencimiento de la tarea.");
        sb.AppendLine();
        sb.AppendLine("4. **Tareas Completadas sin Registros**:");
        sb.AppendLine("   - Si Estado='Completado', debe haber al menos un registro en Clockify.");
        sb.AppendLine();
        sb.AppendLine("5. **Incoherencias en Horas**:");
        sb.AppendLine("   - Suma de horas por tarea > límites razonables (ej: > 80 horas).");
        sb.AppendLine();
        sb.AppendLine("6. **Registros Recientes para Tareas Activas**:");
        sb.AppendLine("   - Si Estado='En Progreso', debería haber registros recientes (últimos 3 días).");
        sb.AppendLine();

        // === SECCIÓN 7: Estructura de salida ===
        sb.AppendLine("### ESTRUCTURA JSON DE SALIDA (SIN CLASIFICACIONES)");
        sb.AppendLine("Retorna ÚNICAMENTE un array JSON con la siguiente estructura para CADA hallazgo:");
        sb.AppendLine("⚠️ IMPORTANTE: NO incluyas campos de severidad, clasificación ni riesgo.");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"descripcion\": \"Descripción detallada del desvío encontrado\",");
        sb.AppendLine("    \"fuente\": \"Trello vs Clockify\",");
        sb.AppendLine("    \"idTareaRelacionada\": \"TRE-001 o null\",");
        sb.AppendLine("    \"reglaIncumplida\": \"Descripción breve de la regla incumplida\",");
        sb.AppendLine("    \"detallesJSON\": \"{JSON con información específica del hallazgo}\"");
        sb.AppendLine("  }");
        sb.AppendLine("]");
        sb.AppendLine("```");
        sb.AppendLine();

        // === SECCIÓN 8: Ejemplo ===
        sb.AppendLine("### EJEMPLO DE HALLAZGO (SIN SEVERIDAD)");
        sb.AppendLine("```json");
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"descripcion\": \"La tarea TRE-001 está asignada a 'Juan Pérez' en Trello, pero en Clockify hay registros de 'Carlos López' para la misma tarea.\",");
        sb.AppendLine("    \"fuente\": \"Trello vs Clockify\",");
        sb.AppendLine("    \"idTareaRelacionada\": \"TRE-001\",");
        sb.AppendLine("    \"reglaIncumplida\": \"El responsable en Trello debe ser coherente con quien registra tiempo en Clockify\",");
        sb.AppendLine("    \"detallesJSON\": \"{\\\"responsableTrello\\\":\\\"Juan Pérez\\\",\\\"usuarioClockify\\\":\\\"Carlos López\\\",\\\"registroId\\\":\\\"CLO-007\\\"}\"");
        sb.AppendLine("  }");
        sb.AppendLine("]");
        sb.AppendLine("```");
        sb.AppendLine();

        // === SECCIÓN 9: Contexto final ===
        sb.AppendLine("### CONTEXTO DEL ANÁLISIS");
        sb.AppendLine($"- **Proceso ID**: {_contexto.ProcesoId}");
        sb.AppendLine($"- **Proyecto ID**: {_contexto.ProjectId}");
        sb.AppendLine($"- **Fecha de Análisis**: {_contexto.FechaAnalisis:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Total Tareas Trello**: {_contexto.TareasTrello.Count}");
        sb.AppendLine($"- **Total Registros Clockify**: {_contexto.RegistrosClockify.Count}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("**AHORA PROCEDE CON EL ANÁLISIS Y RETORNA SOLO EL JSON CON LOS HALLAZGOS.**");

        return sb.ToString();
    }
}
