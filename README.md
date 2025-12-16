# 爽文视频自动生成器

一个基于AI的自动化爽文小说配音动画视频生成工具，支持自动生成、配音、动画制作和B站上传。

## 功能特性

- 🤖 **AI内容生成**：集成OpenAI GPT，自动生成各类爽文小说
- 🗣️ **智能配音**：Azure语音合成，多种音色可选
- 🎨 **动画生成**：自动生成配图和动画场景
- 🎬 **视频合成**：FFmpeg驱动的高质量视频合成
- 📺 **自动上传**：支持B站一键上传
- ⏰ **定时任务**：每天自动生成和发布

## 技术栈

- **前端**: WPF + Material Design
- **后端**: .NET 8 + C#
- **AI服务**: OpenAI GPT + Azure Cognitive Services
- **视频处理**: FFmpeg + SkiaSharp
- **定时任务**: Quartz.NET

## 环境要求

- Windows 10/11
- .NET 8.0 Runtime
- FFmpeg (需要添加到系统PATH)

## 安装步骤

### 1. 克隆项目

```bash
git clone https://github.com/xyc200602/DailyStoryVideoGenerator.git
cd DailyStoryVideoGenerator
```

### 2. 安装依赖

```bash
dotnet restore
```

### 3. 配置API密钥

编辑 `DailyStoryVideoGenerator/appsettings.json`：

```json
{
  "AppConfig": {
    "OpenAIApiKey": "your-openai-api-key-here",
    "AzureSpeechKey": "your-azure-speech-key-here",
    "AzureSpeechRegion": "eastasia"
  }
}
```

### 4. 运行项目

```bash
dotnet run --project DailyStoryVideoGenerator
```

## 配置说明

### OpenAI配置
- `OpenAIApiKey`: OpenAI API密钥
- `OpenAIEndpoint`: API端点（默认为官方）
- `OpenAIDeploymentName`: 模型名称（gpt-3.5-turbo或gpt-4）

### Azure语音配置
- `AzureSpeechKey`: Azure Speech服务密钥
- `AzureSpeechRegion`: 服务区域（如：eastasia）

### B站配置（可选）
- `BilibiliCookie`: B站登录Cookie
- `BilibiliCsrf`: CSRF令牌

获取B站Cookie的方法：
1. 登录B站
2. 打开开发者工具(F12)
3. 在Network标签页找到任意请求
4. 复制请求头中的Cookie值
5. 提取bili_jct值作为CSRF

## 使用指南

### 快速生成视频

1. **设置故事参数**
   - 选择故事类型（爽文、玄幻、都市等）
   - 设置主角名字和背景
   - 调整字数和文风

2. **自定义提示词**（可选）
   - 勾选"使用自定义提示词"
   - 输入具体的故事要求

3. **生成视频**
   - 点击"立即生成"按钮
   - 等待AI生成故事内容
   - 点击"生成视频"制作动画
   - 点击"上传B站"发布视频

### 定时任务

1. **启用定时任务**
   - 勾选"启用定时任务"
   - 设置执行时间（默认每天9点）
   - 选择重复频率

2. **自动发布**
   - 配置B站账号信息
   - 启用"自动上传"选项
   - 系统将自动生成并上传视频

## 故障排除

### 常见问题

1. **AI生成失败**
   - 检查OpenAI API密钥是否正确
   - 确认网络连接正常
   - 检查API配额是否充足

2. **语音合成失败**
   - 检查Azure Speech密钥
   - 确认服务区域是否正确
   - 检查网络防火墙设置

3. **视频生成失败**
   - 确认FFmpeg已安装并添加到PATH
   - 检查输出目录权限
   - 确认磁盘空间充足

4. **B站上传失败**
   - 检查Cookie是否过期
   - 确认CSRF令牌是否正确
   - 检查视频格式是否支持

### 日志查看

日志文件位置：`logs/app-YYYYMMDD.log`

## 开发说明

### 项目结构

```
DailyStoryVideoGenerator/
├── Core/                    # 核心模型和接口
│   ├── Models/             # 数据模型
│   └── Interfaces/         # 服务接口
├── Services/               # 服务实现
│   └── Implementations/    # 具体实现
├── DailyStoryVideoGenerator/  # WPF应用程序
│   ├── Views/              # 视图
│   ├── ViewModels/         # 视图模型
│   └── Services/           # 应用服务
├── backgrounds/            # 背景图片
├── output/                 # 输出目录
└── logs/                   # 日志文件
```

### 扩展开发

1. **添加新的故事类型**
   - 在 `StoryGenerator` 中添加新的提示词模板
   - 更新UI中的故事类型列表

2. **添加新的语音类型**
   - 在 `AzureTextToSpeech` 中添加新的语音映射
   - 更新UI中的语音类型列表

3. **添加新的动画风格**
   - 在 `AnimationGenerator` 中实现新的动画效果
   - 更新配置选项

## 许可证

MIT License

## 贡献

欢迎提交Issue和Pull Request！

## 联系方式

- 邮箱：dc42857@um.edu.mo
- GitHub：https://github.com/xyc200602
