#!/usr/bin/env python3
"""
爽文视频生成器配置检查工具
"""

import os
import json
import sys
import requests
from pathlib import Path

def print_status(status, message):
    """打印状态信息"""
    status_icon = "✅" if status else "❌"
    print(f"{status_icon} {message}")

def check_config():
    """检查配置文件"""
    print("==========================================")
    print("        配置检查工具")
    print("==========================================")
    print()

    # 检查配置文件是否存在
    config_path = Path("DailyStoryVideoGenerator/appsettings.json")
    if not config_path.exists():
        print_status(False, "配置文件不存在，请先运行 setup.bat")
        return False

    # 读取配置
    with open(config_path, 'r', encoding='utf-8') as f:
        config = json.load(f)

    app_config = config.get('AppConfig', {})

    # 检查OpenAI配置
    print("检查OpenAI配置...")
    openai_key = app_config.get('OpenAIApiKey', '')
    if not openai_key or openai_key == 'your-openai-api-key-here':
        print_status(False, "OpenAI API密钥未配置")
    else:
        # 测试API连接
        try:
            response = requests.post(
                'https://api.openai.com/v1/chat/completions',
                headers={
                    'Authorization': f'Bearer {openai_key}',
                    'Content-Type': 'application/json'
                },
                json={
                    'model': 'gpt-3.5-turbo',
                    'messages': [{'role': 'user', 'content': 'Hello'}],
                    'max_tokens': 10
                },
                timeout=10
            )
            if response.status_code == 200:
                print_status(True, "OpenAI API配置正常")
            else:
                print_status(False, f"OpenAI API错误: {response.status_code}")
        except Exception as e:
            print_status(False, f"OpenAI API连接失败: {str(e)}")

    # 检查Azure Speech配置
    print("\n检查Azure Speech配置...")
    azure_key = app_config.get('AzureSpeechKey', '')
    azure_region = app_config.get('AzureSpeechRegion', '')
    if not azure_key or azure_key == 'your-azure-speech-key-here':
        print_status(False, "Azure Speech密钥未配置")
    else:
        print_status(True, f"Azure Speech配置正常 (区域: {azure_region})")

    # 检查B站配置
    print("\n检查B站配置...")
    bili_cookie = app_config.get('UploadConfig', {}).get('BilibiliCookie', '')
    if not bili_cookie:
        print_status(False, "B站Cookie未配置（自动上传功能不可用）")
    else:
        print_status(True, "B站Cookie已配置")

    # 检查目录
    print("\n检查目录结构...")
    for dir_name in ['output', 'logs', 'backgrounds']:
        if Path(dir_name).exists():
            print_status(True, f"目录 {dir_name}/ 存在")
        else:
            print_status(False, f"目录 {dir_name}/ 不存在")
            Path(dir_name).mkdir(exist_ok=True)
            print_status(True, f"已创建目录 {dir_name}/")

    # 检查FFmpeg
    print("\n检查FFmpeg...")
    if os.system('ffmpeg -version > nul 2>&1' if os.name == 'nt' else 'which ffmpeg > /dev/null 2>&1') == 0:
        print_status(True, "FFmpeg已安装")
    else:
        print_status(False, "FFmpeg未安装，请安装并添加到PATH")

    # 检查.NET
    print("\n检查.NET Runtime...")
    if os.system('dotnet --version > nul 2>&1') == 0:
        import subprocess
        result = subprocess.run(['dotnet', '--version'], capture_output=True, text=True)
        print_status(True, f".NET Runtime: {result.stdout.strip()}")
    else:
        print_status(False, ".NET Runtime未安装")

    print("\n==========================================")
    print("配置检查完成")
    print("==========================================")

def create_test_prompt():
    """创建测试提示词"""
    test_prompt = """
请生成一个100字左右的爽文小故事片段：

要求：
- 主角：林风
- 背景：现代都市
- 情节：普通人获得系统后的打脸片段
- 风格：热血爽快

示例：
林风本是个普通的上班族，今天却意外获得了【神豪系统】。
【叮！检测到宿主被嘲讽，激活打脸模式！】
看着面前趾高气昂的经理，林风淡淡一笑："这个公司，我买了。"
"""

    with open('test_prompt.txt', 'w', encoding='utf-8') as f:
        f.write(test_prompt)
    print("\n已创建测试提示词文件：test_prompt.txt")

if __name__ == '__main__':
    check_config()

    print("\n是否创建测试提示词文件？(y/n): ", end='')
    if input().lower() == 'y':
        create_test_prompt()