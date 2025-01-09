using System.ComponentModel.DataAnnotations;

namespace Docker.Admin.Models;

/// <summary>
/// 请求消息基类
/// </summary>
public class BaseRequest
{
    /// <summary>
    /// 镜像ID 或容器ID
    /// </summary>
    [Required]
    public string Id { get; set; }
}