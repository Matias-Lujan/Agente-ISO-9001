using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ISOAuditAgent.API.Tests;

// Tests de la lógica de negocio de AuthService: validaciones de alta de usuario
// y reglas de login (dominio, existencia, password, estado activo). El
// repositorio y la configuración se mockean — no se toca BD ni JWT real salvo
// para verificar que el login devuelve un token.
public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _repo = new();
    private readonly Mock<IConfiguration> _config = new();

    private AuthService CrearService()
    {
        _config.Setup(c => c["Jwt:SecretKey"]).Returns("clave-super-secreta-para-tests-0123456789");
        _config.Setup(c => c["Jwt:Issuer"]).Returns("ISOAuditAgent");
        _config.Setup(c => c["Jwt:Audience"]).Returns("ISOAuditAgent");
        return new AuthService(_repo.Object, _config.Object, NullLogger<AuthService>.Instance);
    }

    private static Usuario UsuarioConPassword(string password, bool activo = true,
        RolUsuario rol = RolUsuario.Auditor) => new()
    {
        Id = 1,
        Nombre = "Juan",
        Email = "juan@bdtglobal.com.ar",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11),
        Rol = rol,
        Activo = activo,
    };

    // ── CrearUsuarioAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CrearUsuario_NombreVacio_LanzaArgumentException()
    {
        var svc = CrearService();
        var req = new CrearUsuarioRequest("", "juan@bdtglobal.com.ar", "password123", "Auditor");

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CrearUsuarioAsync(req));
    }

    [Fact]
    public async Task CrearUsuario_DominioNoPermitido_LanzaArgumentException()
    {
        var svc = CrearService();
        var req = new CrearUsuarioRequest("Juan", "juan@gmail.com", "password123", "Auditor");

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CrearUsuarioAsync(req));
    }

    [Fact]
    public async Task CrearUsuario_EmailDuplicado_LanzaInvalidOperationException()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>()))
             .ReturnsAsync(new Usuario { Id = 9, Email = "juan@bdtglobal.com.ar" });
        var svc = CrearService();
        var req = new CrearUsuarioRequest("Juan", "juan@bdtglobal.com.ar", "password123", "Auditor");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CrearUsuarioAsync(req));
    }

    [Fact]
    public async Task CrearUsuario_PasswordCorta_LanzaArgumentException()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
        var svc = CrearService();
        var req = new CrearUsuarioRequest("Juan", "juan@bdtglobal.com.ar", "1234567", "Auditor");

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CrearUsuarioAsync(req));
    }

    [Fact]
    public async Task CrearUsuario_RolInvalido_LanzaArgumentException()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
        var svc = CrearService();
        var req = new CrearUsuarioRequest("Juan", "juan@bdtglobal.com.ar", "password123", "SuperAdmin");

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CrearUsuarioAsync(req));
    }

    [Fact]
    public async Task CrearUsuario_Valido_HasheaPasswordConBCryptYNoGuardaTextoPlano()
    {
        Usuario? capturado = null;
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
        _repo.Setup(r => r.CrearAsync(It.IsAny<Usuario>()))
             .ReturnsAsync((Usuario u) => { u.Id = 7; capturado = u; return u; });

        var svc = CrearService();
        var req = new CrearUsuarioRequest("Juan", "juan@bdtglobal.com.ar", "password123", "Auditor");

        var resp = await svc.CrearUsuarioAsync(req);

        Assert.Equal(7, resp.Id);
        Assert.NotNull(capturado);
        Assert.NotEqual("password123", capturado!.PasswordHash);   // nunca texto plano
        Assert.StartsWith("$2", capturado.PasswordHash);            // hash BCrypt
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", capturado.PasswordHash));
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_DominioNoPermitido_RetornaNullSinTocarRepo()
    {
        var svc = CrearService();

        var resp = await svc.LoginAsync(new LoginRequest("hacker@gmail.com", "password123"));

        Assert.Null(resp);
        _repo.Verify(r => r.ObtenerPorEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_UsuarioNoExiste_RetornaNull()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
        var svc = CrearService();

        var resp = await svc.LoginAsync(new LoginRequest("juan@bdtglobal.com.ar", "password123"));

        Assert.Null(resp);
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_RetornaNull()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>()))
             .ReturnsAsync(UsuarioConPassword("la-correcta"));
        var svc = CrearService();

        var resp = await svc.LoginAsync(new LoginRequest("juan@bdtglobal.com.ar", "la-incorrecta"));

        Assert.Null(resp);
    }

    [Fact]
    public async Task Login_UsuarioInactivo_RetornaNull()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>()))
             .ReturnsAsync(UsuarioConPassword("password123", activo: false));
        var svc = CrearService();

        var resp = await svc.LoginAsync(new LoginRequest("juan@bdtglobal.com.ar", "password123"));

        Assert.Null(resp);
    }

    [Fact]
    public async Task Login_CredencialesValidas_DevuelveTokenYDatos()
    {
        _repo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>()))
             .ReturnsAsync(UsuarioConPassword("password123", rol: RolUsuario.Administrador));
        var svc = CrearService();

        var resp = await svc.LoginAsync(new LoginRequest("juan@bdtglobal.com.ar", "password123"));

        Assert.NotNull(resp);
        Assert.False(string.IsNullOrEmpty(resp!.Token));
        Assert.Equal("juan@bdtglobal.com.ar", resp.Email);
        Assert.Equal("Administrador", resp.Rol);
    }
}
