namespace Docker.Admin.Models.Network;

/// <summary>
/// 表示创建 Docker 网络请求的模型类
/// </summary>
public sealed class CreateNetworkRequest
{
    /// <summary>
    /// 网络配置
    /// </summary>
    [Required]
    public NetworkConfig NetworkConfig { get; set; }
}