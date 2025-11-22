## 改造目标
- 优化整体排版：信息分组清晰、左右分栏、滚动良好、按钮位置直观
- 统一样式：控件圆角、间距、字号一致，视觉层级明确
- 提高可读性：标签对齐、文本换行与日志显示更舒适

## 框架与入口
- GUI 使用 Avalonia（C# + `.axaml`）
- 核心文件：
  - `BBDown.GUI/App.axaml:1-14` 应用主题与基础样式
  - `BBDown.GUI/MainWindow.axaml:1-182` 主窗口布局
  - `BBDown.GUI/MainWindow.axaml.cs:18-42` 事件绑定与初始化

## 设计原则
- 左右分栏：左侧输入与设置；右侧操作按钮与日志区
- 分组清晰：使用 `GroupBox`/`Border`/`Expander` 分类“下载选项”“命名”“aria2c”“高级设置”等
- 标签对齐：左固定宽度列，右扩展列；减少横向长串控件
- 统一间距：全局 `Spacing`、`Margin` 规范化

## 修改点与方案
### 1) 应用级样式（`App.axaml`）
- 将 `RequestedThemeVariant` 设为 `Default`，跟随系统深浅色
- 增加通用样式：`StackPanel.Spacing`、`TextBlock.FontSize`、控件高度与圆角统一
- 示例（替换/扩充 `App.axaml:1-14` 的 `<Application.Styles>`）：
```xml
<Application xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" x:Class="BBDown.GUI.App" RequestedThemeVariant="Default">
  <Application.Styles>
    <FluentTheme/>
    <Style Selector="StackPanel">
      <Setter Property="Spacing" Value="8"/>
    </Style>
    <Style Selector="TextBlock">
      <Setter Property="FontSize" Value="14"/>
    </Style>
    <Style Selector="Button">
      <Setter Property="Height" Value="36"/>
      <Setter Property="CornerRadius" Value="8"/>
    </Style>
    <Style Selector="TextBox">
      <Setter Property="Height" Value="32"/>
      <Setter Property="CornerRadius" Value="8"/>
    </Style>
    <Style Selector="ComboBox">
      <Setter Property="Height" Value="32"/>
      <Setter Property="CornerRadius" Value="8"/>
      <Setter Property="MinWidth" Value="160"/>
    </Style>
  </Application.Styles>
</Application>
```

### 2) 顶部输入区（`MainWindow.axaml:3-13`）
- 保持一行，但用固定标签列宽+可扩展输入列，保证对齐：
```xml
<Grid Grid.Row="0" ColumnDefinitions="160,*,160">
  <TextBlock Text="URL" VerticalAlignment="Center"/>
  <TextBox Name="UrlBox" Grid.Column="1" Margin="8,0"/>
  <ComboBox Name="ApiTypeBox" Grid.Column="2" Margin="8,0,0,0">
    <ComboBoxItem Content="WEB"/>
    <ComboBoxItem Content="TV"/>
    <ComboBoxItem Content="APP"/>
    <ComboBoxItem Content="INTL"/>
  </ComboBox>
</Grid>
```

