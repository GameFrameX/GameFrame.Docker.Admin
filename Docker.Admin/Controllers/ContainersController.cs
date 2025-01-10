using Docker.Admin.Models;
using Docker.Admin.Models.Container;
using Docker.Admin.Models.Network;
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
        try
        {
            var containers = await _dockerService.GetContainersAsync();
            return Ok(containers);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving container list: {ex.Message}");
        }
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
        try
        {
            var logs = await _dockerService.GetContainerLogsAsync(request.Id);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving logs for container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] BaseRequest request)
    {
        try
        {
            await _dockerService.StartContainerAsync(request.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Stop([FromBody] BaseRequest request)
    {
        try
        {
            await _dockerService.StopContainerAsync(request.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error stopping container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定容器的统计信息
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    /// <returns>容器统计信息</returns>
    [HttpPost]
    public async Task<IActionResult> GetStats([FromBody] BaseRequest request)
    {
        try
        {
            var stats = await _dockerService.GetContainerStatsAsync(request.Id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving stats for container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 重启指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Restart([FromBody] BaseRequest request)
    {
        try
        {
            await _dockerService.RestartContainerAsync(request.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error restarting container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除指定的容器
    /// </summary>
    /// <param name="request">包含容器ID和强制删除标志的请求</param>
    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] BaseDeleteRequest request)
    {
        try
        {
            await _dockerService.RemoveContainerAsync(request.Id, request.IsForce);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error removing container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建新容器
    /// </summary>
    /// <param name="request">包含创建容器所需信息的请求</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContainerRequest request)
    {
        try
        {
            var containerId = await _dockerService.CreateContainerAsync(
                                  request.Image,
                                  request.Name,
                                  request.Environments,
                                  request.Ports,
                                  request.Volumes);
            return Ok(new { ContainerId = containerId });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating container: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定容器的详细信息
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Inspect([FromBody] BaseRequest request)
    {
        try
        {
            var info = await _dockerService.InspectContainerAsync(request.Id);
            return Ok(info);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error inspecting container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 重命名指定的容器
    /// </summary>
    /// <param name="request">包含容器ID和新名称的请求</param>
    [HttpPost]
    public async Task<IActionResult> Rename([FromBody] RenameContainerRequest request)
    {
        try
        {
            await _dockerService.RenameContainerAsync(request.Id, request.Name);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error renaming container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 暂停指定的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Pause([FromBody] BaseRequest request)
    {
        try
        {
            await _dockerService.PauseContainerAsync(request.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error pausing container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复暂停的容器
    /// </summary>
    /// <param name="request">包含容器ID的请求</param>
    [HttpPost]
    public async Task<IActionResult> Unpause([FromBody] BaseRequest request)
    {
        try
        {
            await _dockerService.UnpauseContainerAsync(request.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error unpausing container {request.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据镜像名称和标签创建并启动容器
    /// </summary>
    /// <param name="request">包含Docker Compose配置的请求</param>
    /// <returns>执行结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateAndStart([FromBody] ContainerCreateAndStartRequest request)
    {
        try
        {
            if (request.IsRecreate)
            {
                var containers = await _dockerService.GetContainersAsync();
                bool isFind = false;
                foreach (var container in containers)
                {
                    foreach (var containerName in container.Names)
                    {
                        if (containerName.Substring(1).Equals(request.ContainerName, StringComparison.OrdinalIgnoreCase))
                        {
                            await _dockerService.RemoveContainerAsync(container.ID, true);
                            isFind = true;
                            break;
                        }
                    }

                    if (isFind)
                    {
                        break;
                    }
                }
            }

            CreateNetworkRequest createNetworkRequest = new CreateNetworkRequest()
            {
                Name = request.NetworkName,
                Subnet = request.NetworkSubnet,
                IpAddress = request.NetworkIpAddress,
                IsRecreate = request.NetworkIsRecreate,
            };

            await _dockerService.CreateAndStartContainer(request.ImageName, request.Tag, request.Registry, request.Username, request.Password, request.ContainerName, request.Environments, request.Ports, request.Volumes, createNetworkRequest);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating and starting container: {ex.Message}");
        }
    }
}