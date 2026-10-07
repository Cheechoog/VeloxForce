param (
    [string]$Cpu = "yes",
    [string]$Energy = "yes",
    [string]$Services = "yes",
    [string]$Fortnite = "yes",
    [string]$HAGS = "yes",
    [string]$RAM = "yes",
    [string]$visuals = "yes",
    [string]$CoreParking = "yes",
    [string]$Network = "yes",
    [string]$GameDVR = "yes",
    [string]$wallpaper = "yes",
    [string]$Notifications = "yes",
    [string]$MouseInput = "yes",
    [string]$PowerThrottle = "yes",
    [string]$TimerRes = "yes",
    [string]$KillProcesses = "yes",
    [string]$Disk = "yes",
    [string]$GameMode = "yes",
    [string]$GPU = "yes"
)
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
# Cambia la ruta antigua por esta:
$logFile = "$env:TEMP\VeloxForge_Script.log"
"" | Out-File $logFile

function Log($msg)               { "$((Get-Date).ToString('s')) | $msg" | Out-File -Append $logFile; Write-Host $msg }
function EmitProgress([int]$pct) { Write-Output "PROGRESS:$pct" }
function EmitStep([string]$txt)  { Write-Output "STEP:$txt" }
function EmitResult([string]$txt){ Write-Output "RESULT:$txt" }

Log "=== FortniteBoost OptimizeCompetitive v3 ==="
EmitProgress 0

$rebootNeeded = $false
$applied = 0
$skipped = 0

function Yes($p) { return ($p -eq "yes") }

function SetReg($path, $name, $value, $type = "DWord") {
    try {
        if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
        Set-ItemProperty -Path $path -Name $name -Value $value -Type $type -ErrorAction Stop
        return $true
    } catch {
        Log "  ERR SetReg [$path > $name]: $_"
        return $false
    }
}

