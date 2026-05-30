// ============================================================================
//  AuthDtos.cs — DTOs de autenticacion
// ----------------------------------------------------------------------------
//  Records inmutables, convencion del proyecto (igual que CrearAuditoriaRequest
//  en AuditoriaEndpoints.cs).
// ============================================================================

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Body del POST /api/auth/login.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Respuesta del login exitoso. El frontend guarda el token y lo manda
/// en cada request siguiente en el header Authorization.
/// </summary>
public sealed record LoginResponse(
    string Token,
    string Nombre,
    string Email,
    string Rol,
    DateTime Expiracion
);
