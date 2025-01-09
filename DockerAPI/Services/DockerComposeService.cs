using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class DockerComposeService
{
    public async Task ExecuteDockerComposeAsync(string yamlFilePath)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = $"-f {yamlFilePath} up -d",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Docker Compose failed: {error}");
        }
    }
} 