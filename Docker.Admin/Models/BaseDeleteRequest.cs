namespace Docker.Admin.Models;

/// <summary>
/// 删除镜像请求
/// </summary>
public sealed class BaseDeleteRequest : BaseRequest
{
    /// <summary>
    /// 是否强制删除
    /// </summary>
    public bool IsForce { get; set; }
}