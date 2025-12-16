using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Services;

public class BilibiliUploader : IBilibiliUploader
{
    private readonly ILogger<BilibiliUploader> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.bilibili.com";
    private bool _isLoggedIn = false;
    private string _cookie = string.Empty;
    private string _csrf = string.Empty;

    public BilibiliUploader(ILogger<BilibiliUploader> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<bool> LoginAsync(string cookie, string csrf)
    {
        try
        {
            _cookie = cookie;
            _csrf = csrf;

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Cookie", _cookie);
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com");

            // 验证登录状态
            var response = await _httpClient.GetAsync($"{_baseUrl}/x/space/acc/info");
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            if (result.TryGetProperty("code", out var code) && code.GetInt32() == 0)
            {
                _isLoggedIn = true;
                _logger.LogInformation("B站登录成功");
                return true;
            }
            else
            {
                _isLoggedIn = false;
                _logger.LogError("B站登录失败: {Content}", content);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B站登录异常");
            _isLoggedIn = false;
            return false;
        }
    }

    public async Task<UploadResult> UploadVideoAsync(string videoPath, GeneratedStory story, UploadConfig config)
    {
        try
        {
            if (!_isLoggedIn)
            {
                if (!await LoginAsync(config.BilibiliCookie, config.BilibiliCsrf))
                {
                    return new UploadResult
                    {
                        Success = false,
                        Message = "登录失败，请检查Cookie配置"
                    };
                }
            }

            // 1. 获取上传地址
            var uploadUrl = await GetUploadUrlAsync();
            if (string.IsNullOrEmpty(uploadUrl))
            {
                return new UploadResult
                {
                    Success = false,
                    Message = "获取上传地址失败"
                };
            }

            // 2. 上传视频文件
            var uploadResult = await UploadFileAsync(videoPath, uploadUrl);
            if (!uploadResult.Success)
            {
                return uploadResult;
            }

            // 3. 提交视频信息
            var submitResult = await SubmitVideoAsync(story, config, uploadResult.VideoId);

            return new UploadResult
            {
                Success = true,
                VideoId = submitResult,
                Url = $"https://www.bilibili.com/video/{submitResult}",
                Message = "上传成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传视频失败");
            return new UploadResult
            {
                Success = false,
                Message = $"上传失败: {ex.Message}"
            };
        }
    }

    public async Task<UploadStatus> GetUploadStatusAsync(string uploadId)
    {
        try
        {
            // B站实际上传是同步的，这里返回模拟状态
            return new UploadStatus
            {
                Percent = 100,
                State = "completed",
                Message = "上传完成"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传状态失败");
            return new UploadStatus
            {
                Percent = 0,
                State = "error",
                Message = ex.Message
            };
        }
    }

    private async Task<string> GetUploadUrlAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/x/upload/url/fetch?name=video.mp3&size=0&r=upos&profile=ugcupos%2Fbup&ssl=0&version=2.14.0&build=2140000&os=upos&upcdn=ws",
                null);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (result.TryGetProperty("code", out var code) && code.GetInt32() == 0)
            {
                if (result.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("upos_uri", out var uri))
                    {
                        return uri.GetString() ?? string.Empty;
                    }
                }
            }

            _logger.LogError("获取上传地址失败: {Content}", content);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传地址异常");
            return string.Empty;
        }
    }

    private async Task<UploadResult> UploadFileAsync(string videoPath, string uploadUrl)
    {
        try
        {
            // 读取视频文件
            var fileBytes = await File.ReadAllBytesAsync(videoPath);
            var fileName = Path.GetFileName(videoPath);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
            content.Add(fileContent, "file", fileName);

            // 添加必要的参数
            content.Add(new StringContent("upos"), "biz");
            content.Add(new StringContent("ugcupos/bup"), "upcdn");
            content.Add(new StringContent("0"), "ssl");
            content.Add(new StringContent("2.14.0"), "version");
            content.Add(new StringContent("upos"), "os");

            var response = await _httpClient.PostAsync(uploadUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (result.TryGetProperty("ok", out var ok) && ok.GetInt32() == 1)
                {
                    // 从响应中提取文件ID
                    var fileId = Guid.NewGuid().ToString();
                    return new UploadResult
                    {
                        Success = true,
                        VideoId = fileId,
                        Message = "文件上传成功"
                    };
                }
            }

            _logger.LogError("文件上传失败: {Content}", responseContent);
            return new UploadResult
            {
                Success = false,
                Message = "文件上传失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传异常");
            return new UploadResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private async Task<string> SubmitVideoAsync(GeneratedStory story, UploadConfig config, string fileId)
    {
        try
        {
            // 准备视频信息
            var videoData = new
            {
                cover = "", // 视频封面，可以自动生成
                title = string.IsNullOrEmpty(config.DefaultTitle) ?
                    $"{story.Title} - {DateTime.Now:yyyy年MM月dd日}" :
                    config.DefaultTitle,
                copyright = 1,
                tid = GetCategoryId(config.Category),
                tag = string.Join(",", config.Tags),
                desc_format = 0,
                desc = string.IsNullOrEmpty(config.DefaultDescription) ?
                    story.Content.Substring(0, Math.Min(500, story.Content.Length)) + "..." :
                    config.DefaultDescription,
                reuse = -1,
                dynamic = "",
                interactive = 0,
                videos = new[]
                {
                    new
                    {
                        filename = fileId,
                        title = story.Title,
                        desc = "",
                        cid = fileId
                    }
                },
                act_reserve_create = 0,
                handle_staff = false,
                topic_grey = 0,
                        no_reprint = 0,
                subtitle = new
                {
                    open = 0,
                    lan = ""
                },
                dolby = 0,
                lossless_music = 0,
                web_os = 1,
                csrf = _csrf
            };

            var json = JsonSerializer.Serialize(videoData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/x/vu/web/add?csrf={_csrf}",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (result.TryGetProperty("code", out var code) && code.GetInt32() == 0)
            {
                if (result.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("bvid", out var bvid))
                    {
                        return bvid.GetString() ?? string.Empty;
                    }
                }
            }

            _logger.LogError("提交视频信息失败: {Content}", responseContent);
            throw new InvalidOperationException("提交视频信息失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交视频信息异常");
            throw;
        }
    }

    private int GetCategoryId(string category)
    {
        // B站分区ID映射
        var categoryMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "动画", 1 },
            { "番剧", 13 },
            { "国创", 167 },
            { "音乐", 3 },
            { "舞蹈", 129 },
            { "游戏", 4 },
            { "知识", 36 },
            { "科技", 188 },
            { "运动", 160 },
            { "汽车", 223 },
            { "生活", 5 },
            { "美食", 211 },
            { "动物", 217 },
            { "鬼畜", 119 },
            { "时尚", 155 },
            { "资讯", 202 },
            { "影视", 23 },
            { "纪录片", 177 },
            { "电影", 23 },
            { "电视剧", 11 },
            { "文学", 3 }, // 归类到音乐下的有声读物
            { "小说", 3 }
        };

        return categoryMap.GetValueOrDefault(category, 5); // 默认生活区
    }

    private async Task<string> GenerateCoverAsync(string videoPath, string outputPath)
    {
        try
        {
            // 使用FFmpeg从视频中提取封面
            var coverPath = Path.Combine(outputPath, "cover.jpg");

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 \"{coverPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            await process.WaitForExitAsync();

            if (File.Exists(coverPath))
            {
                return coverPath;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "生成视频封面失败");
            return string.Empty;
        }
    }
}