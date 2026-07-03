-- ============================================================================
--  seed-install.sql — Seed de INSTALACIÓN (marco de calidad de BDT)
-- ----------------------------------------------------------------------------
--  Carga el MARCO que el sistema necesita para poder auditar: el procedimiento
--  PR 11-13, sus etapas, los artefactos esperados, qué artefacto es el tailoring
--  y la referencia de vigencias del Departamento de Calidad.
--
--  NO incluye proyectos ni usuarios: los proyectos se cargan desde el front,
--  y el primer usuario administrador se crea aparte (ver README).
--
--  Ejecutar UNA vez sobre la base ya creada (después de 'dotnet ef database
--  update'). Es idempotente (INSERT IGNORE / UPDATE): puede correrse de nuevo
--  sin duplicar ni romper.
--
--  ⚠️  ANTES DE USAR: reemplazar el folder de templates (sección 4) por el de BDT.
-- ============================================================================

USE iso_audit_agent;

-- ----------------------------------------------------------------------------
-- 1. Procedimiento maestro
-- ----------------------------------------------------------------------------
INSERT IGNORE INTO procedimientos (Id, Codigo, Nombre, Descripcion) VALUES
  (1, 'PR 11-13', 'Diseño y Análisis, Desarrollo e Implementación de Software',
   'Procedimiento maestro de BDT Global. Establece la metodología para diseño, desarrollo, pruebas e implementación de software. Revisión 13 vigente desde 13/01/2026.');

-- ----------------------------------------------------------------------------
-- 2. Etapas del PR 11-13
-- ----------------------------------------------------------------------------
INSERT IGNORE INTO etapas (Id, ProcedimientoId, Nombre, Orden, Descripcion) VALUES
  (1, 1, 'Planificación',     1, 'Definición del alcance, tailoring, control de costos inicial, cronograma y configuración de Trello/Clockify.'),
  (2, 1, 'Análisis y diseño', 2, 'Definición de tarjetas en Trello, arquitectura (cuando aplica) y escritura de casos de prueba en TestLodge.'),
  (3, 1, 'Desarrollo',        3, 'Programación, versionado del código fuente, generación de paquetes de despliegue y liberación de software.'),
  (4, 1, 'Testing',           4, 'Ejecución de los casos de prueba en TestLodge y verificación de resultados.'),
  (5, 1, 'Implementación',    5, 'Instalación en entorno productivo, pruebas de aceptación, cierre con Sign-Off y encuesta de satisfacción.');

-- ----------------------------------------------------------------------------
-- 3. Artefactos esperados del PR 11-13 (18)
-- ----------------------------------------------------------------------------
-- MandatorioTipoA/B: tipo A = Mandatorio (TRUE), tipo B = Evaluar y Justificar.
-- Excepción: FR 85 y FR 47 son FALSE/FALSE (siempre evaluar y justificar).

-- Etapa 1: Planificación
INSERT IGNORE INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
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
   'Identificación, evaluación y plan de respuesta a los riesgos del proyecto.',
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
INSERT IGNORE INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
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
INSERT IGNORE INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (12, 3, NULL, 'Código fuente versionado',
   'Código fuente del proyecto administrado en SVN o GIT. Es la documentación principal de esta etapa.',
   TRUE, FALSE, NULL),
  (13, 3, 'FR 25', 'Liberación de Software',
   'Documento que indica funcionalmente qué se está liberando (nuevas funcionalidades, cambios o correcciones). Acompaña al paquete de despliegue cuando se genera una versión.',
   TRUE, FALSE, NULL),
  (14, 3, 'FR 46', 'Manual de Instalación',
   'Manual de instalación del software entregable.',
   TRUE, FALSE, NULL);

-- Etapa 4: Testing
INSERT IGNORE INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
  (15, 4, NULL, 'Ejecución de pruebas',
   'Registro de la ejecución de los casos de prueba: pruebas realizadas, resultados obtenidos y descripción de errores en caso de fallas.',
   TRUE, FALSE, NULL);

-- Etapa 5: Implementación
INSERT IGNORE INTO artefactos_esperados (Id, EtapaId, Codigo, Nombre, Descripcion, MandatorioTipoA, MandatorioTipoB, PathTemplateRelativo) VALUES
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
-- 3.b Fuente de verificación (Trello / Clockify)
-- ----------------------------------------------------------------------------
-- Los demás artefactos quedan con el default 'Drive' que pone la migración.
UPDATE artefactos_esperados SET FuenteVerificacion = 'Trello'   WHERE Id = 7;
UPDATE artefactos_esperados SET FuenteVerificacion = 'Clockify' WHERE Id = 8;
UPDATE artefactos_esperados SET FuenteVerificacion = 'Trello'   WHERE Id = 9;

-- ----------------------------------------------------------------------------
-- 3.c Artefacto de tailoring
-- ----------------------------------------------------------------------------
-- Marca cuál artefacto ES el tailoring del procedimiento (uno solo). El sistema
-- lo usa para ubicar el archivo en Drive por su código/nombre.
UPDATE artefactos_esperados SET EsTailoring = TRUE WHERE Id = 2;   -- FR 29

-- ----------------------------------------------------------------------------
-- 4. Carpeta de templates en Google Drive  ⚠️  COMPLETAR
-- ----------------------------------------------------------------------------
-- Folder ID de Drive donde viven los templates de los formularios (FR 29, FR 30,
-- etc.). El ResolutorContexto lo concatena con PathTemplateRelativo de cada
-- artefacto. REEMPLAZAR el placeholder por el folder real de BDT (o actualizarlo
-- con: UPDATE configuraciones_sistema SET Valor='<folderId>' WHERE Clave='path_carpeta_templates';).
INSERT IGNORE INTO configuraciones_sistema (Id, Clave, Valor, Descripcion) VALUES
  (1, 'path_carpeta_templates', 'REEMPLAZAR_CON_FOLDER_ID_DE_TEMPLATES',
   'Folder ID de Google Drive donde viven los templates de los artefactos.');

-- ----------------------------------------------------------------------------
-- 5. Referencia del Departamento de Calidad — vigencia vigente por formulario
-- ----------------------------------------------------------------------------
-- El sistema compara la vigencia detectada en el documento del proyecto contra
-- la vigente registrada acá: si difieren → OBS (formulario desactualizado); si un
-- FR no figura acá → no se valida su vigencia (solo log). Esta tabla la mantiene
-- el Departamento de Calidad; agregar/actualizar filas a medida que provea sus
-- vigencias. Fechas en dd-mm-yyyy (STR_TO_DATE las convierte a DATE).
INSERT IGNORE INTO formularios_calidad (Id, CodigoFormulario, Nombre, VigenciaVigente) VALUES
  (1, 'FR 30', 'Especificaciones de Requerimiento de Software (ERS)', STR_TO_DATE('13-08-2020', '%d-%m-%Y')),
  (2, 'FR 48', 'Sign-Off', STR_TO_DATE('01-07-2021', '%d-%m-%Y'));
