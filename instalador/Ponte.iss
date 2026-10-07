; Instalador do Ponte (Inno Setup 6). Gere com: powershell -File instalador\build.ps1
; Versao e pasta do scrcpy chegam pelo build.ps1 (/DVersao=... /DScrcpyDir=...).

#ifndef Versao
  #define Versao "0.0.0"
#endif
#ifndef ScrcpyDir
  #error Rode pelo build.ps1 (falta ScrcpyDir)
#endif

[Setup]
AppId={{6B1E0F7C-3F2A-4C8E-9D5B-2A7C1E4F9B30}
AppName=Ponte
AppVersion={#Versao}
AppPublisher=Yag0Buen0
AppPublisherURL=https://github.com/Yag0Buen0/ponte
DefaultDirName={localappdata}\Programs\Ponte
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=saida
OutputBaseFilename=Ponte-Setup-{#Versao}
UninstallDisplayIcon={app}\scrcpy\scrcpy.exe
UninstallDisplayName=Ponte (Celular no PC)
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "pt"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "..\app\Celular.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\app\Ponte.Core.psm1"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ScrcpyDir}\*"; DestDir: "{app}\scrcpy"; Excludes: "open_a_terminal_here.bat,scrcpy-noconsole.vbs"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{userprograms}\Celular"; Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\Celular.ps1"""; \
  WorkingDir: "{app}"; IconFilename: "{app}\scrcpy\scrcpy.exe"; Comment: "Abre a tela do celular no PC"

[Run]
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\Celular.ps1"""; \
  Description: "Conectar o celular agora"; Flags: postinstall nowait skipifsilent runhidden

[UninstallRun]
; Para o servidor do adb para nao travar a remocao dos arquivos
Filename: "{app}\scrcpy\adb.exe"; Parameters: "kill-server"; Flags: runhidden; RunOnceId: "PararAdb"
