using System.Text.Json;
using ISOAuditAgent.API.Agents.FindingsClassification.Contracts;
using ISOAuditAgent.API.Models;
using Microsoft.Extensions.Logging;

namespace ISOAuditAgent.API.Agents.FindingsClassification.Services;

/// <summary>
/// Convierte el JSON de Gemini en HallazgosClasificados tipados.
///
/// AgenteOrigen viene del raíz de HallazgosPreliminares — no de cada hallazgo.
/// El executor lo resuelve antes de llamar al parser pasando un diccionario.
/// </summary>
public static class ClasificacionResponseParser
{
    /// <summary>
    /// Parsea el JSON de Gemini.
    ///
    /// origenPorArtefacto: diccionario ArtefactoEsperadoId → (HallazgoPreliminar, AgenteOrigen)
    /// construido por el executor a partir de los dos grupos de HallazgosPreliminares.
    /// </summary>
    public static HallazgosClasificados Parsear(
        string respuestaLlm,
        IReadOnlyDictionary<int, (HallazgoPreliminar Hallazgo, AgenteOrigen Origen)> origenPorArtefacto,
        int auditoriaId,
        ILogger? logger = null)
    {
        try
        {
            var jsonLimpio = respuestaLlm
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var doc = JsonDocument.Parse(jsonLimpio);
            var resultado = new List<HallazgoClasificado>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var artefactoId = item.TryGetProperty("artefactoEsperadoId", out var aid)
                    ? aid.GetInt32() : 0;
                var tipoStr = item.GetProperty("tipo").GetString() ?? string.Empty;
                var justificacion = item.GetProperty("justificacion").GetString() ?? string.Empty;

                if (!origenPorArtefacto.TryGetValue(artefactoId, out var entrada))
                {
                    logger?.LogWarning(
                        "ArtefactoEsperadoId {ArtefactoId} no encontrado en el input. Se descarta.",
                        artefactoId);
                    continue;
                }

                var hallazgoOriginal = entrada.Hallazgo;
                var agenteOrigen = entrada.Origen;

                resultado.Add(new HallazgoClasificado(
                    hallazgoOriginal.ArtefactoEsperadoId,
                    ParsearTipo(tipoStr, hallazgoOriginal.OrigenRegla, logger),
                    hallazgoOriginal.Descripcion,
                    justificacion,
                    agenteOrigen
                ));
            }

            return new HallazgosClasificados(auditoriaId, resultado.AsReadOnly());
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "JSON inválido de Gemini al clasificar hallazgos.");
            return new HallazgosClasificados(auditoriaId, Array.Empty<HallazgoClasificado>());
        }
    }

    /// <summary>
    /// Convierte el string de tipo al enum TipoHallazgo.
    /// Aplica la regla: si OrigenRegla != Procedimiento → máximo OM.
    /// </summary>
    private static TipoHallazgo ParsearTipo(string tipo, OrigenRegla origenRegla, ILogger? logger)
    {
        var tipoParseado = tipo switch
        {
            "NC" => TipoHallazgo.NC,
            "OBS" => TipoHallazgo.OBS,
            "OM" => TipoHallazgo.OM,
            _ => TipoHallazgo.OBS
        };

        // Regla de negocio: si la regla no viene del Procedimiento → máximo OM
        if (origenRegla != OrigenRegla.Procedimiento && tipoParseado == TipoHallazgo.NC)
        {
            logger?.LogInformation(
                "NC degradado a OM por OrigenRegla={OrigenRegla}", origenRegla);
            return TipoHallazgo.OM;
        }

        return tipoParseado;
    }
}
