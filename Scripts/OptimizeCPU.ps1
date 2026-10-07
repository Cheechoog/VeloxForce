Write-HosWrite-Host "Iniciando optimización CPU..." -ForegroundColor Cyan

# Activar plan de energía alto rendimiento
Write-Host "→ Activando plan de energía alto rendimiento..." -ForegroundColor Yellow
powercfg -setactive SCHEME_MIN
Start-Sleep -Seconds 1

# Activar plan Ultimate Performance si existe
Write-Host "→ Activando plan Ultimate Performance (si disponible)..." -ForegroundColor Yellow
powercfg -setactive SCHEME_MAX
Start-Sleep -Seconds 1

# Desactivar core parking
Write-Host "→ Desactivando core parking..." -ForegroundColor Yellow
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583" -Name "ValueMax" -Value 100
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583" -Name "ValueMin" -Value 0
Start-Sleep -Seconds 1

# Activar HAGS (Hardware Accelerated GPU Scheduling)
Write-Host "→ Activando HAGS..." -ForegroundColor Yellow
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -Name "HwSchMode" -Value 2
Start-Sleep -Seconds 1

# Optimizar red
Write-Host "→ Optimizando configuración de red..." -ForegroundColor Yellow
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" -Name "TcpNoDelay" -Value 1 -PropertyType DWord -Force
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\NetworkThrottlingIndex" -Name "NetworkThrottlingIndex" -Value 0xFFFFFFFF
Start-Sleep -Seconds 1

# Desactivar servicios innecesarios
Write-Host "→ Desactivando servicios innecesarios..." -ForegroundColor Yellow
Stop-Service -Name "SysMain" -Force
Set-Service -Name "SysMain" -StartupType Disabled
Stop-Service -Name "DiagTrack" -Force
Set-Service -Name "DiagTrack" -StartupType Disabled
Start-Sleep -Seconds 1

# Desactivar transparencias y animaciones
Write-Host "→ Desactivando transparencias y animaciones de Windows..." -ForegroundColor Yellow
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v EnableTransparency /t REG_DWORD /d 0 /f
reg add "HKCU\Control Panel\Desktop" /v Win32PrioritySeparation /t REG_DWORD /d 26 /f

Write-Host "✅ Optimización CPU completada." -ForegroundColor Green
