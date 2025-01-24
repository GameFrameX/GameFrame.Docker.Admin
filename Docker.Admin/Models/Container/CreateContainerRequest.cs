using System.ComponentModel.DataAnnotations;

namespace Docker.Admin.Models.Container;

/// <summary>
/// 创建 Docker 容器的请求
/// </summary>
public sealed class CreateContainerRequest
{
    /// <summary>
    /// 镜像名称
    /// </summary>
    [Required]
    public string Image { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    [Required]
    public string Name { get; set; }

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