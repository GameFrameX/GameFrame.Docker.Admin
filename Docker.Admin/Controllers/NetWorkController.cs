using Docker.Admin.Models;
using Docker.Admin.Models.Image;
using Docker.Admin.Models.Network;
using Docker.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Admin.Controllers;

/// <summary>
/// Docker网络管理控制器
/// 提供对Docker网络的管理功能，包括获取网络列表、获取特定网络信息、创建和删除网络。
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public class NetWorkController : ControllerBase
{
    private readonly DockerService _dockerService;

    /// <summary>
    /// 初始化NetWorkController类的新实例。
    /// </summary>
    /// <param name="dockerService">Docker服务实例，用于与Docker进行交互</param>
    public NetWorkController(DockerService dockerService)
    {
        _dockerService = dockerService;
    }

    /// <summary>
    /// 获取所有网络的列表
    /// </summary>
    /// <returns>返回网络列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var networks = await _dockerService.GetNetworks();
            return Ok(networks);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving network list: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定网络的信息
    /// </summary>
    /// <param name="request">包含网络名称的请求</param>
    /// <returns>返回指定网络的信息</returns>
    [HttpGet]
    public async Task<IActionResult> Get([FromBody] GetNetworkRequest request)
    {
        try
        {
            var network = await _dockerService.GetNetwork(request.Name);
            return Ok(network);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving network information: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建自定义网络
    /// </summary>
    /// <param name="request">包含网络创建信息的请求</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNetworkRequest request)
    {
        try
        {
            await _dockerService.CreateNetwork(request);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating network: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除指定网络
    /// </summary>
    /// <param name="request">包含要删除的网络名称的请求</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] DeleteNetworkRequest request)
    {
        try
        {
            await _dockerService.DeleteNetwork(request);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error removing network: {ex.Message}");
        }
    }
}