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
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Collections.Specialized;
using BBDown.Core.Util;
using QRCoder;
using static BBDown.BBDownUtil;

namespace BBDown.GUI;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _loginCts;
    private string _ffmpegPath = "";
    private string _mp4boxPath = "";
    private string _aria2cPath = "";

    public MainWindow()
    {
        InitializeComponent();
        ApiTypeBox.SelectedIndex = 0;
        StartButton.Click += OnStart;
        StopButton.Click += OnStop;
        
        BrowseWorkDirButton.Click += async (_, __) => WorkDirBox.Text = await PickFolderAsync("选择工作目录") ?? WorkDirBox.Text;

        // 默认启用最高画质相关设置
        UseAria2cBox.IsChecked = true;
        if (string.IsNullOrWhiteSpace(Aria2cArgsBox.Text)) Aria2cArgsBox.Text = "-x16 -s16 -j16 -k5M";

        // 优先使用 AppContext.BaseDirectory (程序所在目录) 以支持打包后的场景
        // 备选使用 Environment.CurrentDirectory (当前工作目录) 以支持开发环境直接运行
        var searchDirs = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        
        foreach (var dir in searchDirs)
        {
            if (string.IsNullOrEmpty(_ffmpegPath))
            {
                var fp = Path.Combine(dir, "ffmpeg");
                if (File.Exists(fp)) _ffmpegPath = fp;
            }
            
            if (string.IsNullOrEmpty(_mp4boxPath))
            {
                var mp = Path.Combine(dir, "MP4Box");
                if (File.Exists(mp)) _mp4boxPath = mp;
            }
            
            if (string.IsNullOrEmpty(_aria2cPath))
            {
                var ap = Path.Combine(dir, "aria2c");
                if (File.Exists(ap)) _aria2cPath = ap;
            }
        }
    }

    private async void LoginWeb_Click(object? sender, RoutedEventArgs e)
    {
        await LoginAsync(false);
    }

    private async void LoginTV_Click(object? sender, RoutedEventArgs e)
    {
        await LoginAsync(true);
    }

    private async Task LoginAsync(bool isTv)
    {
        try { _loginCts?.Cancel(); } catch { }
        _loginCts = new CancellationTokenSource();
        var token = _loginCts.Token;

        QrCodePanel.IsVisible = true;
        QrCodeStatusLabel.Text = "正在获取登录地址...";
        QrCodeImage.Source = null;

        try
        {
            if (isTv)
            {
                 // TV Login Logic
                 string loginUrl = "https://passport.snm0516.aisee.tv/x/passport-tv-login/qrcode/auth_code";
                 string pollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
                 var parms = GetTVLoginParms();
                 var dict = parms.AllKeys.ToDictionary(k => k!, k => parms[k]!);
                 
                 var response = await (await HTTPUtil.AppHttpClient.PostAsync(loginUrl, new FormUrlEncodedContent(dict), token)).Content.ReadAsByteArrayAsync(token);
                 string web = Encoding.UTF8.GetString(response);
                 var json = JsonDocument.Parse(web);
                 string url = json.RootElement.GetProperty("data").GetProperty("url").ToString();
                 string authCode = json.RootElement.GetProperty("data").GetProperty("auth_code").ToString();

                 DisplayQrCode(url);
                 QrCodeStatusLabel.Text = "请使用 Bilibili App 扫码登录 (TV)";

                 parms.Set("auth_code", authCode);
                 parms.Set("ts", GetTimeStamp(true));
                 parms.Remove("sign");
                 parms.Add("sign", GetSign(ToQueryString(parms)));
                 dict = parms.AllKeys.ToDictionary(k => k!, k => parms[k]!);

                 while (!token.IsCancellationRequested)
                 {
                     await Task.Delay(1500, token);
                     var pollResp = await (await HTTPUtil.AppHttpClient.PostAsync(pollUrl, new FormUrlEncodedContent(dict), token)).Content.ReadAsByteArrayAsync(token);
                     string pollWeb = Encoding.UTF8.GetString(pollResp);
                     var pollJson = JsonDocument.Parse(pollWeb);
                     string code = pollJson.RootElement.GetProperty("code").ToString();

                     if (code == "0")
                     {
                         string accessToken = pollJson.RootElement.GetProperty("data").GetProperty("access_token").ToString();
                         TokenBox.Text = accessToken;
                         await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data"), "access_token=" + accessToken, token);
                         QrCodeStatusLabel.Text = "登录成功!";
                         await Task.Delay(1500, token);
                         QrCodePanel.IsVisible = false;
                         break;
                     }
                     else if (code == "86038")
                     {
                         QrCodeStatusLabel.Text = "二维码已过期，请重新点击登录";
                         break;
                     }
                 }
            }
            else
            {
                // Web Login Logic
                string loginUrl = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate?source=main-fe-header";
                string web = await HTTPUtil.GetWebSourceAsync(loginUrl);
                string url = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("url").ToString();
                string qrcodeKey = GetQueryString("qrcode_key", url);

                DisplayQrCode(url);
                QrCodeStatusLabel.Text = "请使用 Bilibili App 扫码登录 (Web)";

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1500, token);
                    string queryUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}&source=main-fe-header";
                    string w = await HTTPUtil.GetWebSourceAsync(queryUrl);
                    
                    int code = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("code").GetInt32();
                    if (code == 0)
                    {
                        string cc = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("url").ToString();
                        string cookieStr = cc[(cc.IndexOf('?') + 1)..].Replace("&", ";").Replace(",", "%2C");
                        CookieBox.Text = cookieStr;
                         await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "BBDown.data"), cookieStr, token);
                         QrCodeStatusLabel.Text = "登录成功!";
                         await Task.Delay(1500, token);
                         QrCodePanel.IsVisible = false;
                         break;
                    }
                    else if (code == 86038)
                    {
                         QrCodeStatusLabel.Text = "二维码已过期，请重新点击登录";
                         break;
                    }
                    else if (code == 86090)
                    {
                        QrCodeStatusLabel.Text = "扫码成功, 请在手机上确认...";
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            QrCodeStatusLabel.Text = "登录出错: " + ex.Message;
        }
    }

    private void DisplayQrCode(string url)
    {
        QRCodeGenerator qrGenerator = new();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode pngByteCode = new(qrCodeData);
        byte[] bytes = pngByteCode.GetGraphic(20);
        using var stream = new MemoryStream(bytes);
        QrCodeImage.Source = new Avalonia.Media.Imaging.Bitmap(stream);
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
            FilePattern = "", // Removed per user request
            Language = "",
            UserAgent = "",
            Cookie = CookieBox.Text ?? "",
            AccessToken = TokenBox.Text ?? "",
            UseMP4box = UseMP4boxBox.IsChecked == true,
            EncodingPriority = "hevc,av1,avc", // Hardcoded Best Quality
            DfnPriority = "杜比视界,HDR 真彩,8K 超高清,4K 超清,1080P 高码率,1080P 高清,720P 高清,480P 清晰,360P 流畅", // Hardcoded Best Quality
            ShowAll = false, // Removed per user request
            SimplyMux = SimplyMuxBox.IsChecked == true,
            DanmakuOnly = DanmakuOnlyBox.IsChecked == true,
            CoverOnly = CoverOnlyBox.IsChecked == true,
            SubOnly = SubOnlyBox.IsChecked == true,
            SkipMux = SkipMuxBox.IsChecked == true,
            SkipSubtitle = SkipSubtitleBox.IsChecked == true,
            SkipCover = SkipCoverBox.IsChecked == true,
            ForceHttp = false, // Removed
            DownloadDanmaku = DownloadDanmakuBox.IsChecked == true,
            DownloadDanmakuFormats = DanmakuFormatsBox.Text ?? "",
            SkipAi = SkipAiBox.IsChecked == true,
            VideoAscending = false, // Removed
            AudioAscending = false, // Removed
            AllowPcdn = false, // Removed
            MultiFilePattern = "", // Removed
            WorkDir = WorkDirBox.Text ?? "",
            DelayPerPage = "0", // Default
            FFmpegPath = _ffmpegPath,
            Mp4boxPath = _mp4boxPath,
            Aria2cPath = _aria2cPath,
            UposHost = "", // Removed
            ForceReplaceHost = false, // Removed
            SaveArchivesToFile = false, // Removed
            Host = "", // Removed
            EpHost = "", // Removed
            TvHost = "", // Removed
            Area = "", // Removed
            ConfigFile = null, // Removed
            Debug = false // Removed
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
        // Removed CliPathBox reference, assume default or bundled
        var bundledCli = FindBundledCli();
        var useBundled = bundledCli != null && File.Exists(bundledCli);
        // Fallback to dotnet run if bundled not found, ignoring CliPathBox as it's removed
        var psi = new ProcessStartInfo
        {
            FileName = useBundled ? bundledCli! : "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (useBundled)
        {
            foreach (var a in args) psi.ArgumentList.Add(a);
        }
        else
        {
            if (!DotnetAvailable())
            {
                AppendLog("未检测到 dotnet，请确保安装了 .NET SDK 或使用打包版。");
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
            LogBox.CaretIndex = int.MaxValue;
        });
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
