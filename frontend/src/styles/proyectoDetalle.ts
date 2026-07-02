// CSS del detalle de proyecto. Usa las variables de tema de styles/shared.ts.
export const proyectoDetalleCss = `
.pd-back{
  display:inline-flex;align-items:center;gap:6px;font-size:13px;font-weight:500;color:var(--text-muted);
  background:none;border:none;cursor:pointer;font-family:inherit;padding:0;margin-bottom:1rem;
}
.pd-back:hover{color:var(--text);}
.pd-back svg{width:15px;height:15px;}

/* ----- Header del proyecto ----- */
.pd-head{
  background:var(--surface);border:0.5px solid var(--border);border-radius:14px;padding:1.5rem 1.7rem;margin-bottom:1.4rem;
}
.pd-head-top{display:flex;align-items:flex-start;justify-content:space-between;gap:14px;flex-wrap:wrap;}
.pd-title{font-family:"Syne",sans-serif;font-size:24px;font-weight:700;color:var(--text);line-height:1.2;}
.pd-badges{display:flex;gap:7px;flex-wrap:wrap;align-items:center;}
.pd-desc{font-size:13.5px;color:var(--text-muted);margin-top:.6rem;max-width:60ch;line-height:1.5;}
.pd-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem 1.6rem;margin-top:1.3rem;}
.pd-field .k{font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:var(--text-muted);margin-bottom:4px;}
.pd-field .v{font-size:13.5px;color:var(--text);font-weight:500;}
.pd-resp{display:flex;flex-wrap:wrap;gap:8px;margin-top:4px;}
.pd-resp .chip{display:inline-flex;align-items:center;gap:7px;background:var(--surface-2);border-radius:999px;padding:4px 11px 4px 4px;}
.pd-resp .mini{width:24px;height:24px;border-radius:50%;background:var(--sb-avatar-bg);color:#fff;display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;}
.pd-resp .who{font-size:12px;font-weight:600;color:var(--text);}
.pd-resp .ro{font-size:11px;color:var(--text-muted);}
.pd-chips{display:flex;flex-wrap:wrap;gap:7px;margin-top:4px;}
.pd-chip{display:inline-flex;align-items:center;gap:6px;font-size:11.5px;font-weight:600;padding:5px 10px;border-radius:7px;background:var(--surface-2);color:var(--text-muted);}
.pd-chip .dot{width:7px;height:7px;border-radius:50%;}
.pd-chip code{font-family:ui-monospace,Menlo,monospace;font-weight:500;opacity:.8;}

.badge{display:inline-block;padding:3px 10px;border-radius:999px;font-size:11px;font-weight:600;white-space:nowrap;}
.b-ok{background:var(--ok-bg);color:var(--ok-fg);}
.b-err{background:var(--err-bg);color:var(--err-fg);}
.b-warn{background:var(--warn-bg);color:var(--warn-fg);}
.b-info{background:var(--info-bg);color:var(--info-fg);}
.b-gray{background:var(--surface-3);color:var(--text-muted);}

/* ----- Layout de columnas ----- */
.pd-cols{display:grid;grid-template-columns:300px 1fr;gap:1.4rem;align-items:start;}
@media (max-width:900px){.pd-cols{grid-template-columns:1fr;}}
.pd-section-title{font-family:"Syne",sans-serif;font-size:16px;font-weight:700;color:var(--text);margin-bottom:.9rem;}

/* ----- Lista de auditorías ----- */
.pd-aud{
  width:100%;text-align:left;background:var(--surface);border:0.5px solid var(--border);border-radius:11px;
  padding:.9rem 1rem;margin-bottom:.7rem;cursor:pointer;font-family:inherit;transition:border-color .15s,background-color .15s;
}
.pd-aud:hover{border-color:var(--accent);}
.pd-aud.sel{border-color:var(--primary);background:var(--info-bg);}
.pd-aud .row1{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:5px;}
.pd-aud .etapa{font-size:13.5px;font-weight:600;color:var(--text);}
.pd-aud .sub{font-size:11.5px;color:var(--text-muted);}

/* ----- Panel derecho ----- */
.pd-panel{background:var(--surface);border:0.5px solid var(--border);border-radius:14px;padding:1.4rem 1.6rem;margin-bottom:1.3rem;}
.pd-stats{display:grid;grid-template-columns:repeat(3,1fr);gap:1rem;margin-bottom:1.4rem;}
.pd-stat{background:var(--surface-2);border-radius:11px;padding:1rem 1.1rem;}
.pd-stat .n{font-family:"Syne",sans-serif;font-size:26px;font-weight:700;color:var(--text);line-height:1;}
.pd-stat .l{font-size:11.5px;color:var(--text-muted);margin-top:6px;}

/* anillo (igual al de las tarjetas) */
.pd-ringbox{display:flex;align-items:center;gap:14px;margin-bottom:1.4rem;padding-bottom:1.3rem;border-bottom:0.5px solid var(--border-soft);}
.pd-ring{position:relative;width:64px;height:64px;flex-shrink:0;}
.pd-ring svg{transform:rotate(-90deg);display:block;}
.pd-ring .track{stroke:var(--border);}
.pd-ring .bar{stroke-linecap:round;transition:stroke-dashoffset .6s ease;}
.pd-ring .bar.ok{stroke:var(--ok-fg);} .pd-ring .bar.warn{stroke:var(--warn-fg);} .pd-ring .bar.err{stroke:var(--err-fg);}
.pd-ring .center{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;font-size:15px;font-weight:700;color:var(--text);}
.pd-ring.empty .track{stroke-dasharray:3 4;} .pd-ring.empty .center{color:var(--text-muted);font-size:18px;}
.pd-ring-t{font-size:13.5px;font-weight:600;color:var(--text);}
.pd-ring-s{font-size:12px;color:var(--text-muted);margin-top:2px;}

/* ----- Artefactos evaluados ----- */
.pd-art{border:0.5px solid var(--border);border-radius:11px;margin-bottom:.7rem;overflow:hidden;}
.pd-art-head{display:flex;align-items:center;gap:10px;padding:.85rem 1rem;cursor:pointer;}
.pd-art-head:hover{background:var(--surface-hover);}
.pd-art-name{flex:1;min-width:0;font-size:13.5px;font-weight:600;color:var(--text);}
.pd-art-name code{font-family:ui-monospace,Menlo,monospace;font-size:11px;color:var(--text-muted);font-weight:500;margin-right:6px;}
.pd-caret{width:16px;height:16px;color:var(--text-muted);transition:transform .2s;flex-shrink:0;}
.pd-art.open .pd-caret{transform:rotate(90deg);}
.pd-art-body{padding:0 1rem 1rem;display:none;}
.pd-art.open .pd-art-body{display:block;}
.pd-art-obs{font-size:12.5px;color:var(--text-muted);line-height:1.5;margin:.5rem 0 .8rem;}
.pd-sub-t{font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:var(--text-muted);margin:.6rem 0 .4rem;}
.pd-hz{background:var(--surface-2);border-radius:8px;padding:.7rem .9rem;margin-bottom:.5rem;}
.pd-hz .top{display:flex;align-items:center;gap:8px;margin-bottom:4px;}
.pd-hz .desc{font-size:12.5px;color:var(--text);line-height:1.45;}
.pd-hz .just{font-size:11.5px;color:var(--text-muted);margin-top:4px;line-height:1.45;}
.pd-hz .ag{font-size:10.5px;color:var(--text-muted);margin-top:5px;}
.pd-doc{display:flex;align-items:center;gap:8px;font-size:12px;color:var(--text);padding:5px 0;}
.pd-doc .fa{color:var(--text-muted);flex-shrink:0;}
.pd-doc .hash{font-family:ui-monospace,Menlo,monospace;font-size:10.5px;color:var(--text-muted);}

/* ----- Informes ----- */
.pd-inf{display:flex;align-items:center;justify-content:space-between;gap:10px;border:0.5px solid var(--border);border-radius:11px;padding:.85rem 1rem;margin-bottom:.6rem;}
.pd-inf .meta{font-size:12.5px;color:var(--text);}
.pd-inf .meta b{font-weight:600;}
.pd-inf .acts{display:flex;gap:4px;}
.pd-link{display:inline-flex;align-items:center;gap:6px;font-size:12.5px;font-weight:600;cursor:pointer;color:var(--primary);background:none;border:none;font-family:inherit;padding:6px 10px;border-radius:8px;}
.pd-link:hover{background:var(--info-bg);}
.pd-link svg{width:14px;height:14px;}

.pd-empty{font-size:13px;color:var(--text-muted);padding:1.2rem;text-align:center;background:var(--surface-2);border-radius:11px;}

/* ----- Panel de auditoría fallida ----- */
.pd-fail-head{display:flex;align-items:flex-start;gap:12px;margin-bottom:1rem;}
.pd-fail-badge{flex-shrink:0;width:34px;height:34px;border-radius:9px;display:flex;align-items:center;justify-content:center;background:var(--err-bg);color:var(--err-fg);}
.pd-fail-badge svg{width:19px;height:19px;}
.pd-fail-title{font-size:15px;font-weight:600;color:var(--text);}
.pd-fail-sub{font-size:13px;color:var(--text-muted);margin-top:3px;line-height:1.45;}
.pd-fail-list{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:8px;}
.pd-fail-item{display:flex;flex-direction:column;gap:3px;background:var(--err-bg);border:1px solid var(--err-fg);border-radius:9px;padding:.7rem .85rem;}
.pd-fail-nodo{font-size:11.5px;font-weight:700;color:var(--err-fg);text-transform:uppercase;letter-spacing:.03em;}
.pd-fail-msg{font-size:13px;color:var(--text);line-height:1.4;}

/* ----- Modal ver informe ----- */
.pd-overlay{position:fixed;inset:0;background:rgba(15,8,40,0.55);display:flex;align-items:center;justify-content:center;z-index:1000;padding:1rem;}
.pd-modal{background:var(--surface);border-radius:12px;max-width:760px;width:100%;max-height:90vh;display:flex;flex-direction:column;box-shadow:0 24px 60px rgba(0,0,0,0.35);}
.pd-modal-head{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;padding:18px 22px 12px;border-bottom:0.5px solid var(--border);}
.pd-modal-title{font-size:15px;font-weight:600;color:var(--text);}
.pd-modal-sub{font-size:12px;color:var(--text-muted);margin-top:3px;}
.pd-modal-close{background:none;border:none;cursor:pointer;color:var(--text-muted);font-size:22px;line-height:1;}
.pd-modal-body{padding:16px 22px;overflow-y:auto;}
.pd-contenido{font-size:13px;line-height:1.6;color:var(--text);white-space:pre-wrap;word-break:break-word;font-family:ui-monospace,Menlo,monospace;}
.pd-pdf-frame{width:100%;height:62vh;border:none;border-radius:8px;background:#fff;display:block;}
.pd-modal-foot{display:flex;justify-content:flex-end;padding:12px 22px 18px;border-top:0.5px solid var(--border-soft);}

/* ----- Curva de evolución de ejecuciones ----- */
.pd-evo-svg{display:block;overflow:visible;}
.pd-evo-grid{stroke:var(--border-soft);stroke-width:1;}
.pd-evo-thr{stroke:var(--border);stroke-width:1;stroke-dasharray:3 4;opacity:.7;}
.pd-evo-axis{fill:var(--text-muted);font-size:13px;}
.pd-evo-line{stroke:var(--primary);stroke-width:2.5;}
.pd-evo-val{font-size:13px;font-weight:700;}
.pd-evo-x{fill:var(--text);font-size:13px;font-weight:600;}
.pd-evo-x2{fill:var(--text-muted);font-size:11.5px;}
.pd-evo-pt{cursor:pointer;}
.pd-evo-pt:hover circle{stroke-width:3.5;}
.pd-evo-foot{font-size:12px;color:var(--text-muted);margin-top:.7rem;line-height:1.45;}
`;