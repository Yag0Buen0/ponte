# Cria atalhos no menu Iniciar (pasta "Ponte") para cada modo do conectar-celular.
# Para fixar: tecla Windows -> digitar "Celular" -> botao direito -> Fixar em Iniciar.
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'conectar-celular.ps1'
$pasta = Join-Path ([Environment]::GetFolderPath('Programs')) 'Ponte'
New-Item -ItemType Directory -Force $pasta | Out-Null
Get-ChildItem $pasta -Filter *.lnk | Remove-Item   # remove atalhos de modos antigos

# Icone do scrcpy (o comando no PATH e um link; o icone esta no exe real)
$scrcpy = Get-Item (Get-Command scrcpy).Source
$icone = if ($scrcpy.Target) { @($scrcpy.Target)[0] } else { $scrcpy.FullName }

$atalhos = [ordered]@{
    'Celular'             = 'normal'
    'Celular PiP'         = 'pip'
    'Celular Livre'       = 'livre'
    'Celular PiP Livre'   = 'pip-livre'
}

$shell = New-Object -ComObject WScript.Shell
foreach ($nome in $atalhos.Keys) {
    $lnk = $shell.CreateShortcut((Join-Path $pasta "$nome.lnk"))
    $lnk.TargetPath = 'powershell.exe'
    $lnk.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`" -Modo $($atalhos[$nome])"
    $lnk.WorkingDirectory = $PSScriptRoot
    $lnk.IconLocation = "$icone,0"
    $lnk.WindowStyle = 7   # minimizado (o -WindowStyle Hidden esconde de vez)
    $lnk.Description = "Abre o celular no modo $($atalhos[$nome])"
    $lnk.Save()
    Write-Host "Criado: $nome"
}
Write-Host "Atalhos em: $pasta" -ForegroundColor Green
