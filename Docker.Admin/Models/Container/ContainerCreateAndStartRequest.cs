using System.ComponentModel.DataAnnotations;

namespace Docker.Admin.Models.Container;

/// <summary>
/// 创建并启动 Docker 容器的请求
/// </summary>
public sealed class ContainerCreateAndStartRequest : BaseAuthRequest
{
    /// <summary>
    /// Docker 镜像名称
    /// </summary>
    [Required]
    public string ImageName { get; set; }

    /// <summary>
    /// Docker 镜像标签
    /// </summary>
    [Required]
    public string Tag { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    [Required]
    public string ContainerName { get; set; }

    /// <summary>
    /// 是否强制重新创建
    /// </summary>
    public bool IsRecreate { get; set; }

    /// <summary>
    /// 获取或设置网络的名称
    /// </summary>
    public string NetworkName { get; set; }

    /// <summary>
    /// 获取或设置网络的子网地址
    /// </summary>
    public string NetworkSubnet { get; set; }

    /// <summary>
    /// 获取或设置网络的IP地址
    /// </summary>
    public string NetworkIpAddress { get; set; }

    /// <summary>
    /// 是否强制重新创建网络
    /// </summary>
    public bool NetworkIsRecreate { get; set; }

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