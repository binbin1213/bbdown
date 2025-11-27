# BBDown GUI 架构与技术文档

本文档旨在详细阐述 BBDown GUI 项目的架构设计、技术选型和核心工作流程，为项目的维护和二次开发提供清晰的指引。

## 1. 项目概述

BBDown GUI 是一个基于 [nilaoda/BBDown](https://github.com/nilaoda/BBDown) 核心功能构建的跨平台桌面应用程序。它为用户提供了一个直观的图形界面，以替代原有的命令行操作，从而简化在 Windows 和 macOS 平台上下载哔哩哔哩视频的体验。

## 2. 核心架构

本项目的解决方案 (`BBDown.sln`) 包含三个关键的项目，它们共同协作，构成了一个典型的“前端-后端-核心库”架构模式。

```
[ BBDown.GUI ]  <-- (用户交互)
      |
      | (调用命令行)
      v
[  BBDown CLI  ]  <-- (作为后端服务)
      |
      | (调用核心功能)
      v
[ BBDown.Core  ]  <-- (视频解析与下载)
```

### 2.1. BBDown.GUI (前端)

*   **角色**: 用户界面层 (UI)。
*   **职责**:
    *   提供所有用户可见的图形界面元素，如输入框、按钮、视频列表等。
    *   接收用户的输入（如视频链接、登录请求、下载选项）。
    *   将用户的请求转换为对 `BBDown` CLI 的命令行调用。
    *   解析 CLI 的输出，并将任务状态和进度实时反馈到界面上。
*   **关键实现**:
    *   当用户点击“开始下载”时，GUI 程序会启动一个隐藏的 `BBDown.exe` (或 `BBDown`) 进程，并通过命令行参数传递视频链接、Cookie、清晰度选项等所有必要信息。

### 2.2. BBDown (后端/CLI)

*   **角色**: 命令行工具 & GUI 的后端服务。
*   **职责**:
    *   作为一个独立的、功能完整的命令行下载工具存在。
    *   接收并解析来自 `BBDown.GUI` 的命令行参数。
    *   调用 `BBDown.Core` 库来执行实际的视频信息解析和下载任务。
    *   将执行过程中的日志、进度和错误信息通过标准输出（stdout）流实时输出。
*   **与 GUI 的关系**:
    *   `BBDown.GUI` 依赖 `BBDown` 的可执行文件来完成所有核心下载任务。在打包发布时，`BBDown.exe` (或 `BBDown`) 会被一同打包到 GUI 应用的程序目录中。

### 2.3. BBDown.Core (核心库)

*   **角色**: 核心功能库。
*   **职责**:
    *   封装了所有与哔哩哔哩服务器交互的底层逻辑。
    *   负责解析视频链接，获取视频的各种清晰度、编码格式、音频和字幕信息。
    *   处理用户登录和认证。
    *   执行实际的文件下载和数据流合并。
*   **独立性**:
    *   这是一个完全独立的库，不包含任何界面或命令行逻辑，可以被任何 .NET 项目复用。

## 3. 技术栈

*   **.NET**: 项目基于 .NET 平台构建，确保了其跨平台能力。
*   **C#**: 所有项目的开发语言。
*   **Avalonia UI**: 用于构建 `BBDown.GUI` 的跨平台 UI 框架。它使得同一套代码可以编译并运行在 Windows 和 macOS 上。

## 4. 外部依赖

为了完成视频的下载和合并，项目依赖于两个业界知名的开源工具：

*   **FFmpeg**: 一个功能强大的音视频处理工具。在本项目中，它主要用于将下载的视频流和音频流合并成一个完整的 MP4 文件。
*   **Aria2c**: 一款轻量级的多协议和多源命令行下载工具。`BBDown` 可以调用它来实现多线程下载，从而显著提升下载速度。

这两个工具的可执行文件同样会在应用发布时被打包到程序目录中。

## 5. 项目结构

```
/
├── .github/workflows/      # GitHub Actions 配置文件，用于自动化构建和发布
├── BBDown.Core/            # 核心功能库项目
├── BBDown.GUI/             # Avalonia UI 图形界面项目
├── BBDown/                 # 命令行工具项目
├── images/                 # 存放项目相关图片，如 README 中的截图
├── .gitignore              # 定义了应被 Git 忽略的文件和目录
├── ARCHITECTURE.md         # 本架构文档
├── BBDown.sln              # Visual Studio 解决方案文件
└── README.md               # 项目介绍和使用说明
```

## 6. 构建与发布

项目的构建和发布流程由 `.github/workflows/build_latest.yml` 文件定义，并通过 GitHub Actions 实现自动化。

*   **触发条件**:
    *   当一个以 `v` 开头的标签（如 `v1.2.0`）被推送到仓库时。
    *   当在 GitHub 仓库页面上手动触发时。
*   **主要步骤**:
    1.  **构建 GUI 和 CLI**: 分别为 Windows (x64) 和 macOS (x64, arm64) 平台编译 `BBDown.GUI` 和 `BBDown`。
    2.  **下载外部工具**: 下载对应平台的 `ffmpeg` 和 `aria2c`。
    3.  **打包**:
        *   对于 Windows，将所有文件（GUI, CLI, FFmpeg, Aria2c）打包成一个 `.zip` 压缩包。
        *   对于 macOS，将所有文件打包成一个标准的 `.app` 应用，并最终生成一个 `.dmg` 磁盘映像文件。
    4.  **创建 Release**: 将生成的 `.zip` 和 `.dmg` 文件作为产物，在 GitHub 上创建一个新的 Release 草稿。

## 7. 参与 GUI 界面开发

### 7.1. 环境准备
- 安装 .NET SDK 9.0 或更高版本（项目基于 .NET 平台构建）。
- 推荐使用以下 IDE 之一进行开发：
  - Visual Studio 2022 或更高版本（Windows/macOS）。
  - JetBrains Rider（跨平台）。
  - Visual Studio Code（需安装 Avalonia 相关扩展）。

### 7.2. 启动项目
1. 克隆仓库到本地（包含子模块）：`git clone --recurse-submodules https://github.com/binbin1213/bbdown.git`。
2. 打开 `BBDown.sln` 解决方案文件。
3. 在 IDE 中设置 `BBDown.GUI` 项目为启动项目。
4. 点击“启动”按钮，即可运行 GUI 程序进行开发调试。

### 7.3. 开发建议
- `BBDown.GUI` 基于 Avalonia UI 框架构建，建议参考 [Avalonia 官方文档](https://docs.avaloniaui.net/) 进行界面开发。
- 界面相关的代码主要位于 `BBDown.GUI/Views` 和 `BBDown.GUI/ViewModels` 目录下，遵循 MVVM 设计模式。
