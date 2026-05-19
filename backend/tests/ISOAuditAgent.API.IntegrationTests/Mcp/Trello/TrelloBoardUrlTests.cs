using ISOAuditAgent.API.Mcp.Trello;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Trello;

/// <summary>
/// Verifica la extracción del board id/shortLink de URLs típicas de Trello.
/// </summary>
public sealed class TrelloBoardUrlTests
{
    [Theory]
    [InlineData("https://trello.com/b/abc12345/proyecto-test",     "abc12345")]
    [InlineData("https://trello.com/b/ABC12345xyz",                "ABC12345xyz")]
    [InlineData("http://trello.com/b/shortlink/algo-mas/?token=1", "shortlink")]
    [InlineData("https://trello.com/b/507f1f77bcf86cd799439011/board-name",
                "507f1f77bcf86cd799439011")]
    public void Extrae_shortLink_o_boardId_de_URL_canonica(string url, string expected)
    {
        Assert.Equal(expected, TrelloBoardUrl.TryExtractBoardIdOrShortLink(url));
    }

    [Theory]
    [InlineData("https://trello.com/c/xyz12345/tarjeta")] // card, no board
    [InlineData("https://trello.com/")]
    [InlineData("https://trello.com/b/short")]              // shortLink muy corto (<8)
    public void Devuelve_null_si_la_URL_no_es_un_board(string url)
    {
        Assert.Null(TrelloBoardUrl.TryExtractBoardIdOrShortLink(url));
    }
}
