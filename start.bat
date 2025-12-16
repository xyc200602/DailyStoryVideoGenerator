@echo off
echo 爽文视频自动生成器
echo ==================

REM 检查.NET运行时
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo 错误：未检测到.NET 8.0运行时
    echo 请访问 https://dotnet.microsoft.com/download 安装.NET 8.0
    pause
    exit /b 1
)

REM 检查FFmpeg
ffmpeg -version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo 警告：未检测到FFmpeg
    echo 请确保FFmpeg已安装并添加到系统PATH
    echo 下载地址：https://ffmpeg.org/download.html
    echo.
)

REM 创建必要目录
if not exist "output" mkdir output
if not exist "logs" mkdir logs
if not exist "backgrounds" mkdir backgrounds

REM 运行程序
echo 启动应用程序...
cd DailyStoryVideoGenerator
dotnet run

pause