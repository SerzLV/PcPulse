param([string]$OutIco, [string]$PreviewPng)
Add-Type -AssemblyName System.Drawing

function Render([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded square with the logo's cyan -> violet diagonal gradient.
    $m = [Math]::Max(0.5, $s * 0.03)
    $w = $s - 2 * $m
    $r = $w * 0.26
    $d = 2 * $r
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($m, $m, $d, $d, 180, 90)
    $path.AddArc($m + $w - $d, $m, $d, $d, 270, 90)
    $path.AddArc($m + $w - $d, $m + $w - $d, $d, $d, 0, 90)
    $path.AddArc($m, $m + $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF $m, $m, $w, $w
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, `
        ([System.Drawing.Color]::FromArgb(255, 0x4D, 0xC9, 0xFF)), `
        ([System.Drawing.Color]::FromArgb(255, 0xB4, 0x7C, 0xFF)), 45.0
    $g.FillPath($grad, $path)

    # Soft top highlight for a bit of depth on larger sizes.
    if ($s -ge 32) {
        $g.SetClip($path)
        $hl = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, `
            ([System.Drawing.Color]::FromArgb(60, 255, 255, 255)), `
            ([System.Drawing.Color]::FromArgb(0, 255, 255, 255)), 90.0
        $blend = New-Object System.Drawing.Drawing2D.Blend 3
        $blend.Factors = [single[]](0, 1, 1)
        $blend.Positions = [single[]](0, 0.55, 1)
        $hl.Blend = $blend
        $g.FillRectangle($hl, $rect)
        $g.ResetClip()
    }

    # Pulse line: same points as the in-app logo (24x24 box), scaled to fit.
    $box = $w * 0.72
    $ox = $m + ($w - $box) / 2
    $oy = $m + ($w - $box) / 2
    $k = $box / 24.0
    $pts = @(@(2,12), @(7,12), @(10,5), @(14,19), @(17,12), @(22,12)) | ForEach-Object {
        New-Object System.Drawing.PointF ($ox + $_[0] * $k), ($oy + $_[1] * $k)
    }
    $stroke = [Math]::Max(1.6, $s * 0.075)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $stroke
    $pen.LineJoin = 'Round'
    $pen.StartCap = 'Round'
    $pen.EndCap = 'Round'
    $g.DrawLines($pen, [System.Drawing.PointF[]]$pts)

    $g.Dispose()
    return $bmp
}

# 32-bit BGRA DIB as stored in .ico: header with doubled height, bottom-up pixels, then an empty AND mask.
function ToDib([System.Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([UInt32]40); $bw.Write([Int32]$s); $bw.Write([Int32]($s * 2))
    $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]0)
    $bw.Write([UInt32]0); $bw.Write([Int32]0); $bw.Write([Int32]0); $bw.Write([UInt32]0); $bw.Write([UInt32]0)
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    $maskRow = [Math]::Ceiling($s / 32) * 4
    $bw.Write((New-Object byte[] ($maskRow * $s)))
    $bw.Flush()
    return ,$ms.ToArray()
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) {
    $bmp = Render $s
    if ($s -ge 256) {
        # Only the 256px entry is PNG-compressed; small PNG entries break older GDI readers.
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        if ($PreviewPng) { $bmp.Save($PreviewPng, [System.Drawing.Imaging.ImageFormat]::Png) }
        $bytes = $ms.ToArray()
    } else {
        $bytes = ToDib $bmp
    }
    $bmp.Dispose()
    ,$bytes
}

# ICO container: directory, then the image data.
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$pngs[$i].Length); $bw.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($OutIco, $out.ToArray())
"wrote $OutIco ($($out.Length) bytes)"
