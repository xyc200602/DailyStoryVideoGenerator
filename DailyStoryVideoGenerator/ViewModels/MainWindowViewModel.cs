using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;
using DailyStoryVideoGenerator.Services;
using Microsoft.Extensions.Logging;

namespace DailyStoryVideoGenerator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IStoryGenerator _storyGenerator;
    private readonly IVideoGenerator _videoGenerator;
    private readonly IBilibiliUploader _bilibiliUploader;
    private readonly VideoGenerationService _videoGenerationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private StoryConfig _storyConfig = new();

    [ObservableProperty]
    private VideoConfig _videoConfig = new();

    [ObservableProperty]
    private GeneratedStory? _generatedStory;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showProgress;

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    [ObservableProperty]
    private bool _showResult;

    [ObservableProperty]
    private bool _enableSchedule;

    [ObservableProperty]
    private TimeSpan _scheduleTime = new TimeSpan(9, 0, 0);

    [ObservableProperty]
    private string _scheduleType = "每天";

    [ObservableProperty]
    private string _scheduleStatus = "定时任务未启用";

    [ObservableProperty]
    private DateTime? _nextRunTime;

    [ObservableProperty]
    private DateTime? _lastRunTime;

    private string _keywordsText = string.Empty;
    public string KeywordsText
    {
        get => _keywordsText;
        set
        {
            SetProperty(ref _keywordsText, value);
            StoryConfig.Keywords = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(k => k.Trim())
                                      .Where(k => !string.IsNullOrEmpty(k))
                                      .ToList();
        }
    }

    public ObservableCollection<string> StoryTypes { get; } = new()
    {
        "爽文", "玄幻", "都市", "修仙", "武侠", "科幻", "历史", "游戏"
    };

    public ObservableCollection<string> Settings { get; } = new()
    {
        "现代都市", "玄幻世界", "古代江湖", "校园生活", "办公室", "异世界", "修仙界", "未来科技"
    };

    public ObservableCollection<string> Styles { get; } = new()
    {
        "热血沸腾", "轻松搞笑", "深情款款", "悬疑紧张", "震撼人心", "感人肺腑", "刺激爽快", "温馨治愈"
    };

    public ObservableCollection<string> VoiceTypes { get; } = new()
    {
        { "xiaoxiao - 温柔女声" },
        { "xiaoyan - 成熟女声" },
        { "yunjian - 成熟男声" },
        { "yunxi - 年轻男声" },
        { "xiaochen - 活泼女声" },
        { "xiaohan - 甜美女声" },
        { "xiaomeng - 可爱女声" },
        { "xiaomo - 温和男声" },
        { "xiaoxuan - 优雅女声" },
        { "xiaoyou - 磁性男声" }
    };

    public ObservableCollection<string> ScheduleTypes { get; } = new()
    {
        "每天", "每周", "每月", "工作日", "周末"
    };

    public MainWindowViewModel(
        IStoryGenerator storyGenerator,
        IVideoGenerator videoGenerator,
        IBilibiliUploader bilibiliUploader,
        VideoGenerationService videoGenerationService,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _storyGenerator = storyGenerator;
        _videoGenerator = videoGenerator;
        _bilibiliUploader = bilibiliUploader;
        _videoGenerationService = videoGenerationService;
        _dialogService = dialogService;
        _logger = logger;

        // 初始化默认值
        StoryConfig.Keywords = new List<string> { "逆袭", "打脸", "系统", "美女总裁", "神豪" };
        KeywordsText = string.Join(", ", StoryConfig.Keywords);
    }

    [RelayCommand]
    private async Task GenerateStoryAsync()
    {
        try
        {
            IsBusy = true;
            ShowProgress = true;
            ShowResult = false;
            StatusMessage = "正在生成爽文...";
            ProgressMessage = "调用AI生成内容...";
            ProgressPercent = 20;

            GeneratedStory = await _storyGenerator.GenerateStoryAsync(StoryConfig);

            ProgressMessage = "爽文生成完成！";
            ProgressPercent = 100;
            StatusMessage = $"成功生成爽文：{GeneratedStory.Title}";

            await Task.Delay(1000); // 显示成功消息

            ShowProgress = false;
            ShowResult = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成爽文失败");
            StatusMessage = $"生成失败：{ex.Message}";
            ShowProgress = false;

            await _dialogService.ShowErrorAsync($"生成爽文失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateVideoAsync()
    {
        if (GeneratedStory == null)
        {
            await _dialogService.ShowWarningAsync("提示", "请先生成爽文内容");
            return;
        }

        try
        {
            IsBusy = true;
            ShowProgress = true;
            StatusMessage = "正在生成视频...";
            ProgressPercent = 10;
            ProgressMessage = "准备生成环境...";

            var outputPath = await _videoGenerationService.GenerateVideoAsync(
                GeneratedStory,
                VideoConfig,
                (percent, message) =>
                {
                    ProgressPercent = percent;
                    ProgressMessage = message;
                });

            ProgressPercent = 100;
            ProgressMessage = "视频生成完成！";
            StatusMessage = $"视频已保存到：{outputPath}";

            await Task.Delay(2000);

            ShowProgress = false;

            // 询问是否打开输出文件夹
            var result = await _dialogService.ShowQuestionAsync("提示", "视频生成完成，是否打开输出文件夹？");
            if (result)
            {
                System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(outputPath)!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成视频失败");
            StatusMessage = $"生成视频失败：{ex.Message}";
            ShowProgress = false;

            await _dialogService.ShowErrorAsync("生成视频失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UploadVideoAsync()
    {
        if (GeneratedStory == null)
        {
            await _dialogService.ShowWarningAsync("提示", "请先生成爽文内容和视频");
            return;
        }

        try
        {
            IsBusy = true;
            ShowProgress = true;
            StatusMessage = "正在上传到B站...";
            ProgressPercent = 20;
            ProgressMessage = "连接B站服务器...";

            // 这里应该从配置中获取上传配置，简化处理
            var uploadConfig = new UploadConfig();

            ProgressPercent = 50;
            ProgressMessage = "上传视频中...";

            var result = await _bilibiliUploader.UploadVideoAsync(
                "output/generated_video.mp4", // 实际应该是生成的视频路径
                GeneratedStory,
                uploadConfig);

            if (result.Success)
            {
                ProgressPercent = 100;
                ProgressMessage = "上传成功！";
                StatusMessage = $"视频已上传：{result.Url}";

                await Task.Delay(2000);

                // 询问是否打开视频页面
                var openResult = await _dialogService.ShowQuestionAsync("上传成功", $"视频已成功上传到B站！\n\n是否打开视频页面？");
                if (openResult)
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = result.Url,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                throw new InvalidOperationException(result.Message);
            }
        }
        catch (Exception ex)
            {
                _logger.LogError(ex, "上传视频失败");
                StatusMessage = $"上传失败：{ex.Message}";
                ShowProgress = false;

                await _dialogService.ShowErrorAsync("上传失败", ex.Message);
            }
            finally
            {
                IsBusy = false;
                ShowProgress = false;
            }
        }

    [RelayCommand]
    private async Task SaveScheduleAsync()
    {
        try
        {
            if (EnableSchedule)
            {
                // 这里应该保存到配置并启动定时任务
                ScheduleStatus = $"定时任务已启用 - 每天{_scheduleTime:HH:mm}执行";
                NextRunTime = DateTime.Today.Add(_scheduleTime);
                if (NextRunTime < DateTime.Now)
                {
                    NextRunTime = NextRunTime.Value.AddDays(1);
                }

                StatusMessage = "定时任务设置已保存";
            }
            else
            {
                ScheduleStatus = "定时任务已禁用";
                NextRunTime = null;
                StatusMessage = "定时任务已禁用";
            }

            await _dialogService.ShowInfoAsync("提示", "定时任务设置已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存定时任务设置失败");
            await _dialogService.ShowErrorAsync("保存失败", ex.Message);
        }
    }

    [RelayCommand]
    private void NavigateToConfig()
    {
        // 导航到配置页面
        _dialogService.ShowInfoAsync("提示", "配置页面开发中...");
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        // 导航到历史记录页面
        _dialogService.ShowInfoAsync("提示", "历史记录页面开发中...");
    }

    [RelayCommand]
    private void ShowAbout()
    {
        _dialogService.ShowInfoAsync(
            "关于",
            "爽文视频自动生成器 v1.0\n\n" +
            "功能特性：\n" +
            "• AI自动生成爽文内容\n" +
            "• 智能语音合成\n" +
            "• 动画场景生成\n" +
            "• 视频自动合成\n" +
            "• B站自动上传\n" +
            "• 定时任务管理\n\n" +
            "技术栈：\n" +
            "WPF + .NET 8 + Azure AI + FFmpeg");
    }

    partial void OnStoryConfigChanged(StoryConfig value)
    {
        KeywordsText = string.Join(", ", value.Keywords);
    }

    partial void OnVideoConfigChanged(VideoConfig value)
    {
        // 处理视频配置变化
    }
}