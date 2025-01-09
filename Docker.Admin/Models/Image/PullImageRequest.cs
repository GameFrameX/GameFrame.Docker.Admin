using System.ComponentModel.DataAnnotations;

namespace Docker.Admin.Models.Image;

/// <summary>
/// 拉取镜像请求
/// </summary>
public sealed class PullImageRequest
{
    /// <summary>
    /// Docker 镜像名称
    /// </summary>
    [Required]
    public string ImageName { get; set; }

    /// <summary>
    /// 可选的镜像仓库地址，用于从特定的注册中心拉取镜像
    /// </summary>
    public string Registry { get; set; }

    /// <summary>
    /// 可选的用户名，用于身份验证以访问私有镜像仓库
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 可选的密码，用于身份验证以访问私有镜像仓库
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Docker 镜像标签
    /// </summary>
    [Required]
    public string Tag { get; set; }
}