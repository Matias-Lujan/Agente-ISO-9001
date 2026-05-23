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
--   - Usa llaves naturales: codigo, nombre, email.
--   - Compatible con MySQL Workbench Safe Updates: no usa UPDATE sin key.
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

INSERT INTO configuraciones_sistema (clave, valor, descripcion)
SELECT
    'path_carpeta_templates',
    'templates',
    'Carpeta local configurable para templates de artefactos. Seed D7.'
WHERE NOT EXISTS (
    SELECT 1
    FROM configuraciones_sistema
    WHERE clave = 'path_carpeta_templates'
);

-- ---------------------------------------------------------------------------
-- 1. Procedimiento PR 11-13
-- ---------------------------------------------------------------------------

INSERT INTO procedimientos (codigo, nombre, descripcion)
SELECT
    'PR 11-13',
    'Diseño y Análisis, Desarrollo e Implementación de Software',
    'Procedimiento maestro para proyectos llave en mano y customizaciones. Seed D7.'
WHERE NOT EXISTS (
    SELECT 1
    FROM procedimientos
    WHERE codigo = 'PR 11-13'
);

-- ---------------------------------------------------------------------------
-- 2. Etapa Planificación
-- ---------------------------------------------------------------------------

INSERT INTO etapas (procedimiento_id, nombre, orden, descripcion)
SELECT
    p.id,
    'Planificación',
    1,
    'Etapa inicial del proyecto: tailoring, ERS, cronograma, riesgos. Seed D7.'
FROM procedimientos p
WHERE p.codigo = 'PR 11-13'
  AND NOT EXISTS (
      SELECT 1
      FROM etapas e
      WHERE e.procedimiento_id = p.id
        AND e.nombre = 'Planificación'
  );

-- ---------------------------------------------------------------------------
-- 3. Usuario demo
-- ---------------------------------------------------------------------------
-- password_hash es placeholder porque el MVP actual todavía no implementa
-- login/JWT.
-- ---------------------------------------------------------------------------

INSERT INTO usuarios (email, nombre, rol, password_hash, activo, fecha_creacion)
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
    WHERE email = 'test@bdt-demo.local'
);

-- ---------------------------------------------------------------------------
-- 4. Proyecto demo
-- ---------------------------------------------------------------------------

INSERT INTO proyectos (
    nombre,
    descripcion,
    fecha_inicio,
    fecha_fin,
    tipo_proyecto,
    horas_estimadas,
    procedimiento_id,
    trello_board_id,
    clockify_project_id,
    drive_folder_id,
    activo
)
SELECT
    'Proyecto Demo D7 (E2E)',
    'Proyecto de demo para validar el flujo E2E del MVP. Seed D7.',
    UTC_TIMESTAMP(),
    NULL,
    'A',
    1500,
    p.id,
    NULL,
    NULL,
    '15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n',
    1
FROM procedimientos p
WHERE p.codigo = 'PR 11-13'
  AND NOT EXISTS (
      SELECT 1
      FROM proyectos pr
      WHERE pr.nombre = 'Proyecto Demo D7 (E2E)'
  );

-- ---------------------------------------------------------------------------
-- 5. Relación proyecto_usuario
-- ---------------------------------------------------------------------------

INSERT INTO proyecto_usuarios (proyecto_id, usuario_id)
SELECT
    pr.id,
    u.id
FROM proyectos pr
CROSS JOIN usuarios u
WHERE pr.nombre = 'Proyecto Demo D7 (E2E)'
  AND u.email = 'test@bdt-demo.local'
  AND NOT EXISTS (
      SELECT 1
      FROM proyecto_usuarios pu
      WHERE pu.proyecto_id = pr.id
        AND pu.usuario_id = u.id
  );

-- ---------------------------------------------------------------------------
-- 6. Artefactos esperados — etapa Planificación
-- ---------------------------------------------------------------------------
-- Subconjunto D7: 6 artefactos alineados con el FR-29 demo.
-- Idempotencia por (etapa_id, nombre).
-- ---------------------------------------------------------------------------

-- 6.1 FR 29 — Tailoring del Proceso
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    'FR 29',
    'Tailoring del Proceso',
    'Tailoring inicial del proyecto. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Tailoring del Proceso'
  );

