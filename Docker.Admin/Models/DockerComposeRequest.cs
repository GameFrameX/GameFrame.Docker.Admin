namespace Docker.Admin.Models;

/// <summary>
/// Docker Compose 配置请求模型
/// </summary>
public sealed class DockerComposeRequest
{
    /// <summary>
    /// Docker 镜像名称
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Docker 镜像标签
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    public string ContainerName { get; set; }

    /// <summary>
    /// 端口映射配置，格式如: "8080:80"
    /// </summary>
    public Dictionary<string, string> Ports { get; set; }

    /// <summary>
    /// 卷映射配置，格式如: "/host/path:/container/path"
    /// </summary>
    public Dictionary<string, string> Volumes { get; set; }

    /// <summary>
    /// 环境变量配置
    /// </summary>
    public Dictionary<string, string> Environments { get; set; }
}