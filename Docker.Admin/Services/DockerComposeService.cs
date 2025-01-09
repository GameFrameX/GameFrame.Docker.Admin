using System.Diagnostics;

namespace Docker.Admin.Services;

/// <summary>
/// Docker Compose服务类，提供Docker Compose相关功能
/// </summary>
public sealed class DockerComposeService
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
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = $"-f {yamlFilePath} up -d",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Docker compose failed with exit code {process.ExitCode}. Error: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute docker-compose command. Error: {ex.Message}", ex);
        }
    }
}