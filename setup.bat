@echo off
chcp 65001 >nul
cls
echo.
echo ████████╗██╗  ██╗███████╗    ██████╗ ███████╗ █████╗ ████████╗
echo ╚══██╔══╝██║  ██║██╔════╝    ██╔══██╗██╔════╝██╔══██╗╚══██╔══╝
echo    ██║   ███████║█████╗      ██████╔╝█████╗  ███████║   ██║
echo    ██║   ██╔══██║██╔══╝      ██╔══██╗██╔══╝  ██╔══██║   ██║
echo    ██║   ██║  ██║███████╗    ██████╔╝███████╗██║  ██║   ██║
echo    ╚═╝   ╚═╝  ╚═╝╚══════╝    ╚═════╝ ╚══════╝╚═╝  ╚═╝   ╚═╝
echo.
echo                 爽文视频自动生成器 - 一键配置工具
echo                 ================================
echo.

:: 检查管理员权限
net session >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo 提示：建议以管理员身份运行此脚本
    echo.
)

echo 正在检查系统环境...
echo.

:: 检查.NET
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ 未检测到.NET 8.0
    echo 正在打开下载页面，请下载并安装.NET 8.0 Desktop Runtime
    start https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo ✅ .NET Runtime: %DOTNET_VERSION%
)

:: 检查FFmpeg
ffmpeg -version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ 未检测到FFmpeg
    echo.
    echo 选择FFmpeg安装方式：
    echo 1. 自动下载安装（推荐）
    echo 2. 手动安装（我已安装）
    echo 3. 跳过（稍后手动配置）
    echo.
    set /p ffmpeg_choice="请输入选择 (1-3): "

    if "%ffmpeg_choice%"=="1" (
        echo 正在下载FFmpeg...
        powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' -OutFile 'ffmpeg.zip'}"
        echo 正在解压...
        powershell -Command "Expand-Archive -Path 'ffmpeg.zip' -DestinationPath '.' -Force"
        set FFMPEG_PATH=%cd%\ffmpeg-release-essentials\bin
        setx PATH "%PATH%;%FFMPEG_PATH%" /M
        del ffmpeg.zip
        echo ✅ FFmpeg已安装并添加到PATH
    ) else if "%ffmpeg_choice%"=="3" (
        echo ⚠️  请稍后手动安装FFmpeg并添加到PATH
    )
) else (
    echo ✅ FFmpeg已安装
)

:: 创建配置
echo.
echo ===============================
echo        配置API密钥
echo ===============================
echo.

:: 获取OpenAI API密钥
echo [1/3] OpenAI API配置
echo 请访问: https://platform.openai.com/api-keys
echo.
set /p OPENAI_KEY="请输入OpenAI API密钥: "
if "%OPENAI_KEY%"=="" (
    echo ⚠️  OpenAI API密钥为空，稍后需要在配置文件中填写
) else (
    echo ✅ OpenAI API密钥已设置
)

:: 获取Azure Speech密钥
echo.
echo [2/3] Azure语音服务配置
echo 请访问: https://portal.azure.com
echo 创建"语音服务"资源，获取密钥和区域
echo.
set /p AZURE_KEY="请输入Azure Speech密钥: "
set /p AZURE_REGION="请输入Azure区域 (默认: eastasia): "
if "%AZURE_REGION%"=="" set AZURE_REGION=eastasia
if "%AZURE_KEY%"=="" (
    echo ⚠️  Azure Speech密钥为空，稍后需要在配置文件中填写
) else (
    echo ✅ Azure Speech配置完成
)

:: B站配置
echo.
echo [3/3] B站配置（可选）
echo 如需自动上传功能，请配置B站Cookie
echo.
set /p BILIBILI_CONFIG="是否配置B站上传? (y/n): "
if /i "%BILIBILI_CONFIG%"=="y" (
    echo.
    echo 请按以下步骤获取B站Cookie：
    echo 1. 登录 https://www.bilibili.com
    echo 2. 按F12打开开发者工具
    echo 3. 刷新页面，在Network标签找到任意请求
    echo 4. 复制请求头中的Cookie值
    echo.
    set /p BILIBILI_COOKIE="请输入Cookie值: "

    echo.
    echo 请查找Cookie中的 "bili_jct" 值：
    set /p BILIBILI_CSRF="请输入bili_jct值: "

    if "%BILIBILI_COOKIE%"=="" (
        echo ⚠️  B站Cookie为空
    ) else (
        echo ✅ B站配置完成
    )
) else (
    echo ⚠️  跳过B站配置
)

