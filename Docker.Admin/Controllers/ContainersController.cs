using Docker.Admin.Models;
using Docker.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Admin.Controllers;

/// <summary>
/// Docker容器管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public class ContainersController : ControllerBase
{
    private readonly DockerService _dockerService;

    /// <summary>
    /// 初始化ContainersController类的新实例。
    /// </summary>
    /// <param name="dockerService">Docker服务实例，用于与Docker进行交互</param>
    public ContainersController(DockerService dockerService)
    {
        _dockerService = dockerService;
    }

    /// <summary>
    /// 获取所有容器列表
    /// </summary>
    /// <returns>容器列表</returns>
    /// <response code="200">成功返回容器列表</response>
    /// <response code="500">服务器错误</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList()
    {
        var containers = await _dockerService.GetContainersAsync();
        return Ok(containers);
    }

    /// <summary>
    /// 获取指定容器的日志
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    /// <returns>容器日志内容</returns>
    /// <response code="200">成功返回容器日志</response>
    /// <response code="404">容器未找到</response>
    /// <response code="500">服务器错误</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLogs([FromBody] BaseRequest request)
    {
        var logs = await _dockerService.GetContainerLogsAsync(request.Id);
        return Ok(logs);
    }

    /// <summary>
    /// 启动指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] BaseRequest request)
    {
        await _dockerService.StartContainerAsync(request.Id);
        return Ok();
    }

    /// <summary>
    /// 停止指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Stop([FromBody] BaseRequest request)
    {
        await _dockerService.StopContainerAsync(request.Id);
        return Ok();
    }

    /// <summary>
    /// 获取指定容器的统计信息
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    /// <returns>容器统计信息</returns>
    [HttpPost]
    public async Task<IActionResult> GetStats([FromBody] BaseRequest request)
    {
        var stats = await _dockerService.GetContainerStatsAsync(request.Id);
        return Ok(stats);
    }

    /// <summary>
    /// 重启指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Restart([FromBody] BaseRequest request)
    {
        await _dockerService.RestartContainerAsync(request.Id);
        return Ok();
    }

    /// <summary>
    /// 删除指定的容器
    /// </summary>
    /// <param name="request">包含容器ID和强制删除标志的请求</param>
    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] BaseDeleteRequest request)
    {
        await _dockerService.RemoveContainerAsync(request.Id, request.IsForce);
        return Ok();
    }

    /// <summary>
    /// 创建新容器
    /// </summary>
    /// <param name="request">包含创建容器所需信息的请求</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContainerRequest request)
    {
        var containerId = await _dockerService.CreateContainerAsync(
                              request.Image,
                              request.Name,
                              request.Environments,
                              request.Ports,
                              request.Volumes);
        return Ok(new { ContainerId = containerId });
    }

    /// <summary>
    /// 获取指定容器的详细信息
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Inspect([FromBody] BaseRequest request)
    {
        var info = await _dockerService.InspectContainerAsync(request.Id);
        return Ok(info);
    }

    /// <summary>
    /// 重命名指定的容器
    /// </summary>
    /// <param name="request">包含容器ID和新名称的请求</param>
    [HttpPost]
    public async Task<IActionResult> Rename([FromBody] RenameContainerRequest request)
    {
        await _dockerService.RenameContainerAsync(request.Id, request.Name);
        return Ok();
    }

    /// <summary>
    /// 暂停指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Pause([FromBody] BaseRequest request)
    {
        await _dockerService.PauseContainerAsync(request.Id);
        return Ok();
    }

    /// <summary>
    /// 恢复暂停的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Unpause([FromBody] BaseRequest request)
    {
        await _dockerService.UnpauseContainerAsync(request.Id);
        return Ok();
    }

    /// <summary>
    /// 根据镜像名称和标签创建并启动容器
    /// </summary>
    /// <param name="request">包含Docker Compose配置的请求</param>
    /// <returns>执行结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateAndStart([FromBody] ContainerCreateAndStartRequest request)
    {
        await _dockerService.CreateAndStartContainer(request.ImageName, request.Tag, request.Registry, request.Username, request.Password, request.ContainerName, request.Environments, request.Ports, request.Volumes);
        return Ok();
    }
}