namespace Docker.Admin.Services;

/// <summary>
/// Docker服务类，提供Docker容器和镜像管理功能
/// </summary>
public sealed partial class DockerService
{
    /// <summary>
    /// 获取所有镜像列表
    /// </summary>
    /// <returns>镜像列表</returns>
    /// <response code="200">成功返回镜像列表</response>
    /// <response code="500">服务器错误</response>
    public async Task<IList<ImagesListResponse>> GetImagesAsync()
    {
        return await _dockerClient.Images.ListImagesAsync(
                   new ImagesListParameters { All = true });
    }

    /// <summary>
    /// 拉取镜像
    /// </summary>
    /// <param name="imageName">镜像名称</param>
    /// <param name="tag">镜像标签，默认为latest</param>
    /// <param name="auth">可选的Docker仓库认证信息</param>
    /// <remarks>
    /// 此方法用于从指定的镜像仓库拉取Docker镜像。
    /// 如果未提供标签，则默认使用"latest"标签。
    /// 该方法会创建一个新的镜像，并在后台处理进度。
    /// </remarks>
    public async Task PullImageAsync(string imageName, string tag = "latest", BaseAuthRequest auth = null)
    {
        var imagesCreateParameters = new ImagesCreateParameters
        {
            FromImage = imageName,
            Tag = tag,
        };
        if (auth != null)
        {
            var authConfig = new AuthConfig { Username = auth.Username, Password = auth.Password, ServerAddress = auth.Registry, Email = auth.Email, };
            await _dockerClient.Images.CreateImageAsync(imagesCreateParameters, authConfig, new Progress<JSONMessage>());
        }
        else
        {
            await _dockerClient.Images.CreateImageAsync(imagesCreateParameters, null, new Progress<JSONMessage>());
        }
    }

    /// <summary>
    /// 删除镜像
    /// </summary>
    /// <param name="imageId">镜像ID</param>
    /// <param name="force">是否强制删除</param>
    public async Task RemoveImageAsync(string imageId, bool force = false)
    {
        await _dockerClient.Images.DeleteImageAsync(
            imageId,
            new ImageDeleteParameters
            {
                Force = force
            });
    }
}