using Docker.Admin.Models.Image;

namespace Docker.Admin.Controllers;

/// <summary>
/// Docker镜像管理控制器
/// </summary>
[Produces("application/json")]
public class ImageController : BaseController
{
    private readonly ILogger<ImageController> _logger;

    /// <summary>
    /// 初始化ImageController类的新实例。
    /// </summary>
    /// <param name="dockerService">Docker服务实例，用于与Docker进行交互</param>
    /// <param name="logger">日志记录器</param>
    public ImageController(DockerService dockerService, ILogger<ImageController> logger) : base(dockerService)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取所有镜像列表
    /// </summary>
    /// <returns>返回所有镜像的列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ImagesListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<HttpJsonResult> GetList()
    {
        try
        {
            var images = await Service.GetImagesAsync();
            return HttpJsonResult.Success(images);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, ex.Message);
            return HttpJsonResult.Error(StatusCodes.Status500InternalServerError, $"Error retrieving image list: {ex.Message}");
        }
    }

    /// <summary>
    /// 拉取镜像
    /// </summary>
    /// <param name="request">拉取镜像请求参数，包括镜像名称、标签、仓库地址、用户名和密码</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<HttpJsonResult> Pull([FromBody] PullImageRequest request)
    {
        try
        {
            await Service.PullImageAsync(request.ImageName, request.Tag, request.Auth);
            return HttpJsonResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, ex.Message);
            return HttpJsonResult.Error(StatusCodes.Status500InternalServerError, $"Error pulling image: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除镜像
    /// </summary>
    /// <param name="request">删除镜像请求参数，包括镜像ID和是否强制删除的标志</param>
    /// <returns>返回操作结果</returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<HttpJsonResult> Remove([FromBody] BaseDeleteRequest request)
    {
        try
        {
            await Service.RemoveImageAsync(request.Id, request.IsForce);
            return HttpJsonResult.Success(request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, ex.Message);
            return HttpJsonResult.Error(StatusCodes.Status500InternalServerError, $"Error removing image: {ex.Message}");
        }
    }
}