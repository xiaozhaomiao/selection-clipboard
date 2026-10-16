Add-Type -AssemblyName System.Drawing
$ErrorActionPreference='Stop'
$output=Join-Path $PSScriptRoot 'clipboard.ico'
$images=@()
foreach($size in @(16,20,24,32,48,64,128,256)){
 $bitmap=New-Object Drawing.Bitmap($size,$size)
 $g=[Drawing.Graphics]::FromImage($bitmap)
 $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $g.ScaleTransform(($size/64.0),($size/64.0))
 $g.Clear([Drawing.Color]::Transparent)
 $body=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(35,110,210))
 $white=New-Object Drawing.SolidBrush([Drawing.Color]::White)
 $clip=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(19,64,128))
 $line=New-Object Drawing.Pen([Drawing.Color]::FromArgb(35,110,210),4)
 $line.StartCap=[Drawing.Drawing2D.LineCap]::Round;$line.EndCap=[Drawing.Drawing2D.LineCap]::Round
 $g.FillRectangle($body,9,9,46,51)
 $g.FillRectangle($white,15,16,34,37)
 $g.FillRectangle($clip,22,4,20,13)
 $g.DrawLine($line,23,27,41,27)
 $g.DrawLine($line,23,36,41,36)
 $g.DrawLine($line,23,45,35,45)
 $stream=New-Object IO.MemoryStream
 $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
 $images+=,[byte[]]$stream.ToArray()
 if($size -eq 256){$bitmap.Save((Join-Path $PSScriptRoot 'clipboard-preview.png'),[Drawing.Imaging.ImageFormat]::Png)}
 $stream.Dispose();$line.Dispose();$body.Dispose();$white.Dispose();$clip.Dispose();$g.Dispose();$bitmap.Dispose()
}
$file=[IO.File]::Create($output);$writer=New-Object IO.BinaryWriter($file)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count)
$offset=6+16*$images.Count;$sizes=@(16,20,24,32,48,64,128,256)
for($i=0;$i -lt $images.Count;$i++){$s=$sizes[$i];$dimension=if($s -eq 256){0}else{$s};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$images[$i].Length);$writer.Write([uint32]$offset);$offset+=$images[$i].Length}
foreach($data in $images){$writer.Write([byte[]]$data)}
$writer.Dispose()
