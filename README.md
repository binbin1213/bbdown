

> 本项目仅供个人学习、研究和非商业性用途。用户在使用本工具时，需自行确保遵守相关法律法规，特别是与版权相关的法律条款。开发者不对因使用本工具而产生的任何版权纠纷或法律责任承担责任。请用户在使用时谨慎，确保其行为合法合规，并仅在有合法授权的情况下使用相关内容。

# BBDown GUI
一个简洁易用的哔哩哔哩下载器图形界面版，基于强大的 [nilaoda/BBDown](https://github.com/nilaoda/BBDown) 核心。

## 下载
请从本页右侧的 **[GitHub Releases](https://github.com/binbin1213/bbdown/releases)** 页面下载适用于您操作系统的最新版本。
*   **Windows**: 下载 `.zip` 格式的压缩包，解压后运行 `BBDown.GUI.exe` 即可。
*   **macOS**: 下载 `.dmg` 格式的磁盘映像，打开后将 `BBDown.GUI` 应用拖拽到“应用程序”文件夹中。

## 界面预览
![BBDown.GUI](images/BBDown.GUI.png)

## 特别致谢

本项目的 GUI 是基于 [nilaoda/BBDown](https://github.com/nilaoda/BBDown) 的强大核心功能构建的。由衷感谢原作者的杰出工作！

## 如何使用
1.  启动 BBDown GUI 应用程序。
2.  在顶部的输入框中粘贴哔哩哔哩视频链接 (支持 av, bv, ep, ss 等格式)。
3.  程序将自动开始解析视频信息。
4.  如需下载会员专属内容，请点击界面上的“登录”按钮，并使用您的哔哩哔哩手机客户端扫描弹出的二维码。
5.  在视频列表中，根据需要选择您想下载的视频画质、音频、编码格式和字幕。
6.  点击“开始下载”按钮，任务将自动开始。

## 功能
- [x] 番剧下载(Web|TV|App)
- [x] 课程下载(Web)
- [x] 普通内容下载(Web|TV|App)
- [x] 合集/列表/收藏夹/个人空间解析
- [x] 多分P自动下载
- [x] 选择指定分P进行下载
- [x] 选择指定清晰度进行下载
- [x] 下载外挂字幕并转换为srt格式
- [x] 自动合并音频+视频流+字幕流+**章节信息**
- [x] 二维码登录账号
- [x] 多线程下载
- [x] 支持调用aria2c下载
- [x] 支持AVC/HEVC/AV1编码
- [x] **支持8K/HDR/杜比视界/杜比全景声下载**
- [x] 自定义存储文件名

## 致谢

* https://github.com/codebude/QRCoder
* https://github.com/icsharpcode/SharpZipLib
* https://github.com/protocolbuffers/protobuf
* https://github.com/grpc/grpc
* https://github.com/dotnet/command-line-api
* https://github.com/SocialSisterYi/bilibili-API-collect
* https://github.com/SeeFlowerX/bilibili-grpc-api
* https://github.com/FFmpeg/FFmpeg
* https://github.com/gpac/gpac
* https://github.com/aria2/aria2
