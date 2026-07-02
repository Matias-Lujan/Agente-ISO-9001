// ============================================================================
//  TrelloChecker — Verifica evidencia de artefactos en Trello (D6.Trello)
// ----------------------------------------------------------------------------
//  El board ID se extrae de la URL del tailoring (urlRef). No se usa
//  proyectos.trello_board_id.
//
//  Parseo de URL:
//   https://trello.com/b/{boardId}/{slug}
//   Regex: trello\.com/b/([A-Za-z0-9]{8})(?:[/?#]|$)
//   - Captura exactamente 8 chars alfanuméricos (formato estándar de Trello).
//   - Anclado con (?:[/?#]|$) para no capturar segmentos más largos.
//   - URL ausente, mal formada o sin board ID → NoEncontrado (NC via Compliance).
//
//  Criterio de conformidad (cerrado):
//   Board existe (no cerrado) Y tiene ≥ 2 tarjetas con título no vacío.
//
//  Resiliencia: cualquier fallo de API (timeout/auth/rate-limit/red caída)
//   → NoEncontrado + log warning. Nunca propaga al workflow.
// ============================================================================

using System.Text.RegularExpressions;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;

public sealed class TrelloChecker
{
    private const int MinTarjetasValidas = 2;

    private static readonly Regex BoardIdRegex = new(
        @"trello\.com/b/([A-Za-z0-9]{8})(?:[/?#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly VerificacionFisica NoEncontrado = new(
        Encontrado: false,
        Fuente: FuenteDocumento.Trello,
        NombreArchivo: null,
        HashContenido: null,
        Secciones: Array.Empty<SeccionDetectada>(),
        SeccionesTemplate: Array.Empty<SeccionDetectada>(),
        NombreTemplateArchivo: null);

    private readonly ITrelloMcpClient _trello;
    private readonly ILogger<TrelloChecker> _logger;

    public TrelloChecker(ITrelloMcpClient trello, ILogger<TrelloChecker> logger)
    {
        _trello = trello;
        _logger = logger;
    }

    /// <summary>
    /// Extrae el board ID de la URL del tailoring y verifica que el board
    /// exista con al menos 2 tarjetas válidas.
    /// URL ausente, mal formada o sin board ID extraíble → NoEncontrado.
    /// </summary>
    public async ValueTask<VerificacionFisica> VerificarAsync(string? urlRef, CancellationToken ct)
    {
        var boardId = ParseBoardId(urlRef);
        if (boardId is null)
        {
            _logger.LogInformation(
                "TrelloChecker: URL '{Url}' no tiene board ID Trello extraíble " +
                "(esperado: trello.com/b/<8chars>/...). Devuelvo NoEncontrado.",
                urlRef ?? "(null)");
            return NoEncontrado;
        }

        try
        {
            var board = await _trello.GetBoardAsync(boardId, ct).ConfigureAwait(false);
            if (board is null || board.Closed)
            {
                _logger.LogInformation(
                    "TrelloChecker: board {BoardId} no encontrado o cerrado.", boardId);
                return NoEncontrado;
            }

            var cards = await _trello.ListCardsAsync(boardId, ct).ConfigureAwait(false);
            var tarjetasValidas = cards.Count(c => !string.IsNullOrWhiteSpace(c.Name));

            if (tarjetasValidas < MinTarjetasValidas)
            {
                _logger.LogInformation(
                    "TrelloChecker: board '{Nombre}' ({BoardId}) tiene {N} tarjetas válidas " +
                    "(mínimo: {Min}). Devuelvo NoEncontrado.",
                    board.Name, boardId, tarjetasValidas, MinTarjetasValidas);
                return NoEncontrado;
            }

            _logger.LogInformation(
                "TrelloChecker: board '{Nombre}' ({BoardId}) OK — {N} tarjetas válidas.",
                board.Name, boardId, tarjetasValidas);

            return new VerificacionFisica(
                Encontrado: true,
                Fuente: FuenteDocumento.Trello,
                NombreArchivo: board.Name,
                HashContenido: string.Empty,
                Secciones: Array.Empty<SeccionDetectada>(),
                SeccionesTemplate: Array.Empty<SeccionDetectada>(),
                NombreTemplateArchivo: null);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex,
                "TrelloChecker: timeout al verificar board {BoardId}. Devuelvo NoEncontrado.", boardId);
            return NoEncontrado;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TrelloChecker: error verificando board {BoardId} " +
                "(auth inválida, rate-limit o red caída). Devuelvo NoEncontrado.", boardId);
            return NoEncontrado;
        }
    }

    /// <summary>
    /// Extrae el board ID de una URL de Trello.
    /// Patrón: trello.com/b/{8chars}/...
    /// Devuelve null si la URL no encaja.
    /// </summary>
    internal static string? ParseBoardId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = BoardIdRegex.Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }
}
