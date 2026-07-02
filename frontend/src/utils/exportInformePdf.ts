// ============================================================================
//  PDF de un informe de auditoría.
//
//  Usa el MISMO generador "lindo" que la pantalla de Hallazgos
//  (construirHallazgosPdf), alimentado con los hallazgos reales de la auditoría
//  del informe. Así el PDF de Informes y el de Hallazgos son idénticos en
//  formato. La previsualización embebe el PDF real (blob) en un iframe, de modo
//  que lo que se ve es exactamente lo que se descarga.
// ============================================================================
import type { jsPDF } from 'jspdf';
import { type Informe } from '../api/informes';
import { listarHallazgosDeAuditoria } from '../api/hallazgos';
import { construirHallazgosPdf } from './exportHallazgosPdf';

function fmt(iso: string): string {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('es-AR');
}

function filename(inf: Informe): string {
  const slug = inf.nombreProyecto.replace(/\s+/g, '_').replace(/[^\w\-]/g, '');
  return `informe-${slug}-${inf.id}.pdf`;
}

/** Arma el PDF del informe a partir de los hallazgos de su auditoría. */
async function construirInformeDoc(inf: Informe): Promise<jsPDF> {
  const hallazgos = (await listarHallazgosDeAuditoria(inf.auditoriaId))
    .map((h) => ({ ...h, proyecto: inf.nombreProyecto }));

  return construirHallazgosPdf({
    titulo: 'Informe de auditoría',
    metaLineas: [
      `Proyecto: ${inf.nombreProyecto}  ·  Fecha de generación: ${fmt(inf.fechaGeneracion)}`,
      `Total de hallazgos: ${hallazgos.length}`,
    ],
    hallazgos,
  });
}

/** Descarga el PDF del informe. */
export async function descargarInformePdf(inf: Informe): Promise<void> {
  const doc = await construirInformeDoc(inf);
  doc.save(filename(inf));
}

/**
 * Genera el PDF y devuelve un blob URL para previsualizarlo en un <iframe>.
 * Quien lo llama debe hacer URL.revokeObjectURL(url) cuando deje de usarlo.
 */
export async function previsualizarInformePdf(inf: Informe): Promise<string> {
  const doc = await construirInformeDoc(inf);
  return URL.createObjectURL(doc.output('blob'));
}
