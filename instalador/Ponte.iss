; Instalador do Celular Remoto (Inno Setup 6). Gere com: powershell -File instalador\build.ps1
; Versao e pasta do scrcpy chegam pelo build.ps1 (/DVersao=... /DScrcpyDir=...).

#ifndef Versao
  #define Versao "0.0.0"
#endif
#ifndef ScrcpyDir
  #error Rode pelo build.ps1 (falta ScrcpyDir)
#endif

[Setup]
; Mesmo AppId da v0.1 ("Ponte"): instala por cima e atualiza
AppId={{6B1E0F7C-3F2A-4C8E-9D5B-2A7C1E4F9B30}
AppName=Celular Remoto
AppVersion={#Versao}
AppPublisher=Yag0Buen0
AppPublisherURL=https://github.com/Yag0Buen0/ponte
DefaultDirName={localappdata}\Programs\Ponte
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=saida
OutputBaseFilename=CelularRemoto-Setup-{#Versao}
#if FileExists(AddBackslash(SourcePath) + "..\app\icone.ico")
SetupIconFile=..\app\icone.ico
#endif
UninstallDisplayIcon={app}\CelularRemoto.exe
UninstallDisplayName=Celular Remoto
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "pt"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "areadetrabalho"; Description: "Criar atalho na Área de Trabalho"

[InstallDelete]
; Restos da v0.1 (launcher em PowerShell e atalho "Celular")
Type: files; Name: "{app}\Celular.ps1"
Type: files; Name: "{app}\Ponte.Core.psm1"
Type: files; Name: "{userprograms}\Celular.lnk"

[Files]
Source: "..\app\bin\CelularRemoto.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ScrcpyDir}\*"; DestDir: "{app}\scrcpy"; Excludes: "open_a_terminal_here.bat,scrcpy-noconsole.vbs"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{userprograms}\Celular Remoto"; Filename: "{app}\CelularRemoto.exe"; Comment: "Veja e controle o celular no PC"
Name: "{userdesktop}\Celular Remoto"; Filename: "{app}\CelularRemoto.exe"; Comment: "Veja e controle o celular no PC"; Tasks: areadetrabalho

[Run]
Filename: "{app}\CelularRemoto.exe"; Description: "Abrir o Celular Remoto"; Flags: postinstall nowait skipifsilent

[Code]
// O servidor do adb (de uma versao ja instalada) trava o adb.exe e impede a atualizacao
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Codigo: Integer;
  Adb: String;
begin
  Adb := ExpandConstant('{app}\scrcpy\adb.exe');
  if FileExists(Adb) then
    Exec(Adb, 'kill-server', '', SW_HIDE, ewWaitUntilTerminated, Codigo);
  Result := '';
end;

[UninstallRun]
; Para o servidor do adb para nao travar a remocao dos arquivos
Filename: "{app}\scrcpy\adb.exe"; Parameters: "kill-server"; Flags: runhidden; RunOnceId: "PararAdb"
