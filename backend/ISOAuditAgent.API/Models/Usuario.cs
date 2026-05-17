namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa un usuario del sistema.
/// Esta clase mapea exactamente a la tabla "usuario" en Supabase.
/// Cada propiedad corresponde a una columna de la tabla.
/// </summary>
public class Usuario
{
    // Identificador unico — lo genera la base de datos automaticamente
    public int Id { get; set; }

    // Nombre completo del usuario
    public string Nombre { get; set; } = string.Empty;

    // Email — se usa para el login y para envio de reportes
    public string Email { get; set; } = string.Empty;

    // Contrasena hasheada — NUNCA guardamos la contrasena en texto plano
    // El hash es una transformacion irreversible: si alguien roba la BD
    // no puede saber la contrasena original
    public string PasswordHash { get; set; } = string.Empty;

    // Rol del usuario: Administrador, Operador o Auditor
    // Define que puede hacer dentro del sistema
    public string Rol { get; set; } = string.Empty;

    // Permite deshabilitar un usuario sin borrarlo
    // Util para mantener el historial de auditorias que hizo
    public bool Activo { get; set; } = true;

    // Fecha de creacion del usuario — para trazabilidad
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enum con los roles posibles del sistema.
/// Usamos un enum para evitar errores de tipeo —
/// es mas seguro que usar strings sueltos.
/// </summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Operador      = "Operador";
    public const string Auditor       = "Auditor";
}