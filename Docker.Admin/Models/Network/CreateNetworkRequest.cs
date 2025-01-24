using System.ComponentModel.DataAnnotations;

namespace Docker.Admin.Models.Network;

/// <summary>
/// 表示创建 Docker 网络请求的模型类
/// </summary>
public sealed class CreateNetworkRequest
{
    /// <summary>
    /// 获取或设置网络的名称
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置网络的子网地址
    /// </summary>
    [Required]
    public string Subnet { get; set; }

    /// <summary>
    /// 获取或设置是否重新创建同名网络的标志
    /// </summary>
    [Required]
    public bool IsRecreate { get; set; }

    /// <summary>
    /// 获取或设置网络的IP地址
    /// </summary>
    [Required]
    public string IpAddress { get; set; }
}