### 3) 主体改为双栏（重构 `MainWindow.axaml:15-176`）
- 用 `Grid` 两列：左侧设置滚动区，右侧操作+日志区；日志不再挤占左侧空间：
```xml
<Grid Grid.Row="1" ColumnDefinitions="2*,1*" Margin="0,16,0,0">
  <ScrollViewer Grid.Column="0">
    <StackPanel>
      <GroupBox Header="下载选项" Margin="0,0,0,12">
        <StackPanel>
          <WrapPanel>
            <CheckBox Name="OnlyInfoBox" Content="仅解析不下载 (-info)"/>
            <CheckBox Name="MultiThreadBox" Content="多线程下载 (-mt)" IsChecked="True"/>
            <CheckBox Name="VideoOnlyBox" Content="仅视频 (--video-only)"/>
            <CheckBox Name="AudioOnlyBox" Content="仅音频 (--audio-only)"/>
            <CheckBox Name="UseAria2cBox" Content="使用 aria2c (--use-aria2c)"/>
            <CheckBox Name="DebugBox" Content="调试日志 (--debug)"/>
          </WrapPanel>
        </StackPanel>
      </GroupBox>

      <GroupBox Header="命名与分P" Margin="0,0,0,12">
        <StackPanel>
          <Grid ColumnDefinitions="160,*" Margin="0,4,0,0">
            <TextBlock Text="分P (-p)" VerticalAlignment="Center"/>
            <TextBox Name="SelectPageBox" Grid.Column="1" Watermark="如: 1,2,10 或 1-3 或 ALL"/>
          </Grid>
          <Grid ColumnDefinitions="160,*" Margin="0,4,0,0">
            <TextBlock Text="文件名 (-F)" VerticalAlignment="Center"/>
            <TextBox Name="FilePatternBox" Grid.Column="1" Watermark="如: &lt;videoTitle&gt;[&lt;dfn&gt;]"/>
          </Grid>
        </StackPanel>
      </GroupBox>

      <Expander Header="aria2c" IsExpanded="False" Margin="0,0,0,12">
        <Grid ColumnDefinitions="160,*">
          <TextBlock Text="参数" VerticalAlignment="Center"/>
          <TextBox Name="Aria2cArgsBox" Grid.Column="1" Watermark="默认: -x16 -s16 -j16 -k 5M"/>
        </Grid>
      </Expander>

      <!-- 高级设置保持 ScrollViewer，但按两列网格分组对齐（沿用现有控件名称） -->
      <GroupBox Header="高级设置">
        <ScrollViewer>
          <StackPanel>
            <!-- 例：语言/UA -->
            <Grid ColumnDefinitions="160,*,160,*" Margin="0,4,0,0">
              <TextBlock Text="语言 (--language)" VerticalAlignment="Center"/>
              <TextBox Name="LanguageBox" Grid.Column="1"/>
              <TextBlock Text="UA (-ua)" Grid.Column="2" VerticalAlignment="Center"/>
              <TextBox Name="UserAgentBox" Grid.Column="3"/>
            </Grid>
            <!-- 其余行以相同模式将标签置于固定列、输入置于伸缩列，避免超长横向堆叠 -->
          </StackPanel>
        </ScrollViewer>
      </GroupBox>
    </StackPanel>
  </ScrollViewer>

  <Grid Grid.Column="1" RowDefinitions="Auto,*" Margin="12,0,0,0">
    <StackPanel Orientation="Horizontal" Spacing="12">
      <Button Name="StartButton" Width="160">开始</Button>
      <Button Name="StopButton" Width="120">停止</Button>
    </StackPanel>
    <TextBox Name="LogBox" Grid.Row="1" AcceptsReturn="True" IsReadOnly="True" FontFamily="Menlo,Consolas" TextWrapping="Wrap" ScrollViewer.VerticalScrollBarVisibility="Auto"/>
  </Grid>
</Grid>
```
- 优点：日志常驻右侧不抢内容高度；左侧设置可滚动且分组清晰；标签与输入对齐，阅读顺序更自然

### 4) 底部提示（`MainWindow.axaml:178-180`）
- 保留为状态栏区域，字体缩小、灰度弱化：
```xml
<StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,12,0,0" Spacing="8">
  <TextBlock Text="提示: 可在高级设置中配置全部命令行参数。" Opacity="0.7" FontSize="12"/>
</StackPanel>
```

## 交互与细节优化
- `TextBox.Watermark` 已使用，整体保留并统一控件高度，避免视觉跳变
- 将长串 `CheckBox` 改为 `WrapPanel` 自动换行，避免横向挤压
- 重要操作按钮靠右侧置顶，优先可见；日志区使用等宽字体并自动滚动

## 风险与兼容性
- 不引入新依赖，完全基于当前 `Avalonia` 与现有控件
- 保持控件名称不变，`MainWindow.axaml.cs` 逻辑无需改动（事件绑定位置见 `MainWindow.axaml.cs:18-42`）

## 验证步骤
- 构建并运行桌面应用，检查：
  - 左右分栏是否正常、滚动是否顺畅
  - 各标签与输入对齐、间距统一
  - 日志区可读性与按钮操作是否正常
- 不影响命令行参数拼装（逻辑位于 `BuildArgs`：`MainWindow.axaml.cs:63-119`）

## 交付
- 按上述方案修改 `App.axaml` 与 `MainWindow.axaml`，不改动后端代码
- 提供前后对比截图与说明，确保布局与视觉体验显著改善