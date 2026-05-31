// ============================================================================
//  ContentHasher — SHA-256 hex 64-char minúsculas (D3.3)
// ----------------------------------------------------------------------------
//  Único método público: hash sobre bytes en memoria. Nuestro flujo MCP
//  devuelve siempre byte[] (DriveFileContent.Bytes), así que no hace falta
//  variante por stream.
//
//  Formato del output: hex en minúsculas, 64 caracteres. Es el formato que
//  pide InvariantsValidator (regex ^[a-f0-9]{64}$).
// ============================================================================

using System.Security.Cryptography;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public static class ContentHasher
{
    public static string Sha256Hex(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}