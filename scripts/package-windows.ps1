$ErrorActionPreference = "Stop"
param([string]$rid = "win-x64")
dotnet publish "BBDown.GUI/BBDown.GUI.csproj" -c Release -r $rid /p:SelfContained=true /p:PublishTrimmed=$false /p:PublishSingleFile=$true
$pub = Join-Path "BBDown.GUI/bin/Release/net9.0/$rid/publish" ""
foreach ($t in @("ffmpeg.exe","MP4Box.exe","aria2c.exe")) {
  $src = Join-Path (Get-Location) $t
  if (Test-Path $src) { Copy-Item $src (Join-Path $pub $t) -Force }
}
Write-Output $pub