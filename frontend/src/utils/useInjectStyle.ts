import { useEffect } from 'react';

// Inyecta un bloque <style> en el <head> bajo una clave única. Si la clave ya
// existe, no duplica. Mismo patrón del prototipo (untitled/src/utils): permite
// que cada componente lleve su CSS dentro del archivo sin necesidad de un
// preprocesador o de Tailwind. Útil para mantener visualmente fiel el look del
// prototipo del Google AI Studio sin agregar dependencias.
export function useInjectStyle(css: string, key: string) {
  useEffect(() => {
    if (document.getElementById(key)) return;
    const style = document.createElement('style');
    style.id = key;
    style.textContent = css;
    document.head.appendChild(style);
  }, [css, key]);
}
