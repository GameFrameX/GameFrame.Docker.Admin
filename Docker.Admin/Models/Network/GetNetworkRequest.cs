// 定义请求类以获取网络信息
namespace Docker.Admin.Models.Network;

/// <summary>
/// 表示获取网络请求的模型类
/// </summary>
public sealed class GetNetworkRequest
{
    /// <summary>
    /// 获取或设置网络名称
    /// </summary>
    public string Name { get; set; }
}