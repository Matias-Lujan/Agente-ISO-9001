// CSS base de la app (shell + sidebar + chrome común de las pantallas).
//
// TEMA: se maneja con variables CSS. El modo se controla con el atributo
// data-theme en <html> (lo setea AuthContext según la preferencia del usuario).
//   :root                      → tema claro (default)
//   :root[data-theme="dark"]   → tema oscuro (paleta calcada del prototipo)
// Todas las pantallas usan estas variables, así que cambiar de tema afecta a
// toda la app sin tocar cada componente. El Login NO usa estas variables
// (mantiene su diseño propio).
export const sharedCss = `
:root{
  --bg:#f0eefa;
  --surface:#ffffff;
  --surface-2:#faf8ff;      /* inputs */
  --surface-3:#f8f5ff;      /* paneles sutiles, headers de tabla */
  --surface-hover:#faf8ff;
  --text:#1e1050;
  --text-muted:#7b6aaa;
  --text-label:#5a4a8a;
  --border:rgba(120,80,200,0.15);
  --border-soft:rgba(120,80,200,0.08);
  --primary:#4B2DAB;
  --primary-hover:#3a2090;
  --accent:#7B52E8;
  --on-primary:#ffffff;

  /* Sidebar */
  --sb-bg:#1e1050;
  --sb-border:rgba(255,255,255,0.1);
  --sb-text:rgba(255,255,255,0.6);
  --sb-hover-bg:rgba(255,255,255,0.07);
  --sb-active-bg:rgba(75,45,171,0.5);
  --sb-active-color:#ffffff;
  --sb-role:rgba(255,255,255,0.5);
  --sb-avatar-bg:#4B2DAB;
  --sb-avatar-color:#ffffff;

  /* Semánticos (badges / estados) */
  --ok-bg:#EAF3DE;   --ok-fg:#27500A;
  --err-bg:#FCEBEB;  --err-fg:#A32D2D;
  --warn-bg:#FAEEDA; --warn-fg:#633806;
  --info-bg:#EDE8FC; --info-fg:#4B2DAB;
  --blue-bg:#E8F0FE; --blue-fg:#1A4480;
  --running-bg:#EDE8FC;
  --switch-off:#e8e0f8;
}

:root[data-theme="dark"]{
  --bg:#0d0d14;
  --surface:#12121e;
  --surface-2:#1a1a2e;
  --surface-3:#16162a;
  --surface-hover:#16162a;
  --text:#e2dcf8;
  --text-muted:rgba(180,160,255,0.55);
  --text-label:rgba(180,160,255,0.55);
  --border:rgba(120,80,200,0.18);
  --border-soft:rgba(120,80,200,0.12);
  --primary:#4B2DAB;
  --primary-hover:#5c3bc0;
  --accent:#7B52E8;
  --on-primary:#ffffff;

  --sb-bg:#08080f;
  --sb-border:rgba(255,255,255,0.06);
  --sb-text:rgba(255,255,255,0.4);
  --sb-hover-bg:rgba(255,255,255,0.04);
  --sb-active-bg:rgba(100,70,220,0.18);
  --sb-active-color:#c4b5f8;
  --sb-role:rgba(255,255,255,0.35);
  --sb-avatar-bg:#3a2090;
  --sb-avatar-color:#c4b5f8;

  --ok-bg:rgba(39,80,10,0.25);   --ok-fg:#c0dd97;
  --err-bg:rgba(163,45,45,0.2);  --err-fg:#f09595;
  --warn-bg:rgba(99,56,6,0.25);  --warn-fg:#fac775;
  --info-bg:rgba(100,70,220,0.18); --info-fg:#c4b5f8;
  --blue-bg:rgba(84,160,255,0.18); --blue-fg:#9ec5ff;
  --running-bg:#16162a;
  --switch-off:rgba(255,255,255,0.08);
}

*{box-sizing:border-box;margin:0;padding:0;}

body{
  font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;
  background:var(--bg);
  color:var(--text);
  transition:background-color .2s,color .2s;
}

/* ----- Shell ----- */
/* App shell: alto fijo al viewport. El único que scrollea es .main; así el
   sidebar no se estira con el contenido (Cerrar sesión queda siempre abajo). */
.shell{display:flex;height:100vh;}

/* ----- Sidebar ----- */
.sidebar{
  width:220px;
  display:flex;flex-direction:column;
  padding:1.5rem 0;
  flex-shrink:0;
  height:100vh;            /* alto del viewport, independiente del contenido */
  overflow-y:auto;         /* si el propio sidebar no entra, scrollea él solo */
  background:var(--sb-bg);
  transition:background-color .2s;
}

.sb-logo{display:block;cursor:pointer;padding:0 1.25rem 1.5rem;border-bottom:0.5px solid var(--sb-border);}
.sb-logo img{height:28px;width:auto;filter:brightness(10);}

.sb-user{
  padding:1rem 1.25rem;
  display:flex;align-items:center;gap:8px;
  margin-bottom:0.5rem;
  border-bottom:0.5px solid var(--sb-border);
  text-decoration:none;
  cursor:pointer;
  transition:all .15s;
}
.sb-user:hover{background:var(--sb-hover-bg);}
.sb-user:hover .sb-role{color:rgba(255,255,255,0.8);}
.sb-user.active{background:var(--sb-active-bg);border-right:2px solid var(--accent);}
.sb-avatar{
  width:32px;height:32px;border-radius:50%;
  display:flex;align-items:center;justify-content:center;
  font-size:12px;font-weight:500;flex-shrink:0;
  background:var(--sb-avatar-bg);color:var(--sb-avatar-color);
}
.sb-name{font-size:13px;font-weight:500;color:#fff;}
.sb-role{font-size:11px;color:var(--sb-role);}
.sb-user-placeholder{font-size:12px;color:var(--sb-role);font-style:italic;}

.nav-item{
  display:flex;align-items:center;gap:10px;
  padding:10px 1.25rem;font-size:13px;
  color:var(--sb-text);
  text-decoration:none;
  cursor:default;
  transition:all .15s;
}
.nav-item.clickable{cursor:pointer;}
.nav-item.clickable:hover{color:#fff;background:var(--sb-hover-bg);}
.nav-item.active{
  color:var(--sb-active-color);background:var(--sb-active-bg);
  border-right:2px solid var(--accent);
}
.nav-item.disabled{opacity:0.45;}
.nav-icon{width:16px;height:16px;flex-shrink:0;}

/* ----- Main panel ----- */
.main{flex:1;min-height:0;padding:1.5rem;overflow:auto;}

.topbar{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;margin-bottom:1.5rem;}

/* ----- Escala tipográfica (alineada en todas las pantallas) -----
   Título de pantalla : 22px / 600   (.page-title y los *-header-title)
   Descripción/subtítulo: 13px / 400 (.page-sub y los *-header-sub)
   Número grande (stat) : 28px / 600
   Título de card       : 14px / 500
   Label                : 12px
   Texto/celda          : 13px
   Meta/hint            : 11px
*/
.page-title{font-size:22px;font-weight:600;color:var(--text);}
.page-sub{font-size:13px;margin-top:4px;color:var(--text-muted);}

/* ----- Cards ----- */
.card{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;
  padding:1.25rem;
  margin-bottom:1rem;
  transition:background-color .2s,border-color .2s;
}
.card-title{
  font-size:14px;font-weight:500;
  margin-bottom:1rem;
  display:flex;align-items:center;gap:6px;
  color:var(--text);
}
.card-title svg{width:16px;height:16px;flex-shrink:0;color:var(--primary);}

/* Etiqueta de sección arriba de un bloque de contenido (mismo estilo que el
   dashboard: "Proyectos recientes" / "Últimos hallazgos"). */
.section-title{
  font-size:11px;font-weight:500;margin-bottom:0.75rem;
  text-transform:uppercase;letter-spacing:.04em;color:var(--text-muted);
}

/* ----- Form ----- */
.field{margin-bottom:1rem;}
.field:last-child{margin-bottom:0;}
.field label{display:block;font-size:12px;margin-bottom:5px;font-weight:500;color:var(--text-label);}
.field select{
  width:100%;padding:9px 12px;border-radius:8px;font-size:13px;
  outline:none;font-family:inherit;
  border:0.5px solid var(--border);
  background:var(--surface-2);color:var(--text);
  transition:all .2s;
}
.field select:focus{border-color:var(--accent);background:var(--surface);}
.field select:disabled{opacity:0.5;cursor:not-allowed;}
.field-row{display:grid;grid-template-columns:1fr 1fr;gap:12px;}

/* ----- Resumen ----- */
.resumen-box{background:var(--surface-3);border-radius:8px;padding:12px;margin-bottom:1rem;}
.res-row{display:flex;justify-content:space-between;padding:4px 0;font-size:13px;}
.res-key{color:var(--text-muted);}
.res-val{font-weight:500;color:var(--text);}
.res-val.placeholder{color:var(--text-muted);font-weight:400;font-style:italic;}

/* ----- Botones ----- */
.btn-row{display:flex;gap:8px;justify-content:flex-end;}
.btn-pri{
  display:inline-flex;align-items:center;gap:6px;
  padding:9px 20px;border-radius:8px;border:none;
  background:var(--primary);font-size:13px;color:var(--on-primary);
  cursor:pointer;font-weight:500;font-family:inherit;
  transition:all .2s;
}
.btn-pri:hover:not(:disabled){background:var(--primary-hover);}
.btn-pri:disabled{opacity:0.5;cursor:not-allowed;}

/* ----- Ejecución ----- */
.running-box{
  background:var(--running-bg);border-radius:10px;
  padding:1.5rem;text-align:center;
}
.running-title{font-size:15px;font-weight:500;margin-bottom:0.5rem;color:var(--text);}
.running-sub{font-size:13px;margin-bottom:1rem;color:var(--text-muted);}

.agent-list{display:flex;flex-direction:column;gap:6px;text-align:left;}
.agent-row{
  display:flex;align-items:center;gap:8px;
  padding:8px 10px;border-radius:8px;font-size:12px;
  background:var(--surface);color:var(--text);
}
.agent-status{font-size:11px;margin-left:auto;}
.agent-status.pendiente{color:var(--text-muted);}
.agent-status.encurso{color:var(--warn-fg);}
.agent-status.completado{color:var(--ok-fg);}
.agent-status.fallido{color:var(--err-fg);}

.spin{display:inline-block;animation:spin 1s linear infinite;}
@keyframes spin{to{transform:rotate(360deg);}}

/* ----- Estados finales ----- */
.final-box{
  background:var(--ok-bg);border-radius:10px;
  padding:1.5rem;text-align:center;
  font-size:15px;font-weight:500;color:var(--ok-fg);
}
.final-box.error{background:var(--err-bg);color:var(--err-fg);}

/* ----- Resultado al terminar la auditoría ----- */
.ea-result-head{display:flex;align-items:flex-start;gap:12px;margin-bottom:1.3rem;text-align:left;}
.ea-badge{
  flex-shrink:0;width:34px;height:34px;border-radius:50%;
  display:inline-flex;align-items:center;justify-content:center;
  background:var(--ok-bg);color:var(--ok-fg);
}
.ea-badge svg{width:18px;height:18px;}
.ea-badge.warn{background:var(--warn-bg);color:var(--warn-fg);}
.ea-badge.err{background:var(--err-bg);color:var(--err-fg);}
.ea-result-title{font-size:16px;font-weight:600;color:var(--text);}
.ea-result-sub{font-size:13px;color:var(--text-muted);margin-top:3px;}

.ea-result-row{display:flex;align-items:center;gap:14px;margin-bottom:1.3rem;}
.ea-ring{position:relative;width:64px;height:64px;flex-shrink:0;}
.ea-ring svg{transform:rotate(-90deg);display:block;}
.ea-ring .track{stroke:var(--border);}
.ea-ring .bar{stroke-linecap:round;transition:stroke-dashoffset .6s ease;}
.ea-ring .bar.ok{stroke:var(--ok-fg);}.ea-ring .bar.warn{stroke:var(--warn-fg);}.ea-ring .bar.err{stroke:var(--err-fg);}
.ea-ring .center{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;font-size:15px;font-weight:700;color:var(--text);}
.ea-ring-t{font-size:13.5px;font-weight:600;color:var(--text);}
.ea-ring-s{font-size:12px;color:var(--text-muted);margin-top:2px;}

.ea-stats{display:grid;grid-template-columns:repeat(3,1fr);gap:.8rem;margin-bottom:1.4rem;}
.ea-stat{background:var(--surface-2);border-radius:11px;padding:.9rem 1rem;text-align:left;}
.ea-stat .n{font-size:24px;font-weight:700;color:var(--text);line-height:1;}
.ea-stat .l{font-size:11.5px;color:var(--text-muted);margin-top:6px;}

.ea-error-list{list-style:none;margin:0 0 1.3rem;padding:0;display:flex;flex-direction:column;gap:8px;}
.ea-error-item{
  display:flex;flex-direction:column;gap:3px;
  background:var(--err-bg);border:1px solid var(--err-fg);border-radius:9px;
  padding:.7rem .85rem;
}
.ea-error-nodo{font-size:11.5px;font-weight:700;color:var(--err-fg);text-transform:uppercase;letter-spacing:.03em;}
.ea-error-msg{font-size:13px;color:var(--text);line-height:1.4;}

.ea-actions{display:flex;gap:8px;flex-wrap:wrap;}
.btn-sec{
  display:inline-flex;align-items:center;gap:6px;
  padding:9px 18px;border-radius:8px;
  background:var(--surface);color:var(--text);border:1px solid var(--border);
  font-size:13px;font-weight:500;cursor:pointer;font-family:inherit;transition:all .2s;
}
.btn-sec:hover{background:var(--surface-2);}

.error-banner{
  background:var(--err-bg);color:var(--err-fg);
  padding:10px 14px;border-radius:8px;
  font-size:13px;margin-bottom:1rem;
}

.loading-text{font-size:13px;color:var(--text-muted);text-align:center;padding:2rem;}
`;
