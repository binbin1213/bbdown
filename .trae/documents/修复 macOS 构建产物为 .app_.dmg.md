## 问题定位
- 当前 macOS GUI 构建产物只是 `dotnet publish` 的输出目录，不是 macOS 应用包。
- 证据：工作流在 `.github/workflows/build_latest.yml:121-129` 使用 `dotnet publish ... /p:PublishSingleFile=true` 并直接上传 `publish` 目录。
- `BBDown.GUI.csproj` 未添加用于打包 `.app` 的打包目标或包（`BBDown.GUI/BBDown.GUI.csproj:3-14`）。
- 本地脚本 `scripts/package-macos.sh:6-13` 同样只做 `publish`，没有执行 `.app` 打包步骤，因而回退到裸目录。
- 参考：Avalonia 官方文档建议通过 `dotnet-bundle` 的 `BundleApp` 目标创建 `.app`（https://docs.avaloniaui.net/docs/deployment/macOS）。

## 修复方案
- 引入 `dotnet-bundle` 并使用 `BundleApp` 目标生成 `BBDown.GUI.app`。
- 取消 `PublishSingleFile`（它与 `.app` 打包相悖），保留 `SelfContained` 与 `UseAppHost`。
- 在 `.app/Contents/MacOS` 中加入外部依赖二进制（`ffmpeg`、`MP4Box`、`aria2c`）。
- 使用 macOS 自带 `hdiutil` 生成 `.dmg` 并作为构件上传。
- 保留 Windows 流程不变。

## 具体改动
1) 项目文件
- 在 `BBDown.GUI.csproj` 添加：`<PackageReference Include="dotnet-bundle" Version="*" PrivateAssets="all" />`。

2) GitHub Actions（macOS GUI）
- 移除 `PublishSingleFile=true`，改为：
  - `dotnet restore -r ${{ matrix.rid }}`
  - `dotnet msbuild BBDown.GUI/BBDown.GUI.csproj -t:BundleApp -p:RuntimeIdentifier=${{ matrix.rid }} -p:UseAppHost=true -p:SelfContained=true -property:Configuration=Release`
- 复制外部工具到 `.app`：
  - 目标目录：`BBDown.GUI/bin/Release/net9.0/${{ matrix.rid }}/publish/BBDown.GUI.app/Contents/MacOS`
  - `cp ffmpeg MP4Box aria2c`（存在时）
- 生成 DMG：
  - `hdiutil create -volname "BBDown" -srcfolder BBDown.GUI/bin/Release/net9.0/${{ matrix.rid }}/publish/BBDown.GUI.app -ov -format UDZO BBDown.GUI-${{ matrix.rid }}.dmg`
- 上传构件：
  - `BBDown.GUI-${{ matrix.rid }}.dmg`

3) 本地打包脚本
- 将 `scripts/package-macos.sh` 改为执行 `BundleApp` 并输出 `.app` 路径（与 CI 一致）。

## 验证
- 在 CI 的 macOS 任务完成后，下载 `BBDown.GUI-osx-*.dmg`，确认其中包含 `BBDown.GUI.app`，可双击运行。
- 本地可执行脚本验证：`./scripts/package-macos.sh` 输出 `.app` 目录；打开 `BBDown.GUI.app` 测试 UI 与依赖二进制调用。

## 注意事项
- 未签名的 `.app/.dmg` 在首次运行可能需要右键打开或通过系统设置允许。
- 若需分发到大众用户，后续可增补代码签名与公证步骤；目前先确保产物形态正确。

如果同意，我将按以上方案更新项目文件与工作流，并提交变更。