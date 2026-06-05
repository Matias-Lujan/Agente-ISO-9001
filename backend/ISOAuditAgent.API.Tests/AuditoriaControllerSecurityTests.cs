using System.Reflection;
using ISOAuditAgent.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace ISOAuditAgent.API.Tests;

// Guard test: la creación de auditorías debe estar restringida por rol a
// Administrador + Auditor (el Operador NO puede ejecutar auditorías). Si alguien
// quita o cambia esa restricción en el atributo [Authorize], este test falla.
public class AuditoriaControllerSecurityTests
{
    [Fact]
    public void Crear_RestringidoAAdministradorYAuditor()
    {
        var metodo = typeof(AuditoriaController).GetMethod(nameof(AuditoriaController.Crear));
        Assert.NotNull(metodo);

        var authorize = metodo!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);

        var roles = (authorize!.Roles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Contains("Administrador", roles);
        Assert.Contains("Auditor", roles);
        Assert.DoesNotContain("Operador", roles);
    }
}
