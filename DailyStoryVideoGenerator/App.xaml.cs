using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using Core.Interfaces;
using Services;
using Core.Models;
using DailyStoryVideoGenerator.ViewModels;
using DailyStoryVideoGenerator.Services;
using DailyStoryVideoGenerator.Views;
using Quartz;

namespace DailyStoryVideoGenerator;

public partial class App : Application
{
    private IHost? Host { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 配置服务
                services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));

                // Quartz服务
                services.AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();
                    q.UseSimpleTypeLoader();
                    q.UseInMemoryStore();
                });

                // 核心服务
                services.AddSingleton<IStoryGenerator, StoryGenerator>();
                services.AddSingleton<ITextToSpeech, AzureTextToSpeech>();
                services.AddSingleton<IAnimationGenerator, AnimationGenerator>();
                services.AddSingleton<IVideoGenerator, VideoGenerator>();
                services.AddSingleton<IBilibiliUploader, BilibiliUploader>();

                // 应用服务
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<VideoGenerationService>();
                services.AddHostedService<ScheduleService>();

                // ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<ConfigViewModel>();
                services.AddTransient<HistoryViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<ConfigView>();
                services.AddTransient<HistoryView>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
                logging.AddFile("logs/app-.log", rollingInterval: RollingInterval.Day);
            });

        Host = builder.Build();

        // 显示主窗口
        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host?.Dispose();
        base.OnExit(e);
    }
}