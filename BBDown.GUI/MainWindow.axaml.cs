using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BBDown.GUI;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        ApiTypeBox.SelectedIndex = 0;
        StartButton.Click += OnStart;
        StopButton.Click += OnStop;
        TestFFmpegButton.Click += async (_, __) => await TestBinary(string.IsNullOrWhiteSpace(FFmpegPathBox.Text) ? "ffmpeg" : FFmpegPathBox.Text!, "-version");
        TestMp4boxButton.Click += async (_, __) => await TestBinary(string.IsNullOrWhiteSpace(Mp4boxPathBox.Text) ? "MP4Box" : Mp4boxPathBox.Text!, "-version");
        TestAria2cButton.Click += async (_, __) => await TestBinary(string.IsNullOrWhiteSpace(Aria2cPathBox.Text) ? "aria2c" : Aria2cPathBox.Text!, "-v");
        PresetBestButton.Click += (_, __) => ApplyBestPreset();
        TestCliButton.Click += async (_, __) => await TestBinary(string.IsNullOrWhiteSpace(CliPathBox.Text) ? "BBDown" : CliPathBox.Text!, "--version");
        BrowseFFmpegButton.Click += async (_, __) => FFmpegPathBox.Text = await PickFileAsync("选择 ffmpeg") ?? FFmpegPathBox.Text;
        BrowseMp4boxButton.Click += async (_, __) => Mp4boxPathBox.Text = await PickFileAsync("选择 MP4Box") ?? Mp4boxPathBox.Text;
        BrowseAria2cButton.Click += async (_, __) => Aria2cPathBox.Text = await PickFileAsync("选择 aria2c") ?? Aria2cPathBox.Text;
        BrowseConfigFileButton.Click += async (_, __) => ConfigFileBox.Text = await PickFileAsync("选择配置文件") ?? ConfigFileBox.Text;
        BrowseWorkDirButton.Click += async (_, __) => WorkDirBox.Text = await PickFolderAsync("选择工作目录") ?? WorkDirBox.Text;
        BrowseCliButton.Click += async (_, __) => CliPathBox.Text = await PickFileAsync("选择 BBDown 可执行文件") ?? CliPathBox.Text;
        var cwd = Environment.CurrentDirectory;
        var fp = Path.Combine(cwd, "ffmpeg");
        if (File.Exists(fp)) FFmpegPathBox.Text = fp;
        var mp = Path.Combine(cwd, "MP4Box");
        if (File.Exists(mp)) Mp4boxPathBox.Text = mp;
        var ap = Path.Combine(cwd, "aria2c");
        if (File.Exists(ap)) Aria2cPathBox.Text = ap;
    }

    private async void OnStart(object? sender, RoutedEventArgs e)
    {
        StopRunning();
        _cts = new CancellationTokenSource();
        var args = BuildArgs();
        AppendLog(string.Join(' ', args));
        await RunCliAsync(args, _cts.Token);
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        StopRunning();
    }

    private void StopRunning()
    {
        try { _cts?.Cancel(); } catch { }
    }

    private List<string> BuildArgs()
    {
        var url = UrlBox.Text ?? string.Empty;
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(url)) list.Add(url);
        var api = (ApiTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "WEB";
        if (api == "TV") list.Add("-tv");
        else if (api == "APP") list.Add("-app");
        else if (api == "INTL") list.Add("-intl");
        if (OnlyInfoBox.IsChecked == true) list.Add("-info");
        if (MultiThreadBox.IsChecked == true) list.Add("-mt");
        if (VideoOnlyBox.IsChecked == true) list.Add("--video-only");
        if (AudioOnlyBox.IsChecked == true) list.Add("--audio-only");
        if (UseAria2cBox.IsChecked == true) list.Add("--use-aria2c");
        if (!string.IsNullOrWhiteSpace(Aria2cArgsBox.Text)) { list.Add("--aria2c-args"); list.Add(Aria2cArgsBox.Text!); }
        if (!string.IsNullOrWhiteSpace(SelectPageBox.Text)) { list.Add("-p"); list.Add(SelectPageBox.Text!); }
        if (!string.IsNullOrWhiteSpace(FilePatternBox.Text)) { list.Add("-F"); list.Add(FilePatternBox.Text!); }
        if (!string.IsNullOrWhiteSpace(LanguageBox.Text)) { list.Add("--language"); list.Add(LanguageBox.Text!); }
        if (!string.IsNullOrWhiteSpace(UserAgentBox.Text)) { list.Add("-ua"); list.Add(UserAgentBox.Text!); }
        if (!string.IsNullOrWhiteSpace(CookieBox.Text)) { list.Add("-c"); list.Add(CookieBox.Text!); }
        if (!string.IsNullOrWhiteSpace(TokenBox.Text)) { list.Add("-token"); list.Add(TokenBox.Text!); }
        if (UseMP4boxBox.IsChecked == true) list.Add("--use-mp4box");
        if (!string.IsNullOrWhiteSpace(EncodingPriorityBox.Text)) { list.Add("-e"); list.Add(EncodingPriorityBox.Text!); }
        if (!string.IsNullOrWhiteSpace(DfnPriorityBox.Text)) { list.Add("-q"); list.Add(DfnPriorityBox.Text!); }
        if (ShowAllBox.IsChecked == true) list.Add("--show-all");
        if (SimplyMuxBox.IsChecked == true) list.Add("--simply-mux");
        if (DanmakuOnlyBox.IsChecked == true) list.Add("--danmaku-only");
        if (CoverOnlyBox.IsChecked == true) list.Add("--cover-only");
        if (SubOnlyBox.IsChecked == true) list.Add("--sub-only");
        if (SkipMuxBox.IsChecked == true) list.Add("--skip-mux");
        if (SkipSubtitleBox.IsChecked == true) list.Add("--skip-subtitle");
        if (SkipCoverBox.IsChecked == true) list.Add("--skip-cover");
        if (ForceHttpBox.IsChecked == true) list.Add("--force-http");
        if (DownloadDanmakuBox.IsChecked == true) list.Add("-dd");
        if (!string.IsNullOrWhiteSpace(DanmakuFormatsBox.Text)) { list.Add("--download-danmaku-formats"); list.Add(DanmakuFormatsBox.Text!); }
        if (SkipAiBox.IsChecked == true) list.Add("--skip-ai");
        if (VideoAscendingBox.IsChecked == true) list.Add("--video-ascending");
        if (AudioAscendingBox.IsChecked == true) list.Add("--audio-ascending");
        if (AllowPcdnBox.IsChecked == true) list.Add("--allow-pcdn");
        if (!string.IsNullOrWhiteSpace(MultiFilePatternBox.Text)) { list.Add("-M"); list.Add(MultiFilePatternBox.Text!); }
        if (!string.IsNullOrWhiteSpace(WorkDirBox.Text)) { list.Add("--work-dir"); list.Add(WorkDirBox.Text!); }
        if (!string.IsNullOrWhiteSpace(DelayPerPageBox.Text)) { list.Add("--delay-per-page"); list.Add(DelayPerPageBox.Text!); }
        if (!string.IsNullOrWhiteSpace(FFmpegPathBox.Text)) { list.Add("--ffmpeg-path"); list.Add(FFmpegPathBox.Text!); }
        if (!string.IsNullOrWhiteSpace(Mp4boxPathBox.Text)) { list.Add("--mp4box-path"); list.Add(Mp4boxPathBox.Text!); }
        if (!string.IsNullOrWhiteSpace(Aria2cPathBox.Text)) { list.Add("--aria2c-path"); list.Add(Aria2cPathBox.Text!); }
        if (!string.IsNullOrWhiteSpace(Aria2cArgsBoxAdv.Text)) { list.Add("--aria2c-args"); list.Add(Aria2cArgsBoxAdv.Text!); }
        if (!string.IsNullOrWhiteSpace(UposHostBox.Text)) { list.Add("--upos-host"); list.Add(UposHostBox.Text!); }
        if (ForceReplaceHostBox.IsChecked == true) list.Add("--force-replace-host");
        if (SaveArchivesToFileBox.IsChecked == true) list.Add("--save-archives-to-file");
        if (!string.IsNullOrWhiteSpace(HostBox.Text)) { list.Add("--host"); list.Add(HostBox.Text!); }
        if (!string.IsNullOrWhiteSpace(EpHostBox.Text)) { list.Add("--ep-host"); list.Add(EpHostBox.Text!); }
        if (!string.IsNullOrWhiteSpace(TvHostBox.Text)) { list.Add("--tv-host"); list.Add(TvHostBox.Text!); }
        if (!string.IsNullOrWhiteSpace(AreaBox.Text)) { list.Add("--area"); list.Add(AreaBox.Text!); }
        if (!string.IsNullOrWhiteSpace(ConfigFileBox.Text)) { list.Add("--config-file"); list.Add(ConfigFileBox.Text!); }
        if (DebugBox.IsChecked == true) list.Add("--debug");
        return list;
    }

    private async Task RunCliAsync(List<string> args, CancellationToken token)
    {
        var cliPath = CliPathBox.Text;
        var useCliExe = !string.IsNullOrWhiteSpace(cliPath) && File.Exists(cliPath);
        var psi = new ProcessStartInfo
        {
            FileName = useCliExe ? cliPath! : "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (useCliExe)
        {
            foreach (var a in args) psi.ArgumentList.Add(a);
        }
        else
        {
            if (!DotnetAvailable())
            {
                AppendLog("未检测到 dotnet，可在高级设置中指定 CLI 路径后直接运行。");
                return;
            }
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add("BBDown/BBDown.csproj");
            psi.ArgumentList.Add("--");
            foreach (var a in args) psi.ArgumentList.Add(a);
        }

        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await Task.Run(() =>
        {
            while (!p.HasExited)
            {
                if (token.IsCancellationRequested)
                {
                    try { p.Kill(true); } catch { }
                    break;
                }
                Thread.Sleep(100);
            }
        });
    }

    private void AppendLog(string line)
    {
        var s = LogBox.Text ?? string.Empty;
        s += line + Environment.NewLine;
        LogBox.Text = s;
    }

    private async Task TestBinary(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = new Process { StartInfo = psi };
            p.Start();
            var stdout = await p.StandardOutput.ReadToEndAsync();
            var stderr = await p.StandardError.ReadToEndAsync();
            p.WaitForExit();
            if (!string.IsNullOrWhiteSpace(stdout)) AppendLog(stdout);
            if (!string.IsNullOrWhiteSpace(stderr)) AppendLog(stderr);
        }
        catch (Exception ex)
        {
            AppendLog(ex.Message);
        }
    }

    private void ApplyBestPreset()
    {
        EncodingPriorityBox.Text = "hevc,av1,avc";
        DfnPriorityBox.Text = "杜比视界,HDR 真彩,8K 超高清,4K 超清,1080P 高码率,1080P 高清,720P 高清,480P 清晰,360P 流畅";
        UseMP4boxBox.IsChecked = false;
        MultiThreadBox.IsChecked = true;
        UseAria2cBox.IsChecked = true;
        if (string.IsNullOrWhiteSpace(Aria2cArgsBox.Text)) Aria2cArgsBox.Text = "-x16 -s16 -j16 -k5M";
        AppendLog("已应用最高画质预设");
    }

    private async Task<string?> PickFileAsync(string title)
    {
        var dlg = new OpenFileDialog { Title = title, AllowMultiple = false };
        var res = await dlg.ShowAsync(this);
        return res != null && res.Length > 0 ? res[0] : null;
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var dlg = new OpenFolderDialog { Title = title };
        return await dlg.ShowAsync(this);
    }

    private bool DotnetAvailable()
    {
        try
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.WaitForExit(2000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}