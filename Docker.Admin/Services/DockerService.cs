namespace Docker.Admin.Services;

/// <summary>
/// Docker服务类，提供Docker容器和镜像管理功能
/// </summary>
public sealed partial class DockerService
{
    /// <summary>
    /// Docker客户端实例
    /// </summary>
    private readonly DockerClient _dockerClient;

    /// <summary>
    /// 初始化Docker服务
    /// </summary>
    public DockerService()
    {
        var uri = (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32S || Environment.OSVersion.Platform == PlatformID.WinCE || Environment.OSVersion.Platform == PlatformID.Win32Windows)
                      ? new Uri("npipe://./pipe/docker_engine") // Windows
                      : new Uri("unix:///var/run/docker.sock"); // Linux
        _dockerClient = new DockerClientConfiguration(uri).CreateClient();
    }
}