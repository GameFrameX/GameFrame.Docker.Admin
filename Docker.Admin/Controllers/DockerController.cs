using System.ComponentModel;
using Docker.Admin.Models;
using Docker.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DockerController : ControllerBase
{
    private readonly DockerService _dockerService;

    public DockerController(DockerService dockerService)
    {
        _dockerService = dockerService;
    }

    /// <summary>
    /// 获取所有容器列表
    /// </summary>
    /// <returns>容器列表</returns>
    /// <response code="200">成功返回容器列表</response>
    /// <response code="500">服务器错误</response>
    [HttpGet("containers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetContainers()
    {
        var containers = await _dockerService.GetContainersAsync();
        return Ok(containers);
    }

    /// <summary>
    /// 获取所有镜像列表
    /// </summary>
    /// <returns>镜像列表</returns>
    [HttpGet("images")]
    public async Task<IActionResult> GetImages()
    {
        var images = await _dockerService.GetImagesAsync();
        return Ok(images);
    }

    /// <summary>
    /// 获取容器日志
    /// </summary>
    /// <param name="id">容器ID</param>
    /// <returns>容器日志内容</returns>
    /// <response code="200">成功返回容器日志</response>
    /// <response code="404">容器未找到</response>
    /// <response code="500">服务器错误</response>
    [HttpGet("containers/{id}/logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetContainerLogs([Description("容器ID")] string id)
    {
        var logs = await _dockerService.GetContainerLogsAsync(id);
        return Ok(logs);
    }

    /// <summary>
    /// 启动容器
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpPost("containers/{id}/start")]
    public async Task<IActionResult> StartContainer(string id)
    {
        await _dockerService.StartContainerAsync(id);
        return Ok();
    }

    /// <summary>
    /// 停止容器
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpPost("containers/{id}/stop")]
    public async Task<IActionResult> StopContainer(string id)
    {
        await _dockerService.StopContainerAsync(id);
        return Ok();
    }

    /// <summary>
    /// 获取容器统计信息
    /// </summary>
    /// <param name="id">容器ID</param>
    /// <returns>容器统计信息</returns>
    [HttpGet("containers/{id}/stats")]
    public async Task<IActionResult> GetContainerStats(string id)
    {
        var stats = await _dockerService.GetContainerStatsAsync(id);
        return Ok(stats);
    }

    /// <summary>
    /// 重启容器
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpPost("containers/{id}/restart")]
    public async Task<IActionResult> RestartContainer(string id)
    {
        await _dockerService.RestartContainerAsync(id);
        return Ok();
    }

    /// <summary>
    /// 删除容器
    /// </summary>
    /// <param name="id">容器ID</param>
    /// <param name="force">是否强制删除</param>
    [HttpDelete("containers/{id}")]
    public async Task<IActionResult> RemoveContainer(string id, [FromQuery] bool force = false)
    {
        await _dockerService.RemoveContainerAsync(id, force);
        return Ok();
    }

    /// <summary>
    /// 拉取镜像
    /// </summary>
    /// <param name="imageName">镜像名称</param>
    /// <param name="tag">标签</param>
    [HttpPost("images/pull")]
    public async Task<IActionResult> PullImage([FromQuery] string imageName, [FromQuery] string tag = "latest")
    {
        await _dockerService.PullImageAsync(imageName, tag);
        return Ok();
    }

    /// <summary>
    /// 删除镜像
    /// </summary>
    /// <param name="id">镜像ID</param>
    /// <param name="force">是否强制删除</param>
    [HttpDelete("images/{id}")]
    public async Task<IActionResult> RemoveImage(string id, [FromQuery] bool force = false)
    {
        await _dockerService.RemoveImageAsync(id, force);
        return Ok();
    }

    /// <summary>
    /// 创建新容器
    /// </summary>
    /// <param name="request">创建容器请求</param>
    [HttpPost("containers")]
    public async Task<IActionResult> CreateContainer([FromBody] CreateContainerRequest request)
    {
        var containerId = await _dockerService.CreateContainerAsync(
                              request.Image,
                              request.Name,
                              request.EnvironmentVariables,
                              request.PortBindings);
        return Ok(new { ContainerId = containerId });
    }

    /// <summary>
    /// 获取容器详细信息
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpGet("containers/{id}")]
    public async Task<IActionResult> InspectContainer(string id)
    {
        var info = await _dockerService.InspectContainerAsync(id);
        return Ok(info);
    }

    /// <summary>
    /// 重命名容器
    /// </summary>
    /// <param name="id">容器ID</param>
    /// <param name="newName">新名称</param>
    [HttpPost("containers/{id}/rename")]
    public async Task<IActionResult> RenameContainer(string id, [FromQuery] string newName)
    {
        await _dockerService.RenameContainerAsync(id, newName);
        return Ok();
    }

    /// <summary>
    /// 暂停容器
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpPost("containers/{id}/pause")]
    public async Task<IActionResult> PauseContainer(string id)
    {
        await _dockerService.PauseContainerAsync(id);
        return Ok();
    }

    /// <summary>
    /// 恢复暂停的容器
    /// </summary>
    /// <param name="id">容器ID</param>
    [HttpPost("containers/{id}/unpause")]
    public async Task<IActionResult> UnpauseContainer(string id)
    {
        await _dockerService.UnpauseContainerAsync(id);
        return Ok();
    }
}