# BLOQUE 1 - GAME DVR / XBOX BAR
EmitStep "Game DVR y Xbox Bar"
EmitProgress 3
if (Yes $GameDVR) {
    Log "Game DVR / Xbox..."
    SetReg "HKCU:\System\GameConfigStore" "GameDVR_Enabled" 0 | Out-Null
    SetReg "HKCU:\System\GameConfigStore" "GameDVR_FSEBehaviorMode" 2 | Out-Null
    SetReg "HKCU:\System\GameConfigStore" "GameDVR_HonorUserFSEBehaviorMode" 1 | Out-Null
    SetReg "HKCU:\System\GameConfigStore" "GameDVR_FSEBehavior" 2 | Out-Null
    SetReg "HKCU:\System\GameConfigStore" "GameDVR_DXGIHonorFSEWindowsCompatible" 1 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR" "AppCaptureEnabled" 0 | Out-Null
    SetReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" "AllowGameDVR" 0 | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR" "value" 0 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameBar" "UseNexusForGameBarEnabled" 0 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameBar" "ShowStartupPanel" 0 | Out-Null
    Log "  OK Game DVR y Xbox Bar desactivados"
    EmitResult "OK Game DVR OFF - reduce stutter de GPU"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 2 - GAME MODE
EmitStep "Game Mode avanzado"
EmitProgress 7
if (Yes $GameMode) {
    Log "Game Mode..."
    SetReg "HKCU:\SOFTWARE\Microsoft\GameBar" "AllowAutoGameMode" 1 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\GameBar" "AutoGameModeEnabled" 1 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameBar" "GamePanelStartupTipIndex" 3 | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" "GPU Priority" 8 "DWord" | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" "Priority" 6 "DWord" | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" "Scheduling Category" "High" "String" | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" "SFIO Priority" "High" "String" | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" "Background Only" "False" "String" | Out-Null
    Log "  OK Game Mode activado GPU=8 CPU=6"
    EmitResult "OK Game Mode ON - scheduler prioriza Fortnite"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 3 - CPU
EmitStep "CPU Responsiveness y prioridad Fortnite"
EmitProgress 12
if (Yes $Cpu) {
    Log "CPU..."
    $mmPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
    SetReg $mmPath "SystemResponsiveness" 0 | Out-Null
    SetReg $mmPath "NetworkThrottlingIndex" 0xFFFFFFFF | Out-Null

    $fn  = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FortniteClient-Win64-Shipping.exe\PerfOptions"
    $fn2 = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fortniteclient-win64-shipping_eac_eos.exe\PerfOptions"
    SetReg $fn  "CpuPriorityClass" 3 | Out-Null
    SetReg $fn2 "CpuPriorityClass" 3 | Out-Null

    try {
        $cores = (Get-WmiObject Win32_Processor).NumberOfLogicalProcessors
        bcdedit /set numproc $cores 2>&1 | Out-Null
        Log "  OK Nucleos habilitados: $cores"
        $rebootNeeded = $true
    } catch { Log "  WARN bcdedit: $_" }

    Log "  OK SystemResponsiveness=0 NetworkThrottle=OFF Fortnite=HIGH"
    EmitResult "OK CPU 100% para Fortnite"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 4 - PLAN DE ENERGIA
EmitStep "Plan de energia Ultimate Performance"
EmitProgress 17
if (Yes $Energy) {
    Log "Plan de energia..."
    try {
        $dup = powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 2>&1
        if ($dup -match "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})") {
            powercfg -setactive $matches[1] 2>&1 | Out-Null
            Log "  OK Ultimate Performance activado"
            EmitResult "OK Ultimate Performance - CPU sin throttling"
        } else {
            powercfg -setactive SCHEME_MIN 2>&1 | Out-Null
            Log "  OK Alto Rendimiento activado (fallback)"
            EmitResult "OK Alto Rendimiento activado"
        }
        $applied++
    } catch { Log "  ERR Energy: $_"; $skipped++ }
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 5 - POWER THROTTLING
EmitStep "Power Throttling desactivado"
EmitProgress 21
if (Yes $PowerThrottle) {
    Log "Power Throttling..."
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" "PowerThrottlingOff" 1 | Out-Null
    Log "  OK Power Throttling OFF"
    EmitResult "OK Power Throttling OFF"
    $applied++
    $rebootNeeded = $true
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 6 - CORE PARKING
EmitStep "Core Parking desactivado"
EmitProgress 25
if (Yes $CoreParking) {
    Log "Core Parking..."
    $cpPath = "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583"
    SetReg $cpPath "ValueMax" 100 | Out-Null
    SetReg $cpPath "ValueMin" 0 | Out-Null
    SetReg $cpPath "Attributes" 0 | Out-Null
    Log "  OK Core Parking OFF"
    EmitResult "OK Core Parking OFF - todos los nucleos activos"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 7 - HAGS
EmitStep "HAGS Hardware Accelerated GPU Scheduling"
EmitProgress 29
if (Yes $HAGS) {
    Log "HAGS..."
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" "HwSchMode" 2 | Out-Null
    Log "  OK HAGS activado"
    EmitResult "OK HAGS ON - GPU maneja su cola menos latencia"
    $applied++
    $rebootNeeded = $true
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 8 - GPU TWEAKS
EmitStep "GPU Tweaks NVIDIA AMD"
EmitProgress 33
if (Yes $GPU) {
    Log "GPU tweaks..."
    $gpuName = (Get-WmiObject Win32_VideoController | Select-Object -First 1).Name
    Log "  GPU detectada: $gpuName"

    SetReg "HKLM:\SOFTWARE\Microsoft\Windows\Dwm" "OverlayTestMode" 5 | Out-Null
    SetReg "HKLM:\SOFTWARE\Microsoft\Windows\Dwm" "Latency" 1 | Out-Null
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" "Win32PrioritySeparation" 38 | Out-Null

    if ($gpuName -match "NVIDIA|GeForce|RTX|GTX") {
        Log "  Aplicando tweaks NVIDIA..."
        $nvTelServices = @("NvTelemetryContainer","nvagent","NvContainerLocalSystem")
        foreach ($svc in $nvTelServices) {
            Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
            Set-Service  -Name $svc -StartupType Disabled -ErrorAction SilentlyContinue
        }
        SetReg "HKCU:\SOFTWARE\Microsoft\DirectX\UserGpuPreferences" "DirectXUserGlobalSettings" "VRROptimizeEnable=0;SwapEffectUpgradeEnable=1;" "String" | Out-Null
        Log "  OK NVIDIA telemetria OFF GPU dedicada forzada"
    }

    if ($gpuName -match "AMD|Radeon|RX ") {
        Log "  Aplicando tweaks AMD..."
        SetReg "HKLM:\SOFTWARE\Microsoft\Windows\Dwm" "OverlayTestMode" 5 | Out-Null
        Log "  OK AMD MPO desactivado"
    }

    Log "  OK GPU tweaks aplicados: $gpuName"
    EmitResult "OK GPU optimizada - MPO OFF prioridad grafica aumentada"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 9 - TIMER RESOLUTION
EmitStep "Timer Resolution del sistema"
EmitProgress 38
if (Yes $TimerRes) {
    Log "Timer Resolution..."
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" "GlobalTimerResolutionRequests" 1 | Out-Null

    try {
        bcdedit /set disabledynamictick yes 2>&1 | Out-Null
        Log "  OK Dynamic Tick deshabilitado"
        $rebootNeeded = $true
    } catch { Log "  WARN DynamicTick: $_" }

    try {
        bcdedit /deletevalue useplatformclock 2>&1 | Out-Null
    } catch { }

    Log "  OK Timer Resolution GlobalTimerResolutionRequests=1"
    EmitResult "OK Timer Resolution optimizado - menos stutter"
    $applied++
    $rebootNeeded = $true
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 10 - RAM
EmitStep "RAM Optimizacion de memoria"
EmitProgress 42
if (Yes $RAM) {
    Log "RAM..."
    $mmgPath  = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"
    $prefPath = "$mmgPath\PrefetchParameters"

    SetReg $mmgPath  "DisablePagingExecutive" 1 | Out-Null
    SetReg $mmgPath  "LargeSystemCache" 0 | Out-Null
    SetReg $prefPath "EnablePrefetcher" 0 | Out-Null
    SetReg $prefPath "EnableSuperfetch" 0 | Out-Null

    Log "  OK RAM kernel en RAM Prefetch OFF"
    EmitResult "OK RAM optimizada - kernel en memoria sin paginacion"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 11 - SERVICIOS
EmitStep "Servicios innecesarios"
EmitProgress 47
if (Yes $Services) {
    Log "Servicios..."

    $serviceList = @(
        @{ Name="SysMain";            Desc="Superfetch" },
        @{ Name="DiagTrack";          Desc="Telemetria Microsoft" },
        @{ Name="WSearch";            Desc="Windows Search Indexer" },
        @{ Name="OneSyncSvc";         Desc="Sincronizacion de cuentas" },
        @{ Name="XblAuthManager";     Desc="Xbox Live Auth" },
        @{ Name="XblGameSave";        Desc="Xbox Live Game Save" },
        @{ Name="XboxNetApiSvc";      Desc="Xbox Live Networking" },
        @{ Name="XboxGipSvc";         Desc="Xbox Accessory Management" },
        @{ Name="TabletInputService"; Desc="Panel tactil" },
        @{ Name="Fax";                Desc="Servicio de fax" },
        @{ Name="RemoteRegistry";     Desc="Acceso remoto al registro" },
        @{ Name="WMPNetworkSvc";      Desc="Windows Media Player Network" },
        @{ Name="icssvc";             Desc="Zona WiFi compartida" },
        @{ Name="MapsBroker";         Desc="Mapas de Windows" },
        @{ Name="lfsvc";              Desc="Servicio de ubicacion" },
        @{ Name="RetailDemo";         Desc="Modo demo tienda" },
        @{ Name="SharedAccess";       Desc="Compartir internet ICS" }
    )

    $disabledCount = 0
    foreach ($svc in $serviceList) {
        try {
            $s = Get-Service -Name $svc.Name -ErrorAction SilentlyContinue
            if ($s) {
                Stop-Service -Name $svc.Name -Force -ErrorAction SilentlyContinue
                Set-Service  -Name $svc.Name -StartupType Disabled -ErrorAction SilentlyContinue
                Log "  OK Desactivado: $($svc.Name) - $($svc.Desc)"
                $disabledCount++
            }
        } catch { Log "  WARN: $($svc.Name): $_" }
    }

    Log "  OK $disabledCount servicios desactivados"
    EmitResult "OK $disabledCount servicios OFF - CPU y disco libres"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 12 - KILL PROCESOS
EmitStep "Cerrando procesos en segundo plano"
EmitProgress 53
if (Yes $KillProcesses) {
    Log "Kill procesos..."

    $processesToKill = @(
        "chrome", "firefox", "msedge", "opera", "brave", "vivaldi",
        "Discord", "Teams", "Slack", "zoom", "skype",
        "OneDrive", "Dropbox", "GoogleDriveFS", "BoxDrive",
        "RazerSynapse3", "RzAgent",
        "LogiOptions", "LGHUB",
        "NZXT_CAM", "MSIAfterburner",
        "NahimicSvc", "Nahimic3",
        "Spotify",
        "EpicWebHelper",
        "AdobeUpdateService",
        "acrotray",
        "node"
    )

    $killedCount = 0
    foreach ($proc in $processesToKill) {
        try {
            $running = Get-Process -Name $proc -ErrorAction SilentlyContinue
            if ($running) {
                Stop-Process -Name $proc -Force -ErrorAction SilentlyContinue
                Log "  OK Cerrado: $proc"
                $killedCount++
            }
        } catch { }
    }

    try {
        Remove-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "OneDrive" -ErrorAction SilentlyContinue
        Log "  OK OneDrive eliminado del inicio automatico"
    } catch { }

    Log "  OK $killedCount procesos cerrados"
    EmitResult "OK $killedCount procesos cerrados - RAM liberada"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 13 - EFECTOS VISUALES
EmitStep "Efectos visuales Mejor rendimiento"
EmitProgress 58
if (Yes $Visuals) {
    Log "Efectos visuales..."

    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" "VisualFXSetting" 2 | Out-Null

    $desktopPath = "HKCU:\Control Panel\Desktop"
    $wmPath      = "HKCU:\Control Panel\Desktop\WindowMetrics"
    $advPath     = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"

    SetReg $desktopPath "DragFullWindows"     "0" "String" | Out-Null
    SetReg $desktopPath "MenuShowDelay"       "0" "String" | Out-Null
    SetReg $wmPath      "MinAnimate"          "0" "String" | Out-Null
    SetReg $advPath     "TaskbarAnimations"   0 | Out-Null
    SetReg $advPath     "ListviewAlphaSelect" 0 | Out-Null
    SetReg $advPath     "ListviewShadow"      0 | Out-Null

    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize" "EnableTransparency" 0 | Out-Null

    SetReg $desktopPath "UserPreferencesMask" ([byte[]](0x90,0x12,0x03,0x80,0x10,0x00,0x00,0x00)) "Binary" | Out-Null

    try {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinMsg2 {
    [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr h, uint m, UIntPtr w, IntPtr l, uint f, uint t, out UIntPtr r);
}
"@ -ErrorAction SilentlyContinue
        $r = [UIntPtr]::Zero
        [WinMsg2]::SendMessageTimeout([IntPtr]0xFFFF, 0x001A, [UIntPtr]::Zero, [IntPtr]::Zero, 2, 3000, [ref]$r) | Out-Null
    } catch { }

    Log "  OK Efectos visuales desactivados"
    EmitResult "OK Efectos visuales OFF - animaciones y transparencias eliminadas"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 14 - FONDO NEGRO
EmitStep "Fondo negro solido"
EmitProgress 62
if (Yes $Wallpaper) {
    Log "Wallpaper..."
    try {
        $current = (Get-ItemProperty "HKCU:\Control Panel\Desktop" -Name "WallPaper" -ErrorAction SilentlyContinue).WallPaper
        if ($current) { SetReg "HKCU:\Control Panel\Desktop" "WallPaperBackup" $current "String" | Out-Null }

        SetReg "HKCU:\Control Panel\Desktop" "WallPaper"      ""  "String" | Out-Null
        SetReg "HKCU:\Control Panel\Desktop" "WallpaperStyle" "0" "String" | Out-Null
        SetReg "HKCU:\Control Panel\Desktop" "TileWallpaper"  "0" "String" | Out-Null
        SetReg "HKCU:\Control Panel\Colors" "Background" "0 0 0" "String" | Out-Null

        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WPChanger2 {
    [DllImport("user32.dll", CharSet=CharSet.Auto)]
    public static extern int SystemParametersInfo(int a, int b, string c, int d);
}
"@ -ErrorAction SilentlyContinue
        [WPChanger2]::SystemParametersInfo(0x0014, 0, "", 0x01 -bor 0x02) | Out-Null

        Log "  OK Fondo negro aplicado"
        EmitResult "OK Fondo negro - GPU libera recursos del wallpaper"
        $applied++
    } catch { Log "  ERR Wallpaper: $_"; $skipped++ }
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 15 - NOTIFICACIONES
EmitStep "Notificaciones y background apps"
EmitProgress 66
if (Yes $Notifications) {
    Log "Notificaciones..."
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications"            "ToastEnabled"         0 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" "GlobalUserDisabled"   1 | Out-Null
    SetReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"                         "LetAppsRunInBackground" 2 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"                       "BingSearchEnabled"    0 | Out-Null
    SetReg "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"                       "CortanaConsent"       0 | Out-Null
    SetReg "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"                     "AllowTelemetry"       0 | Out-Null
    SetReg "HKLM:\SOFTWARE\Policies\Microsoft\Dsh"                                        "AllowNewsAndInterests" 0 | Out-Null
    Log "  OK Notificaciones y background apps OFF"
    EmitResult "OK Notificaciones OFF - sin pop-ups ni CPU en background"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 16 - RED
EmitStep "Red Nagle Algorithm y TCP"
EmitProgress 71
if (Yes $Network) {
    Log "Red..."
    try {
        $localIP  = (Test-Connection -ComputerName $env:COMPUTERNAME -Count 1 -ErrorAction SilentlyContinue).IPV4Address.IPAddressToString
        $ifPath   = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"
        $ifaces   = Get-ChildItem $ifPath -ErrorAction SilentlyContinue

        $found = $false
        foreach ($iface in $ifaces) {
            $dhcp = (Get-ItemProperty $iface.PSPath -Name "DhcpIPAddress" -ErrorAction SilentlyContinue).DhcpIPAddress
            if ($dhcp -eq $localIP) {
                SetReg $iface.PSPath "TcpAckFrequency" 1 | Out-Null
                SetReg $iface.PSPath "TcpNoDelay"      1 | Out-Null
                Log "  OK Nagle OFF en interfaz activa"
                $found = $true
                break
            }
        }
        if (-not $found) {
            foreach ($iface in $ifaces) {
                SetReg $iface.PSPath "TcpAckFrequency" 1 | Out-Null
                SetReg $iface.PSPath "TcpNoDelay"      1 | Out-Null
            }
            Log "  OK Nagle OFF en todas las interfaces"
        }
    } catch { Log "  ERR Network: $_" }

    SetReg "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" "DefaultTTL" 64 | Out-Null
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" "MaxCacheEntryTtlLimit"    86400 | Out-Null
    SetReg "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" "MaxSOACacheEntryTtlLimit"   300 | Out-Null

    ipconfig /flushdns 2>&1 | Out-Null
    Log "  OK Red Nagle OFF DNS flushed"
    EmitResult "OK Red optimizada - Nagle OFF reduce ping"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 17 - MOUSE
EmitStep "Mouse aceleracion OFF"
EmitProgress 75
if (Yes $MouseInput) {
    Log "Mouse..."
    SetReg "HKCU:\Control Panel\Mouse" "MouseSpeed"      "0"  "String" | Out-Null
    SetReg "HKCU:\Control Panel\Mouse" "MouseThreshold1" "0"  "String" | Out-Null
    SetReg "HKCU:\Control Panel\Mouse" "MouseThreshold2" "0"  "String" | Out-Null
    SetReg "HKCU:\Control Panel\Mouse" "SmoothMouseXCurve" ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00,0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00,0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00,0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00)) "Binary" | Out-Null
    SetReg "HKCU:\Control Panel\Mouse" "SmoothMouseYCurve" ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x38,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xA8,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xE0,0x00,0x00,0x00,0x00,0x00)) "Binary" | Out-Null
    Log "  OK Mouse aceleracion OFF curvas raw input aplicadas"
    EmitResult "OK Mouse aceleracion OFF - aim mas preciso"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 18 - DISCO
EmitStep "Disco TRIM y optimizacion"
EmitProgress 80
if (Yes $Disk) {
    Log "Disco..."
    try {
        $diskType = (Get-PhysicalDisk | Where-Object { $_.BusType -ne "USB" } | Select-Object -First 1).MediaType
        Log "  Tipo de disco: $diskType"

        if ($diskType -eq "SSD") {
            fsutil behavior set DisableDeleteNotify 0 2>&1 | Out-Null
            Log "  OK TRIM habilitado en SSD"
        } else {
            Log "  HDD detectado - omitiendo desfrag en background"
        }
    } catch { Log "  WARN Disk detect: $_" }

    fsutil behavior set Disable8dot3 1 2>&1 | Out-Null
    fsutil behavior set DisableLastAccess 1 2>&1 | Out-Null

    Log "  OK Disco optimizado"
    EmitResult "OK Disco optimizado - TRIM ON timestamps OFF"
    $applied++
} else { $skipped++ }

Start-Sleep -Milliseconds 300

# BLOQUE 19 - FORTNITE INI
EmitStep "Fortnite GameUserSettings.ini"
EmitProgress 88
if (Yes $Fortnite) {
    $iniPath = "$env:LOCALAPPDATA\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"

    # Si Fortnite esta abierto, al cerrarse sobrescribe el .ini y se pierde todo
    $fnRunning = Get-Process -Name "FortniteClient-Win64-Shipping","FortniteLauncher" -ErrorAction SilentlyContinue
    if ($fnRunning) {
        Log "  WARN Fortnite abierto, no se toca el .ini"
        EmitResult "WARN Cierra Fortnite y vuelve a aplicar para los graficos"
        $skipped++
    }
    elseif (Test-Path $iniPath) {
        try {
            # Backup pristino (lo usa Restaurar). Solo la primera vez.
            $bak = "$iniPath.bak"
            if (-not (Test-Path $bak)) { Copy-Item $iniPath $bak -Force }

            # Calidad grafica: todo en bajo (0), resolucion 3D en 65
            $sg = [ordered]@{
                "sg.ResolutionQuality"         = "65"
                "sg.ViewDistanceQuality"       = "0"
                "sg.AntiAliasingQuality"       = "0"
                "sg.ShadowQuality"             = "0"
                "sg.GlobalIlluminationQuality" = "0"
                "sg.ReflectionQuality"         = "0"
                "sg.PostProcessQuality"        = "0"
                "sg.TextureQuality"            = "0"
                "sg.EffectsQuality"            = "0"
                "sg.FoliageQuality"            = "0"
                "sg.ShadingQuality"            = "0"
            }
            # Ajustes generales
            $gen = [ordered]@{
                "bUseVSync"             = "False"
                "bUseDynamicResolution" = "False"
                "FrameRateLimit"        = "0.000000"
                "bMotionBlur"           = "False"
                "bShowGrass"            = "False"
            }

            function Set-IniKey([System.Collections.Generic.List[string]]$buf, [string]$section, [string]$key, [string]$val) {
                $secIdx = -1
                for ($i = 0; $i -lt $buf.Count; $i++) {
                    if ($buf[$i].Trim() -eq $section) { $secIdx = $i; break }
                }
                if ($secIdx -lt 0) {
                    $buf.Add(""); $buf.Add($section); $buf.Add("$key=$val"); return
                }
                $endIdx = $buf.Count
                for ($j = $secIdx + 1; $j -lt $buf.Count; $j++) {
                    if ($buf[$j].Trim() -match '^\[.*\]$') { $endIdx = $j; break }
                }
                for ($k = $secIdx + 1; $k -lt $endIdx; $k++) {
                    if ($buf[$k] -match "^\s*$([regex]::Escape($key))\s*=") { $buf[$k] = "$key=$val"; return }
                }
                $buf.Insert($endIdx, "$key=$val")
            }

            $lines = [System.Collections.Generic.List[string]](Get-Content $iniPath)
            foreach ($key in $sg.Keys)  { Set-IniKey $lines "[ScalabilityGroups]" $key $sg[$key] }
            foreach ($key in $gen.Keys) { Set-IniKey $lines "[/Script/FortniteGame.FortGameUserSettings]" $key $gen[$key] }
            Set-Content $iniPath $lines -Force -Encoding UTF8

            Log "  OK GameUserSettings.ini aplicado (res 65, todo en bajo)"
            EmitResult "OK Fortnite configurado - resolucion 65 y calidad baja para mas FPS"
            $applied++
        } catch { Log "  ERR Fortnite: $_"; $skipped++ }
    } else {
        Log "  WARN Fortnite no encontrado en: $iniPath"
        EmitResult "WARN Fortnite no encontrado - instalalo y abrelo una vez"
        $skipped++
    }
} else { $skipped++ }

Start-Sleep -Milliseconds 300


# REFRESCAR EXPLORADOR (para que los cambios visuales se vean sin reiniciar)
if ((Yes $Visuals) -or (Yes $Wallpaper)) {
    EmitStep "Efectos visuales aplicandose"
    EmitProgress 94
    try {
        Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 900
        if (-not (Get-Process -Name explorer -ErrorAction SilentlyContinue)) { Start-Process explorer.exe }
        Log "  OK Explorador reiniciado (cambios visibles al instante)"
        EmitResult "OK Barra de tareas, iconos y efectos aplicados al instante"
    } catch { Log "  WARN explorer: $_" }
}

# BLOQUE 20 - LIMPIEZA FINAL
EmitStep "Limpieza final del sistema"
EmitProgress 95
Log "Limpieza..."
try {
    Remove-Item "$env:TEMP\*"           -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\Windows\Temp\*"     -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\Windows\Prefetch\*" -Force         -ErrorAction SilentlyContinue
    ipconfig /flushdns 2>&1 | Out-Null

    $shaders = "$env:LOCALAPPDATA\FortniteGame\Saved\PipelineCaches"
    if (Test-Path $shaders) { Remove-Item "$shaders\*" -Recurse -Force -ErrorAction SilentlyContinue }

    Log "  OK Limpieza completada"
    EmitResult "OK Sistema limpio - temp y shader cache eliminados"
    $applied++
} catch { Log "  WARN Cleanup: $_" }

# RESUMEN FINAL
EmitProgress 100
EmitStep "Optimizacion completada"

$summary = "Aplicadas=$applied | Omitidas=$skipped | Reinicio=$rebootNeeded"
Log "=== FIN: $summary ==="
EmitResult "SUMMARY:$summary"

if ($rebootNeeded) {
    $flag = Join-Path $scriptRoot "REBOOT_REQUIRED.flag"
    "$(Get-Date)" | Out-File -Force $flag
    Write-Output "REBOOT_REQUIRED"
}

Log "=== OptimizeCompetitive v3 finalizado ==="




