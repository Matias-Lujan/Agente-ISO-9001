// ============================================================================
//  exportDashboardPdf — exporta el dashboard a PDF (A4) capturando los
//  gráficos TAL CUAL se ven en pantalla.
//
//  Usa html2canvas para rasterizar cada bloque marcado con la clase
//  `.dash-pdf-block` y los va colocando en el PDF (jsPDF). Se captura bloque
//  por bloque para que ningún gráfico quede cortado al cambiar de página.
// ============================================================================
import { jsPDF } from 'jspdf';
import html2canvas from 'html2canvas';

const C_TEXT: [number, number, number] = [30, 16, 80];
const C_MUTED: [number, number, number] = [123, 106, 170];
const C_INFO: [number, number, number] = [75, 45, 171];

export async function exportarDashboardPdf(
  container: HTMLElement,
  generadoPor: string,
): Promise<void> {
  const pdf = new jsPDF({ unit: 'mm', format: 'a4' });
  const pageW = pdf.internal.pageSize.getWidth();
  const pageH = pdf.internal.pageSize.getHeight();
  const M = 10;
  const usableW = pageW - M * 2;
  let y = M;

  // ── Encabezado ──
  pdf.setFont('helvetica', 'bold'); pdf.setFontSize(14);
  pdf.setTextColor(...C_TEXT);
  pdf.text('Dashboard de auditoría ISO 9001', M, y + 5);
  pdf.setFont('helvetica', 'normal'); pdf.setFontSize(9);
  pdf.setTextColor(...C_MUTED);
  pdf.text(
    `BDT Global · Generado el ${new Date().toLocaleDateString('es-AR')} por ${generadoPor}`,
    M, y + 10,
  );
  pdf.setFillColor(...C_INFO);
  pdf.rect(M, y + 13, usableW, 0.6, 'F');
  y += 18;

  // ── Bloques visuales ──
  const blocks = Array.from(container.querySelectorAll<HTMLElement>('.dash-pdf-block'));

  for (const block of blocks) {
    const canvas = await html2canvas(block, {
      scale: 2,
      backgroundColor: '#ffffff',
      useCORS: true,
      logging: false,
    });

    let drawW = usableW;
    let drawH = (canvas.height * drawW) / canvas.width;

    // Si un bloque es más alto que una página, lo escalamos para que entre.
    const maxH = pageH - M * 2;
    if (drawH > maxH) {
      drawH = maxH;
      drawW = (canvas.width * drawH) / canvas.height;
    }
    // Salto de página si no entra en lo que queda.
    if (y + drawH > pageH - M) {
      pdf.addPage();
      y = M;
    }
    const x = M + (usableW - drawW) / 2; // centrado si quedó más angosto
    pdf.addImage(canvas.toDataURL('image/png'), 'PNG', x, y, drawW, drawH);
    y += drawH + 5;
  }

  // ── Numeración ──
  const total = pdf.getNumberOfPages();
  for (let i = 1; i <= total; i++) {
    pdf.setPage(i);
    pdf.setFont('helvetica', 'normal'); pdf.setFontSize(8);
    pdf.setTextColor(...C_MUTED);
    pdf.text(`Página ${i} de ${total}`, pageW - M, pageH - 6, { align: 'right' });
  }

  pdf.save(`dashboard-iso9001-${new Date().toISOString().slice(0, 10)}.pdf`);
}