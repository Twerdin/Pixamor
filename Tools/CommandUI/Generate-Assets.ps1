# Harita Natural Earth verisinden, arayüz ikonları özgün vektör çizimlerinden üretilir.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$artRoot = Join-Path $projectRoot 'Assets/Resources/CommandUI/Art'
New-Item -ItemType Directory -Force -Path $artRoot | Out-Null
function Color([int]$r,[int]$g,[int]$b,[int]$a=255) { [Drawing.Color]::FromArgb($a,$r,$g,$b) }
function Project([double]$lon,[double]$lat) {
    $limited = [Math]::Min([double]80,[Math]::Max([double]-60,$lat))
    $upper = [Math]::Log([Math]::Tan([Math]::PI/4 + 80*[Math]::PI/360))
    $lower = [Math]::Log([Math]::Tan([Math]::PI/4 - 60*[Math]::PI/360))
    $mercator = [Math]::Log([Math]::Tan([Math]::PI/4 + $limited*[Math]::PI/360))
    [Drawing.PointF]::new(($lon+180)/360*2048,($upper-$mercator)/($upper-$lower)*1152)
}
$bitmap = [Drawing.Bitmap]::new(2048,1152)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = 'AntiAlias'
$graphics.Clear((Color 12 25 36))
$land = [Drawing.SolidBrush]::new((Color 35 57 68))
$border = [Drawing.Pen]::new((Color 65 91 103),1.1)
$grid = [Drawing.Pen]::new((Color 37 61 75 140),1)
for($lon=-180;$lon -le 180;$lon+=30) {
    $p=Project $lon 0
    $graphics.DrawLine($grid,$p.X,0,$p.X,1152)
}
for($lat=-60;$lat -le 80;$lat+=20) {
    $p=Project 0 $lat
    $graphics.DrawLine($grid,0,$p.Y,2048,$p.Y)
}
$geo = Get-Content (Join-Path $PSScriptRoot 'countries.geojson') -Raw | ConvertFrom-Json -Depth 100
foreach($feature in $geo.features) {
    if($feature.properties.ADMIN -eq 'Antarctica') { continue }
    # Atama doğrudan yapılır; pipeline tek poligonun dış halkasını açmamalı.
    if($feature.geometry.type -eq 'Polygon') { $polygons = ,$feature.geometry.coordinates }
    else { $polygons = $feature.geometry.coordinates }
    foreach($polygon in $polygons) {
        $path=[Drawing.Drawing2D.GraphicsPath]::new()
        foreach($ring in $polygon) {
            $points=[Collections.Generic.List[Drawing.PointF]]::new()
            foreach($point in $ring) { $points.Add((Project $point[0] $point[1])) }
            if($points.Count -ge 3) { $path.AddPolygon($points.ToArray()) }
        }
        $graphics.FillPath($land,$path)
        $graphics.DrawPath($border,$path)
        $path.Dispose()
    }
}
# İnce nokta dokusu, büyük haritanın düz renk alanlarını yumuşatır.
$dot=[Drawing.SolidBrush]::new((Color 114 153 165 35))
for($x=12;$x -lt 2048;$x+=24) { for($y=12;$y -lt 1152;$y+=24) { $graphics.FillEllipse($dot,$x,$y,1.3,1.3) } }
$bitmap.Save((Join-Path $artRoot 'WorldMap.png'),[Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose();$bitmap.Dispose();$land.Dispose();$border.Dispose();$grid.Dispose();$dot.Dispose()

function Icon([string]$name,[scriptblock]$draw) {
    $image=[Drawing.Bitmap]::new(128,128)
    $g=[Drawing.Graphics]::FromImage($image)
    $g.SmoothingMode='AntiAlias'
    $g.Clear([Drawing.Color]::Transparent)
    $pen=[Drawing.Pen]::new([Drawing.Color]::White,6)
    $pen.StartCap='Round';$pen.EndCap='Round';$pen.LineJoin='Round'
    & $draw $g $pen
    $image.Save((Join-Path $artRoot "$name.png"),[Drawing.Imaging.ImageFormat]::Png)
    $pen.Dispose();$g.Dispose();$image.Dispose()
}
Icon 'Flood' { param($g,$p)
    foreach($y in @(43,65,87)) {
        $path=[Drawing.Drawing2D.GraphicsPath]::new()
        $path.AddBezier(17,$y,32,($y-16),46,($y+16),62,$y)
        $path.AddBezier(62,$y,78,($y-16),94,($y+16),111,$y)
        $g.DrawPath($p,$path);$path.Dispose()
    }
}
Icon 'Drought' { param($g,$p)
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddBezier(64,15,56,37,28,58,28,79)
    $path.AddBezier(28,79,28,119,101,119,100,79)
    $path.AddBezier(100,79,100,58,73,37,64,15)
    $g.DrawPath($p,$path);$path.Dispose()
    $g.DrawLines($p,[Drawing.PointF[]]@([Drawing.PointF]::new(67,48),[Drawing.PointF]::new(55,71),[Drawing.PointF]::new(71,76),[Drawing.PointF]::new(60,99)))
}
Icon 'Wildfire' { param($g,$p)
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddBezier(66,12,74,41,37,50,43,75)
    $path.AddBezier(43,75,31,73,29,58,30,53)
    $path.AddBezier(30,53,2,94,37,117,66,115)
    $path.AddBezier(66,115,115,113,115,65,91,47)
    $path.AddBezier(91,47,93,69,79,68,78,58)
    $path.AddBezier(78,58,80,37,76,23,66,12)
    $g.DrawPath($p,$path);$path.Dispose()
}
Icon 'Heatwave' { param($g,$p)
    $g.DrawEllipse($p,40,40,48,48)
    for($a=0;$a -lt 360;$a+=45) {
        $r=$a*[Math]::PI/180
        $g.DrawLine($p,[single](64+38*[Math]::Cos($r)),[single](64+38*[Math]::Sin($r)),[single](64+51*[Math]::Cos($r)),[single](64+51*[Math]::Sin($r)))
    }
}
Icon 'People' { param($g,$p)
    $g.DrawEllipse($p,48,20,32,32);$g.DrawArc($p,32,64,64,64,180,180)
    $g.DrawArc($p,10,34,24,24,70,280);$g.DrawArc($p,94,34,24,24,-170,280)
    $g.DrawArc($p,4,69,40,40,180,90);$g.DrawArc($p,84,69,40,40,-90,90)
}
Icon 'Climate' { param($g,$p)
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddBezier(25,94,8,24,61,24,108,17)
    $path.AddBezier(108,17,113,73,90,119,25,94)
    $g.DrawPath($p,$path);$path.Dispose()
    $g.DrawLine($p,20,108,89,43);$g.DrawLine($p,59,73,59,47);$g.DrawLine($p,59,73,87,73)
}
Icon 'Budget' { param($g,$p)
    $g.DrawRectangle($p,20,37,89,65);$g.DrawLine($p,20,37,88,19);$g.DrawLine($p,88,19,88,37)
    $g.DrawRectangle($p,78,59,31,23);$g.DrawEllipse($p,88,68,3,3)
}
Icon 'Globe' { param($g,$p)
    $g.DrawEllipse($p,17,17,94,94);$g.DrawEllipse($p,42,17,44,94)
    $g.DrawLine($p,18,64,110,64);$g.DrawArc($p,19,27,90,20,0,180);$g.DrawArc($p,19,81,90,20,180,180)
}
Icon 'Clock' { param($g,$p)
    $g.DrawEllipse($p,20,20,88,88);$g.DrawLine($p,64,36,64,65);$g.DrawLine($p,64,65,84,78)
}
Write-Output 'CommandUI map and 9 original icons generated.'
