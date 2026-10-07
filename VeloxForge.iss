; ============================================================
;  VeloxForge - Instalador (Inno Setup 6)
;  Empaqueta la publicacion self-contained: no requiere .NET
;  instalado en el PC del cliente.
;  Colocar este archivo en la carpeta del PROYECTO (junto a
;  FortniteBoost.csproj) y compilarlo con ISCC.exe.
; ============================================================

#define MyAppName    "Velox Force"
#define MyAppVersion "1.0.0"
#define MyAppExe     "VeloxForce.exe"
#define MyPublish    "bin\Release\net10.0-windows\win-x64\publish"

[Setup]
AppId={{9F3C7A10-4B2D-4E8A-9C11-VELOXFORGE01}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Nelson Gil
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=VeloxForceSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Empaqueta TODO lo que haya en la carpeta publish (exe, dlls, Scripts, etc.)
Source: "{#MyPublish}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";              Filename: "{app}\{#MyAppExe}"
Name: "{group}\Desinstalar {#MyAppName}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";        Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent runascurrentuser
