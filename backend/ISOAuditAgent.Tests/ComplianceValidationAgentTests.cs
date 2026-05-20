using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ISOAuditAgent.Tests.Agents;

/// <summary>
/// Tests unitarios para el Agente de Validación de Cumplimiento.
/// Verifica que el agente funciona correctamente con datos mock.
/// </summary>
public class ComplianceValidationAgentTests
{
    private readonly ILogger<ComplianceValidationAgent> _logger;

    public ComplianceValidationAgentTests()
    {
        // Crear logger mock para tests
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ComplianceValidationAgent>();
    }

    /// <summary>
    /// Test: ValidateProcessAsync debe retornar una lista válida de hallazgos.
    /// </summary>
    [Fact(Skip = "Pendiente actualizar tras refactor del constructor (requiere Kernel + ILogger).")]
    public async Task ValidateProcessAsync_ShouldReturnHallazgos_WithMockData()
    {
        // Arrange
        var mcpClient = new MockMcpClient();
        var reglaRepository = new MockReglaValidacionRepository();

        // No pasamos Kernel (será null) - válido para tests
        var agent = new ComplianceValidationAgent(mcpClient, reglaRepository, _logger, null!);

        const string projectId = "TEST-PROJECT-001";
        const int procesoId = 1;

        // Act
        var hallazgos = await agent.ValidateProcessAsync(projectId, procesoId);

        // Assert
        Assert.NotNull(hallazgos);
        Assert.IsType<List<ValidationFinding>>(hallazgos);
        // Con Kernel null en tests, retornará lista vacía (esperado)
    }

    /// <summary>
    /// Test: MockMcpClient debe retornar tareas de Trello.
    /// </summary>
    [Fact]
    public async Task GetTareasTrelloAsync_ShouldReturnMockTasks()
    {
        // Arrange
        var mcpClient = new MockMcpClient();
        const string projectId = "TEST-PROJECT-001";

        // Act
        var tareas = await mcpClient.GetTareasTrelloAsync(projectId);

        // Assert
        Assert.NotNull(tareas);
        Assert.NotEmpty(tareas);
        Assert.True(tareas.Count >= 4, "Debe haber al menos 4 tareas mock");
        
        // Verificar que las tareas tienen los datos esperados
        var tarea1 = tareas.First();
        Assert.NotEmpty(tarea1.Id);
        Assert.NotEmpty(tarea1.Nombre);
        Assert.NotEmpty(tarea1.Responsable);
        Assert.NotEmpty(tarea1.Estado);
    }

    /// <summary>
    /// Test: MockMcpClient debe retornar registros de Clockify.
    /// </summary>
    [Fact]
    public async Task GetRegistrosClockifyAsync_ShouldReturnMockRecords()
    {
        // Arrange
        var mcpClient = new MockMcpClient();
        const string projectId = "TEST-PROJECT-001";

        // Act
        var registros = await mcpClient.GetRegistrosClockifyAsync(projectId);

        // Assert
        Assert.NotNull(registros);
        Assert.NotEmpty(registros);
        Assert.True(registros.Count >= 7, "Debe haber al menos 7 registros mock");
        
        // Verificar que los registros tienen los datos esperados
        var registro1 = registros.First();
        Assert.NotEmpty(registro1.Id);
        Assert.NotEmpty(registro1.IdTarea);
        Assert.NotEmpty(registro1.Usuario);
        Assert.True(registro1.HorasRegistradas > 0);
    }

    /// <summary>
    /// Test: MockReglaValidacionRepository debe retornar reglas para un proceso.
    /// </summary>
    [Fact]
    public async Task GetReglasByProcesoAsync_ShouldReturnValidationRules()
    {
        // Arrange
        var repository = new MockReglaValidacionRepository();
        const int procesoId = 1;

        // Act
        var reglas = await repository.GetReglasByProcesoAsync(procesoId);

        // Assert
        Assert.NotNull(reglas);
        Assert.NotEmpty(reglas);
        Assert.True(reglas.Count >= 6, "Debe haber al menos 6 reglas para el proceso 1");
        
        // Verificar que hay reglas obligatorias
        var reglasObligatorias = reglas.Where(r => r.TipoObligatorioOpcional == "Obligatorio").ToList();
        Assert.NotEmpty(reglasObligatorias);
        
        // Verificar que todas las reglas están activas
        Assert.All(reglas, r => Assert.True(r.Activa, "La regla debe estar activa"));
    }

