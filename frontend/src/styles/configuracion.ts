// ============================================================================
//  Estilos de la pantalla Configuracion.
//  Usa las variables de tema de sharedCss (claro/oscuro). Patron:
//  CSS-in-string inyectado por useInjectStyle.
// ============================================================================

export const configuracionCss = `
/*  HEADER  */
.cfg-header{
  display:flex;align-items:flex-start;justify-content:space-between;
  gap:1rem;margin-bottom:1.5rem;
}
.cfg-header-title{font-size:22px;font-weight:600;color:var(--text);margin:0;}
.cfg-header-sub{font-size:13px;color:var(--text-muted);margin-top:4px;}

/*  SWITCH MODO OSCURO (esquina superior derecha)  */
.cfg-theme{
  display:flex;align-items:center;gap:10px;
  cursor:pointer;user-select:none;
  background:none;border:none;font-family:inherit;padding:4px;
}
.cfg-theme-label{
  display:flex;align-items:center;gap:6px;
  font-size:13px;font-weight:500;color:var(--text);white-space:nowrap;
}
.cfg-switch{
  width:40px;height:22px;border-radius:11px;
  background:var(--switch-off);position:relative;
  transition:background .2s;flex-shrink:0;
}
.cfg-switch.on{background:var(--primary);}
.cfg-switch::after{
  content:'';position:absolute;
  width:16px;height:16px;border-radius:50%;
  background:#fff;top:3px;left:3px;
  transition:left .2s;box-shadow:0 1px 2px rgba(0,0,0,0.25);
}
.cfg-switch.on::after{left:21px;}

/*  TABS  */
.cfg-tabs{
  display:flex;gap:4px;margin-bottom:1.25rem;
  background:var(--surface);border-radius:10px;
  border:0.5px solid var(--border);padding:6px;width:fit-content;
}
.cfg-tab{
  padding:7px 16px;border-radius:7px;font-size:13px;
  color:var(--text-muted);cursor:pointer;transition:all .12s;
  border:none;background:transparent;font-family:inherit;
}
.cfg-tab:hover:not(.active){background:var(--surface-2);color:var(--primary);}
.cfg-tab.active{background:var(--primary);color:var(--on-primary);font-weight:500;}

/*  CARDS Y FIELDS  */
.cfg-card{
  background:var(--surface);border-radius:10px;
  border:0.5px solid var(--border);padding:1.25rem;margin-bottom:1rem;
  transition:background-color .2s,border-color .2s;
}
.cfg-card-title{font-size:14px;font-weight:500;color:var(--text);margin-bottom:1rem;}
.cfg-card-sub{font-size:12px;color:var(--text-muted);margin-top:-8px;margin-bottom:1rem;}

.cfg-field{margin-bottom:1rem;}
.cfg-field label{display:block;font-size:12px;color:var(--text-label);margin-bottom:5px;font-weight:500;}
.cfg-input{
  width:100%;padding:9px 12px;border-radius:8px;
  border:0.5px solid var(--border);background:var(--surface-2);
  font-size:13px;color:var(--text);outline:none;font-family:inherit;
  transition:border-color .15s,background-color .2s;
}
.cfg-input:focus{border-color:var(--accent);background:var(--surface);}
.cfg-input:disabled,.cfg-input.readonly{
  background:var(--surface-2);color:var(--text-muted);cursor:not-allowed;opacity:0.8;
}
.cfg-field-hint{font-size:11px;color:var(--text-muted);margin-top:4px;}

/*  AVATAR (perfil)  */
.cfg-avatar-section{display:flex;align-items:center;gap:16px;margin-bottom:1.25rem;}
.cfg-big-avatar{
  width:56px;height:56px;border-radius:50%;background:var(--primary);
  display:flex;align-items:center;justify-content:center;
  font-size:20px;font-weight:500;color:#fff;
}
.cfg-avatar-name{font-size:15px;font-weight:500;color:var(--text);}
.cfg-role-badge{
  display:inline-flex;align-items:center;font-size:11px;font-weight:500;
  padding:2px 8px;border-radius:20px;
  background:var(--info-bg);color:var(--info-fg);margin-top:4px;
}

/*  CARD INFORMATIVA DE CONTRASEÑA  */
.cfg-pass-info{
  display:flex;align-items:flex-start;gap:14px;padding:14px;
  background:var(--surface-2);border:0.5px solid var(--border);border-radius:10px;
}
.cfg-pass-info-icon{
  width:40px;height:40px;border-radius:10px;
  background:var(--info-bg);color:var(--info-fg);
  display:flex;align-items:center;justify-content:center;flex-shrink:0;
}
.cfg-pass-info-text{flex:1;min-width:0;}
.cfg-pass-info-title{font-size:13px;font-weight:600;color:var(--text);margin-bottom:4px;}
.cfg-pass-info-desc{font-size:12px;color:var(--text-label);line-height:1.5;}

/*  BADGE "EN DESARROLLO"  */
.cfg-dev-badge{
  display:inline-flex;align-items:center;gap:6px;
  font-size:11px;font-weight:500;padding:5px 10px;border-radius:20px;
  background:var(--warn-bg);color:var(--warn-fg);
  border:0.5px solid var(--border-soft);cursor:not-allowed;
  flex-shrink:0;white-space:nowrap;
}
.cfg-dev-badge::before{content:'';width:6px;height:6px;border-radius:50%;background:var(--warn-fg);}

/*  TOGGLE ROW (items de notif/agente)  */
.cfg-toggle-row{
  display:flex;align-items:center;justify-content:space-between;
  padding:10px 0;border-bottom:0.5px solid var(--border-soft);gap:1rem;
}
.cfg-toggle-row:last-child{border-bottom:none;}
.cfg-toggle-info{flex:1;min-width:0;}
.cfg-toggle-name{font-size:13px;color:var(--text);font-weight:500;}
.cfg-toggle-desc{font-size:11px;color:var(--text-muted);margin-top:2px;}

/*  FORM DE CAMBIO DE CONTRASEÑA  */
.cfg-form-actions{display:flex;justify-content:flex-end;margin-top:0.5rem;}
.cfg-btn{
  padding:9px 18px;border-radius:8px;border:none;
  background:var(--primary);color:var(--on-primary);
  font-size:13px;font-weight:500;font-family:inherit;cursor:pointer;
  transition:opacity .15s;
}
.cfg-btn:hover:not(:disabled){opacity:0.9;}
.cfg-btn:disabled{opacity:0.6;cursor:not-allowed;}
.cfg-pass-show{
  display:flex;align-items:center;gap:7px;
  font-size:12px;color:var(--text-muted);cursor:pointer;user-select:none;
  margin-top:-4px;margin-bottom:6px;
}
.cfg-pass-show input{cursor:pointer;}
.cfg-feedback{
  font-size:12px;padding:9px 12px;border-radius:8px;margin-bottom:1rem;
  border:0.5px solid var(--border-soft);
}
.cfg-feedback.error{background:var(--err-bg);color:var(--err-fg);}
.cfg-feedback.success{background:var(--ok-bg);color:var(--ok-fg);}

/*  AVISO "PRÓXIMAS FUNCIONES" (roadmap)  */
.cfg-roadmap-note{
  display:flex;align-items:center;gap:10px;
  padding:10px 14px;margin-bottom:1rem;border-radius:10px;
  background:var(--info-bg);border:0.5px solid var(--border-soft);
}
.cfg-roadmap-tag{
  font-size:11px;font-weight:600;padding:3px 9px;border-radius:20px;
  background:var(--info-fg);color:var(--surface);white-space:nowrap;flex-shrink:0;
}
.cfg-roadmap-text{font-size:12px;color:var(--info-fg);line-height:1.45;}

/*  EDITOR DE SYSTEM PROMPT (admin)  */
.cfg-prompt-head{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;margin-bottom:12px;}
.cfg-prompt-tag{
  font-size:11px;font-weight:600;padding:3px 10px;border-radius:20px;
  white-space:nowrap;flex-shrink:0;
}
.cfg-prompt-tag.is-default{background:var(--ok-bg);color:var(--ok-fg);}
.cfg-prompt-tag.is-mod{background:var(--warn-bg);color:var(--warn-fg);}
.cfg-prompt-warn{
  font-size:12px;line-height:1.5;padding:10px 12px;border-radius:8px;margin-bottom:12px;
  background:var(--warn-bg);color:var(--warn-fg);border:0.5px solid var(--border-soft);
}
.cfg-prompt-textarea{
  width:100%;min-height:320px;resize:vertical;
  padding:12px;border-radius:8px;border:0.5px solid var(--border);
  background:var(--surface-2);color:var(--text);outline:none;
  font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;
  font-size:12px;line-height:1.5;white-space:pre;overflow:auto;
  transition:border-color .15s,background-color .2s;
}
.cfg-prompt-textarea:focus{border-color:var(--accent);background:var(--surface);}
.cfg-prompt-textarea:disabled{opacity:0.7;cursor:not-allowed;}
.cfg-prompt-actions{display:flex;align-items:center;gap:8px;margin-top:12px;flex-wrap:wrap;}
.cfg-btn-sec{
  padding:9px 14px;border-radius:8px;border:0.5px solid var(--border);
  background:var(--surface-2);color:var(--text);
  font-size:13px;font-weight:500;font-family:inherit;cursor:pointer;
  transition:background-color .15s,border-color .15s;
}
.cfg-btn-sec:hover:not(:disabled){border-color:var(--accent);color:var(--primary);}
.cfg-btn-sec:disabled{opacity:0.5;cursor:not-allowed;}
.cfg-btn-sec.sm{padding:5px 10px;font-size:12px;}
.cfg-prompt-hist{
  margin-top:14px;border-top:0.5px solid var(--border-soft);padding-top:10px;
  display:flex;flex-direction:column;gap:2px;
}
.cfg-prompt-hist-row{
  display:flex;align-items:center;justify-content:space-between;gap:1rem;
  padding:8px 0;border-bottom:0.5px solid var(--border-soft);
}
.cfg-prompt-hist-row:last-child{border-bottom:none;}
.cfg-prompt-hist-info{display:flex;flex-direction:column;gap:2px;min-width:0;}
.cfg-prompt-hist-ver{font-size:12px;font-weight:600;color:var(--text);}
.cfg-prompt-hist-meta{
  font-size:11px;color:var(--text-muted);
  overflow:hidden;text-overflow:ellipsis;white-space:nowrap;
}

/*  AGENTE IA — modelo actual  */
.cfg-model-current{
  display:flex;align-items:center;justify-content:space-between;gap:1rem;
  padding:10px 14px;background:var(--surface-2);
  border:0.5px solid var(--border);border-radius:8px;
}
.cfg-model-info{display:flex;align-items:center;gap:10px;}
.cfg-model-icon{
  width:28px;height:28px;border-radius:6px;background:var(--info-bg);
  display:flex;align-items:center;justify-content:center;
  color:var(--info-fg);font-size:14px;flex-shrink:0;
}
.cfg-model-name{font-size:13px;color:var(--text);font-weight:500;}
.cfg-model-meta{font-size:11px;color:var(--text-muted);margin-top:1px;}

/*  KPI DE CONSUMO DE TOKENS (admin)  */
.cfg-kpi-grid{
  display:grid;grid-template-columns:repeat(auto-fit,minmax(120px,1fr));
  gap:10px;margin-bottom:1rem;
}
.cfg-kpi{
  padding:12px 14px;border-radius:8px;
  background:var(--surface-2);border:0.5px solid var(--border);
}
.cfg-kpi-value{font-size:20px;font-weight:600;color:var(--text);line-height:1.1;}
.cfg-kpi-label{font-size:11px;color:var(--text-muted);margin-top:4px;}

.cfg-consumo-tabla{
  border-top:0.5px solid var(--border-soft);padding-top:8px;
  display:flex;flex-direction:column;
}
.cfg-consumo-row{
  display:grid;grid-template-columns:1.6fr 1fr 1fr 1fr 0.9fr;gap:8px;
  padding:8px 0;border-bottom:0.5px solid var(--border-soft);
  font-size:12px;color:var(--text);align-items:center;
}
.cfg-consumo-row:last-child{border-bottom:none;}
.cfg-consumo-row span:not(.cfg-consumo-agente){text-align:right;font-variant-numeric:tabular-nums;}
.cfg-consumo-head{
  font-size:11px;font-weight:600;color:var(--text-muted);
  text-transform:uppercase;letter-spacing:0.03em;
}
.cfg-consumo-agente{font-weight:500;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
`;
