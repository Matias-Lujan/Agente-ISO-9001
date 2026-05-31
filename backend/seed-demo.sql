-- ============================================================================
-- seed-demo.sql — Seed mínimo de demo para correr una auditoría E2E
-- ----------------------------------------------------------------------------
-- Ejecutar sobre MySQL / base iso_audit_agent.
--
-- Propósito:
--   Dejar datos mínimos para poder disparar una auditoría E2E con:
--   POST /api/auditorias
--
-- Propiedades:
--   - Idempotente: se puede correr más de una vez sin duplicar registros.
--   - No modifica schema.
--   - No incluye secrets ni private keys.
--   - Usa llaves naturales: Codigo, Nombre, Email.
--   - Compatible con MySQL Workbench Safe Updates: no usa UPDATE sin key.
--
-- IMPORTANTE — nombres en PascalCase:
--   Las COLUMNAS están en PascalCase (PasswordHash, TipoProyecto, FechaCreacion…)
--   porque EF Core mapea por nombre de propiedad. Las TABLAS son snake_case
--   plural (usuarios, proyectos_usuarios, artefactos_esperados) según el
--   DbContext (.ToTable). Si cambiás la convención del DbContext, actualizá
--   este script en consecuencia.
--
-- Requisitos previos:
--   - Migraciones EF aplicadas.
--   - Gemini:ApiKey configurada en User Secrets.
--   - google-service-account.json local, NO commiteado.
--   - La service account debe tener acceso lector al folder:
--     15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n
-- ============================================================================

USE iso_audit_agent;

-- ---------------------------------------------------------------------------
-- 0. ConfiguracionSistema: path de templates
-- ---------------------------------------------------------------------------

INSERT INTO configuraciones_sistema (Clave, Valor, Descripcion)
SELECT
    'path_carpeta_templates',
    'templates',
    'Carpeta local configurable para templates de artefactos. Seed D7.'
WHERE NOT EXISTS (
    SELECT 1
    FROM configuraciones_sistema
    WHERE Clave = 'path_carpeta_templates'
);

-- ---------------------------------------------------------------------------
-- 1. Procedimiento PR 11-13
-- ---------------------------------------------------------------------------

INSERT INTO procedimientos (Codigo, Nombre, Descripcion)
SELECT
    'PR 11-13',
    'Diseño y Análisis, Desarrollo e Implementación de Software',
    'Procedimiento maestro para proyectos llave en mano y customizaciones. Seed D7.'
WHERE NOT EXISTS (
    SELECT 1
    FROM procedimientos
    WHERE Codigo = 'PR 11-13'
);

-- ---------------------------------------------------------------------------
-- 2. Etapa Planificación
-- ---------------------------------------------------------------------------

INSERT INTO etapas (ProcedimientoId, Nombre, Orden, Descripcion)
SELECT
    p.Id,
    'Planificación',
    1,
    'Etapa inicial del proyecto: tailoring, ERS, cronograma, riesgos. Seed D7.'
FROM procedimientos p
WHERE p.Codigo = 'PR 11-13'
  AND NOT EXISTS (
      SELECT 1
      FROM etapas e
      WHERE e.ProcedimientoId = p.Id
        AND e.Nombre = 'Planificación'
  );

-- ---------------------------------------------------------------------------
-- 3. Usuario demo
-- ---------------------------------------------------------------------------
-- OJO: password_hash es un placeholder, NO un hash BCrypt válido. Además el
-- email NO es @bdtglobal.com.ar, así que este usuario NO puede loguearse con
-- el AuthService actual (validación de dominio + BCrypt). Sirve solo como
-- "responsable" del proyecto en proyectos_usuarios. Para disparar el E2E
-- (POST /api/auditorias, que es [Authorize]) usá el JWT del admin real
-- (admin@bdtglobal.com.ar). El backend toma el usuarioId del JWT.
-- ---------------------------------------------------------------------------

INSERT INTO usuarios (Email, Nombre, Rol, PasswordHash, Activo, FechaCreacion)
SELECT
    'test@bdt-demo.local',
    'Usuario Demo D7',
    'Administrador',
    '$placeholder-no-auth-yet$',
    1,
    UTC_TIMESTAMP()
WHERE NOT EXISTS (
    SELECT 1
    FROM usuarios
    WHERE Email = 'test@bdt-demo.local'
);

-- ---------------------------------------------------------------------------
-- 4. Proyecto demo
-- ---------------------------------------------------------------------------

INSERT INTO proyectos (
    Nombre,
    Descripcion,
    FechaInicio,
    FechaFin,
    TipoProyecto,
    HorasEstimadas,
    ProcedimientoId,
    TrelloBoardId,
    ClockifyProjectId,
    DriveFolderId,
    Activo
)
SELECT
    'Proyecto Demo D7 (E2E)',
    'Proyecto de demo para validar el flujo E2E del MVP. Seed D7.',
    UTC_TIMESTAMP(),
    NULL,
    'A',
    1500,
    p.Id,
    NULL,
    NULL,
    '15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n',
    1
FROM procedimientos p
WHERE p.Codigo = 'PR 11-13'
  AND NOT EXISTS (
      SELECT 1
      FROM proyectos pr
      WHERE pr.Nombre = 'Proyecto Demo D7 (E2E)'
  );

-- ---------------------------------------------------------------------------
-- 5. Relación proyecto_usuario
-- ---------------------------------------------------------------------------

