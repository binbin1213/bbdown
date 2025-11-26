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
# copy GUI executable into .app
if [ -f "$pub_dir/BBDown.GUI" ]; then cp "$pub_dir/BBDown.GUI" "$target_dir/BBDown.GUI"; fi
# build and copy CLI
dotnet publish BBDown/BBDown.csproj -c Release -r "$rid" -p:SelfContained=true -p:PublishSingleFile=true -o "$pub_dir/cli"
if [ -f "$pub_dir/cli/BBDown" ]; then cp "$pub_dir/cli/BBDown" "$target_dir/BBDown"; fi
# --- Start of modification ---
# Check for required external tools before copying
for t in ffmpeg aria2c; do
  if [ ! -f "$PWD/$t" ]; then
    echo "Error: Required external tool '$t' not found in the project root directory."
    echo "Please download it and place it in '$PWD' before running this script."
    exit 1
  fi
done
# --- End of modification ---

for t in ffmpeg MP4Box aria2c; do
  if [ -f "$PWD/$t" ]; then cp "$PWD/$t" "$target_dir/$t" || true; fi
done
# ensure executables are marked executable
chmod +x "$target_dir/BBDown.GUI" 2>/dev/null || true
chmod +x "$target_dir/BBDown" 2>/dev/null || true
chmod +x "$target_dir/ffmpeg" 2>/dev/null || true
chmod +x "$target_dir/aria2c" 2>/dev/null || true
for f in "$pub_dir"/lib*.dylib; do [ -f "$f" ] && cp "$f" "$target_dir/"; done
for f in "$pub_dir"/*.dll; do [ -f "$f" ] && cp "$f" "$target_dir/"; done
for f in "$pub_dir"/*.json; do [ -f "$f" ] && cp "$f" "$target_dir/"; done
for d in cs de es fr it ja ko pl pt-BR ru tr zh-Hans zh-Hant; do [ -d "$pub_dir/$d" ] && cp -a "$pub_dir/$d" "$target_dir/"; done
# fix Info.plist keys robustly
if [ -f "$app_path/Contents/Info.plist" ]; then 
  /usr/libexec/PlistBuddy -c 'Delete :CFBundleExecutable' "$app_path/Contents/Info.plist" 2>/dev/null || true
  /usr/libexec/PlistBuddy -c 'Add :CFBundleExecutable string BBDown.GUI' "$app_path/Contents/Info.plist"
  /usr/libexec/PlistBuddy -c 'Delete :CFBundlePackageType' "$app_path/Contents/Info.plist" 2>/dev/null || true
  /usr/libexec/PlistBuddy -c 'Add :CFBundlePackageType string APPL' "$app_path/Contents/Info.plist"
fi
xattr -dr com.apple.quarantine "$app_path" || true
zip_name="BBDown.GUI-${rid}.zip"
rm -f "$zip_name"
zip -r "$zip_name" "$app_path"
echo "$app_path"
echo "$zip_name"