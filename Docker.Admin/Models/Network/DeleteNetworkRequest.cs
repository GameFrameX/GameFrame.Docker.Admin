namespace Docker.Admin.Models.Network;

/// <summary>
/// 表示删除网络请求的模型类
/// </summary>
public sealed class DeleteNetworkRequest
{
    /// <summary>
    /// 获取或设置要删除的网络名称
    /// </summary>
    [Required]
    public string Name { get; set; }
}