using Docker.Admin.Models;
using Docker.Admin.Models.Image;
using Docker.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Admin.Controllers;

/// <summary>
/// Docker镜像管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public class ImageController : ControllerBase
{
    private readonly DockerService _dockerService;

    /// <summary>
    /// 初始化ImageController类的新实例。
    /// </summary>
    /// <param name="dockerService">Docker服务实例，用于与Docker进行交互</param>
    public ImageController(DockerService dockerService)
    {
        _dockerService = dockerService;
    }

    /// <summary>
    /// 获取所有镜像列表
    /// </summary>
    /// <returns>返回所有镜像的列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var images = await _dockerService.GetImagesAsync();
            return Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving image list: {ex.Message}");
        }
    }

    /// <summary>
    /// 拉取镜像
    /// </summary>
    /// <param name="request">拉取镜像请求参数，包括镜像名称、标签、仓库地址、用户名和密码</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    public async Task<IActionResult> Pull([FromBody] PullImageRequest request)
    {
        try
        {
            await _dockerService.PullImageAsync(request.ImageName, request.Tag, request.Registry, request.Username, request.Password);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error pulling image: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除镜像
    /// </summary>
    /// <param name="request">删除镜像请求参数，包括镜像ID和是否强制删除的标志</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] BaseDeleteRequest request)
    {
        try
        {
            await _dockerService.RemoveImageAsync(request.Id, request.IsForce);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error removing image: {ex.Message}");
        }
    }
}