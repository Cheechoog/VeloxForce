# ============================================================
#  VeloxForge - OptimizeStreaming
#  Balance calidad/FPS para transmitir: buena imagen en el
#  stream, juego fluido. Toque ligero (no cierra OBS ni apps).
# ============================================================
param(
    [string]$Fortnite = "yes",
    [string]$Services = "yes"
)

$logFile = "$env:TEMP\VeloxForge_Streaming.log"
"" | Out-File $logFile
function Log($m)                { "$((Get-Date).ToString('s')) | $m" | Out-File -Append $logFile; Write-Host $m }
function EmitProgress([int]$p)  { Write-Output "PROGRESS:$p" }
function EmitStep([string]$t)   { Write-Output "STEP:$t" }
function EmitResult([string]$t) { Write-Output "RESULT:$t" }
function Yes($p) { return ($p -eq "yes") }

Log "=== VeloxForge OptimizeStreaming ==="
EmitProgress 0
EmitStep "Preparando optimizacion de streaming"
$applied = 0

# ── Servicios: solo telemetria (toque ligero) ──
EmitStep "Servicios (telemetria)"
EmitProgress 30
if (Yes $Services) {
    try {
        Stop-Service -Name "DiagTrack" -Force -ErrorAction SilentlyContinue
        Set-Service  -Name "DiagTrack" -StartupType Disabled -ErrorAction SilentlyContinue
        EmitResult "OK Telemetria desactivada"
        $applied++
    } catch { Log "  ERR servicios: $_" }
}

# ── Fortnite: calidad media con buena imagen ──
EmitStep "Fortnite GameUserSettings.ini"
EmitProgress 60
if (Yes $Fortnite) {
    $iniPath = "$env:LOCALAPPDATA\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"
    $fnRunning = Get-Process -Name "FortniteClient-Win64-Shipping","FortniteLauncher" -ErrorAction SilentlyContinue
    if ($fnRunning) {
        EmitResult "WARN Cierra Fortnite y vuelve a aplicar para los graficos"
    }
    elseif (Test-Path $iniPath) {
        try {
            $bak = "$iniPath.bak"
            if (-not (Test-Path $bak)) { Copy-Item $iniPath $bak -Force }

            # Calidad media: se ve bien en el stream y mantiene FPS
            $sg = [ordered]@{
                "sg.ResolutionQuality"         = "100"
                "sg.ViewDistanceQuality"       = "2"
                "sg.AntiAliasingQuality"       = "2"
                "sg.ShadowQuality"             = "1"
                "sg.GlobalIlluminationQuality" = "1"
                "sg.ReflectionQuality"         = "1"
                "sg.PostProcessQuality"        = "2"
                "sg.TextureQuality"            = "2"
                "sg.EffectsQuality"            = "2"
                "sg.FoliageQuality"            = "2"
                "sg.ShadingQuality"            = "2"
            }
            $gen = [ordered]@{
                "bUseVSync"             = "False"
                "bUseDynamicResolution" = "False"
                "FrameRateLimit"        = "0.000000"
                "bMotionBlur"           = "False"
                "bShowGrass"            = "True"
            }

            function Set-IniKey([System.Collections.Generic.List[string]]$buf, [string]$section, [string]$key, [string]$val) {
                $secIdx = -1
                for ($i = 0; $i -lt $buf.Count; $i++) {
                    if ($buf[$i].Trim() -eq $section) { $secIdx = $i; break }
                }
                if ($secIdx -lt 0) { $buf.Add(""); $buf.Add($section); $buf.Add("$key=$val"); return }
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

            EmitResult "OK Fortnite en calidad media - buena imagen y FPS estables"
            $applied++
        } catch { Log "  ERR ini: $_" }
    } else {
        EmitResult "WARN Fortnite no encontrado - instalalo y abrelo una vez"
    }
}

EmitProgress 100
EmitStep "Optimizacion completada"
Log "=== FIN Streaming | Aplicadas=$applied ==="
EmitResult "SUMMARY:Aplicadas=$applied"
