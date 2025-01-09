using Docker.Admin.Models;
using Docker.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DockerComposeController : ControllerBase
{
    private readonly DockerComposeService _dockerComposeService;

    public DockerComposeController(DockerComposeService dockerComposeService)
    {
        _dockerComposeService = dockerComposeService;
    }

    /// <summary>
    /// 上传并执行 Docker Compose 文件
    /// </summary>
    /// <param name="file">Docker Compose 文件</param>
    /// <returns>执行结果</returns>
    [HttpPost]
    public async Task<IActionResult> ExecuteDockerCompose(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        await _dockerComposeService.ExecuteDockerComposeAsync(filePath);
        System.IO.File.Delete(filePath);

        return Ok();
    }

    /// <summary>
    /// 根据镜像名称和标签创建并执行Docker Compose文件
    /// </summary>
    /// <param name="request">Docker Compose 配置请求</param>
    /// <returns>执行结果</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateAndExecuteDockerCompose(DockerComposeRequest request)
    {
        if (string.IsNullOrEmpty(request.ImageName) || string.IsNullOrEmpty(request.Tag))
        {
            return BadRequest("Image name and tag are required");
        }

        if (string.IsNullOrEmpty(request.ContainerName))
        {
            return BadRequest("Container name is required");
        }

        var volumeConfig = string.Join("\n", request.VolumeMappings.Select(v => $"      - {v}"));
        var portConfig = string.Join("\n", request.PortMappings.Select(p => $"      - \"{p}\""));
        var envConfig = string.Join("\n", request.EnvironmentVariables.Select(e => $"      - {e.Key}={e.Value}"));

        var composeContent = $@"services:
  app:
    image: {request.ImageName}:{request.Tag}
    container_name: {request.ContainerName.ToLower()}
    restart: always
    ports:
{portConfig}
    volumes:
{volumeConfig}
    environment:
{envConfig}
    ";

        var filePath = request.ContainerName.ToLower() + "-docker-compose.yml";
        await System.IO.File.WriteAllTextAsync(filePath, composeContent);

        try
        {
            await _dockerComposeService.ExecuteDockerComposeAsync(filePath);
            return Ok();
        }
        finally
        {
            System.IO.File.Delete(filePath);
        }
    }
}