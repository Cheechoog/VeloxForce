# ============================================================
#  VeloxForge - RestoreDefault
#  Revierte TODO lo que aplica el perfil competitivo:
#  servicios, energia, registro, red, mouse, timer, efectos
#  visuales, fondo y el .ini de Fortnite (desde backup).
#  Devuelve Windows a sus valores por defecto.
# ============================================================

$logFile = "$env:TEMP\VeloxForge_Restore.log"
"" | Out-File $logFile
function Log($m)                { "$((Get-Date).ToString('s')) | $m" | Out-File -Append $logFile; Write-Host $m }
function EmitProgress([int]$p)  { Write-Output "PROGRESS:$p" }
function EmitStep([string]$t)   { Write-Output "STEP:$t" }
function EmitResult([string]$t) { Write-Output "RESULT:$t" }

function SetReg($path, $name, $value, $type = "DWord") {
    try {
        if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
        Set-ItemProperty -Path $path -Name $name -Value $value -Type $type -ErrorAction Stop
    } catch { Log "  WARN SetReg $path\$name : $_" }
}
function DelReg($path, $name) {
    try { Remove-ItemProperty -Path $path -Name $name -ErrorAction SilentlyContinue } catch {}
}

Log "=== VeloxForge RestoreDefault ==="
EmitProgress 0
$reboot = $false

# ── 1. SERVICIOS: reactivar a su arranque por defecto ──
EmitStep "Servicios restaurados"
EmitProgress 10
$svcDefaults = @{
    "SysMain"="Automatic"; "DiagTrack"="Automatic"; "WSearch"="Automatic";
    "OneSyncSvc"="Automatic"; "XblAuthManager"="Manual"; "XblGameSave"="Manual";
    "XboxNetApiSvc"="Manual"; "XboxGipSvc"="Manual"; "TabletInputService"="Manual";
    "Fax"="Manual"; "RemoteRegistry"="Disabled"; "WMPNetworkSvc"="Manual";
    "icssvc"="Manual"; "MapsBroker"="Automatic"; "lfsvc"="Manual";
    "RetailDemo"="Manual"; "SharedAccess"="Manual";
    "NvTelemetryContainer"="Automatic"; "nvagent"="Manual"; "NvContainerLocalSystem"="Automatic"
}
foreach ($name in $svcDefaults.Keys) {
    try {
        if (Get-Service -Name $name -ErrorAction SilentlyContinue) {
            Set-Service -Name $name -StartupType $svcDefaults[$name] -ErrorAction SilentlyContinue
            if ($svcDefaults[$name] -eq "Automatic") { Start-Service -Name $name -ErrorAction SilentlyContinue }
        }
    } catch {}
}
EmitResult "OK Servicios de Windows reactivados"

# ── 2. PLAN DE ENERGIA: Balanceado ──
EmitStep "Plan de energia (Balanceado)"
EmitProgress 22
powercfg -setactive SCHEME_BALANCED 2>&1 | Out-Null
DelReg "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" "PowerThrottlingOff"
EmitResult "OK Energia en Balanceado"

# ── 3. CPU / PRIORIDADES ──
EmitStep "Prioridades del procesador"
EmitProgress 34
$mm = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
SetReg $mm "SystemResponsiveness" 20
SetReg $mm "NetworkThrottlingIndex" 10
SetReg "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" "Win32PrioritySeparation" 2
# Quitar prioridad forzada a Fortnite
foreach ($exe in "FortniteClient-Win64-Shipping.exe","FortniteClient-Win64-Shipping_EAC_EOS.exe") {
    $p = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\$exe\PerfOptions"
    DelReg $p "CpuPriorityClass"
}
try { bcdedit /deletevalue numproc 2>&1 | Out-Null } catch {}
EmitResult "OK Prioridades del CPU a valores normales"

# ── 4. GPU / DWM ──
EmitStep "Graficos (MPO y prioridades)"
EmitProgress 46
DelReg "HKLM:\SOFTWARE\Microsoft\Windows\Dwm" "OverlayTestMode"
DelReg "HKLM:\SOFTWARE\Microsoft\Windows\Dwm" "Latency"
SetReg "HKCU:\SOFTWARE\Microsoft\DirectX\UserGpuPreferences" "DirectXUserGlobalSettings" "SwapEffectUpgradeEnable=1;" "String"
EmitResult "OK Graficos a valores por defecto (MPO reactivado)"

# ── 5. TIMER ──
EmitStep "Timer del sistema"
EmitProgress 56
DelReg "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" "GlobalTimerResolutionRequests"
try { bcdedit /deletevalue disabledynamictick 2>&1 | Out-Null; $reboot = $true } catch {}
EmitResult "OK Timer del sistema normalizado"

# ── 6. MEMORIA ──
EmitStep "Memoria (prefetch y superfetch)"
EmitProgress 64
$mmg  = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"
$pref = "$mmg\PrefetchParameters"
SetReg $mmg  "DisablePagingExecutive" 0
SetReg $pref "EnablePrefetcher" 3
SetReg $pref "EnableSuperfetch" 3
EmitResult "OK Memoria a valores por defecto"