:: 生成配置文件
echo.
echo 正在生成配置文件...

(
echo {
echo   "AppConfig": {
echo     "OpenAIApiKey": "%OPENAI_KEY%",
echo     "OpenAIEndpoint": "https://api.openai.com/",
echo     "OpenAIDeploymentName": "gpt-3.5-turbo",
echo     "AzureSpeechKey": "%AZURE_KEY%",
echo     "AzureSpeechRegion": "%AZURE_REGION%",
echo     "OutputPath": "output",
echo     "EnableAutoUpload": true,
echo     "ScheduleTime": "09:00:00",
echo     "StoryConfig": {
echo       "UseCustomPrompt": false,
echo       "CustomPrompt": "",
echo       "StoryType": "爽文",
echo       "WordCount": 2000,
echo       "Style": "热血沸腾",
echo       "ProtagonistName": "叶凡",
echo       "Setting": "现代都市",
echo       "Keywords": ["逆袭", "打脸", "系统", "美女总裁", "神豪"]
echo     },
echo     "VideoConfig": {
echo       "BackgroundMusic": "",
echo       "VoiceType": "xiaoxiao",
echo       "VoiceSpeed": 1.0,
echo       "AnimationStyle": "dynamic",
echo       "VideoWidth": 1920,
echo       "VideoHeight": 1080,
echo       "FramesPerSecond": 30
echo     },
echo     "UploadConfig": {
echo       "BilibiliCookie": "%BILIBILI_COOKIE%",
echo       "BilibiliCsrf": "%BILIBILI_CSRF%",
echo       "DefaultTitle": "今日爽文推荐",
echo       "DefaultDescription": "每日更新精彩爽文，配有配音动画",
echo       "Tags": ["爽文", "小说", "配音", "动画"],
echo       "Category": "文学",
echo       "Public": true
echo     }
echo   },
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information",
echo       "Microsoft": "Warning",
echo       "Microsoft.Hosting.Lifetime": "Information"
echo     }
echo   }
echo }
) > DailyStoryVideoGenerator\appsettings.json

echo ✅ 配置文件已生成

:: 安装项目依赖
echo.
echo ===============================
echo        安装项目依赖
echo ===============================
echo.
echo 正在还原NuGet包...
cd DailyStoryVideoGenerator
dotnet restore

if %ERRORLEVEL% neq 0 (
    echo ❌ NuGet包还原失败
    pause
    exit /b 1
)

echo ✅ 依赖安装完成

:: 编译项目
echo.
echo 正在编译项目...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo ❌ 项目编译失败
    pause
    exit /b 1
)

echo ✅ 项目编译成功

:: 创建桌面快捷方式
echo.
echo 是否创建桌面快捷方式? (y/n)
set /p shortcut_choice=
if /i "%shortcut_choice%"=="y" (
    powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\爽文视频生成器.lnk'); $Shortcut.TargetPath = '%cd%\bin\Release\net8.0-windows\DailyStoryVideoGenerator.exe'; $Shortcut.Save()"
    echo ✅ 桌面快捷方式已创建
)

:: 完成提示
echo.
echo ===============================
echo        配置完成！
echo ===============================
echo.
echo 🎉 恭喜！配置已完成
echo.
echo 使用方法：
echo 1. 双击桌面快捷方式启动程序
echo 2. 或运行: DailyStoryVideoGenerator\bin\Release\net8.0-windows\DailyStoryVideoGenerator.exe
echo.
echo 📁 重要目录：
echo - 输出视频: output\
echo - 日志文件: logs\
echo - 背景图片: backgrounds\
echo.
echo 💡 提示：
echo - 首次生成需要下载依赖，请保持网络连接
echo - 可以在程序中调整各项参数
echo - 建议先手动生成测试一次
echo.
echo 按任意键启动程序...
pause >nul

start "" "bin\Release\net8.0-windows\DailyStoryVideoGenerator.exe"