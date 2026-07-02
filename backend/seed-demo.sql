-- seed-30052.sql
-- Carga la BD en estado "proyecto BDT 30.052 (App Productores) listo para auditar".
--
-- Supone BD vacía (sin filas, IDs autoincrementales reseteados). Para vaciar:
--   - Opción A: drop database + dotnet ef database update
--   - Opción B: TRUNCATEs manuales (SET FOREIGN_KEY_CHECKS = 0; ...; SET FOREIGN_KEY_CHECKS = 1;)
--
-- Referencias:
--   - PR 11-13 Diseño y Análisis, Desarrollo e Implementación de Software (rev. 13, 01/2026)
--   - calidad-auditoria.md §6 (mapeo etapa → artefactos)
--   - modelo_bd.md (schema)
--
-- Convención validada contra migraciones EF / ISOAuditAgentDbContext:
--   tablas en plural snake_case (procedimientos, etapas, artefactos_esperados, etc.)
--   columnas en PascalCase según propiedades C# (EF no configura HasColumnName).
-- Convive con seed-demo.sql — no lo reemplaza.

USE iso_audit_agent;

-- ----------------------------------------------------------------------------
-- 1. Procedimiento maestro
-- ----------------------------------------------------------------------------
INSERT INTO procedimientos (Id, Codigo, Nombre, Descripcion) VALUES
  (1, 'PR 11-13', 'Diseño y Análisis, Desarrollo e Implementación de Software',
   'Procedimiento maestro de BDT Global. Establece la metodología para diseño, desarrollo, pruebas e implementación de software. Revisión 13 vigente desde 13/01/2026.');

-- ----------------------------------------------------------------------------
-- 2. Etapas del PR 11-13
-- ----------------------------------------------------------------------------
-- Se modelan 5 etapas. "Revisión" no se modela como etapa auditable porque
-- no produce artefactos formales propios (decisión en calidad-auditoria.md §5).
INSERT INTO etapas (Id, ProcedimientoId, Nombre, Orden, Descripcion) VALUES
  (1, 1, 'Planificación',     1, 'Definición del alcance, tailoring, control de costos inicial, cronograma y configuración de Trello/Clockify.'),
  (2, 1, 'Análisis y diseño', 2, 'Definición de tarjetas en Trello, arquitectura (cuando aplica) y escritura de casos de prueba en TestLodge.'),
  (3, 1, 'Desarrollo',        3, 'Programación, versionado del código fuente, generación de paquetes de despliegue y liberación de software.'),
  (4, 1, 'Testing',           4, 'Ejecución de los casos de prueba en TestLodge y verificación de resultados.'),
  (5, 1, 'Implementación',    5, 'Instalación en entorno productivo, pruebas de aceptación, cierre con Sign-Off y encuesta de satisfacción.');
  
-- ----------------------------------------------------------------------------
-- 3. Artefactos esperados del PR 11-13
-- ----------------------------------------------------------------------------
-- 18 artefactos derivados del análisis textual del PR + tabla REGISTROS Y ARCHIVO.
-- Etapa de FR 31 y FR 46 tomada del tailoring del 30.052 (PR no las indica
-- explícitamente).
--
-- MandatorioTipoA/B: regla organizacional del FR 29 (hoja Referencias) —
-- tipo A = Mandatorio (TRUE), tipo B = Evaluar y Justificar (FALSE).
-- Excepción: FR 85 y FR 47 son FALSE/FALSE (siempre evaluar y justificar).
-- Limitación documentada: el sistema no detecta inconsistencias del tipo
-- "tailoring dice no aplica por <300h, pero el proyecto tiene >300h"
-- (ver punto D pendiente en calidad-auditoria.md).

