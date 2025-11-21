# BBDown 项目分析

## 概览
- 类型：命令行式哔哩哔哩下载器，支持 WEB/TV/APP/国际版解析与下载，弹幕/字幕处理，分段合并，服务端模式。
- 语言与框架：.NET 9.0；CLI 项目可作为 Dotnet Tool 发布使用。
- 外部工具：`ffmpeg` 或 `mp4box` 用于混流；可选 `aria2c` 提升下载并发与速度。
- 目标用户：需要离线保存 B 站视频及相关资源（弹幕、字幕、封面）的用户与服务端集成场景。

## 目录结构
- `BBDown`（CLI 与服务端）
  - 入口与命令：`Program.cs`、`CommandLineInvoker.cs`
  - 下载与混流：`BBDownDownloadUtil.cs`、`BBDownMuxer.cs`
  - 登录与二维码：`BBDownLoginUtil.cs`、`ConsoleQRCode.cs`
  - 服务器：`BBDownApiServer.cs`
  - 配置与参数：`MyOption.cs`、`BBDownConfigParser.cs`
- `BBDown.Core`（核心解析库）
  - 抓取器：`Fetcher/*.cs`（番剧/课程/空间/合集/收藏等）
  - 解析器：`Parser.cs`（DASH/FLV 轨道、杜比/Hi-Res 处理、分段信息）
  - 实体与工具：`Entity/*.cs`、`Util/*.cs`（HTTP、字幕、BV 转换等）
  - Proto 定义：`APP/**.proto`（生成 gRPC 客户端）

## 构建与运行
- 本地构建（需安装 .NET 9 SDK）：
  - 构建：`dotnet build BBDown.sln`
  - 运行帮助：`dotnet run --project BBDown/BBDown.csproj -- --help`
- Dotnet Tool（无需源码构建）：
  - 安装：`dotnet tool install --global BBDown`
  - 更新：`dotnet tool update --global BBDown`
- 服务器模式（HTTP，仅建议配合反向代理提供 HTTPS）：
  - 启动：`dotnet run --project BBDown/BBDown.csproj -- serve -l http://0.0.0.0:12450`

## 入口与命令
- 主入口：`BBDown/Program.cs:64`（`Main`）构建根命令，挂载 `login`、`logintv`、`serve`。
- 根命令与选项：`BBDown/CommandLineInvoker.cs:162` 注册解析模式（TV/APP/INTL）、下载控制（音/视频/弹幕/字幕/封面）、模板命名、UA/Cookie/Token 等。
- 登录：
  - WEB 登录：`BBDown/Program.cs:91`
  - TV 登录：`BBDown/Program.cs:94`
- 服务器：
  - 注册：`BBDown/Program.cs:81`（`serve` 子命令）
  - 启动逻辑：`BBDown/Program.cs:171`（`StartServer`）

## 核心流程
1. 输入解析与环境准备
   - 旧参数兼容与提示：`BBDown/Program.Methods.cs:23`
   - 冲突选项处理：`BBDown/Program.Methods.cs:199`
   - 外部工具探测：`BBDown/Program.Methods.cs:143`
   - 工作目录切换：`BBDown/Program.Methods.cs:222`
2. 视频信息获取与分 P 列表
   - 抓取器选择：`BBDown.Core/FetcherFactory.cs:12`
   - 视频信息与页列表：`BBDown/Program.cs:225`
3. 轨道提取与排序
   - DASH/FLV 解析：`BBDown.Core/Parser.cs:96`
   - 排序策略（画质/编码/升序）：`BBDown/Program.cs:820`
4. 下载与混流
   - 多线程/aria2c 下载：`BBDown/Program.Methods.cs:502`
   - 混流（音视频/字幕/章节）：`BBDown/Program.cs:661`
   - FLV 分段合并：`BBDown/Program.cs:746`
5. 命名模板
   - 模板替换器：`BBDown/Program.cs:846`
   - 单/多 P 默认模板：`BBDown/Program.cs:28-29`

## 网络与认证
- 随机 UA 与自定义 UA：`BBDown.Core/Util/HTTPUtil.cs:35`（随机）、`BBDown/Program.cs:201`（自定义）
- Cookie/Token 加载顺序（本地文件与参数）：`BBDown/Program.Methods.cs:243-265`
- HTTP 客户端配置与请求头：`BBDown.Core/Util/HTTPUtil.cs:10-18`、`37-56`
- CDN/域名替换与 PCDN 处理：`BBDown/Program.Methods.cs:360`

