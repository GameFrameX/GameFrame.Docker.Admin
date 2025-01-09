using System;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Docker Compose服务类，提供Docker Compose相关功能
/// </summary>
public class DockerComposeService
{
    /// <summary>
    /// 执行Docker Compose命令
    /// </summary>
    /// <param name="yamlFilePath">docker-compose.yml文件路径</param>
    /// <returns>异步任务</returns>
    /// <exception cref="Exception">当Docker Compose执行失败时抛出异常</exception>
    public async Task ExecuteDockerComposeAsync(string yamlFilePath)
    {
        // 配置进程启动信息
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = $"-f {yamlFilePath} up -d", // 使用指定的yaml文件启动容器，-d表示后台运行
            RedirectStandardOutput = true, // 重定向标准输出
            RedirectStandardError = true,  // 重定向标准错误
            UseShellExecute = false,       // 不使用shell执行
            CreateNoWindow = true          // 不创建窗口
        };

        // 创建并启动进程
        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // 检查执行结果
        if (process.ExitCode != 0)
        {
            throw new Exception($"Docker Compose failed: {error}");
        }
    }
} 