-- Etapa 1: Planificación
INSERT INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (1, 1, 'FR 11', 'Minuta de Kickoff',
   'Acta de la reunión de inicio del proyecto donde se establecen fecha de inicio, roles del equipo y fechas objetivo con el cliente.',
   TRUE, FALSE, NULL),
  (2, 1, 'FR 29', 'Tailoring del Proyecto',
   'Documento donde el Gerente de Operaciones define qué artefactos son obligatorios para el proyecto, su responsable y eventuales justificaciones.',
   TRUE, FALSE, 'FR29-05_Tailoring_de_proyecto_template.xlsx'),
  (3, 1, 'FR 30', 'Especificaciones de Requerimiento de Software (ERS)',
   'Definición del alcance del proyecto. En proyectos tipo B puede reemplazarse por el alcance descripto en la propuesta comercial según el nivel de detalle.',
   TRUE, FALSE, 'FR30-02_ERS_template.docx'),
  (4, 1, 'FR 31', 'Matriz de Riesgo',
   'Identificación, evaluación y plan de respuesta a los riesgos del proyecto. Etapa asignada según tailoring del 30.052 (PR no la indica explícitamente).',
   TRUE, FALSE, 'FR31-03_Matriz_de_Riesgo_template.xlsx'),
  (5, 1, NULL, 'Cronograma del proyecto',
   'Planificación temporal del proyecto generada con MS Project. Puede tener mayor o menor detalle según la magnitud del proyecto.',
   TRUE, FALSE, NULL),
  (6, 1, 'FR 71', 'Control de Costos',
   'Planilla base de control de costos. Se mantiene y actualiza en cada etapa; el sistema selecciona la última versión por fecha en el nombre (formato FR 71-YYYYMMDD).',
   TRUE, FALSE, 'FR71-01_Control_de_costos_template.xlsx'),
  (7, 1, NULL, 'Instancia de proyecto en Trello',
   'Tablero de Trello creado para gestionar las tarjetas del proyecto.',
   TRUE, FALSE, NULL),
  (8, 1, NULL, 'Instancia de proyecto en Clockify',
   'Instancia en Clockify donde se registran las horas insumidas por el equipo en todas las etapas.',
   TRUE, FALSE, NULL);

-- Etapa 2: Análisis y diseño
INSERT INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (9,  2, NULL, 'Tarjetas en Trello',
   'Tarjetas del proyecto definidas por el Líder de Proyecto junto con los programadores, basadas en el documento de alcance.',
   TRUE, FALSE, NULL),
  (10, 2, NULL, 'Documento de Arquitectura',
   'Arquitectura de la solución generada por el Arquitecto. No se genera cuando se trata de implementación de un producto BDT existente. El PR no define template formal: se audita por presencia, no por estructura.',
   TRUE, FALSE, NULL),
  (11, 2, NULL, 'Casos de prueba',
   'Casos de prueba escritos por el Tester en colaboración con el Analista Funcional. Mandatorio para tipo A; en tipo B se evalúan condiciones y se justifica si no es posible.',
   TRUE, FALSE, NULL);

-- Etapa 3: Desarrollo
INSERT INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (12, 3, NULL, 'Código fuente versionado',
   'Código fuente del proyecto administrado en SVN o GIT. Es la documentación principal de esta etapa.',
   TRUE, FALSE, NULL),
  (13, 3, 'FR 25', 'Liberación de Software',
   'Documento que indica funcionalmente qué se está liberando (nuevas funcionalidades, cambios o correcciones). Acompaña al paquete de despliegue cuando se genera una versión.',
   TRUE, FALSE, NULL),
  (14, 3, 'FR 46', 'Manual de Instalación',
   'Manual de instalación del software entregable. Etapa asignada según tailoring del 30.052 (PR no la indica explícitamente).',
   TRUE, FALSE, NULL);

-- Etapa 4: Testing
INSERT INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (15, 4, NULL, 'Ejecución de pruebas',
   'Registro de la ejecución de los casos de prueba: pruebas realizadas, resultados obtenidos y descripción de errores en caso de fallas.',
   TRUE, FALSE, NULL);

-- Etapa 5: Implementación
INSERT INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (16, 5, 'FR 48', 'Sign-Off',
   'Cierre formal del proyecto. Describe los entregables y registra la conformidad del cliente (correo electrónico guardado junto al documento).',
   TRUE, FALSE, 'FR48-01_Sign_Off_template.docx'),
  (17, 5, 'FR 85', 'Encuesta de Sign-Off',
   'Encuesta de satisfacción enviada al cliente al cierre del proyecto. Según el PR aplica a proyectos cerrados de más de 300 horas; la exigibilidad la determina el tailoring del proyecto auditado.',
   FALSE, FALSE, NULL),
  (18, 5, 'FR 47', 'Reclamo de Clientes',
   'Registro de respuestas insatisfactorias detectadas en la encuesta de Sign-Off (FR 85), gestionado hasta su cierre por el Líder Comercial y el Gerente responsable de desarrollo.',
   FALSE, FALSE, NULL);

-- ----------------------------------------------------------------------------
-- 4. Usuario auditor
-- ----------------------------------------------------------------------------
-- PasswordHash: placeholder — el MVP aún no implementa login/JWT ni hasher
-- (mismo criterio que seed-demo.sql). Cuando exista AuthService, reemplazar
-- por hash real (ej. contraseña en claro sugerida: auditor123).
-- INSERT INTO usuarios (Id, Nombre, Email, PasswordHash, Rol, Activo, FechaCreacion) VALUES
--  (1, 'Auditor', 'auditor@bdtglobal.com.ar', '$placeholder-no-auth-yet$', 'Auditor', TRUE, NOW());

