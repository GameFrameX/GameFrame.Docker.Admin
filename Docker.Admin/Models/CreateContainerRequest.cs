namespace Docker.Admin.Models;

public sealed class CreateContainerRequest
{
    /// <summary>
    /// 镜像名称
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; }

    /// <summary>
    /// 端口映射 (容器端口 -> 主机端口)
    /// </summary>
    public Dictionary<string, string> PortBindings { get; set; }
}