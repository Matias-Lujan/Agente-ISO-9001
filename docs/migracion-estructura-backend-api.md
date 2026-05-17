# Migración de estructura del backend (proyectos bajo `ISOAuditAgent.API`)

Este documento resume los cambios de directorio y de referencias MSBuild que se aplicaron para agrupar la solución bajo el host web y dejar nombres de carpetas más claros.

## Objetivo

- Centralizar la API y los proyectos relacionados bajo `backend/ISOAuditAgent.API/`.
- Ubicar el agente de análisis documental en `Agents/DocumentAnalysis`.
- Acortar los nombres de carpetas del contrato y del corpus (`Contracts`, `Corpus`) sin renombrar ensamblados ni namespaces C#.

Los **namespaces** (`ISOAuditAgent.Contracts`, `ISOAuditAgent.DocumentAnalysis`, `ISOAuditAgent.Corpus`) y los nombres de **proyecto/.csproj** (`ISOAuditAgent.*.csproj`) se mantienen; solo cambió la **ubicación en disco** de las carpetas y las rutas en `.csproj` / `.sln`.

## Estado anterior (referencia)

Los proyectos vivían como hermanos de la API en `backend/`:

| Proyecto | Ruta antigua |
|---------|----------------|
| API | `backend/ISOAuditAgent.API/` |
| Contratos | `backend/ISOAuditAgent.Contracts/` |
| DocumentAnalysis | `backend/ISOAuditAgent.DocumentAnalysis/` |
| Corpus | `backend/ISOAuditAgent.Corpus/` |

Las pruebas seguían en `backend/tests/` con `ProjectReference` relativas a esas rutas.

## Cambios por etapa

### 1. Bibliotecas dentro de la carpeta de la API

Se movieron **`ISOAuditAgent.Contracts`**, **`ISOAuditAgent.Corpus`** e **`ISOAuditAgent.DocumentAnalysis`** a:

`backend/ISOAuditAgent.API/<nombre-proyecto>/`

**Problema técnico importante:** el SDK `Microsoft.NET.Sdk.Web` incluye por defecto todos los `*.cs` bajo el directorio del proyecto de la API. Con proyectos anidados, el compilador intentaba compilar también los fuentes (y a veces artefactos generados bajo `obj/`) de los subproyectos dentro de la misma compilación que la API.

**Solución:** en `ISOAuditAgent.API.csproj` se añadió `DefaultItemExcludes` para **excluir** del glob de compilación de la API los subárboles de proyectos referenciados. Esa exclusión se fue ajustando en las etapas siguientes (ver abajo el valor actual).

### 2. Agente DocumentAnalysis bajo `Agents/DocumentAnalysis`

El código del agente se reubicó en:

`backend/ISOAuditAgent.API/Agents/DocumentAnalysis/`

El archivo de proyecto sigue siendo `ISOAuditAgent.DocumentAnalysis.csproj` en esa carpeta.

**Referencias relativas actualizadas:**

- Desde `Agents/DocumentAnalysis/` hacia el contrato: dos niveles hasta la raíz de la API, luego `Contracts\...` (tras el renombrado de carpetas de la etapa 3).

### 3. Carpetas `Contracts` y `Corpus` (sin prefijo `ISOAuditAgent.`)

Solo se renombraron **carpetas** en disco:

| Antes | Después |
|-------|---------|
| `ISOAuditAgent.API/ISOAuditAgent.Contracts/` | `ISOAuditAgent.API/Contracts/` |
| `ISOAuditAgent.API/ISOAuditAgent.Corpus/` | `ISOAuditAgent.API/Corpus/` |

Los archivos `ISOAuditAgent.Contracts.csproj` y `ISOAuditAgent.Corpus.csproj` permanecen **dentro** de esas carpetas.

**Nota operativa:** en Windows, si `bin/` u `obj/` locales bloquean el renombrado (`Access denied`), conviene ejecutar `dotnet clean` y eliminar `bin`/`obj` de esas carpetas antes de renombrar.

## Estructura actual (resumen)

```
backend/
  ISOAuditAgent.API.sln
  ISOAuditAgent.API/
    ISOAuditAgent.API.csproj
    Contracts/
      ISOAuditAgent.Contracts.csproj
      …
    Corpus/
      ISOAuditAgent.Corpus.csproj
      …
    Agents/
      DocumentAnalysis/
        ISOAuditAgent.DocumentAnalysis.csproj
        …
    data/
      …
  tests/
    ISOAuditAgent.DocumentAnalysis.Tests/
    ISOAuditAgent.API.IntegrationTests/
```

## Archivos tocados en referencias

- **`ISOAuditAgent.API/ISOAuditAgent.API.csproj`**
  - `ProjectReference` a `Agents\DocumentAnalysis\...`, `Corpus\...`, `Contracts\...`.
  - `DefaultItemExcludes` incluye `Contracts\**`, `Corpus\**`, `Agents\**` para que la API no compile esos árboles como si fueran parte del mismo ensamblado.

- **`ISOAuditAgent.API.sln`**
  - Rutas de los proyectos anidados bajo `ISOAuditAgent.API\...`.

- **`Corpus/ISOAuditAgent.Corpus.csproj`**
  - Referencia al contrato: `..\Contracts\ISOAuditAgent.Contracts.csproj`.

- **`Agents/DocumentAnalysis/ISOAuditAgent.DocumentAnalysis.csproj`**
  - Referencia al contrato: `..\..\Contracts\ISOAuditAgent.Contracts.csproj`.

- **Tests**
  - `ISOAuditAgent.DocumentAnalysis.Tests.csproj`: referencias a `..\..\ISOAuditAgent.API\Contracts\...` y `..\..\ISOAuditAgent.API\Agents\DocumentAnalysis\...`.
  - `ISOAuditAgent.API.IntegrationTests.csproj`: referencia explícita a DocumentAnalysis bajo `Agents\DocumentAnalysis` además de la API (según lo que exija cada test).

## Qué no cambió

- Lógica de negocio y firmas públicas de los ensamblados (mismos nombres de proyecto salvo rutas).
- Contenido versionado del PDF bajo `ISOAuditAgent.API/data/norma/` (sigue referenciado desde el `.csproj` de la API con ruta relativa al proyecto host).
- Comportamiento del agente en tiempo de ejecución atribuible solo a rutas de carpetas: no había código que resolviera rutas fijas al layout antiguo entre proyectos.

## Verificación recomendada

Desde `backend/`:

```bash
dotnet build ISOAuditAgent.API.sln
dotnet test ISOAuditAgent.API.sln
```

## Versionado con Git

En `.gitignore` entraban antes `*.md` y todo `docs/`; se añadieron excepciones para poder versionar sólo **`docs/migracion-estructura-backend-api.md`**. Otros `.md` y archivos dentro de `docs/` siguen ignorados salvo esa ruta explícita.