-- ----------------------------------------------------------------------------
-- 5. Proyecto 30.052 — App Productores
-- ----------------------------------------------------------------------------
-- HorasEstimadas: 800 (hardcodeado, no tenemos el dato real; consistente
-- con tipo B = ≤1200h).
-- FechaInicio: NOT NULL en schema (Proyecto.FechaInicio) — se deja hardcodeada.
-- FechaFin: nullable en schema (Proyecto.FechaFin) — NULL.
-- TrelloBoardId, ClockifyProjectId: NULL (fuera de scope MVP según
-- calidad-auditoria.md).
INSERT INTO proyectos (Id, Nombre, Descripcion, FechaInicio, FechaFin, TipoProyecto, HorasEstimadas, ProcedimientoId, TrelloBoardId, ClockifyProjectId, DriveFolderId, Activo) VALUES
  (1, 'App Productores',
   'Proyecto 30.052. Material real cedido por BDT Global para validar la calidad de la auditoría.',
   '2024-01-01', NULL, 'B', 800, 1, NULL, NULL, '15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n', TRUE);

-- ----------------------------------------------------------------------------
-- 6. Asignación auditor → proyecto
-- ----------------------------------------------------------------------------
INSERT INTO proyectos_usuarios (Id, ProyectoId, UsuarioId) VALUES
  (1, 1, 1);

-- ----------------------------------------------------------------------------
-- 7. Configuración global del sistema
-- ----------------------------------------------------------------------------
INSERT INTO configuraciones_sistema (Id, Clave, Valor, Descripcion) VALUES
  (1, 'path_carpeta_templates', '1OK2qSmcqJD5UkhZ0WR8KwBT-oaZR-l9S',
   'Folder ID de Google Drive donde viven los templates de los artefactos. El ResolutorContexto lo concatena con artefacto_esperado.PathTemplateRelativo para obtener la referencia completa al archivo del template.');

-- ----------------------------------------------------------------------------
-- 8. FuenteVerificacion para artefactos Trello y Clockify
-- ----------------------------------------------------------------------------
-- Se aplica después de la migración AddFuenteVerificacion.
-- Los demás artefactos tienen el default 'Drive' puesto por la migración.
UPDATE artefactos_esperados SET FuenteVerificacion = 'Trello'   WHERE Id = 7;
UPDATE artefactos_esperados SET FuenteVerificacion = 'Clockify'  WHERE Id = 8;
UPDATE artefactos_esperados SET FuenteVerificacion = 'Trello'   WHERE Id = 9;

-- ----------------------------------------------------------------------------
-- 8.b Artefacto de tailoring (FR 29)
-- ----------------------------------------------------------------------------
-- Marca cuál artefacto ES el tailoring del procedimiento. TailoringReader lo usa
-- para ubicar el archivo en Drive por su código/nombre, en vez de un "FR 29"
-- hardcodeado. Debe haber exactamente uno por procedimiento.
UPDATE artefactos_esperados SET EsTailoring = TRUE WHERE Id = 2;

-- ----------------------------------------------------------------------------
-- 9. Referencia del Departamento de Calidad — vigencia vigente por formulario
-- ----------------------------------------------------------------------------
-- Fuente de la "vigencia esperada". El sistema compara la Vigencia detectada en
-- el documento del proyecto contra la vigente acá registrada:
--   - difieren (o el documento no declara vigencia) → hallazgo OBS (formulario
--     desactualizado);
--   - coinciden → Conforme;
--   - el FR no figura acá → no se valida la vigencia (solo log/warning).
--
-- ILUSTRATIVA hasta que BDT comparta el registro real del Depto. de Calidad. Se
-- cargan las vigencias reales conocidas de los formularios del 30.052: coinciden
-- con los documentos del proyecto, así que por defecto dan Conforme (sin falsos
-- positivos). Para ver el OBS en una demo, basta modificar una vigencia acá para
-- que difiera de la del documento (el cliente pidió justamente poder "modificar
-- datos para ver el comportamiento"). Agregar más FRs a medida que Calidad provea
-- sus vigencias.
-- Fechas escritas en dd-mm-yyyy (como aparecen en los formularios). STR_TO_DATE
-- las convierte a la columna DATE (vigencia_vigente); el comparador interno usa
-- DateOnly, no texto, así que el formato de acá es solo para legibilidad del seed.
INSERT INTO formularios_calidad (Id, CodigoFormulario, Nombre, VigenciaVigente) VALUES
  (1, 'FR 30', 'Especificaciones de Requerimiento de Software (ERS)', STR_TO_DATE('13-08-2020', '%d-%m-%Y')),
  (2, 'FR 48', 'Sign-Off', STR_TO_DATE('01-07-2021', '%d-%m-%Y'));