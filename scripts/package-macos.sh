#!/usr/bin/env bash
set -euo pipefail
arch=$(uname -m)
rid="osx-x64"
if [ "$arch" = "arm64" ]; then rid="osx-arm64"; fi
dotnet restore BBDown.GUI/BBDown.GUI.csproj -r "$rid"
dotnet msbuild BBDown.GUI/BBDown.GUI.csproj -t:BundleApp -p:RuntimeIdentifier="$rid" -p:UseAppHost=true -p:SelfContained=true -property:Configuration=Release
pub_dir="BBDown.GUI/bin/Release/net9.0/${rid}/publish"
app_path="$pub_dir/BBDown.GUI.app"
target_dir="$app_path/Contents/MacOS"
mkdir -p "$target_dir"
dotnet publish BBDown/BBDown.csproj -c Release -r "$rid" -p:SelfContained=true -p:PublishSingleFile=true -o "$pub_dir/cli"
if [ -f "$pub_dir/cli/BBDown" ]; then cp "$pub_dir/cli/BBDown" "$target_dir/BBDown"; fi
for t in ffmpeg MP4Box aria2c; do
  if [ -f "$PWD/$t" ]; then cp "$PWD/$t" "$target_dir/$t" || true; fi
done
if [ -f "$app_path/Contents/Info.plist" ]; then /usr/libexec/PlistBuddy -c 'Set :CFBundlePackageType APPL' "$app_path/Contents/Info.plist"; fi
xattr -dr com.apple.quarantine "$app_path" || true
zip_name="BBDown.GUI-${rid}.zip"
rm -f "$zip_name"
zip -r "$zip_name" "$app_path"
echo "$app_path"
echo "$zip_name"