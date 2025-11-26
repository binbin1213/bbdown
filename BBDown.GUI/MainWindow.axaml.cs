using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using BBDown.Core;

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
        var bbDownArgs = BuildBBDownArguments();
        var stringArgs = ConvertArgumentsToStringList(bbDownArgs);
        AppendLog(string.Join(' ', stringArgs));
        await RunCliAsync(stringArgs, _cts.Token);
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        StopRunning();
    }

    private void StopRunning()
    {
        try { _cts?.Cancel(); } catch { }
    }

    private BBDownArguments BuildBBDownArguments()
    {
        var args = new BBDownArguments
        {
            Url = UrlBox.Text ?? string.Empty,
            UseTvApi = (ApiTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "TV",
            UseAppApi = (ApiTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "APP",
            UseIntlApi = (ApiTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "INTL",
            OnlyShowInfo = OnlyInfoBox.IsChecked == true,
            MultiThread = MultiThreadBox.IsChecked == true,
            VideoOnly = VideoOnlyBox.IsChecked == true,
            AudioOnly = AudioOnlyBox.IsChecked == true,
            UseAria2c = UseAria2cBox.IsChecked == true,
            Aria2cArgs = Aria2cArgsBox.Text ?? "",
            SelectPage = SelectPageBox.Text ?? "",
            FilePattern = FilePatternBox.Text ?? "",
            Language = LanguageBox.Text ?? "",
            UserAgent = "",
            Cookie = CookieBox.Text ?? "",
            AccessToken = TokenBox.Text ?? "",
            UseMP4box = UseMP4boxBox.IsChecked == true,
            EncodingPriority = EncodingPriorityBox.Text ?? "",
            DfnPriority = DfnPriorityBox.Text ?? "",
            ShowAll = ShowAllBox.IsChecked == true,
            SimplyMux = SimplyMuxBox.IsChecked == true,
            DanmakuOnly = DanmakuOnlyBox.IsChecked == true,
            CoverOnly = CoverOnlyBox.IsChecked == true,
            SubOnly = SubOnlyBox.IsChecked == true,
            SkipMux = SkipMuxBox.IsChecked == true,
            SkipSubtitle = SkipSubtitleBox.IsChecked == true,
            SkipCover = SkipCoverBox.IsChecked == true,
            ForceHttp = ForceHttpBox.IsChecked == true,
            DownloadDanmaku = DownloadDanmakuBox.IsChecked == true,
            DownloadDanmakuFormats = DanmakuFormatsBox.Text ?? "",
            SkipAi = SkipAiBox.IsChecked == true,
            VideoAscending = VideoAscendingBox.IsChecked == true,
            AudioAscending = AudioAscendingBox.IsChecked == true,
            AllowPcdn = AllowPcdnBox.IsChecked == true,
            MultiFilePattern = MultiFilePatternBox.Text ?? "",
            WorkDir = WorkDirBox.Text ?? "",
            DelayPerPage = DelayPerPageBox.Text ?? "0",
            FFmpegPath = FFmpegPathBox.Text ?? "",
            Mp4boxPath = Mp4boxPathBox.Text ?? "",
            Aria2cPath = Aria2cPathBox.Text ?? "",
            UposHost = UposHostBox.Text ?? "",
            ForceReplaceHost = ForceReplaceHostBox.IsChecked == true,
            SaveArchivesToFile = SaveArchivesToFileBox.IsChecked == true,
            Host = HostBox.Text ?? "",
            EpHost = EpHostBox.Text ?? "",
            TvHost = TvHostBox.Text ?? "",
            Area = AreaBox.Text ?? "",
            ConfigFile = string.IsNullOrWhiteSpace(ConfigFileBox.Text) ? null : ConfigFileBox.Text,
            Debug = DebugBox.IsChecked == true
        };

        return args;
    }

    private List<string> ConvertArgumentsToStringList(BBDownArguments args)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(args.Url)) list.Add(args.Url);
        if (args.UseTvApi) list.Add("-tv");
        if (args.UseAppApi) list.Add("-app");
        if (args.UseIntlApi) list.Add("-intl");
        if (args.OnlyShowInfo) list.Add("-info");
        if (args.MultiThread) list.Add("-mt");
        if (args.VideoOnly) list.Add("--video-only");
        if (args.AudioOnly) list.Add("--audio-only");
        if (args.UseAria2c) list.Add("--use-aria2c");
        if (!string.IsNullOrWhiteSpace(args.Aria2cArgs)) { list.Add("--aria2c-args"); list.Add(args.Aria2cArgs); }
        if (!string.IsNullOrWhiteSpace(args.SelectPage)) { list.Add("-p"); list.Add(args.SelectPage); }
        if (!string.IsNullOrWhiteSpace(args.FilePattern)) { list.Add("-F"); list.Add(args.FilePattern); }
        if (!string.IsNullOrWhiteSpace(args.Language)) { list.Add("--language"); list.Add(args.Language); }
        if (!string.IsNullOrWhiteSpace(args.UserAgent)) { list.Add("-ua"); list.Add(args.UserAgent); }
        if (!string.IsNullOrWhiteSpace(args.Cookie)) { list.Add("-c"); list.Add(args.Cookie); }
        if (!string.IsNullOrWhiteSpace(args.AccessToken)) { list.Add("-token"); list.Add(args.AccessToken); }
        if (args.UseMP4box) list.Add("--use-mp4box");
        if (!string.IsNullOrWhiteSpace(args.EncodingPriority)) { list.Add("-e"); list.Add(args.EncodingPriority); }
        if (!string.IsNullOrWhiteSpace(args.DfnPriority)) { list.Add("-q"); list.Add(args.DfnPriority); }
        if (args.ShowAll) list.Add("--show-all");
        if (args.SimplyMux) list.Add("--simply-mux");
        if (args.DanmakuOnly) list.Add("--danmaku-only");
        if (args.CoverOnly) list.Add("--cover-only");
        if (args.SubOnly) list.Add("--sub-only");
        if (args.SkipMux) list.Add("--skip-mux");
        if (args.SkipSubtitle) list.Add("--skip-subtitle");
        if (args.SkipCover) list.Add("--skip-cover");
        if (args.ForceHttp) list.Add("--force-http");
        if (args.DownloadDanmaku) list.Add("-dd");
        if (!string.IsNullOrWhiteSpace(args.DownloadDanmakuFormats)) { list.Add("--download-danmaku-formats"); list.Add(args.DownloadDanmakuFormats); }
        if (args.SkipAi) list.Add("--skip-ai");
        if (args.VideoAscending) list.Add("--video-ascending");
        if (args.AudioAscending) list.Add("--audio-ascending");
        if (args.AllowPcdn) list.Add("--allow-pcdn");
        if (!string.IsNullOrWhiteSpace(args.MultiFilePattern)) { list.Add("-M"); list.Add(args.MultiFilePattern); }
        if (!string.IsNullOrWhiteSpace(args.WorkDir)) { list.Add("--work-dir"); list.Add(args.WorkDir); }
        if (!string.IsNullOrWhiteSpace(args.DelayPerPage)) { list.Add("--delay-per-page"); list.Add(args.DelayPerPage); }
        if (!string.IsNullOrWhiteSpace(args.FFmpegPath)) { list.Add("--ffmpeg-path"); list.Add(args.FFmpegPath); }
        if (!string.IsNullOrWhiteSpace(args.Mp4boxPath)) { list.Add("--mp4box-path"); list.Add(args.Mp4boxPath); }
        if (!string.IsNullOrWhiteSpace(args.Aria2cPath)) { list.Add("--aria2c-path"); list.Add(args.Aria2cPath); }
        if (!string.IsNullOrWhiteSpace(args.UposHost)) { list.Add("--upos-host"); list.Add(args.UposHost); }
        if (args.ForceReplaceHost) list.Add("--force-replace-host");
        if (args.SaveArchivesToFile) list.Add("--save-archives-to-file");
        if (!string.IsNullOrWhiteSpace(args.Host)) { list.Add("--host"); list.Add(args.Host); }
        if (!string.IsNullOrWhiteSpace(args.EpHost)) { list.Add("--ep-host"); list.Add(args.EpHost); }
        if (!string.IsNullOrWhiteSpace(args.TvHost)) { list.Add("--tv-host"); list.Add(args.TvHost); }
        if (!string.IsNullOrWhiteSpace(args.Area)) { list.Add("--area"); list.Add(args.Area); }
        if (!string.IsNullOrWhiteSpace(args.ConfigFile)) { list.Add("--config-file"); list.Add(args.ConfigFile); }
        if (args.Debug) list.Add("--debug");
        return list;
    }

    private async Task RunCliAsync(List<string> args, CancellationToken token)
    {
        var cliPath = CliPathBox.Text;
        var bundledCli = FindBundledCli();
        var useBundled = bundledCli != null && File.Exists(bundledCli);
        var useCliExe = !useBundled && !string.IsNullOrWhiteSpace(cliPath) && File.Exists(cliPath);
        var psi = new ProcessStartInfo
        {
            FileName = useBundled ? bundledCli! : (useCliExe ? cliPath! : "dotnet"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (useBundled || useCliExe)
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

    private string? FindBundledCli()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var exeName = OperatingSystem.IsWindows() ? "BBDown.exe" : "BBDown";
            var p = Path.Combine(baseDir, exeName);
            if (File.Exists(p)) return p;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void AppendLog(string line)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => 
        {
            var s = LogBox.Text ?? string.Empty;
            s += line + Environment.NewLine;
            LogBox.Text = s;
        });
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
        if (StorageProvider is null) return null;
        var res = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = false });
        return res != null && res.Count > 0 ? res[0].Path.LocalPath : null;
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        if (StorageProvider is null) return null;
        var res = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return res != null && res.Count > 0 ? res[0].Path.LocalPath : null;
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