-- 6.2 FR 30 — ERS
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    'FR 30',
    'Especificacion de Requerimientos de Software (ERS)',
    'Documento de requerimientos del proyecto. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Especificacion de Requerimientos de Software (ERS)'
  );

-- 6.3 FR 31 — Planilla de riesgos
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    'FR 31',
    'Planilla de riesgos',
    'Registro de riesgos identificados del proyecto. Seed D7.',
    0,
    0,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Planilla de riesgos'
  );

-- 6.4 Cronograma (MS Project)
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    NULL,
    'Cronograma (MS Project)',
    'Cronograma del proyecto en MS Project. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Cronograma (MS Project)'
  );

-- 6.5 Proyecto en Trello - creación
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    NULL,
    'Proyecto en Trello - creación',
    'Tablero de Trello del proyecto. En D7 permite validar el caso NoAplica sin justificación según el FR-29 demo.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Proyecto en Trello - creación'
  );

-- 6.6 FR 48 — Sign-Off
INSERT INTO artefactos_esperados (
    etapa_id,
    codigo,
    nombre,
    descripcion,
    mandatorio_tipo_a,
    mandatorio_tipo_b,
    path_template_relativo
)
SELECT
    e.id,
    'FR 48',
    'Sign-Off',
    'Documento de cierre y conformidad del cliente. Seed D7.',
    1,
    1,
    NULL
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
  AND NOT EXISTS (
      SELECT 1
      FROM artefactos_esperados ae
      WHERE ae.etapa_id = e.id
        AND ae.nombre = 'Sign-Off'
  );

-- ============================================================================
-- Verificación
-- ============================================================================

SELECT
    'CONFIGURACION' AS bloque,
    clave,
    valor
FROM configuraciones_sistema
WHERE clave = 'path_carpeta_templates';

SELECT
    'PROCEDIMIENTO' AS bloque,
    p.id,
    p.codigo,
    p.nombre
FROM procedimientos p
WHERE p.codigo = 'PR 11-13';

SELECT
    'ETAPA' AS bloque,
    e.id,
    e.procedimiento_id,
    e.nombre,
    e.orden
FROM etapas e
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
ORDER BY e.orden;

SELECT
    'USUARIO' AS bloque,
    u.id,
    u.email,
    u.rol,
    u.activo
FROM usuarios u
WHERE u.email = 'test@bdt-demo.local';

SELECT
    'PROYECTO' AS bloque,
    pr.id,
    pr.nombre,
    pr.tipo_proyecto,
    pr.horas_estimadas,
    pr.procedimiento_id,
    pr.drive_folder_id,
    pr.activo
FROM proyectos pr
WHERE pr.nombre = 'Proyecto Demo D7 (E2E)';

SELECT
    'PROYECTO_USUARIOS' AS bloque,
    pu.id,
    pu.proyecto_id,
    pu.usuario_id
FROM proyecto_usuarios pu
JOIN proyectos pr ON pu.proyecto_id = pr.id
JOIN usuarios u ON pu.usuario_id = u.id
WHERE pr.nombre = 'Proyecto Demo D7 (E2E)'
  AND u.email = 'test@bdt-demo.local';

SELECT
    'ARTEFACTOS_ESPERADOS' AS bloque,
    ae.id,
    ae.codigo,
    ae.nombre,
    ae.mandatorio_tipo_a,
    ae.mandatorio_tipo_b
FROM artefactos_esperados ae
JOIN etapas e ON ae.etapa_id = e.id
JOIN procedimientos p ON e.procedimiento_id = p.id
WHERE p.codigo = 'PR 11-13'
  AND e.nombre = 'Planificación'
ORDER BY ae.id;

-- ============================================================================
-- Luego de ejecutar el seed:
--
-- POST http://localhost:5180/api/auditorias
--
-- {
--   "proyectoId": <id_del_proyecto>,
--   "etapaId": <id_de_planificacion>,
--   "usuarioId": <id_del_usuario>
-- }
--
-- GET http://localhost:5180/api/auditorias/<auditoriaId>
-- GET http://localhost:5180/api/_smoke/auditorias/<auditoriaId>/resultado
-- ============================================================================
