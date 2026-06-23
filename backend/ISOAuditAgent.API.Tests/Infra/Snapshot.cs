using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.Tests.Infra;

// ============================================================================
//  Snapshot — red de seguridad determinística del refactor de Agents/
// ----------------------------------------------------------------------------
//  Helper de "golden output" (patrón approval) sin dependencias externas.
//  Congela la salida actual de las piezas PURAS del workflow (prompt builders,
//  generadores de hallazgos determinísticos, parser de clasificación) para
//  detectar cualquier cambio de comportamiento durante el refactor.
//
//  FLUJO:
//   - Primera vez (no existe .approved.txt): escribe <nombre>.received.txt y
//     FALLA. Hay que revisar el .received y renombrarlo a .approved.txt para
//     aprobar el baseline (congelar el comportamiento actual como referencia).
//   - Siguientes corridas: compara contra el .approved.txt. Si difiere, escribe
//     <nombre>.received.txt y FALLA mostrando ambos paths para diff.
//
//  Re-aprobar un cambio INTENCIONAL: borrar el .approved.txt y re-correr, o
//  renombrar el .received.txt sobre el .approved.txt tras revisar el diff.
//
//  Las golden viven en Snapshots/ del proyecto de tests, versionadas en git.
//  La comparación normaliza CRLF→LF para no depender del line-ending de git.
// ============================================================================
internal static class Snapshot
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        // Encoder relajado: tildes y apóstrofes se serializan literales (no í
        // ni '). El golden queda legible para revisar diffs durante el refactor;
        // sigue siendo determinístico.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Congela <paramref name="actual"/> (texto) bajo el nombre dado.</summary>
    public static void Match(string actual, string nombre)
    {
        var dir = Path.Combine(ProjectDir(), "Snapshots");
        Directory.CreateDirectory(dir);

        var approved = Path.Combine(dir, $"{nombre}.approved.txt");
        var received = Path.Combine(dir, $"{nombre}.received.txt");
        var actualNorm = Normalizar(actual);

        if (!File.Exists(approved))
        {
            File.WriteAllText(received, actualNorm);
            Assert.Fail(
                $"Snapshot '{nombre}' sin baseline aprobado. Se escribió:\n  {received}\n" +
                $"Revisalo y renombralo a '{nombre}.approved.txt' para congelar el baseline.");
            return;
        }

        var expected = Normalizar(File.ReadAllText(approved));
        if (expected == actualNorm)
        {
            // Limpiar un .received viejo de una corrida fallida anterior.
            if (File.Exists(received)) File.Delete(received);
            return;
        }

        File.WriteAllText(received, actualNorm);
        Assert.Fail(
            $"Snapshot '{nombre}' difiere del baseline aprobado (cambio de comportamiento).\n" +
            $"  aprobado: {approved}\n  recibido: {received}\n" +
            $"Si el cambio es intencional, revisá el diff y reemplazá el .approved.txt.");
    }

    /// <summary>Serializa <paramref name="actual"/> a JSON estable y lo congela.</summary>
    public static void MatchJson(object actual, string nombre)
        => Match(JsonSerializer.Serialize(actual, JsonOpts), nombre);

    private static string Normalizar(string s) => s.Replace("\r\n", "\n");

    // Sube desde el output de build (bin/Debug/net9.0) hasta el directorio del
    // proyecto de tests (el primer ancestro con un .csproj). Robusto ante el
    // path absoluto de cada máquina.
    private static string ProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.csproj").Any())
            dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
