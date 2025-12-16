using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System.Diagnostics;

namespace Services;

public class ScheduleService : IHostedService
{
    private readonly ILogger<ScheduleService> _logger;
    private readonly IStoryGenerator _storyGenerator;
    private readonly IVideoGenerator _videoGenerator;
    private readonly IBilibiliUploader _bilibiliUploader;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly IScheduler _scheduler;

    public ScheduleService(
        ILogger<ScheduleService> logger,
        IStoryGenerator storyGenerator,
        IVideoGenerator videoGenerator,
        IBilibiliUploader bilibiliUploader,
        IOptions<AppConfig> appConfig,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _storyGenerator = storyGenerator;
        _videoGenerator = videoGenerator;
        _bilibiliUploader = bilibiliUploader;
        _appConfig = appConfig;
        _scheduler = schedulerFactory.GetScheduler().Result;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("定时任务服务启动");

        // 创建每日生成任务
        var dailyJob = JobBuilder.Create<DailyStoryJob>()
            .WithIdentity("daily-story-job", "video-generation")
            .Build();

        // 设置触发时间 - 每天9点执行
        var scheduleTime = _appConfig.Value.ScheduleTime;
        var cronExpression = $"{scheduleTime.Second} {scheduleTime.Minute} {scheduleTime.Hour} * * ?";

        var trigger = TriggerBuilder.Create()
            .WithIdentity("daily-story-trigger", "video-generation")
            .WithCronSchedule(cronExpression)
            .Build();

        // 将需要的实例传递给Job
        var jobDataMap = new JobDataMap
        {
            ["StoryGenerator"] = _storyGenerator,
            ["VideoGenerator"] = _videoGenerator,
            ["BilibiliUploader"] = _bilibiliUploader,
            ["AppConfig"] = _appConfig.Value
        };

        dailyJob.JobDataMap = jobDataMap;

        await _scheduler.ScheduleJob(dailyJob, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        _logger.LogInformation("定时任务已设置：每天 {Time} 执行", scheduleTime);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("定时任务服务停止");
        await _scheduler.Shutdown(cancellationToken);
    }
}

[DisallowConcurrentExecution]
public class DailyStoryJob : IJob
{
    private readonly ILogger<DailyStoryJob> _logger;

    public DailyStoryJob()
    {
        // 注意：这里不能通过构造函数注入，需要通过JobDataMap传递
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DailyStoryJob>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("开始执行定时任务");

            // 获取传递的实例
            var dataMap = context.MergedJobDataMap;
            var storyGenerator = (IStoryGenerator)dataMap["StoryGenerator"];
            var videoGenerator = (IVideoGenerator)dataMap["VideoGenerator"];
            var bilibiliUploader = (IBilibiliUploader)dataMap["BilibiliUploader"];
            var appConfig = (AppConfig)dataMap["AppConfig"];

            // 1. 生成故事
            _logger.LogInformation("步骤1：生成爽文内容");
            var story = await storyGenerator.GenerateStoryWithDefaultAsync();
            _logger.LogInformation("爽文生成完成：{Title}", story.Title);

            // 2. 生成视频
            _logger.LogInformation("步骤2：生成视频");
            var outputPath = Path.Combine(appConfig.OutputPath, $"auto_{story.Id}");
            Directory.CreateDirectory(outputPath);

            var videoPath = await videoGenerator.CreateVideoAsync(story, appConfig.VideoConfig, outputPath);
            _logger.LogInformation("视频生成完成：{Path}", videoPath);

            // 3. 上传到B站（如果启用）
            if (appConfig.EnableAutoUpload)
            {
                _logger.LogInformation("步骤3：上传到B站");
                try
                {
                    var uploadResult = await bilibiliUploader.UploadVideoAsync(
                        videoPath,
                        story,
                        appConfig.UploadConfig);

                    if (uploadResult.Success)
                    {
                        _logger.LogInformation("上传成功：{Url}", uploadResult.Url);

                        // 发送通知
                        await SendNotification($"爽文视频自动生成并上传成功！\n标题：{story.Title}\n链接：{uploadResult.Url}");
                    }
                    else
                    {
                        _logger.LogError("上传失败：{Message}", uploadResult.Message);
                        await SendNotification($"爽文视频生成成功但上传失败：{uploadResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "上传到B站失败");
                    await SendNotification($"爽文视频生成成功但上传异常：{ex.Message}");
                }
            }
            else
            {
                _logger.LogInformation("自动上传未启用");
                await SendNotification($"爽文视频生成成功！\n标题：{story.Title}\n路径：{videoPath}");
            }

            // 4. 清理临时文件
            await CleanupTempFiles(outputPath);

            _logger.LogInformation("定时任务执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时任务执行失败");
            await SendNotification($"爽文视频生成失败：{ex.Message}");
        }
    }

    private async Task SendNotification(string message)
    {
        try
        {
            // 这里可以发送邮件、钉钉、微信等通知
            // 简单起见，只记录日志
            _logger.LogInformation("通知：{Message}", message);

            // 可以使用Windows通知
            if (OperatingSystem.IsWindows())
            {
                // 发送Windows通知（需要额外的库支持）
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送通知失败");
        }
    }

    private async Task CleanupTempFiles(string outputPath)
    {
        try
        {
            // 保留最近7天的文件
            var cutoffDate = DateTime.Now.AddDays(-7);
            var di = new DirectoryInfo(outputPath);

            foreach (var file in di.GetFiles("*.*", SearchOption.AllDirectories))
            {
                if (file.CreationTime < cutoffDate)
                {
                    file.Delete();
                }
            }

            foreach (var dir in di.GetDirectories("*.*", SearchOption.AllDirectories))
            {
                if (dir.CreationTime < cutoffDate && !dir.GetFiles().Any())
                {
                    dir.Delete();
                }
            }

            _logger.LogInformation("临时文件清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理临时文件失败");
        }
    }
}

// Quartz扩展方法
public static class ServiceCollectionQuartzConfiguratorExtensions
{
    public static void AddJobAndTrigger<T>(
        this IServiceCollectionQuartzConfigurator quartz,
        IConfiguration config,
        string jobName,
        string cronExpression) where T : IJob
    {
        var jobKey = new JobKey(jobName);
        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobName + "-trigger")
            .WithCronSchedule(cronExpression));
    }
}