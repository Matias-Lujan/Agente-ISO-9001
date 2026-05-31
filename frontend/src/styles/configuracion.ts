
// ============================================================================
//  Estilos de la pantalla Configuracion.
//  Misma paleta que el resto del sistema (1e1050 / 4B2DAB / f0eefa / faf8ff).
//  Patron: CSS-in-string inyectado por useInjectStyle.
// ============================================================================

export const configuracionCss = `
/* ────────────────────────────────────────────────────────────────────────── */
/*  HEADER                                                                    */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-header{margin-bottom:1.5rem;}
.cfg-header-title{
  font-size:22px;
  font-weight:600;
  color:#1e1050;
  margin:0;
}
.cfg-header-sub{
  font-size:13px;
  color:#7b6aaa;
  margin-top:4px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  TABS                                                                      */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-tabs{
  display:flex;
  gap:4px;
  margin-bottom:1.25rem;
  background:#fff;
  border-radius:10px;
  border:0.5px solid rgba(120,80,200,0.15);
  padding:6px;
  width:fit-content;
}

.cfg-tab{
  padding:7px 16px;
  border-radius:7px;
  font-size:13px;
  color:#7b6aaa;
  cursor:pointer;
  transition:all .12s;
  border:none;
  background:transparent;
  font-family:inherit;
}

.cfg-tab:hover:not(.active){background:#faf8ff;color:#4B2DAB;}

.cfg-tab.active{
  background:#4B2DAB;
  color:#fff;
  font-weight:500;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  CARDS Y FIELDS                                                            */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-card{
  background:#fff;
  border-radius:10px;
  border:0.5px solid rgba(120,80,200,0.15);
  padding:1.25rem;
  margin-bottom:1rem;
}

.cfg-card-title{
  font-size:14px;
  font-weight:500;
  color:#1e1050;
  margin-bottom:1rem;
}

.cfg-card-sub{
  font-size:12px;
  color:#7b6aaa;
  margin-top:-8px;
  margin-bottom:1rem;
}

.cfg-field{margin-bottom:1rem;}

.cfg-field label{
  display:block;
  font-size:12px;
  color:#5a4a8a;
  margin-bottom:5px;
  font-weight:500;
}

.cfg-input{
  width:100%;
  padding:9px 12px;
  border-radius:8px;
  border:0.5px solid rgba(120,80,200,0.25);
  background:#faf8ff;
  font-size:13px;
  color:#1e1050;
  outline:none;
  font-family:inherit;
  transition:border-color .15s;
}

.cfg-input:focus{
  border-color:#7B52E8;
  background:#fff;
}

.cfg-input:disabled,
.cfg-input.readonly{
  background:#f4f4f4;
  color:#7b6aaa;
  cursor:not-allowed;
}

.cfg-field-hint{
  font-size:11px;
  color:#7b6aaa;
  margin-top:4px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  AVATAR (perfil)                                                           */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-avatar-section{
  display:flex;
  align-items:center;
  gap:16px;
  margin-bottom:1.25rem;
}

.cfg-big-avatar{
  width:56px;
  height:56px;
  border-radius:50%;
  background:#4B2DAB;
  display:flex;
  align-items:center;
  justify-content:center;
  font-size:20px;
  font-weight:500;
  color:#fff;
}

.cfg-avatar-name{
  font-size:15px;
  font-weight:500;
  color:#1e1050;
}

.cfg-role-badge{
  display:inline-flex;
  align-items:center;
  font-size:11px;
  font-weight:500;
  padding:2px 8px;
  border-radius:20px;
  background:#EDE8FC;
  color:#3C3489;
  margin-top:4px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  CARD INFORMATIVA DE CONTRASEÑA                                            */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-pass-info{
  display:flex;
  align-items:flex-start;
  gap:14px;
  padding:14px;
  background:#faf8ff;
  border:0.5px solid rgba(120,80,200,0.2);
  border-radius:10px;
}

.cfg-pass-info-icon{
  width:40px;
  height:40px;
  border-radius:10px;
  background:#EDE8FC;
  color:#4B2DAB;
  display:flex;
  align-items:center;
  justify-content:center;
  flex-shrink:0;
}

.cfg-pass-info-text{flex:1;min-width:0;}

.cfg-pass-info-title{
  font-size:13px;
  font-weight:600;
  color:#1e1050;
  margin-bottom:4px;
}

.cfg-pass-info-desc{
  font-size:12px;
  color:#5a4a8a;
  line-height:1.5;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  BADGE "EN DESARROLLO" — reemplaza toggles                                 */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-dev-badge{
  display:inline-flex;
  align-items:center;
  gap:6px;
  font-size:11px;
  font-weight:500;
  padding:5px 10px;
  border-radius:20px;
  background:#FFF4E5;
  color:#BA7517;
  border:0.5px solid rgba(186,117,23,0.2);
  cursor:not-allowed;
  flex-shrink:0;
  white-space:nowrap;
}

.cfg-dev-badge::before{
  content:'';
  width:6px;
  height:6px;
  border-radius:50%;
  background:#BA7517;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  TOGGLE ROW (item de configuracion en notif/agente)                        */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-toggle-row{
  display:flex;
  align-items:center;
  justify-content:space-between;
  padding:10px 0;
  border-bottom:0.5px solid rgba(120,80,200,0.08);
  gap:1rem;
}

.cfg-toggle-row:last-child{border-bottom:none;}

.cfg-toggle-info{flex:1;min-width:0;}

.cfg-toggle-name{
  font-size:13px;
  color:#1e1050;
  font-weight:500;
}

.cfg-toggle-desc{
  font-size:11px;
  color:#7b6aaa;
  margin-top:2px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  INTEGRACIONES                                                             */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-int-list{
  display:flex;
  flex-direction:column;
  gap:8px;
}

.cfg-int-row{
  display:flex;
  align-items:center;
  gap:10px;
  padding:10px 12px;
  border-radius:8px;
  border:0.5px solid rgba(120,80,200,0.12);
  background:#faf8ff;
}

.cfg-int-icon{
  width:32px;
  height:32px;
  border-radius:8px;
  display:flex;
  align-items:center;
  justify-content:center;
  font-size:16px;
  flex-shrink:0;
}

.cfg-int-detail{flex:1;min-width:0;}

.cfg-int-detail-name{
  font-size:13px;
  font-weight:500;
  color:#1e1050;
}

.cfg-int-detail-sub{
  font-size:11px;
  color:#7b6aaa;
  margin-top:1px;
  overflow:hidden;
  text-overflow:ellipsis;
  white-space:nowrap;
}

.cfg-status-pill{
  font-size:11px;
  font-weight:500;
  padding:3px 8px;
  border-radius:20px;
  white-space:nowrap;
  flex-shrink:0;
}

.cfg-status-ok{background:#EAF3DE;color:#27500A;}
.cfg-status-err{background:#FCEBEB;color:#A32D2D;}

/* ────────────────────────────────────────────────────────────────────────── */
/*  AGENTE IA — modelo actual                                                 */
/* ────────────────────────────────────────────────────────────────────────── */

.cfg-model-current{
  display:flex;
  align-items:center;
  justify-content:space-between;
  gap:1rem;
  padding:10px 14px;
  background:#faf8ff;
  border:0.5px solid rgba(120,80,200,0.2);
  border-radius:8px;
}

.cfg-model-info{
  display:flex;
  align-items:center;
  gap:10px;
}

.cfg-model-icon{
  width:28px;
  height:28px;
  border-radius:6px;
  background:#EDE8FC;
  display:flex;
  align-items:center;
  justify-content:center;
  color:#4B2DAB;
  font-size:14px;
  flex-shrink:0;
}

.cfg-model-name{
  font-size:13px;
  color:#1e1050;
  font-weight:500;
}

.cfg-model-meta{
  font-size:11px;
  color:#7b6aaa;
  margin-top:1px;
}
`;