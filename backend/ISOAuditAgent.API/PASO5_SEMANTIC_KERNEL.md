## PASO 5: Configuración de Semantic Kernel

### Estado actual
✅ **Compilación exitosa** - El proyecto compila correctamente con Semantic Kernel 1.30.0

### Configuración realizada

#### 1. **Paquetes NuGet agregados** ([ISOAuditAgent.API.csproj](ISOAuditAgent.API.csproj))
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.30.0" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.30.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.1" />
```

#### 2. **Registración de Semantic Kernel** ([Program.cs](Program.cs))
```csharp
// Leer clave de API desde variables de entorno o appsettings.json
var openAiApiKey = builder.Configuration["OpenAiApiKey"] 
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY no está configurada");

// Registrar el Kernel con OpenAI
builder.Services.AddKernel()
    .AddOpenAIChatCompletion("gpt-4o", openAiApiKey);

// Registrar dependencias del agente
builder.Services.AddScoped<IComplianceValidationAgent, ComplianceValidationAgent>();
```

#### 3. **Inyección del Kernel en el Agente** ([ComplianceValidationAgent.cs](Agents/ComplianceValidation/ComplianceValidationAgent.cs))
```csharp
public class ComplianceValidationAgent : IComplianceValidationAgent
{
    private readonly Kernel _kernel;

    public ComplianceValidationAgent(
        IMcpClient mcpClient,
        IReglaValidacionRepository reglaRepository,
        Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        // ...
    }
}
```

#### 4. **Configuración en appsettings.json** ([appsettings.json](appsettings.json))
```json
{
  "OpenAiApiKey": "{{REEMPLAZAR_CON_CLAVE_REAL}}",
  "OpenAiModelId": "gpt-4o",
  "ComplianceValidation": {
    "Timeout": 30000,
    "MaxRetries": 3
  }
}
```

### ⚠️ IMPORTANTE: Configuración de claves de API

#### Opción 1: Variables de Entorno (Recomendado para Producción)
```powershell
# En PowerShell
$env:OPENAI_API_KEY="sk-..."

# En Bash
export OPENAI_API_KEY="sk-..."
```

#### Opción 2: archivo appsettings.json (Solo desarrollo)
```json
{
  "OpenAiApiKey": "sk-..."
}
```

#### Opción 3: Secretos de Usuario (Desarrollo seguro en .NET)
```powershell
dotnet user-secrets init
dotnet user-secrets set "OpenAiApiKey" "sk-..."
```

### 📝 Notas sobre Google Gemini 2.5 Flash

El usuario solicitó usar **Google Gemini 2.5 Flash**, pero el conector oficial de Microsoft.SemanticKernel.Connectors.Google aún **no está disponible en versiones estables** en NuGet.

**Versión actual:** OpenAI GPT-4o (compatible y funcional)

**Para cambiar a Google Gemini cuando esté disponible:**

1. Agregar el paquete NuGet cuando esté disponible:
```xml
<PackageReference Include="Microsoft.SemanticKernel.Connectors.Google" Version="X.X.X" />
```

2. Cambiar en Program.cs:
```csharp
builder.Services.AddKernel()
    .AddGoogleGenerativeAI("gemini-2.5-flash", geminiApiKey);
```

3. Crear variable de entorno:
```powershell
$env:GEMINI_API_KEY="..."
```

### ✅ Pruebas de compilación
```
dotnet build
// Resultado: Compilación realizado correctamente en 2,1s ✓
```

### Próximo paso: PASO 6
En el PASO 6 implementaremos el **Diseño del Prompt** que será enviado a OpenAI/Gemini para analizar las inconsistencias entre Trello y Clockify.