## 服务器 API 概要
- 任务查询：`/get-tasks`（全部/运行中/已完成/按 id）`BBDown/BBDownApiServer.cs:45-59`
- 添加任务：`POST /add-task`（支持回调 webhook）`BBDown/BBDownApiServer.cs:60-89`
- 清理已完成：`/remove-finished`（全部/失败/按 id）`BBDown/BBDownApiServer.cs:90-94`

## 常用示例
- 下载普通视频（WEB）：
  ```shell
  BBDown "https://www.bilibili.com/video/BV1qt4y1X7TW"
  ```
- 使用 TV 接口下载（通常水印更少）：
  ```shell
  BBDown -tv "https://www.bilibili.com/video/BV1qt4y1X7TW"
  ```
- 仅解析不下载：
  ```shell
  BBDown -info "https://www.bilibili.com/video/BV1qt4y1X7TW"
  ```
- 指定分 P（单个/多个/范围/全部/最新）：
  ```shell
  BBDown -p 10 "https://www.bilibili.com/video/BV1At41167aj"
  BBDown -p 1,2,10 "https://www.bilibili.com/video/BV1At41167aj"
  BBDown -p 1-10 "https://www.bilibili.com/video/BV1At41167aj"
  BBDown -p ALL "https://www.bilibili.com/bangumi/play/ss33073"
  BBDown -p LAST "https://www.bilibili.com/video/BV1At41167aj"
  ```
- 自定义输出文件名（示例，仅展示单 P）：
  ```shell
  BBDown -F "<videoTitle>[<dfn>]" "https://www.bilibili.com/video/BV1qt4y1X7TW"
  ```

## 关键代码位置
- 入口：`BBDown/Program.cs:64`
- 子命令注册：`BBDown/Program.cs:69-87`
- 根命令与选项：`BBDown/CommandLineInvoker.cs:162`
- 抓取器工厂：`BBDown.Core/FetcherFactory.cs:12`
- 轨道解析：`BBDown.Core/Parser.cs:96`
- 请求工具：`BBDown.Core/Util/HTTPUtil.cs:37`
- 多线程下载：`BBDown/Program.Methods.cs:502`
- 混流调用（DASH）：`BBDown/Program.cs:661`
- FLV 合并：`BBDown/Program.cs:746`
- 命名模板替换：`BBDown/Program.cs:846`
- 服务器端点：`BBDown/BBDownApiServer.cs:45-94`

## 外部依赖
- 混流：`ffmpeg`（推荐 ≥ 5.0 以支持杜比视界）或 `mp4box`
- 并发下载：`aria2c`（可配 `--aria2c-args`）
- Protobuf/gRPC：`Google.Protobuf`、`Grpc.Tools`（`BBDown.Core/BBDown.Core.csproj:11-16`）

## 架构与设计要点
- 选项解析清晰，旧参数兼容并给出替换建议，降低升级成本。
- 解析器对多源（WEB/TV/APP/国际版）与多轨（视频/音频/背景音/角色配音）统一建模，排序与选择可配置。
- 下载流程健壮：多线程/aria2c、断点/分段合并、异常自动重试（`BBDown/Program.cs:787-794`）。
- 服务模式适用于批量任务与系统集成，提供基础查询与回调机制。

## 改进建议
- 增加自动化测试与回归用例（解析器与下载/混流流程）。
- 升级 `System.CommandLine` 至稳定版，优化异常处理与解析行为。
- 在启动阶段校验 `ffmpeg/mp4box/aria2c` 的存在与版本，给出更直观提示。
- 增强日志结构化输出与可观测性（进度、速度、失败原因），服务模式下便于追踪。
- 增加取消/限速支持，下载任务暴露 `CancellationToken` 与速率限制参数。
- 服务端增加队列与并发上限、失败重试/回退策略；可选鉴权与速率限制避免滥用。
- 进一步抽象解析与回退策略，减少 `Program.cs` 中的复杂控制流耦合。

## 版本与元信息
- CLI 项目标记为 Dotnet Tool：`BBDown/BBDown.csproj:11-13`
- 目标框架：`BBDown/BBDown.csproj:5`、`BBDown.Core/BBDown.Core.csproj:5`
- 版本：`BBDown/BBDown.csproj:7`（当前 `1.6.3`）

---
本文档用于快速理解项目结构与关键流程，结合 `README.md` 的命令示例即可完成基本下载与服务端集成。