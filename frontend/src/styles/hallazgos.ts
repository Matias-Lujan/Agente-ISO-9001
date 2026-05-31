
// ============================================================================
//  Estilos de la pantalla Hallazgos.
//
//  Sigue el patron del proyecto: CSS-in-string + useInjectStyle.
//  Usa la misma paleta que sharedCss (1e1050 / 4B2DAB / f0eefa / faf8ff / 7B52E8)
//  para que se sienta parte del sistema.
// ============================================================================

export const hallazgosCss = `
/* ────────────────────────────────────────────────────────────────────────── */
/*  HEADER                                                                    */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-header{
  display:flex;
  align-items:flex-start;
  justify-content:space-between;
  gap:1rem;
  margin-bottom:1.5rem;
}

.hz-header-title{
  font-size:22px;
  font-weight:600;
  color:#1e1050;
  margin:0;
}

.hz-header-sub{
  font-size:13px;
  color:#7b6aaa;
  margin-top:4px;
}

.hz-export-btn{
  display:inline-flex;
  align-items:center;
  gap:6px;
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:8px;
  padding:8px 14px;
  font-size:13px;
  font-weight:500;
  color:#4B2DAB;
  cursor:pointer;
  transition:all .15s;
  font-family:inherit;
}
.hz-export-btn:hover:not(:disabled){
  background:#faf8ff;
  border-color:#7B52E8;
}
.hz-export-btn:disabled{opacity:0.5;cursor:not-allowed;}

/* ────────────────────────────────────────────────────────────────────────── */
/*  CARDS RESUMEN (4 tarjetas arriba)                                         */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-stats{
  display:grid;
  grid-template-columns:repeat(4, 1fr);
  gap:12px;
  margin-bottom:1.5rem;
}

@media (max-width:900px){
  .hz-stats{grid-template-columns:repeat(2, 1fr);}
}

.hz-stat-card{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.15);
  border-radius:10px;
  padding:1rem 1.25rem;
}

.hz-stat-label{
  font-size:12px;
  color:#7b6aaa;
  margin-bottom:4px;
}

.hz-stat-value{
  font-size:28px;
  font-weight:600;
  color:#1e1050;
  line-height:1.1;
  margin-bottom:4px;
}

.hz-stat-value.nc{color:#A32D2D;}
.hz-stat-value.obs{color:#BA7517;}
.hz-stat-value.om{color:#27500A;}

.hz-stat-meta{
  font-size:11px;
  color:#9080bb;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  FILTROS Y BUSCADOR                                                        */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-filters-bar{
  display:flex;
  align-items:center;
  gap:10px;
  margin-bottom:1rem;
  flex-wrap:wrap;
}

.hz-search{
  display:flex;
  align-items:center;
  gap:8px;
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:8px;
  padding:8px 12px;
  min-width:280px;
  flex:1;
  max-width:340px;
}

.hz-search svg{flex-shrink:0;color:#9080bb;}

.hz-search input{
  border:none;
  outline:none;
  background:transparent;
  font-family:inherit;
  font-size:13px;
  color:#1e1050;
  width:100%;
}

.hz-search input::placeholder{color:#b5a8d3;}

.hz-pills{
  display:flex;
  gap:6px;
  flex-wrap:wrap;
}

.hz-pill{
  background:transparent;
  border:0.5px solid rgba(120,80,200,0.25);
  color:#5a4a8a;
  border-radius:999px;
  padding:7px 16px;
  font-size:12px;
  font-weight:500;
  cursor:pointer;
  transition:all .15s;
  font-family:inherit;
}

.hz-pill:hover{background:#faf8ff;}

.hz-pill.active{
  background:#4B2DAB;
  color:#fff;
  border-color:#4B2DAB;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  TABLA                                                                     */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-table-card{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.15);
  border-radius:10px;
  overflow:hidden;
}

.hz-table{
  width:100%;
  border-collapse:collapse;
}

.hz-table thead th{
  text-align:left;
  font-size:12px;
  font-weight:500;
  color:#7b6aaa;
  padding:12px 16px;
  background:#faf8ff;
  border-bottom:0.5px solid rgba(120,80,200,0.12);
}

.hz-table tbody td{
  padding:14px 16px;
  font-size:13px;
  color:#1e1050;
  border-bottom:0.5px solid rgba(120,80,200,0.08);
  vertical-align:top;
}

.hz-table tbody tr:last-child td{border-bottom:none;}

.hz-table tbody tr:hover{background:#faf8ff;}

.hz-row-title{
  font-weight:500;
  margin-bottom:3px;
}

.hz-row-meta{
  font-size:11px;
  color:#7b6aaa;
}

/* Badges */
.hz-badge{
  display:inline-block;
  padding:3px 10px;
  border-radius:999px;
  font-size:11px;
  font-weight:600;
  letter-spacing:0.03em;
}

.hz-badge.nc{background:#FCEBEB;color:#A32D2D;}
.hz-badge.obs{background:#FFF4E5;color:#BA7517;}
.hz-badge.om{background:#EAF3DE;color:#27500A;}

.hz-estado-dot{
  display:inline-flex;
  align-items:center;
  gap:6px;
  font-size:12px;
  color:#1e1050;
}

.hz-estado-dot::before{
  content:'';
  width:7px;
  height:7px;
  border-radius:50%;
  background:#A32D2D;
}

.hz-estado-dot.estado-abierto::before    {background:#A32D2D;}
.hz-estado-dot.estado-en-revision::before{background:#BA7517;}
.hz-estado-dot.estado-resuelto::before   {background:#27500A;}

.hz-ver-btn{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:6px;
  padding:5px 12px;
  font-size:12px;
  color:#4B2DAB;
  cursor:pointer;
  font-family:inherit;
  transition:all .15s;
}
.hz-ver-btn:hover{
  background:#faf8ff;
  border-color:#7B52E8;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  PAGINACION                                                                */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-pagination{
  display:flex;
  align-items:center;
  justify-content:space-between;
  padding:14px 16px;
  border-top:0.5px solid rgba(120,80,200,0.08);
}

.hz-pagination-info{
  font-size:12px;
  color:#7b6aaa;
}

.hz-pagination-pages{
  display:flex;
  gap:4px;
}

.hz-page-btn{
  width:30px;
  height:30px;
  display:flex;
  align-items:center;
  justify-content:center;
  background:transparent;
  border:0.5px solid transparent;
  border-radius:6px;
  font-size:12px;
  color:#5a4a8a;
  cursor:pointer;
  font-family:inherit;
}
.hz-page-btn:hover{background:#faf8ff;}
.hz-page-btn.active{
  background:#4B2DAB;
  color:#fff;
  font-weight:500;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  EMPTY STATE                                                               */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-empty{
  padding:3rem 1.5rem;
  text-align:center;
  color:#7b6aaa;
  font-size:13px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  MODAL DE DETALLE                                                          */
/* ────────────────────────────────────────────────────────────────────────── */

.hz-modal-overlay{
  position:fixed;
  inset:0;
  background:rgba(30,16,80,0.4);
  display:flex;
  align-items:center;
  justify-content:center;
  z-index:1000;
  padding:1rem;
  animation:hz-fadeIn .15s ease;
}

@keyframes hz-fadeIn{
  from{opacity:0;}
  to{opacity:1;}
}

.hz-modal{
  background:#fff;
  border-radius:12px;
  max-width:540px;
  width:100%;
  max-height:90vh;
  overflow-y:auto;
  box-shadow:0 24px 60px rgba(30,16,80,0.25);
  animation:hz-slideUp .2s ease;
}

@keyframes hz-slideUp{
  from{transform:translateY(20px);opacity:0;}
  to{transform:translateY(0);opacity:1;}
}

.hz-modal-header{
  display:flex;
  align-items:flex-start;
  justify-content:space-between;
  gap:1rem;
  padding:20px 24px 12px;
  border-bottom:0.5px solid rgba(120,80,200,0.1);
}

.hz-modal-title{
  font-size:17px;
  font-weight:600;
  color:#1e1050;
  margin:0;
  line-height:1.3;
}

.hz-modal-close{
  background:none;
  border:none;
  cursor:pointer;
  color:#9080bb;
  padding:0;
  width:24px;
  height:24px;
  display:flex;
  align-items:center;
  justify-content:center;
  flex-shrink:0;
  font-size:22px;
  line-height:1;
}
.hz-modal-close:hover{color:#1e1050;}

.hz-modal-body{
  padding:18px 24px 24px;
}

.hz-modal-row{
  display:flex;
  flex-direction:column;
  gap:4px;
  margin-bottom:14px;
}

.hz-modal-label{
  font-size:11px;
  font-weight:600;
  color:#9080bb;
  text-transform:uppercase;
  letter-spacing:0.05em;
}

.hz-modal-value{
  font-size:13px;
  color:#1e1050;
  line-height:1.5;
}

.hz-modal-tags{
  display:flex;
  gap:8px;
  margin-bottom:16px;
}
`;