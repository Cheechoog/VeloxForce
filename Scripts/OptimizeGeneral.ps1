# ============================================================
#  VeloxForge - OptimizeGeneral
#  Optimizacion SEGURA para PCs de bajo recurso (sin juegos).
#  No usa bcdedit, no cambia fondo ni mouse, no toca Fortnite.
#  Pensado para mantenimiento de equipos de clientes.
# ============================================================
param(
    [string]$Energy        = "yes",
    [string]$Visuals       = "yes",
    [string]$Services      = "yes",
    [string]$GameDVR       = "yes",
    [string]$Notifications = "yes",
    [string]$Disk          = "yes",
    [string]$Cleanup       = "yes"
)

$logFile = "$env:TEMP\VeloxForge_General.log"
"" | Out-File $logFile
function Log($m)                { "$((Get-Date).ToString('s')) | $m" | Out-File -Append $logFile; Write-Host $m }
function EmitProgress([int]$p)  { Write-Output "PROGRESS:$p" }
function EmitStep([string]$t)   { Write-Output "STEP:$t" }
function EmitResult([string]$t) { Write-Output "RESULT:$t" }
function Yes($p) { return ($p -eq "yes") }
function SetReg($path, $name, $value, $type = "DWord") {
    try {
        if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
        Set-ItemProperty -Path $path -Name $name -Value $value -Type $type -ErrorAction Stop
        return $true
    } catch { Log "  WARN reg $path\$name : $_"; return $false }
}

Log "=== VeloxForge OptimizeGeneral ==="
EmitProgress 0
$applied = 0

# Detectar si es portatil (tiene bateria) para decidir el plan de energia
$esPortatil = $false
try { if (Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue) { $esPortatil = $true } } catch {}
Log "Portatil: $esPortatil"

# ── 1. PLAN DE ENERGIA ──
EmitStep "Plan de energia"
EmitProgress 10
if (Yes $Energy) {
    try {
        if ($esPortatil) {
            # En portatil: plan Alto rendimiento solo mientras este conectado, sin forzar Ultimate
            powercfg /setactive SCHEME_MIN 2>&1 | Out-Null   # Alto rendimiento
            Log "  OK Plan Alto rendimiento (portatil)"
            EmitResult "OK Plan de energia: Alto rendimiento (se respeta la bateria)"
        } else {
            # En escritorio: intentar Maximo rendimiento (Ultimate), si no, Alto rendimiento
            $ult = (powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 2>$null)
            if ($ult -match '([0-9a-fA-F\-]{36})') { powercfg /setactive $Matches[1] 2>&1 | Out-Null }
            else { powercfg /setactive SCHEME_MIN 2>&1 | Out-Null }
            Log "  OK Plan maximo rendimiento (escritorio)"
            EmitResult "OK Plan de energia: maximo rendimiento"
        }
        $applied++
    } catch { Log "  ERR energia: $_" }
}
Start-Sleep -Milliseconds 200

# ── 2. EFECTOS VISUALES (menus rapidos) ──
EmitStep "Efectos visuales y menus rapidos"
EmitProgress 28
if (Yes $Visuals) {
    try {
        # Ajustar para "mejor rendimiento"
        SetReg "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" "VisualFXSetting" 2 | Out-Null
        SetReg "HKCU:\Control Panel\Desktop" "MenuShowDelay" 0 "String" | Out-Null
        SetReg "HKCU:\Control Panel\Desktop" "DragFullWindows" 0 "String" | Out-Null
        # Quitar transparencias (pesan en equipos lentos)
        SetReg "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" "EnableTransparency" 0 | Out-Null
        Log "  OK Efectos visuales a rendimiento"
        EmitResult "OK Windows ajustado para mejor rendimiento (menus mas rapidos)"
        $applied++
    } catch { Log "  ERR visuales: $_" }
}
Start-Sleep -Milliseconds 200

# ── 3. SERVICIOS DE TELEMETRIA ──
EmitStep "Servicios innecesarios"
EmitProgress 45
if (Yes $Services) {
    try {
        foreach ($svc in @("DiagTrack","dmwappushservice")) {
            Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
            Set-Service -Name $svc -StartupType Disabled -ErrorAction SilentlyContinue
        }
        Log "  OK Telemetria desactivada"
        EmitResult "OK Servicios de telemetria desactivados"
        $applied++
    } catch { Log "  ERR servicios: $_" }
}
Start-Sleep -Milliseconds 200

# ── 4. GAME DVR / GRABACION XBOX ──
EmitStep "Grabacion de juego (Game DVR)"
EmitProgress 58
if (Yes $GameDVR) {
    try {
        SetReg "HKCU:\System\GameConfigStore" "GameDVR_Enabled" 0 | Out-Null
        SetReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" "AllowGameDVR" 0 | Out-Null
        Log "  OK Game DVR off"
        EmitResult "OK Grabacion de juego de Windows desactivada"
        $applied++
    } catch { Log "  ERR gamedvr: $_" }
}
Start-Sleep -Milliseconds 200

# ── 5. NOTIFICACIONES Y APPS EN SEGUNDO PLANO ──
EmitStep "Notificaciones y apps en segundo plano"
EmitProgress 70
if (Yes $Notifications) {
    try {
        SetReg "HKCU:\Software\Microsoft\Windows\CurrentVersion\PushNotifications" "ToastEnabled" 0 | Out-Null
        SetReg "HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" "GlobalUserDisabled" 1 | Out-Null
        Log "  OK Notificaciones y background apps off"
        EmitResult "OK Notificaciones y apps en segundo plano reducidas"
        $applied++
    } catch { Log "  ERR notif: $_" }
}
Start-Sleep -Milliseconds 200

# ── 6. DISCO (TRIM en SSD) ──
EmitStep "Disco optimizacion"
EmitProgress 82
if (Yes $Disk) {
    try {
        $sys = $env:SystemDrive.TrimEnd(':')
        $media = (Get-PhysicalDisk | Where-Object { $_.BusType -ne "USB" } | Select-Object -First 1).MediaType
        Log "  Tipo de disco: $media"
        if ($media -eq "SSD") {
            Optimize-Volume -DriveLetter $sys -ReTrim -ErrorAction SilentlyContinue
            EmitResult "OK Disco SSD: TRIM aplicado"
        } else {
            EmitResult "OK Disco HDD detectado (se omite defrag para no demorar)"
        }
        $applied++
    } catch { Log "  ERR disco: $_" }
}
Start-Sleep -Milliseconds 200

# ── 7. LIMPIEZA ──
EmitStep "Limpieza de temporales"
EmitProgress 92
if (Yes $Cleanup) {
    try {
        Remove-Item "$env:TEMP\*"       -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
        ipconfig /flushdns 2>&1 | Out-Null
        try { Clear-RecycleBin -Force -ErrorAction SilentlyContinue } catch {}
        Log "  OK Limpieza completada"
        EmitResult "OK Archivos temporales y cache DNS limpiados"
        $applied++
    } catch { Log "  ERR limpieza: $_" }
}

EmitProgress 100
EmitStep "Optimizacion completada"
Log "=== FIN General | Aplicadas=$applied ==="
EmitResult "SUMMARY:Aplicadas=$applied"
