#!/usr/bin/env bash
set -euo pipefail
arch=$(uname -m)
rid="osx-x64"
if [ "$arch" = "arm64" ]; then rid="osx-arm64"; fi
dotnet publish BBDown.GUI/BBDown.GUI.csproj -c Release -r "$rid" /p:SelfContained=true /p:PublishTrimmed=false /p:PublishSingleFile=true
pub_dir="BBDown.GUI/bin/Release/net9.0/${rid}/publish"
app_path="$pub_dir/BBDown.GUI.app"
if [ -d "$app_path/Contents/MacOS" ]; then
  target_dir="$app_path/Contents/MacOS"
else
  target_dir="$pub_dir"
fi
for t in ffmpeg MP4Box aria2c; do
  if [ -f "$PWD/$t" ]; then cp "$PWD/$t" "$target_dir/$t" || true; fi
done
echo "$target_dir"