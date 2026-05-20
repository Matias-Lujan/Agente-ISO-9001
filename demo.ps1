# ============================================================
# demo.ps1 - Demo del Agente de Verificacion de Consistencia
# Agente Inteligente de Auditoria ISO 9001 - BDT Global
# USO: .\demo.ps1
# Requiere que la API este corriendo en http://localhost:5180
# ============================================================

$jsonPath = "$PSScriptRoot\backend\ISOAuditAgent.API\demo.json"

# -- Verificar que la API este corriendo ---------------------
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5180/api/consistency-verification/health" -Method Get -UseBasicParsing
} catch {
    Write-Host ""
    Write-Host "  ERROR: La API no esta corriendo." -ForegroundColor Red
    Write-Host "  Levantala con: dotnet run" -ForegroundColor Yellow
    Write-Host ""
    exit
}

# -- Verificar que exista el demo.json -----------------------
if (-not (Test-Path $jsonPath)) {
    Write-Host ""
    Write-Host "  ERROR: No se encontro demo.json en:" -ForegroundColor Red
    Write-Host "  $jsonPath" -ForegroundColor Yellow
    Write-Host ""
    exit
}

# -- Llamar a la API -----------------------------------------
Write-Host ""
Write-Host "  Ejecutando analisis de consistencia..." -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod `
        -Uri "http://localhost:5180/api/consistency-verification" `
        -Method Post `
        -ContentType "application/json" `
        -Body (Get-Content $jsonPath -Raw) `
        -UseBasicParsing
} catch {
    Write-Host "  ERROR al llamar a la API: $_" -ForegroundColor Red
    exit
}

# -- Funciones de formato ------------------------------------
function Separador { Write-Host ("=" * 60) -ForegroundColor DarkCyan }
function SeparadorFino { Write-Host ("-" * 60) -ForegroundColor DarkGray }

# ============================================================
# ENCABEZADO
# ============================================================
Separador
Write-Host "  AGENTE DE VERIFICACION DE CONSISTENCIA ISO 9001" -ForegroundColor Cyan
Write-Host "  Sub-agente 3 - ConsistencyVerification" -ForegroundColor DarkCyan
Separador
Write-Host ""
Write-Host "  Auditoria ID  : " -NoNewline; Write-Host $response.auditoriaId -ForegroundColor White
Write-Host "  Agente Origen : " -NoNewline; Write-Host $response.agenteOrigen -ForegroundColor White
Write-Host "  Total hallazgos: " -NoNewline

if ($response.hallazgos.Count -eq 0) {
    Write-Host "0 - Sin problemas detectados" -ForegroundColor Green
} else {
    Write-Host "$($response.hallazgos.Count) problema(s) detectado(s)" -ForegroundColor Red
}

# ============================================================
# ARTEFACTOS ANALIZADOS (del demo.json)
# ============================================================
Write-Host ""
Separador
Write-Host "  ARTEFACTOS RECIBIDOS DE DOCUMENT ANALYSIS" -ForegroundColor Cyan
Separador

$artefactos = Get-Content $jsonPath | ConvertFrom-Json
foreach ($a in $artefactos.artefactos) {
    $icono = switch ($a.estadoDisponibilidad) {
        "Encontrado"          { "[OK]" }
        "Faltante"            { "[X] " }
        "NoBuscado"           { "[-] " }
    }
    $colorEstado = switch ($a.estadoDisponibilidad) {
        "Encontrado"          { "Green" }
        "Faltante"            { "Red" }
        "NoBuscado"           { "DarkGray" }
    }
    $etiqueta = if ($a.exigibilidad -eq "PendienteEtapaFutura") { " [etapa futura]" } else { "" }

    Write-Host "  $icono " -NoNewline -ForegroundColor $colorEstado
    Write-Host "$($a.nombreArtefacto)$etiqueta" -ForegroundColor White

    # Mostrar secciones vacias si tiene
    $seccionesVacias = $a.seccionesDetectadas | Where-Object { -not $_.tieneContenido }
    foreach ($s in $seccionesVacias) {
        Write-Host "       Seccion vacia: $($s.titulo)" -ForegroundColor Yellow
    }
}

# ============================================================
# HALLAZGOS DETECTADOS (Contrato 3 - HallazgosPreliminares)
# ============================================================
Write-Host ""
Separador
Write-Host "  HALLAZGOS PRELIMINARES DETECTADOS" -ForegroundColor Cyan
Write-Host "  (seran clasificados por FindingsClassification)" -ForegroundColor DarkGray
Separador

if ($response.hallazgos.Count -eq 0) {
    Write-Host ""
    Write-Host "  Sin hallazgos. El proceso cumple con los requisitos de consistencia." -ForegroundColor Green
} else {
    $i = 1
    foreach ($h in $response.hallazgos) {
        Write-Host ""
        Write-Host "  Hallazgo #$i" -ForegroundColor Yellow
        SeparadorFino
        Write-Host "  Artefacto ID : $($h.artefactoEsperadoId)" -ForegroundColor Gray
        Write-Host "  Origen regla : $($h.origenRegla)" -ForegroundColor Gray
        Write-Host "  Descripcion  : $($h.descripcion)" -ForegroundColor White
        Write-Host "  Justificacion: $($h.justificacion)" -ForegroundColor DarkGray
        $i++
    }
}

# ============================================================
# RESUMEN
# ============================================================
Write-Host ""
Separador
Write-Host "  RESUMEN" -ForegroundColor Cyan
Separador
Write-Host ""

$porOrigen = $response.hallazgos | Group-Object origenRegla
foreach ($g in $porOrigen) {
    Write-Host "  $($g.Name): $($g.Count) hallazgo(s)" -ForegroundColor White
}

Write-Host ""
if ($response.hallazgos.Count -eq 0) {
    Write-Host "  El proceso no presenta problemas de consistencia." -ForegroundColor Green
} else {
    Write-Host "  Se detectaron $($response.hallazgos.Count) problema(s) de consistencia." -ForegroundColor Red
    Write-Host "  Estos hallazgos seran enviados a FindingsClassification" -ForegroundColor DarkGray
    Write-Host "  para su clasificacion en NC / OBS / OM." -ForegroundColor DarkGray
}

Write-Host ""
Separador
Write-Host ""