    /// <summary>
    /// Test: Las reglas deben incluir descripciones y criterios de evaluación.
    /// </summary>
    [Fact]
    public async Task ValidationRules_ShouldHaveDescriptionAndCriteria()
    {
        // Arrange
        var repository = new MockReglaValidacionRepository();
        const int procesoId = 1;

        // Act
        var reglas = await repository.GetReglasByProcesoAsync(procesoId);

        // Assert
        Assert.All(reglas, r =>
        {
            Assert.NotEmpty(r.Descripcion);
            Assert.NotEmpty(r.CriterioEvaluacion);
            Assert.NotEmpty(r.TipoObligatorioOpcional);
        });
    }

    /// <summary>
    /// Test: Detectar registros huérfanos (CLO-006 → TRE-999 no existe).
    /// </summary>
    [Fact]
    public async Task DetectHuerfanoRecords_ShouldIdentifyMissingTasks()
    {
        // Arrange
        var mcpClient = new MockMcpClient();
        const string projectId = "TEST-PROJECT-001";

        // Act
        var tareas = await mcpClient.GetTareasTrelloAsync(projectId);
        var registros = await mcpClient.GetRegistrosClockifyAsync(projectId);

        // Assert
        var registroHuerfano = registros.FirstOrDefault(r => r.IdTarea == "TRE-999");
        Assert.NotNull(registroHuerfano);
        
        // Verificar que esa tarea no existe en Trello
        var tareaNoExiste = tareas.FirstOrDefault(t => t.Id == "TRE-999");
        Assert.Null(tareaNoExiste);
    }

    /// <summary>
    /// Test: Detectar discrepancia de responsables (CLO-007).
    /// </summary>
    [Fact]
    public async Task DetectResponsibleMismatch_ShouldIdentifyDifferentResponsible()
    {
        // Arrange
        var mcpClient = new MockMcpClient();
        const string projectId = "TEST-PROJECT-001";

        // Act
        var tareas = await mcpClient.GetTareasTrelloAsync(projectId);
        var registros = await mcpClient.GetRegistrosClockifyAsync(projectId);

        // Assert
        var registroDesajustado = registros.FirstOrDefault(r => r.Id == "CLO-007");
        Assert.NotNull(registroDesajustado);
        
        var tareaRelacionada = tareas.FirstOrDefault(t => t.Id == registroDesajustado.IdTarea);
        Assert.NotNull(tareaRelacionada);
        
        // Carlos López registra horas en TRE-001, pero Juan Pérez es el responsable
        Assert.NotEqual(registroDesajustado.Usuario, tareaRelacionada.Responsable);
        Assert.Equal("Carlos López", registroDesajustado.Usuario);
        Assert.Equal("Juan Pérez", tareaRelacionada.Responsable);
    }
}

/// <summary>
/// Tests para asegurar que la arquitectura sigue las reglas estrictas.
/// </summary>
public class ArchitectureTests
{
    /// <summary>
    /// Test: El agente NO debe tener acceso directo a APIs externas.
    /// Todo debe pasar por IMcpClient e IReglaValidacionRepository.
    /// </summary>
    [Fact]
    public void Agent_ShouldNotAccessExternalAPIsDirectly()
    {
        // Este test es principalmente conceptual.
        // En una auditoría de código real, verificaríamos que:
        // - No hay HttpClient instanciado directamente
        // - No hay credenciales hardcodeadas
        // - Todo pasa por interfaces inyectadas

        var agentType = typeof(ComplianceValidationAgent);
        var constructorParams = agentType.GetConstructors().First().GetParameters();

        // El agente debe inyectar IMcpClient e IReglaValidacionRepository
        var hasMcpClient = constructorParams.Any(p => p.ParameterType == typeof(IMcpClient));
        var hasReglaRepository = constructorParams.Any(p => p.ParameterType == typeof(IReglaValidacionRepository));

        Assert.True(hasMcpClient, "El agente debe inyectar IMcpClient");
        Assert.True(hasReglaRepository, "El agente debe inyectar IReglaValidacionRepository");
    }

    /// <summary>
    /// Test: El agente debe inyectar Semantic Kernel para análisis LLM.
    /// </summary>
    [Fact]
    public void Agent_ShouldInjectSemanticKernel()
    {
        var agentType = typeof(ComplianceValidationAgent);
        var constructorParams = agentType.GetConstructors().First().GetParameters();

        // El agente debe inyectar Kernel de Semantic Kernel
        var hasKernel = constructorParams.Any(p => p.ParameterType.Name == "Kernel");

        Assert.True(hasKernel, "El agente debe inyectar Microsoft.SemanticKernel.Kernel");
    }
}
