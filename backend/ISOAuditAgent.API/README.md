# Agente Inteligente de Auditoría ISO 9001 — BDT Global

Trabajo final — Tecnicatura en Análisis de Sistemas (ORT).

## Setup de base de datos (backend)

El modelo de datos se implementa con EF Core (code-first) sobre MySQL. Cada
integrante corre la base en su máquina local.

### Requisitos

- .NET 9 SDK
- MySQL 8.4 (Community Server) corriendo localmente
- Herramienta `dotnet-ef`:

  ```
  dotnet tool install --global dotnet-ef
  ```

### Configuración inicial (una vez por integrante)

La connection string no está en el repo. Cada uno carga la suya en User
Secrets, parado en la carpeta `backend/ISOAuditAgent.API`:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=iso_audit_agent;User=root;Password=PASSWORD;"
```

Reemplazá `PASSWORD` por la contraseña de tu MySQL local.

### Crear / actualizar la base

Desde `backend/ISOAuditAgent.API`:

```
dotnet ef database update
```

Esto crea la base `iso_audit_agent` con todas las tablas (o aplica las
migraciones nuevas si la base ya existe).

### Cambios en el modelo

Si modificás una entidad, generá una migración y commiteala:

```
dotnet ef migrations add NombreDescriptivoDelCambio
```

El resto del equipo aplica el cambio con `dotnet ef database update` después
de hacer `pull`.