# ── 7. RED ──
EmitStep "Red (Nagle y TCP)"
EmitProgress 72
$ifBase = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"
try {
    Get-ChildItem $ifBase -ErrorAction SilentlyContinue | ForEach-Object {
        DelReg $_.PSPath "TcpAckFrequency"
        DelReg $_.PSPath "TcpNoDelay"
    }
} catch {}
DelReg "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" "DefaultTTL"
DelReg "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" "MaxCacheEntryTtlLimit"
DelReg "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" "MaxSOACacheEntryTtlLimit"
EmitResult "OK Red a valores por defecto"

# ── 8. MOUSE: reactivar aceleracion por defecto ──
EmitStep "Mouse (aceleracion por defecto)"
EmitProgress 80
SetReg "HKCU:\Control Panel\Mouse" "MouseSpeed"      "1"  "String"
SetReg "HKCU:\Control Panel\Mouse" "MouseThreshold1" "6"  "String"
SetReg "HKCU:\Control Panel\Mouse" "MouseThreshold2" "10" "String"
DelReg "HKCU:\Control Panel\Mouse" "SmoothMouseXCurve"
DelReg "HKCU:\Control Panel\Mouse" "SmoothMouseYCurve"
EmitResult "OK Mouse a valores por defecto"

# ── 9. NOTIFICACIONES / BUSQUEDA ──
EmitStep "Notificaciones y busqueda"
EmitProgress 86
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications" "ToastEnabled" 1
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" "GlobalUserDisabled" 0
DelReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" "LetAppsRunInBackground"
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search" "BingSearchEnabled" 1
DelReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" "AllowTelemetry"
DelReg "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" "AllowNewsAndInterests"
EmitResult "OK Notificaciones y busqueda reactivadas"

# ── 10. GAME DVR ──
EmitStep "Grabacion de juego (Game DVR)"
EmitProgress 90
SetReg "HKCU:\System\GameConfigStore" "GameDVR_Enabled" 1
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR" "AppCaptureEnabled" 1
DelReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" "AllowGameDVR"
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameBar" "UseNexusForGameBarEnabled" 1
EmitResult "OK Game DVR restaurado"

# ── 11. EFECTOS VISUALES / FONDO ──
EmitStep "Efectos visuales y fondo"
EmitProgress 94
$desktop = "HKCU:\Control Panel\Desktop"
$wm      = "HKCU:\Control Panel\Desktop\WindowMetrics"
$adv     = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" "VisualFXSetting" 0
SetReg $desktop "DragFullWindows" "1" "String"
SetReg $desktop "MenuShowDelay"   "400" "String"
SetReg $wm      "MinAnimate"      "1" "String"
SetReg $adv     "TaskbarAnimations"   1
SetReg $adv     "ListviewAlphaSelect" 1
SetReg $adv     "ListviewShadow"      1
SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize" "EnableTransparency" 1
SetReg $desktop "UserPreferencesMask" ([byte[]](0x9E,0x1E,0x07,0x80,0x12,0x00,0x00,0x00)) "Binary"

# Fondo: restaurar el que estaba guardado
try {
    $backup = (Get-ItemProperty $desktop -Name "WallPaperBackup" -ErrorAction SilentlyContinue).WallPaperBackup
    if ($backup) {
        SetReg $desktop "WallPaper" $backup "String"
        Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public class WP { [DllImport("user32.dll")] public static extern int SystemParametersInfo(int a,int b,string c,int d);}' -ErrorAction SilentlyContinue
        [WP]::SystemParametersInfo(0x0014, 0, $backup, 0x01 -bor 0x02) | Out-Null
    }
    DelReg "HKCU:\Control Panel\Colors" "Background"
} catch { Log "  WARN wallpaper: $_" }
EmitResult "OK Efectos visuales y fondo restaurados"

# ── 12. FORTNITE INI (desde backup) ──
EmitStep "Fortnite GameUserSettings.ini"
EmitProgress 97
$ini = "$env:LOCALAPPDATA\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"
$bak = "$ini.bak"
$fnRunning = Get-Process -Name "FortniteClient-Win64-Shipping","FortniteLauncher" -ErrorAction SilentlyContinue
if ($fnRunning) {
    EmitResult "WARN Cierra Fortnite para restaurar sus graficos"
} elseif (Test-Path $bak) {
    try { Copy-Item $bak $ini -Force; EmitResult "OK Fortnite restaurado a como estaba" }
    catch { Log "  ERR ini: $_" }
} else {
    EmitResult "WARN No habia backup de Fortnite (no se habia optimizado)"
}

# ── Refrescar Explorador para ver los cambios al instante ──
EmitStep "Aplicando efectos visuales"
try {
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 900
    if (-not (Get-Process -Name explorer -ErrorAction SilentlyContinue)) { Start-Process explorer.exe }
} catch {}

EmitProgress 100
EmitStep "Restauracion completada"
Log "=== FIN Restore | Reinicio=$reboot ==="
EmitResult "SUMMARY:Windows restaurado a valores por defecto"
if ($reboot) { Write-Output "REBOOT_REQUIRED" }
