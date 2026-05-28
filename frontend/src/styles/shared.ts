// CSS calcado del prototipo del Google AI Studio (untitled/src/components),
// recortado a las reglas que usan Sidebar + NuevaAuditoria.
// Tema FIJO en claro: el toggle dark/light del prototipo no se incluye en esta
// etapa por decisión del usuario (no es prioritario).
//
// Se mantiene como string para inyectarlo una sola vez via useInjectStyle al
// montar el shell de la app. Si en el futuro se agrega Tailwind o CSS Modules,
// este archivo es el único lugar a refactorizar.
export const sharedCss = `
*{box-sizing:border-box;margin:0;padding:0;}

body{
  font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;
  background:#f0eefa;
  color:#1e1050;
}

/* ----- Shell ----- */
.shell{display:flex;min-height:100vh;}

/* ----- Sidebar ----- */
.sidebar{
  width:220px;
  display:flex;flex-direction:column;
  padding:1.5rem 0;
  flex-shrink:0;
  background:#1e1050;
}

.sb-logo{padding:0 1.25rem 1.5rem;border-bottom:0.5px solid rgba(255,255,255,0.1);}
.sb-logo img{height:28px;width:auto;filter:brightness(10);}

.sb-user{
  padding:1rem 1.25rem;
  display:flex;align-items:center;gap:8px;
  margin-bottom:0.5rem;
  border-bottom:0.5px solid rgba(255,255,255,0.1);
}
.sb-avatar{
  width:32px;height:32px;border-radius:50%;
  display:flex;align-items:center;justify-content:center;
  font-size:12px;font-weight:500;flex-shrink:0;
  background:#4B2DAB;color:#fff;
}
.sb-name{font-size:13px;font-weight:500;color:#fff;}
.sb-role{font-size:11px;color:rgba(255,255,255,0.5);}
.sb-user-placeholder{font-size:12px;color:rgba(255,255,255,0.5);font-style:italic;}

.nav-item{
  display:flex;align-items:center;gap:10px;
  padding:10px 1.25rem;font-size:13px;
  color:rgba(255,255,255,0.6);
  text-decoration:none;
  cursor:default;
  transition:all .15s;
}
.nav-item.clickable{cursor:pointer;}
.nav-item.clickable:hover{color:#fff;background:rgba(255,255,255,0.07);}
.nav-item.active{
  color:#fff;background:rgba(75,45,171,0.5);
  border-right:2px solid #7B52E8;
}
.nav-item.disabled{opacity:0.45;}
.nav-icon{width:16px;height:16px;flex-shrink:0;}

/* ----- Main panel ----- */
.main{flex:1;padding:1.5rem;overflow:auto;}

.topbar{display:flex;align-items:center;justify-content:space-between;margin-bottom:1.5rem;}
.page-title{font-size:18px;font-weight:500;color:#1e1050;}
.page-sub{font-size:13px;margin-top:2px;color:#7b6aaa;}

/* ----- Cards ----- */
.card{
  background:#fff;border:0.5px solid rgba(120,80,200,0.15);
  border-radius:10px;
  padding:1.25rem;
  margin-bottom:1rem;
}
.card-title{
  font-size:14px;font-weight:500;
  margin-bottom:1rem;
  display:flex;align-items:center;gap:6px;
  color:#1e1050;
}
.card-title svg{width:16px;height:16px;flex-shrink:0;color:#4B2DAB;}

/* ----- Form ----- */
.field{margin-bottom:1rem;}
.field:last-child{margin-bottom:0;}
.field label{display:block;font-size:12px;margin-bottom:5px;font-weight:500;color:#5a4a8a;}
.field select{
  width:100%;padding:9px 12px;border-radius:8px;font-size:13px;
  outline:none;font-family:inherit;
  border:0.5px solid rgba(120,80,200,0.25);
  background:#faf8ff;color:#1e1050;
  transition:all .2s;
}
.field select:focus{border-color:#7B52E8;background:#fff;}
.field select:disabled{opacity:0.5;cursor:not-allowed;}
.field-row{display:grid;grid-template-columns:1fr 1fr;gap:12px;}

/* ----- Resumen ----- */
.resumen-box{background:#f8f5ff;border-radius:8px;padding:12px;margin-bottom:1rem;}
.res-row{display:flex;justify-content:space-between;padding:4px 0;font-size:13px;}
.res-key{color:#7b6aaa;}
.res-val{font-weight:500;color:#1e1050;}
.res-val.placeholder{color:#b5a8d3;font-weight:400;font-style:italic;}

/* ----- Botones ----- */
.btn-row{display:flex;gap:8px;justify-content:flex-end;}
.btn-pri{
  padding:9px 20px;border-radius:8px;border:none;
  background:#4B2DAB;font-size:13px;color:#fff;
  cursor:pointer;font-weight:500;
  transition:all .2s;
}
.btn-pri:hover:not(:disabled){background:#3a2090;}
.btn-pri:disabled{opacity:0.5;cursor:not-allowed;}

/* ----- Ejecución ----- */
.running-box{
  background:#EDE8FC;border-radius:10px;
  padding:1.5rem;text-align:center;
}
.running-title{font-size:15px;font-weight:500;margin-bottom:0.5rem;color:#1e1050;}
.running-sub{font-size:13px;margin-bottom:1rem;color:#7b6aaa;}

.agent-list{display:flex;flex-direction:column;gap:6px;text-align:left;}
.agent-row{
  display:flex;align-items:center;gap:8px;
  padding:8px 10px;border-radius:8px;font-size:12px;
  background:#fff;color:#1e1050;
}
.agent-status{font-size:11px;margin-left:auto;}
.agent-status.pendiente{color:#7b6aaa;}
.agent-status.encurso{color:#BA7517;}
.agent-status.completado{color:#27500A;}
.agent-status.fallido{color:#A32D2D;}

.spin{display:inline-block;animation:spin 1s linear infinite;}
@keyframes spin{to{transform:rotate(360deg);}}

/* ----- Estados finales ----- */
.final-box{
  background:#EAF3DE;border-radius:10px;
  padding:1.5rem;text-align:center;
  font-size:15px;font-weight:500;color:#27500A;
}
.final-box.error{background:#FCEBEB;color:#A32D2D;}

.error-banner{
  background:#FCEBEB;color:#A32D2D;
  padding:10px 14px;border-radius:8px;
  font-size:13px;margin-bottom:1rem;
}

.loading-text{font-size:13px;color:#7b6aaa;text-align:center;padding:2rem;}
`;
