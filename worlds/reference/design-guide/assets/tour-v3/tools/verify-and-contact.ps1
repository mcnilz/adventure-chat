$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
public sealed class CellMeasure {
 public int Row,Col,Left=128,Top=128,Right=-1,Bottom=-1,Width,Height,Opaque,Transparent,Partial,FootLeft=128,FootRight=-1;
 public string PixelHash;public int ComponentCount;public int[] ComponentPixels;public double SilhouetteCenterX;public bool SideMarginsClear=true,TopReserveClear=true,Valid;
}
public static class Ticket19Check {
 public static string FileHash(string file){using(var hash=SHA256.Create())using(var stream=System.IO.File.OpenRead(file)){return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}}
 public static CellMeasure Cell(Bitmap image,int row,int col){
  var m=new CellMeasure{Row=row,Col=col};var bytes=new byte[128*128*4];
  for(int y=0;y<128;y++)for(int x=0;x<128;x++){
   Color c=image.GetPixel(col*128+x,row*128+y);int i=(y*128+x)*4;bytes[i]=c.R;bytes[i+1]=c.G;bytes[i+2]=c.B;bytes[i+3]=c.A;
   if(c.A==0)m.Transparent++;else if(c.A==255)m.Opaque++;else m.Partial++;
   if(c.A>0){m.Left=Math.Min(m.Left,x);m.Right=Math.Max(m.Right,x);m.Top=Math.Min(m.Top,y);m.Bottom=Math.Max(m.Bottom,y);if(x==0||x==127)m.SideMarginsClear=false;if(y<8)m.TopReserveClear=false;if(y==127){m.FootLeft=Math.Min(m.FootLeft,x);m.FootRight=Math.Max(m.FootRight,x);}}
  }
  m.Width=m.Right-m.Left+1;m.Height=m.Bottom-m.Top+1;
  m.SilhouetteCenterX=(m.Left+m.Right)/2.0;
  var seen=new bool[128*128];var queue=new int[128*128];var parts=new System.Collections.Generic.List<int>();
  for(int start=0;start<seen.Length;start++)if(bytes[start*4+3]>0&&!seen[start]){
   int head=0,tail=0;queue[tail++]=start;seen[start]=true;
   while(head<tail){int i=queue[head++],x=i%128,y=i/128;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){if(dx==0&&dy==0)continue;int nx=x+dx,ny=y+dy;if(nx<0||nx>=128||ny<0||ny>=128)continue;int ni=ny*128+nx;if(!seen[ni]&&bytes[ni*4+3]>0){seen[ni]=true;queue[tail++]=ni;}}}parts.Add(tail);
  }
  parts.Sort();parts.Reverse();m.ComponentPixels=parts.ToArray();m.ComponentCount=parts.Count;
  using(var hash=SHA256.Create()){m.PixelHash=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  m.Valid=m.Opaque>0&&m.Partial==0&&m.Height==120&&m.Top==8&&m.Bottom==127&&m.TopReserveClear&&m.SideMarginsClear&&m.ComponentCount==1&&Math.Abs(m.SilhouetteCenterX-64)<=0.5;
  return m;
 }
 public static void Contact(Bitmap sheet,string name,string target){
  using(var output=new Bitmap(1152,1080,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(output))using(var font=new Font("Segoe UI",14))using(var title=new Font("Georgia",22))using(var white=new SolidBrush(Color.FromArgb(238,242,243)))using(var cyan=new Pen(Color.FromArgb(95,223,202),1))using(var amber=new Pen(Color.FromArgb(255,199,101),2))using(var grid=new Pen(Color.FromArgb(105,128,137),1)){
   g.Clear(Color.FromArgb(37,61,75));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.SmoothingMode=SmoothingMode.None;
   g.DrawString(name+" \u00b7 QA-Kontaktbogen",title,white,16,12);
   g.DrawString("Gr\u00fcn: obere Reserve y8 \u00b7 Amber: Fu\u00dfanker (64,128) / Unterkante127 \u00b7 Markierungen nur hier",font,white,16,52);
   string[] dirs={"Rechts","Hinten / aufw\u00e4rts","Vorne / abw\u00e4rts"};string[] phases={"0 \u00b7 Kontakt / Stand","1 \u00b7 Durchgang","2 \u00b7 Gegenkontakt","3 \u00b7 Zweiter Durchgang"};
   for(int row=0;row<3;row++)for(int col=0;col<4;col++){
    int x=16+col*288,y=136+row*320;if(col==0)g.DrawString(dirs[row],font,white,16,y-52);g.DrawString(phases[col],font,white,x,y-28);
    g.DrawImage(sheet,new Rectangle(x,y,256,256),new Rectangle(col*128,row*128,128,128),GraphicsUnit.Pixel);
    g.DrawRectangle(grid,x,y,256,256);g.DrawLine(cyan,x,y+16,x+256,y+16);g.DrawLine(amber,x,y+254,x+256,y+254);g.DrawLine(cyan,x+128,y,x+128,y+256);
    g.DrawLine(amber,x+120,y+264,x+128,y+256);g.DrawLine(amber,x+136,y+264,x+128,y+256);
   }output.Save(target,ImageFormat.Png);
  }
 }
}
'@
$root=Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path (Join-Path $root 'qa') -Force | Out-Null
$checks=@()
foreach($id in @('mara','bruno')){
 $sheetPath=Join-Path $root ('characters/'+$id+'-top-down.png')
 $sheet=[System.Drawing.Bitmap]::new($sheetPath)
 try{
  $cells=@();for($row=0;$row -lt 3;$row++){for($col=0;$col -lt 4;$col++){$cells += [Ticket19Check]::Cell($sheet,$row,$col)}}
  $unique=@($cells.PixelHash | Select-Object -Unique).Count
  $passed=($sheet.Width -eq 512 -and $sheet.Height -eq 384 -and $unique -eq 12 -and @($cells | Where-Object {-not $_.Valid}).Count -eq 0)
  $checks += [ordered]@{id=$id;file=('characters/'+$id+'-top-down.png');sha256=[Ticket19Check]::FileHash($sheetPath);decoder='System.Drawing.Bitmap, independent of Node PNG assembly';width=$sheet.Width;height=$sheet.Height;occupiedCells=@($cells | Where-Object {$_.Opaque -gt 0}).Count;uniquePixelFrames=$unique;binaryAlpha=(@($cells | Where-Object {$_.Partial -ne 0}).Count -eq 0);allCellsVisibleHeight120=(@($cells | Where-Object {$_.Height -ne 120}).Count -eq 0);allCellsTop8Bottom127=(@($cells | Where-Object {$_.Top -ne 8 -or $_.Bottom -ne 127}).Count -eq 0);frameAnchor=@(64,128);technicalChecksPassed=$passed;cells=$cells}
  [Ticket19Check]::Contact($sheet,($id.Substring(0,1).ToUpper()+$id.Substring(1)),(Join-Path $root ('qa/'+$id+'-contact.png')))
  Write-Output "$id`: ${unique} unterschiedliche belegte Frames, Raster/Alpha/120px/y127 bestanden=$passed"
 }finally{$sheet.Dispose()}
}
$backgrounds=Get-Content -LiteralPath (Join-Path $root 'backgrounds/provenance.json') -Raw | ConvertFrom-Json
$backgroundChecks=@()
foreach($b in $backgrounds){
 $bgPath=Join-Path $root $b.file;$sourcePath=Join-Path $root $b.source
 $bg=[System.Drawing.Bitmap]::new($bgPath)
 try{$sourceHash=[Ticket19Check]::FileHash($sourcePath);$copyHash=[Ticket19Check]::FileHash($bgPath);$backgroundChecks += [ordered]@{id=$b.id;file=$b.file;source=$b.source;width=$bg.Width;height=$bg.Height;sourceSha256=$sourceHash;sha256=$copyHash;byteIdentical=($sourceHash -eq $copyHash);originalGenerationSource=$b.originalGenerationSource;independentDecoder='System.Drawing.Bitmap';processing='None, byte-identical approved background'}}finally{$bg.Dispose()}
}
$report=[ordered]@{scope='Lokale Endgrafiken; keine Spieleinbindung, keine Laufgeometrie oder Ticketpflege';generationTool='image_gen__imagegen';generationModel='Nicht vom Werkzeug offengelegt';offlineExecutionVerified=$false;newLibrariesInstalled=$false;measurement='Unabhaengige Dekodierung der fertigen PNGs mit System.Drawing; jede Zelle separat gemessen';backgrounds=$backgroundChecks;characters=$checks;visualReview='Siehe qa/visual-review.json und DELIVERY.md; Metriken sind keine Anatomie-/Bewegungsabnahme'}
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $root 'technical-checks.json') -Encoding UTF8
if(@($checks | Where-Object {-not $_.technicalChecksPassed}).Count -ne 0){throw 'Mindestens ein technischer Sheet-Vertrag ist nicht erfüllt.'}
