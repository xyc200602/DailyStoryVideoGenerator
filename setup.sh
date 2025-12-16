#!/bin/bash

# 爽文视频自动生成器 - Linux/Mac 配置脚本

echo "=========================================="
echo "   爽文视频自动生成器 - 一键配置工具"
echo "=========================================="
echo

# 检查操作系统
OS=$(uname -s)
echo "检测到操作系统: $OS"

# 检查.NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ 未检测到.NET 8.0"
    echo "请访问 https://dotnet.microsoft.com/download 安装.NET 8.0"
    exit 1
else
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET Runtime: $DOTNET_VERSION"
fi

# 检查FFmpeg
if ! command -v ffmpeg &> /dev/null; then
    echo "❌ 未检测到FFmpeg"
    echo "正在安装FFmpeg..."

    if [[ "$OS" == "Darwin" ]]; then
        # macOS
        if command -v brew &> /dev/null; then
            brew install ffmpeg
        else
            echo "请先安装Homebrew: /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            exit 1
        fi
    elif [[ "$OS" == "Linux" ]]; then
        # Linux
        if command -v apt-get &> /dev/null; then
            sudo apt-get update
            sudo apt-get install -y ffmpeg
        elif command -v yum &> /dev/null; then
            sudo yum install -y epel-release
            sudo yum install -y ffmpeg
        elif command -v dnf &> /dev/null; then
            sudo dnf install -y ffmpeg
        else
            echo "请手动安装FFmpeg"
            exit 1
        fi
    fi
else
    echo "✅ FFmpeg已安装"
fi

# 创建目录
mkdir -p output logs backgrounds

# 获取配置
echo
echo "=========================================="
echo "          配置API密钥"
echo "=========================================="
echo

# OpenAI API
echo "[1/3] OpenAI API配置"
echo "请访问: https://platform.openai.com/api-keys"
read -p "请输入OpenAI API密钥: " OPENAI_KEY

# Azure Speech
echo
echo "[2/3] Azure语音服务配置"
echo "请访问: https://portal.azure.com"
read -p "请输入Azure Speech密钥: " AZURE_KEY
read -p "请输入Azure区域 (默认: eastasia): " AZURE_REGION
AZURE_REGION=${AZURE_REGION:-eastasia}

# B站配置
echo
echo "[3/3] B站配置（可选）"
read -p "是否配置B站上传? (y/n): " BILIBILI_CONFIG

if [[ "$BILIBILI_CONFIG" == "y" || "$BILIBILI_CONFIG" == "Y" ]]; then
    echo
    echo "请按以下步骤获取B站Cookie："
    echo "1. 登录 https://www.bilibili.com"
    echo "2. 打开开发者工具"
    echo "3. 刷新页面，找到任意请求"
    echo "4. 复制请求头中的Cookie值"
    echo
    read -p "请输入Cookie值: " BILIBILI_COOKIE
    read -p "请输入bili_jct值: " BILIBILI_CSRF
fi

# 生成配置文件
echo
echo "正在生成配置文件..."

cat > DailyStoryVideoGenerator/appsettings.json << EOF
{
  "AppConfig": {
    "OpenAIApiKey": "$OPENAI_KEY",
    "OpenAIEndpoint": "https://api.openai.com/",
    "OpenAIDeploymentName": "gpt-3.5-turbo",
    "AzureSpeechKey": "$AZURE_KEY",
    "AzureSpeechRegion": "$AZURE_REGION",
    "OutputPath": "output",
    "EnableAutoUpload": true,
    "ScheduleTime": "09:00:00",
    "StoryConfig": {
      "UseCustomPrompt": false,
      "CustomPrompt": "",
      "StoryType": "爽文",
      "WordCount": 2000,
      "Style": "热血沸腾",
      "ProtagonistName": "叶凡",
      "Setting": "现代都市",
      "Keywords": ["逆袭", "打脸", "系统", "美女总裁", "神豪"]
    },
    "VideoConfig": {
      "BackgroundMusic": "",
      "VoiceType": "xiaoxiao",
      "VoiceSpeed": 1.0,
      "AnimationStyle": "dynamic",
      "VideoWidth": 1920,
      "VideoHeight": 1080,
      "FramesPerSecond": 30
    },
    "UploadConfig": {
      "BilibiliCookie": "$BILIBILI_COOKIE",
      "BilibiliCsrf": "$BILIBILI_CSRF",
      "DefaultTitle": "今日爽文推荐",
      "DefaultDescription": "每日更新精彩爽文，配有配音动画",
      "Tags": ["爽文", "小说", "配音", "动画"],
      "Category": "文学",
      "Public": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
EOF

echo "✅ 配置文件已生成"

# 安装依赖
echo
echo "=========================================="
echo "         安装项目依赖"
echo "=========================================="
echo

cd DailyStoryVideoGenerator
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ NuGet包还原失败"
    exit 1
fi

echo "✅ 依赖安装完成"

# 编译项目
echo
echo "正在编译项目..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ 项目编译失败"
    exit 1
fi

echo "✅ 项目编译成功"

# 创建启动脚本
cat > ../run.sh << 'EOF'
#!/bin/bash
cd "$(dirname "$0")/DailyStoryVideoGenerator"
dotnet run
EOF

chmod +x ../run.sh

# 完成提示
echo
echo "=========================================="
echo "          配置完成！"
echo "=========================================="
echo
echo "🎉 恭喜！配置已完成"
echo
echo "使用方法："
echo "1. 运行: ./run.sh"
echo "2. 或直接运行: cd DailyStoryVideoGenerator && dotnet run"
echo
echo "📁 重要目录："
echo "- 输出视频: output/"
echo "- 日志文件: logs/"
echo "- 背景图片: backgrounds/"
echo
echo "💡 提示："
echo "- 首次生成需要下载依赖"
echo "- 可以在程序中调整参数"
echo "- 建议先手动测试一次"
echo
echo "按回车键启动程序..."
read

cd ..
./run.sh