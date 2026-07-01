// ============================================================================
//  Genera un PDF con la lista de hallazgos.
//
//  Usa jsPDF (libreria liviana, ~200KB). Hace un PDF basico de texto plano
//  con encabezado + tabla manual + paginacion automatica.
//
//  `construirHallazgosPdf` es el armador compartido (devuelve el jsPDF sin
//  guardar): lo reutiliza la pantalla de Hallazgos (export) y la de Informes
//  (descarga + preview en iframe), así ambos PDF son idénticos en formato.
//
//  Cuando el backend tenga el endpoint /api/informes/hallazgos.pdf vamos a
//  delegar la generacion al servidor (mejor calidad), pero por ahora esto
//  resuelve el caso.
// ============================================================================

import { jsPDF } from 'jspdf';
import {
  type Hallazgo,
  TIPO_LABEL_LARGO,
} from '../api/hallazgos';

interface ExportOpts {
  hallazgos: Hallazgo[];
  generadoPor: string;  // nombre del usuario logueado
}

interface ConstruirPdfOpts {
  titulo: string;
  metaLineas: string[];   // líneas de metadata bajo el subtítulo branded
  hallazgos: Hallazgo[];
}

/**
 * Arma el PDF "lindo" de hallazgos y devuelve el documento (sin guardar).
 * Fuente única de verdad del formato; quien llama decide guardar o previsualizar.
 */
export function construirHallazgosPdf({ titulo, metaLineas, hallazgos }: ConstruirPdfOpts): jsPDF {
  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  const pageWidth = doc.internal.pageSize.getWidth();
  const pageHeight = doc.internal.pageSize.getHeight();
  const margin = 15;
  let y = margin;

  // ── Encabezado ─────────────────────────────────────────────────────────
  doc.setFontSize(16);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(30, 16, 80); // #1e1050
  doc.text(titulo, margin, y);
  y += 7;

  doc.setFontSize(10);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(123, 106, 170); // #7b6aaa
  doc.text('BDT Global — Plataforma de Auditoría ISO 9001', margin, y);
  y += 5;

  for (const linea of metaLineas) {
    doc.text(linea, margin, y);
    y += 4;
  }
  y += 4;

  // ── Resumen por tipo ───────────────────────────────────────────────────
  const nc = hallazgos.filter((h) => h.tipo === 'NoConformidad').length;
  const obs = hallazgos.filter((h) => h.tipo === 'Observacion').length;
  const om = hallazgos.filter((h) => h.tipo === 'OportunidadMejora').length;

  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(30, 16, 80);
  doc.text('Resumen:', margin, y);
  y += 5;

  doc.setFont('helvetica', 'normal');
  doc.setFontSize(10);
  doc.setTextColor(60, 60, 60);
  doc.text(`• No conformidades: ${nc}`, margin + 3, y); y += 4;
  doc.text(`• Observaciones: ${obs}`, margin + 3, y); y += 4;
  doc.text(`• Oportunidades de mejora: ${om}`, margin + 3, y); y += 8;

  // ── Listado de hallazgos ───────────────────────────────────────────────
  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(30, 16, 80);
  doc.text('Detalle:', margin, y);
  y += 6;

  for (let i = 0; i < hallazgos.length; i++) {
    const h = hallazgos[i];

    // Verificar si necesitamos nueva pagina (estimamos ~40mm por hallazgo)
    if (y > pageHeight - 40) {
      doc.addPage();
      y = margin;
    }

    // Numero + titulo
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(30, 16, 80);
    const tituloLines = doc.splitTextToSize(`${i + 1}. ${h.titulo}`, pageWidth - margin * 2);
    doc.text(tituloLines, margin, y);
    y += tituloLines.length * 5;

    // Metadata en una linea
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(123, 106, 170);
    doc.text(
      `${TIPO_LABEL_LARGO[h.tipo]}  ·  Proyecto: ${h.proyecto}`,
      margin + 3,
      y,
    );
    y += 4;

    // Descripcion
    doc.setTextColor(60, 60, 60);
    const descLines = doc.splitTextToSize(`Descripción: ${h.descripcion}`, pageWidth - margin * 2 - 3);
    doc.text(descLines, margin + 3, y);
    y += descLines.length * 4;

    // Evidencia
    const evidLines = doc.splitTextToSize(`Evidencia: ${h.evidencia}`, pageWidth - margin * 2 - 3);
    doc.text(evidLines, margin + 3, y);
    y += evidLines.length * 4 + 5;
  }

  // ── Numerar paginas ────────────────────────────────────────────────────
  const totalPages = doc.getNumberOfPages();
  for (let i = 1; i <= totalPages; i++) {
    doc.setPage(i);
    doc.setFontSize(9);
    doc.setTextColor(150, 150, 150);
    doc.text(
      `Página ${i} de ${totalPages}`,
      pageWidth - margin,
      pageHeight - 8,
      { align: 'right' },
    );
  }

  return doc;
}

/** Exporta la revisión de hallazgos de la pantalla Hallazgos (arma + descarga). */
export function exportarHallazgosPdf({ hallazgos, generadoPor }: ExportOpts): void {
  const ahora = new Date().toLocaleString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
  const doc = construirHallazgosPdf({
    titulo: 'Informe de Hallazgos',
    metaLineas: [
      `Generado por: ${generadoPor}  ·  Fecha: ${ahora}`,
      `Total de hallazgos: ${hallazgos.length}`,
    ],
    hallazgos,
  });
  doc.save(`informe-hallazgos-${new Date().toISOString().slice(0, 10)}.pdf`);
}