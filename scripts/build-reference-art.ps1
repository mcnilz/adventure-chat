# Rebuild runtime art from preserved user-supplied originals. No external dependencies.
Add-Type -AssemblyName System.Drawing
$root = Join-Path $PSScriptRoot '../worlds/reference'
$target = Join-Path $root 'assets/images'
function Points($coordinates) {
 $result = foreach ($pair in $coordinates.Split(' ')) { $xy=$pair.Split(','); [System.Drawing.Point]::new([int]$xy[0],[int]$xy[1]) }
 return ,([System.Drawing.Point[]]$result)
}
function HalfSize($source) {
 $small=[System.Drawing.Bitmap]::new($source.Width/2,$source.Height/2)
 for($y=0;$y -lt $small.Height;$y++){for($x=0;$x -lt $small.Width;$x++){$small.SetPixel($x,$y,$source.GetPixel($x*2,$y*2))}}
 return $small
}
function Table($name,$outline,$holes,$rect) {
 $source=[System.Drawing.Bitmap]::FromFile((Join-Path $root "design-guide/assets/rooms/$name.png"))
 $mask=[System.Drawing.Bitmap]::new(1280,720)
 $g=[System.Drawing.Graphics]::FromImage($mask)
 $path=[System.Drawing.Drawing2D.GraphicsPath]::new()
 foreach($polygon in $outline){$path.AddPolygon((Points $polygon))}
 $region=[System.Drawing.Region]::new($path)
 foreach($hole in $holes){$p=[System.Drawing.Drawing2D.GraphicsPath]::new();$p.AddPolygon((Points $hole));$region.Exclude($p);$p.Dispose()}
 $g.SetClip($region,[System.Drawing.Drawing2D.CombineMode]::Replace)
 $g.DrawImageUnscaled($source,0,0)
 $crop=$mask.Clone([System.Drawing.Rectangle]::new($rect[0],$rect[1],$rect[2],$rect[3]),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $small=HalfSize $crop
 $small.Save((Join-Path $target "$name-table.png"),[System.Drawing.Imaging.ImageFormat]::Png)
 $small.Dispose()
 $crop.Dispose();$g.Dispose();$mask.Dispose();$source.Dispose();$path.Dispose();$region.Dispose()
}
foreach($name in @('lounge','garden')) {
 $source=[System.Drawing.Bitmap]::FromFile((Join-Path $root "design-guide/assets/rooms/$name.png"))
 # Explicit pixel sampling keeps background and foreground in exact registration.
 $small=HalfSize $source
 $small.Save((Join-Path $target "$name-runtime.png"),[System.Drawing.Imaging.ImageFormat]::Png)
 $small.Dispose();$source.Dispose()
}
Table 'lounge' @(
 '476,514 516,478 572,478 572,482 576,476 594,474 616,478 622,488 642,488 642,456 650,456 650,458 668,456 672,462 670,484 700,484 700,478 722,474 768,478 784,478 790,490 808,480 842,512 842,532 824,538 824,550 818,550 818,578 826,586 826,610 774,610 774,594 542,594 542,610 492,610 492,590 498,580 498,552 494,552 494,538 476,534'
 ) @('544,540 754,540 766,554 760,574 544,574') @(476,448,368,164)
Table 'garden' @(
 '506,530 542,506 562,506 562,496 572,494 596,494 598,510 608,510 602,500 596,486 596,476 604,470 618,470 626,476 638,476 642,484 634,492 632,508 654,508 654,478 660,466 664,462 666,444 674,434 682,440 684,456 690,464 698,478 696,508 716,508 716,486 710,478 718,472 714,464 724,460 724,450 736,454 740,468 742,478 738,484 738,510 780,510 812,528 812,546 788,548 790,616 766,618 764,596 558,596 556,618 530,616 530,548 506,546'
 ) @('560,558 580,558 566,576 560,576','582,560 736,560 754,578 568,578','742,560 762,560 762,574') @(504,432,310,186)
foreach($name in @('mara','bruno')) {
 $source=[System.Drawing.Bitmap]::FromFile((Join-Path $root "design-guide/assets/characters/$name.png"))
 $sheet=[System.Drawing.Bitmap]::new(256,288)
 for($row=0;$row -lt 3;$row++){for($col=0;$col -lt 4;$col++){
   $xmin=64;$xmax=0;$ymin=96;$ymax=0
   for($y=0;$y -lt 96;$y++){for($x=0;$x -lt 64;$x++){if($source.GetPixel($col*64+$x,$row*96+$y).A -gt 127){$xmin=[Math]::Min($xmin,$x);$xmax=[Math]::Max($xmax,$x);$ymin=[Math]::Min($ymin,$y);$ymax=[Math]::Max($ymax,$y)}}}
   # Normalize visible body height and anchor, including Mara's oversized rear idle pose.
   $scale=72.0/($ymax-$ymin+1);$center=($xmin+$xmax)/2.0
   for($ny=24;$ny -lt 96;$ny++){for($nx=0;$nx -lt 64;$nx++){
     $x=[int][Math]::Floor(($nx-32)/$scale+$center+0.5);$y=$ymin+[int][Math]::Floor(($ny-24)/$scale)
     if($x -ge 0 -and $x -lt 64 -and $y -le $ymax){$sheet.SetPixel($col*64+$nx,$row*96+$ny,$source.GetPixel($col*64+$x,$row*96+$y))}
   }}
 }}
 $sheet.Save((Join-Path $target "$name.png"),[System.Drawing.Imaging.ImageFormat]::Png)
 $sheet.Dispose();$source.Dispose()
}
