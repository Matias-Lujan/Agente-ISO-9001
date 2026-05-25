# AuditoriaExecutionScreen - Componente React

## Descripción General

Componente funcional de React que implementa una pantalla de ejecución de auditorías ISO 9001 con 3 fases bien definidas:

1. **Formulario de Inicio**: Selección de proyecto y etapa
2. **Estado de Carga**: Indicador visual mientras se ejecuta la auditoría
3. **Vista de Resultados**: Presentación de resultados en 3 tablas principales

## Características

- ✅ **HTML Semántico**: Sin dependencias de librerías de UI
- ✅ **CSS Puro**: Estilos modernos y responsive
- ✅ **React Hooks**: Usa `useState` para gestionar el estado
- ✅ **Datos Mockeados**: Datos de demostración incluidos
- ✅ **Diseño Responsive**: Funciona en desktop, tablet y móvil
- ✅ **Accesibilidad**: Etiquetas label asociadas a inputs

## Props

El componente no requiere props. Funciona de forma independiente con datos mockeados.

## Estado Interno (useState)

```javascript
const [status, setStatus] = useState('form');           // 'form', 'loading', 'results'
const [proyectoSeleccionado, setProyectoSeleccionado] = useState('');
const [etapaSeleccionada, setEtapaSeleccionada] = useState('');
const [resultados, setResultados] = useState(null);
const [error, setError] = useState(null);
```

## Estructura de Datos - Resultados

```javascript
{
  auditoriaId: 100,
  proyectoId: 1,
  etapaId: 1,
  artefactosEvaluados: [
    {
      artefactoEsperadoId: 1,
      nombreArtefacto: 'Manual de Calidad',
      estadoAplicacionTailoring: 'Aplica' | 'No Aplica',
      resultado: 'Conforme' | 'No Conforme' | 'No Evaluado',
      justificacionNoAplica: string
    },
    ...
  ],
  hallazgos: [
    {
      id: string,
      artefactoEsperadoId: number,
      tipo: 'NC' | 'OBS' | 'OM',
      descripcion: string,
      agenteOrigen: string,
      fechaDeteccion: string
    },
    ...
  ],
  documentosAnalizados: [
    {
      id: string,
      nombreArchivo: string,
      fuente: string,
      hashContenido: string,
      tamaño: string,
      fechaCarga: string
    },
    ...
  ]
}
```

## Tablas Incluidas

### 1. Artefactos Evaluados
Columnas:
- **ID Artefacto**: Identificador numérico
- **Nombre**: Nombre descriptivo del artefacto
- **Aplicabilidad Tailoring**: Badge con estado (Aplica/No Aplica)
- **Resultado**: Badge con estado (Conforme/No Conforme/No Evaluado)
- **Justificación**: Texto de justificación si aplica

### 2. Hallazgos
Columnas:
- **ID**: Identificador único del hallazgo
- **Artefacto**: ID del artefacto relacionado
- **Tipo**: Badge con tipo
  - **NC** (No Conformidad) - Rojo
  - **OBS** (Observación) - Amarillo
  - **OM** (Oportunidad de Mejora) - Azul
- **Descripción**: Texto detallado del hallazgo
- **Agente Origen**: Nombre del agente que detectó el hallazgo
- **Fecha**: Fecha de detección

### 3. Documentos Analizados
Columnas:
- **Nombre Archivo**: Nombre del documento
- **Fuente**: Sistema de origen (SharePoint, Teams, OneDrive)
- **Tamaño**: Tamaño del archivo
- **Fecha Carga**: Fecha de carga al sistema
- **Hash Contenido**: Hash SHA del contenido (truncado a 16 caracteres)

## Estilos Principales

### Fases de Color
- **Fase 1 (Formulario)**: Gradiente púrpura-azul
- **Fase 2 (Carga)**: Spinner animado
- **Fase 3 (Resultados)**: Tablas estructuradas

### Badges por Categoría
```css
.estado-aplica:         Verde (aplicable)
.estado-no-aplica:      Rojo (no aplicable)
.resultado-conforme:    Verde (cumple)
.resultado-no-conforme: Rojo (no cumple)
.resultado-no-evaluado: Gris (no evaluado)
.hallazgo-nc:           Rojo (No Conformidad)
.hallazgo-obs:          Amarillo (Observación)
.hallazgo-om:           Azul (Oportunidad de Mejora)
```

## Flujo de Interacción

```
[FASE 1: Formulario]
        ↓
   Usuario selecciona 
   Proyecto + Etapa
        ↓
 Hace clic en "Ejecutar"
        ↓
   [FASE 2: Carga]
   (Simula 2 segundos)
        ↓
   [FASE 3: Resultados]
   Muestra 3 tablas
        ↓
   Usuario puede hacer
   "Nueva Auditoría"
        ↓
   Vuelve a FASE 1
```

## Integración con Backend

### Paso 1: Reemplazar datos mockeados
Cambiar la sección de `resultadosMockeados` por una llamada real al backend:

```javascript
const handleEjecutarAuditoria = async () => {
  if (!proyectoSeleccionado || !etapaSeleccionada) {
    setError('Por favor selecciona un proyecto y una etapa');
    return;
  }

  setError(null);
  setStatus('loading');

  try {
    // Llamada real al backend
    const response = await fetch('/api/auditorias/ejecutar', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        proyectoId: parseInt(proyectoSeleccionado),
        etapaId: parseInt(etapaSeleccionada)
      })
    });

    const data = await response.json();
    setResultados(data);
    setStatus('results');
  } catch (err) {
    setError(`Error: ${err.message}`);
    setStatus('form');
  }
};
```

### Paso 2: Ajustar proyectos y etapas
Reemplazar con datos del backend:

```javascript
const [proyectos, setProyectos] = useState([]);
const [etapas, setEtapas] = useState([]);

useEffect(() => {
  // Cargar proyectos y etapas del backend
  fetch('/api/proyectos').then(r => r.json()).then(setProyectos);
  fetch('/api/etapas').then(r => r.json()).then(setEtapas);
}, []);
```

## Archivos Incluidos

1. **AuditoriaExecutionScreen.jsx** - Componente React
2. **AuditoriaExecutionScreen.css** - Estilos CSS
3. **README.md** - Este archivo

## Instalación y Uso

```bash
# Copiar archivos a tu proyecto React
cp AuditoriaExecutionScreen.jsx src/components/
cp AuditoriaExecutionScreen.css src/components/

# Importar en tu app
import AuditoriaExecutionScreen from './components/AuditoriaExecutionScreen';

# Usar en tu aplicación
function App() {
  return (
    <div>
      <AuditoriaExecutionScreen />
    </div>
  );
}
```

## Responsive Breakpoints

- **Desktop**: 1400px (vista completa)
- **Tablet**: 768px (tablas ajustadas)
- **Mobile**: 480px (tablas compactadas)

## Notas Importantes

- El componente simula una demora de 2 segundos en la fase de carga
- Los datos son completamente mockeados para demostración
- No hay validación backend en esta versión
- Los iconos/badges se basan en CSS puro (sin librerías)

## Próximos Pasos

Para producción:
1. Conectar con el backend real
2. Implementar manejo de errores más robusto
3. Agregar paginación para tablas grandes
4. Implementar exportación a PDF/Excel
5. Agregar filtros y búsqueda
6. Implementar autenticación y autorización

## Autor

Generado como componente educativo/demostrativo.
