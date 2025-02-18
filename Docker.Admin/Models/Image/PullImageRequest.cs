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
    /// Docker 镜像仓库地址授权
    /// </summary>
    public BaseAuthRequest Auth { get; set; }

    /// <summary>
    /// Docker 镜像标签
    /// </summary>
    [Required]
    public string Tag { get; set; }
}