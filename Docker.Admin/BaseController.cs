namespace Docker.Admin;

/// <summary>
/// 统一的控制器基类
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected readonly DockerService Service;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    public BaseController(DockerService service)
    {
        Service = service;
    }
}