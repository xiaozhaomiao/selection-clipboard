$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources = @('ClipboardTool.cs','Bubble.cs','AppSettings.cs','LocalHistoryStore.cs','ClipboardReplay.cs','UsageGuide.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$outputPath = Join-Path $PSScriptRoot 'SelectionClipboard-0.4.6.exe'
& $compiler /nologo /target:winexe "/win32icon:$PSScriptRoot\assets\clipboard.ico" "/out:$outputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll $sources
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
$check = Start-Process -FilePath $outputPath -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($check.ExitCode -ne 0) { throw "自检失败：$($check.ExitCode)" }
Write-Output "已生成：$outputPath"