INSERT INTO proyectos_usuarios (ProyectoId, UsuarioId)
SELECT
    pr.Id,
    u.Id
FROM proyectos pr
CROSS JOIN usuarios u
WHERE pr.Nombre = 'Proyecto Demo D7 (E2E)'
  AND u.Email = 'test@bdt-demo.local'
  AND NOT EXISTS (
      SELECT 1
      FROM proyectos_usuarios pu
      WHERE pu.ProyectoId = pr.Id
        AND pu.UsuarioId = u.Id
  );

-- ---------------------------------------------------------------------------
-- 6. Artefactos esperados — etapa Planificación
-- ---------------------------------------------------------------------------
-- Subconjunto D7: 6 artefactos alineados con el FR-29 demo.
-- Idempotencia por (EtapaId, Nombre).
-- ---------------------------------------------------------------------------

-- 6.1 FR 29 — Tailoring del Proceso
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    'FR 29',
    'Tailoring del Proceso',
    'Tailoring inicial del proyecto. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Tailoring del Proceso'
  );

-- 6.2 FR 30 — ERS
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    'FR 30',
    'Especificacion de Requerimientos de Software (ERS)',
    'Documento de requerimientos del proyecto. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Especificacion de Requerimientos de Software (ERS)'
  );

-- 6.3 FR 31 — Planilla de riesgos
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    'FR 31',
    'Planilla de riesgos',
    'Registro de riesgos identificados del proyecto. Seed D7.',
    0,
    0,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Planilla de riesgos'
  );

-- 6.4 Cronograma (MS Project)
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    NULL,
    'Cronograma (MS Project)',
    'Cronograma del proyecto en MS Project. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Cronograma (MS Project)'
  );

-- 6.5 Proyecto en Trello - creación
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    NULL,
    'Proyecto en Trello - creación',
    'Tablero de Trello del proyecto. En D7 permite validar el caso NoAplica sin justificación según el FR-29 demo.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Proyecto en Trello - creación'
  );

-- 6.6 FR 48 — Sign-Off
INSERT INTO artefactos_esperados (
    EtapaId,
    Codigo,
    Nombre,
    Descripcion,
    MandatorioTipoA,
    MandatorioTipoB,
    PathTemplateRelativo
)
SELECT
    e.Id,
    'FR 48',
    'Sign-Off',
    'Documento de cierre y conformidad del cliente. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.EtapaId = e.Id
        AND ae.Nombre = 'Sign-Off'
  );

-- ============================================================================
-- Verificación
-- ============================================================================

SELECT
    'CONFIGURACION' AS bloque,
    Clave,
    Valor
FROM configuraciones_sistema
WHERE Clave = 'path_carpeta_templates';

SELECT
    'PROCEDIMIENTO' AS bloque,
    p.Id,
    p.Codigo,
    p.Nombre
FROM procedimientos p
WHERE p.Codigo = 'PR 11-13';

SELECT
    'ETAPA' AS bloque,
    e.Id,
    e.ProcedimientoId,
    e.Nombre,
    e.Orden
FROM etapas e
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
ORDER BY e.Orden;

SELECT
    'USUARIO' AS bloque,
    u.Id,
    u.Email,
    u.Rol,
    u.Activo
FROM usuarios u
WHERE u.Email = 'test@bdt-demo.local';

SELECT
    'PROYECTO' AS bloque,
    pr.Id,
    pr.Nombre,
    pr.TipoProyecto,
    pr.HorasEstimadas,
    pr.ProcedimientoId,
    pr.DriveFolderId,
    pr.Activo
FROM proyectos pr
WHERE pr.Nombre = 'Proyecto Demo D7 (E2E)';

SELECT
    'PROYECTOS_USUARIOS' AS bloque,
    pu.Id,
    pu.ProyectoId,
    pu.UsuarioId
FROM proyectos_usuarios pu
JOIN proyectos pr ON pu.ProyectoId = pr.Id
JOIN usuarios u ON pu.UsuarioId = u.Id
WHERE pr.Nombre = 'Proyecto Demo D7 (E2E)'
  AND u.Email = 'test@bdt-demo.local';

SELECT
    'ARTEFACTOS_ESPERADOS' AS bloque,
    ae.Id,
    ae.Codigo,
    ae.Nombre,
    ae.MandatorioTipoA,
    ae.MandatorioTipoB
FROM artefactos_esperados ae
JOIN etapas e ON ae.EtapaId = e.Id
JOIN procedimientos p ON e.ProcedimientoId = p.Id
WHERE p.Codigo = 'PR 11-13'
  AND e.Nombre = 'Planificación'
ORDER BY ae.Id;

-- ============================================================================
-- Luego de ejecutar el seed:
--
-- 1. Logueate como admin real para obtener un JWT:
--    POST http://localhost:5180/api/auth/login
--    { "email": "admin@bdtglobal.com.ar", "password": "admin1234" }
--
-- 2. Disparar la auditoría (el usuarioId sale del JWT, NO va en el body):
--    POST http://localhost:5180/api/auditorias
--    Header: Authorization: Bearer <token>
--    { "proyectoId": <id_del_proyecto>, "etapaId": <id_de_planificacion> }
--
-- 3. Consultar:
--    GET http://localhost:5180/api/auditorias/<auditoriaId>
-- ============================================================================
