# Snapshots — red de seguridad del refactor de Agents/

Golden output (patrón approval) que congela el comportamiento de las piezas
**puras y determinísticas** del workflow, para detectar cualquier cambio de
comportamiento durante el refactor minimalista de `Agents/`.

Qué se congela (frontera determinística, sin LLM):
- `*PromptBuilder.Construir(...)` — los prompts byte-idénticos que se le mandan al LLM.
- `HallazgosDeterministicos.Generar(...)` y `HallazgosEstructurales.Generar(...)` — hallazgos generados en C#.
- `ClasificacionResponseParser.Parsear(...)` — mapeo del JSON del LLM al DTO tipado.

Los `*.approved.txt` son la referencia, **versionados en git**. El helper está en
`../Infra/Snapshot.cs`.

## Flujo

- Si un test escribe un `*.received.txt` y falla → el output actual difiere del
  aprobado. Es un **cambio de comportamiento**: durante el refactor esto NO debería
  pasar (el objetivo es cero cambio). Revisar el diff `approved` vs `received`.
- Para **re-aprobar un cambio intencional**: revisar el diff, reemplazar el
  `.approved.txt` con el contenido del `.received.txt` y borrar el `.received.txt`.
- Los `*.received.txt` no se versionan (son transitorios de una corrida fallida).
