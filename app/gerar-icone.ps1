# Gera app\icone.ico a partir de uma imagem (PNG/JPG) com fundo branco:
# fundo branco vira transparente, corta a margem, centraliza num quadrado e grava
# um .ico com PNGs de 16 a 256 px.   Uso: powershell -File app\gerar-icone.ps1 -Origem logo.jpg
param([Parameter(Mandatory)][string]$Origem, [int]$LimiarBranco = 235)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$img = New-Object System.Drawing.Bitmap (Resolve-Path $Origem).Path
$w = $img.Width; $h = $img.Height
$rgba = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$minX = $w; $minY = $h; $maxX = -1; $maxY = -1

# Fundo = regiao clara ligada a borda da imagem (o branco de dentro do desenho fica)
$claros = New-Object 'byte[,]' $w, $h
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $p = $img.GetPixel($x, $y)
        $claros[$x, $y] = [Math]::Min([Math]::Min($p.R, $p.G), $p.B)
    }
}
$fundo = New-Object 'bool[,]' $w, $h
$fila = New-Object 'System.Collections.Generic.Queue[int]'
for ($x = 0; $x -lt $w; $x++) { $fila.Enqueue($x); $fila.Enqueue(($h - 1) * $w + $x) }
for ($y = 0; $y -lt $h; $y++) { $fila.Enqueue($y * $w); $fila.Enqueue($y * $w + $w - 1) }
while ($fila.Count) {
    $i = $fila.Dequeue(); $x = $i % $w; $y = [Math]::Floor($i / $w)
    if ($fundo[$x, $y] -or $claros[$x, $y] -lt 200) { continue }
    $fundo[$x, $y] = $true
    if ($x -gt 0) { $fila.Enqueue($i - 1) }; if ($x -lt $w - 1) { $fila.Enqueue($i + 1) }
    if ($y -gt 0) { $fila.Enqueue($i - $w) }; if ($y -lt $h - 1) { $fila.Enqueue($i + $w) }
}

# Fundo -> transparente, com borda suave (anti-serrilhado do JPG)
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $p = $img.GetPixel($x, $y)
        $claro = $claros[$x, $y]
        if (-not $fundo[$x, $y]) { $alfa = 255 }
        elseif ($claro -ge $LimiarBranco) { $alfa = 0 }
        else { $alfa = [int](255 * ($LimiarBranco - $claro) / ($LimiarBranco - 200)) }
        $rgba.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alfa, $p.R, $p.G, $p.B))
        if ($alfa -gt 0) {
            if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
$img.Dispose()
if ($maxX -lt 0) { throw 'A imagem parece toda branca' }

$larg = $maxX - $minX + 1; $alt = $maxY - $minY + 1
$lado = [Math]::Max($larg, $alt)

$saida = Join-Path $PSScriptRoot 'icone.ico'
$tamanhos = 16, 24, 32, 48, 64, 128, 256
$pngs = foreach ($t in $tamanhos) {
    $bmp = New-Object System.Drawing.Bitmap $t, $t, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $escala = $t / $lado
    $dl = $larg * $escala; $da = $alt * $escala
    $destino = New-Object System.Drawing.RectangleF (($t - $dl) / 2), (($t - $da) / 2), $dl, $da
    $g.DrawImage($rgba, $destino, (New-Object System.Drawing.RectangleF $minX, $minY, $larg, $alt), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($t -eq 256) { $bmp.Save((Join-Path $PSScriptRoot 'bin\icone-256.png')) }
    $bmp.Dispose()
    , $ms.ToArray()
}
$rgba.Dispose()

# Formato ICO: cabecalho + diretorio (16 bytes por imagem) + PNGs
$f = [System.IO.File]::Create($saida)
$bw = New-Object System.IO.BinaryWriter $f
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$tamanhos.Count)
$offset = 6 + 16 * $tamanhos.Count
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
    $t = $tamanhos[$i]; $dados = $pngs[$i]
    $bw.Write([byte]($t % 256)); $bw.Write([byte]($t % 256))   # 256 vira 0 no formato ICO
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$dados.Length); $bw.Write([uint32]$offset)
    $offset += $dados.Length
}
foreach ($dados in $pngs) { $bw.Write($dados) }
$bw.Close()
Write-Host "Pronto: $saida" -ForegroundColor Green
