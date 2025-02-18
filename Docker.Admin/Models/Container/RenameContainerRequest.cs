namespace Docker.Admin.Models.Container;

/// <summary>
/// 重命名容器请求
/// </summary>
public sealed class RenameContainerRequest : BaseRequest
{
    /// <summary>
    /// 容器名称
    /// </summary>
    [Required]
    public string Name { get; set; }
}