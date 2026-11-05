param([Parameter(Mandatory=$true)][string]$ReliefPath)
# Natural Earth I rasteri: https://naciscdn.org/naturalearth/50m/raster/NE1_50M_SR.zip
# Orijinal büyük TIFF sürüme dahil edilmez; oyuncuya yalnızca işlenmiş PNG verilir.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$target=Join-Path $projectRoot 'Assets/Resources/CommandUI/Art/WorldMap.png'
$width=4096; $height=2304
$upper=[Math]::Log([Math]::Tan([Math]::PI/4+80*[Math]::PI/360))
$lower=[Math]::Log([Math]::Tan([Math]::PI/4-60*[Math]::PI/360))
function Project([double]$lon,[double]$lat) {
    # Açık double sınırları: PowerShell'in int aşırı yükünü seçip enlemi yuvarlamasını önler.
    $latitude=[Math]::Min([double]80,[Math]::Max([double]-60,$lat))
    $merc=[Math]::Log([Math]::Tan([Math]::PI/4+$latitude*[Math]::PI/360))
    [Drawing.PointF]::new(($lon+180)/360*$width,($upper-$merc)/($upper-$lower)*$height)
}
$image=[Drawing.Bitmap]::new($width,$height)
$g=[Drawing.Graphics]::FromImage($image)
$g.SmoothingMode='AntiAlias'
$g.InterpolationMode='HighQualityBilinear'
$g.Clear([Drawing.Color]::FromArgb(9,23,36))
$allLand=[Drawing.Drawing2D.GraphicsPath]::new()
$paths=[Collections.Generic.List[Drawing.Drawing2D.GraphicsPath]]::new()
$geo=Get-Content (Join-Path $PSScriptRoot 'countries.geojson') -Raw | ConvertFrom-Json -Depth 100
foreach($feature in $geo.features) {
    if($feature.properties.ADMIN -eq 'Antarctica') {continue}
    if($feature.geometry.type -eq 'Polygon') {$polygons=,$feature.geometry.coordinates}
    else {$polygons=$feature.geometry.coordinates}
    foreach($polygon in $polygons) {
        $path=[Drawing.Drawing2D.GraphicsPath]::new()
        foreach($ring in $polygon) {
            $points=[Collections.Generic.List[Drawing.PointF]]::new()
            foreach($point in $ring) {$points.Add((Project $point[0] $point[1]))}
            if($points.Count -ge 3) {$path.AddPolygon($points.ToArray())}
        }
        $paths.Add($path)
        $allLand.AddPath($path,$false)
    }
}
$raster=[Drawing.Image]::FromFile((Resolve-Path -LiteralPath $ReliefPath))
$attributes=[Drawing.Imaging.ImageAttributes]::new()
$matrix=[Drawing.Imaging.ColorMatrix]::new()
# Renkli araziyi koyu mavi-gri kabartma görünümüne dönüştür.
$matrix.Matrix00=.24*.299; $matrix.Matrix10=.24*.587; $matrix.Matrix20=.24*.114
$matrix.Matrix01=.32*.299; $matrix.Matrix11=.32*.587; $matrix.Matrix21=.32*.114
$matrix.Matrix02=.36*.299; $matrix.Matrix12=.36*.587; $matrix.Matrix22=.36*.114
$matrix.Matrix40=.025; $matrix.Matrix41=.055; $matrix.Matrix42=.075
$attributes.SetColorMatrix($matrix)
$g.SetClip($allLand)
for($y=0;$y -lt $height;$y++) {
    $merc=$upper-($y+.5)/$height*($upper-$lower)
    $lat=(2*[Math]::Atan([Math]::Exp($merc))-[Math]::PI/2)*180/[Math]::PI
    $sourceY=(90-$lat)/180*$raster.Height
    $dest=[Drawing.Rectangle]::new(0,$y,$width,1)
    $g.DrawImage($raster,$dest,[single]0,[single]$sourceY,[single]$raster.Width,[single]1,[Drawing.GraphicsUnit]::Pixel,$attributes)
}
$g.ResetClip()
$border=[Drawing.Pen]::new([Drawing.Color]::FromArgb(145,92,130,148),1.2)
foreach($path in $paths) {$g.DrawPath($border,$path);$path.Dispose()}
$grid=[Drawing.Pen]::new([Drawing.Color]::FromArgb(38,105,150,170),1)
for($lon=-180;$lon -le 180;$lon+=15) {$p=Project $lon 0;$g.DrawLine($grid,$p.X,0,$p.X,$height)}
for($lat=-60;$lat -le 80;$lat+=10) {$p=Project 0 $lat;$g.DrawLine($grid,0,$p.Y,$width,$p.Y)}
$image.Save($target,[Drawing.Imaging.ImageFormat]::Png)
$grid.Dispose();$border.Dispose();$allLand.Dispose();$attributes.Dispose();$raster.Dispose();$g.Dispose();$image.Dispose()
Write-Output '4096 x 2304 shaded-relief map generated.'
