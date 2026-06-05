namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Lo que el usuario manda cuando quiere iniciar sesion.
/// Solo necesita email y contrasena — nada mas.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Lo que el sistema devuelve cuando el login es exitoso.
/// El Token es el JWT que el usuario va a usar en cada pedido siguiente.
/// El frontend lo guarda y lo manda en el header de cada request.
/// </summary>
public record LoginResponse(
    string Token,           // El JWT — dura 8 horas por defecto
    string Nombre,          // Para mostrar "Bienvenido, Juan" en el frontend
    string Email,
    string Rol,             // Para que el frontend sepa que mostrar segun el rol
    DateTime Expiracion     // Para que el frontend sepa cuando vence el token
);

/// <summary>
/// Lo que el Administrador manda cuando quiere crear un usuario nuevo.
/// </summary>
public record CrearUsuarioRequest(
    string Nombre,
    string Email,
    string Password,
    string Rol              // Debe ser Administrador, Operador o Auditor
);

/// <summary>
/// Lo que el sistema devuelve cuando crea o consulta un usuario.
/// NO incluye el PasswordHash — nunca exponemos la contrasena.
/// </summary>
public record UsuarioResponse(
    int Id,
    string Nombre,
    string Email,
    string Rol,
    bool Activo,
    DateTime FechaCreacion,
    string Tema              // "claro" | "oscuro" — preferencia de tema del usuario
);

/// <summary>
/// Lo que el usuario manda para cambiar su preferencia de tema.
/// </summary>
public record ActualizarTemaRequest(
    string Tema              // "claro" | "oscuro"
);

/// <summary>
/// Lo que el Administrador manda cuando quiere modificar un usuario.
/// Todos los campos son opcionales (nullable) — solo se actualizan
/// los que vienen con valor.
/// </summary>
public record ModificarUsuarioRequest(
    string? Nombre,
    string? Email,
    string? Rol,
    bool? Activo
);

/// <summary>
/// Lo que el Administrador manda cuando quiere resetear la contrasena de
/// otro usuario. No pide la pass actual porque el admin no la conoce.
/// </summary>
public record ResetearPasswordRequest(
    string PasswordNueva
);