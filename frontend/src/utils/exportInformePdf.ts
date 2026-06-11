// ============================================================================
//  Genera un PDF de texto plano con el contenido de un informe de auditoría.
//  Reutilizado por la pantalla Informes y por el detalle de proyecto.
// ============================================================================
import { jsPDF } from 'jspdf';
import { type Informe, TIPO_INFORME_LABEL } from '../api/informes';

function fmt(iso: string): string {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('es-AR');
}

export function descargarInformePdf(inf: Informe): void {
  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  const pageW = doc.internal.pageSize.getWidth();
  const pageH = doc.internal.pageSize.getHeight();
  const margin = 15;
  let y = margin;

  doc.setFontSize(16); doc.setFont('helvetica', 'bold');
  doc.text('Informe de auditoría', margin, y); y += 9;

  doc.setFontSize(11); doc.setFont('helvetica', 'normal');
  doc.text(`Proyecto: ${inf.nombreProyecto}`, margin, y); y += 6;
  doc.text(`Tipo: ${TIPO_INFORME_LABEL[inf.tipo] ?? inf.tipo}`, margin, y); y += 6;
  doc.text(`Fecha de generación: ${fmt(inf.fechaGeneracion)}`, margin, y); y += 6;

  doc.setDrawColor(190); doc.line(margin, y, pageW - margin, y); y += 7;

  doc.setFontSize(10);
  const lineas = doc.splitTextToSize(inf.contenido || '(sin contenido)', pageW - margin * 2);
  for (const linea of lineas) {
    if (y > pageH - margin) { doc.addPage(); y = margin; }
    doc.text(linea, margin, y);
    y += 5;
  }

  const slug = inf.nombreProyecto.replace(/\s+/g, '_').replace(/[^\w\-]/g, '');
  doc.save(`informe-${slug}-${inf.id}.pdf`);
}