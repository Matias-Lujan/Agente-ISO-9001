using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Data;

/// <summary>
/// Siembra de datos para DESARROLLO. Resuelve el problema de que cada dev tiene
/// su propia base local: con esto, todos arrancan con los MISMOS usuarios y un
/// login que funciona, sin insertar usuarios a mano (los inserts manuales suelen
/// dejar el password en texto plano y rompen BCrypt: "Invalid salt version").
///
/// Es idempotente y auto-reparador:
///   - Aplica las migraciones pendientes (crea la BD/esquema si no existen).
///   - Crea los usuarios demo si faltan.
///   - Si un usuario demo existe pero su hash NO es BCrypt válido (quedó mal
///     insertado), le repara el hash.
///
/// SOLO debe ejecutarse en entorno de desarrollo (ver Program.cs): nunca en
/// producción, porque crea usuarios con una contraseña conocida.
/// </summary>
public static class DataSeeder
{
    // Contraseña única para todos los usuarios demo (solo desarrollo).
    public const string PasswordDemo = "admin1234";

    private static readonly (string Email, string Nombre, RolUsuario Rol)[] UsuariosDemo =
    {
        ("admin@bdtglobal.com.ar",    "Administrador Demo", RolUsuario.Administrador),
        ("auditor@bdtglobal.com.ar",  "Auditor Demo",       RolUsuario.Auditor),
        ("operador@bdtglobal.com.ar", "Operador Demo",      RolUsuario.Operador),
    };

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISOAuditAgentDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DataSeeder");

        // Aplica migraciones pendientes (crea la BD y el esquema si no existen).
        await db.Database.MigrateAsync();

        foreach (var (email, nombre, rol) in UsuariosDemo)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario is null)
            {
                db.Usuarios.Add(new Usuario
                {
                    Nombre        = nombre,
                    Email         = email,
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(PasswordDemo, workFactor: 11),
                    Rol           = rol,
                    Activo        = true,
                    FechaCreacion = DateTime.UtcNow,
                    TemaPreferido = TemaPreferido.Claro,
                });
                logger.LogInformation("Usuario demo creado: {Email} ({Rol})", email, rol);
            }
            else if (!usuario.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
            {
                // El hash no es BCrypt (probablemente insertado a mano) → reparar.
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDemo, workFactor: 11);
                logger.LogWarning("Hash reparado para el usuario demo: {Email}", email);
            }
        }

        await db.SaveChangesAsync();
    }
}
