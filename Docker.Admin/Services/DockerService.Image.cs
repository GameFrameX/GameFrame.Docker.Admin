namespace Docker.Admin.Services;

/// <summary>
/// Docker服务类，提供Docker容器和镜像管理功能
/// </summary>
public sealed partial class DockerService
{
    private const long DefaultMinFreeSpaceBytes = 2L * 1024 * 1024 * 1024;
    private const int DefaultKeepRecentImages = 3;
    private const string ImageCleanupEnabledEnv = "DOCKER_ADMIN_IMAGE_CLEANUP_ENABLED";
    private const string MinFreeSpaceBytesEnv = "DOCKER_ADMIN_MIN_FREE_SPACE_BYTES";
    private const string KeepRecentImagesEnv = "DOCKER_ADMIN_KEEP_RECENT_IMAGES";
    private const string ProtectedImageTagsEnv = "DOCKER_ADMIN_PROTECTED_IMAGE_TAGS";
    private const string DockerDiskPathEnv = "DOCKER_ADMIN_DOCKER_DISK_PATH";

    /// <summary>
    /// 获取所有镜像列表
    /// </summary>
    /// <returns>镜像列表</returns>
    /// <response code="200">成功返回镜像列表</response>
    /// <response code="500">服务器错误</response>
    public async Task<IList<ImagesListResponse>> GetImagesAsync()
    {
        var imagesListParameters = new ImagesListParameters { All = true };
        return await _dockerClient.Images.ListImagesAsync(imagesListParameters);
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
        tag = string.IsNullOrWhiteSpace(tag) ? "latest" : tag;
        var targetImageReference = $"{imageName}:{tag}";
        var cleanupSettings = GetImageCleanupSettings();
        await EnsureImagePullDiskSpaceAsync(targetImageReference, cleanupSettings);

        var imagesCreateParameters = new ImagesCreateParameters
        {
            FromImage = imageName,
            Tag = tag,
        };

        try
        {
            await CreateImageAsync(imagesCreateParameters, auth);
        }
        catch (Exception ex) when (cleanupSettings.Enabled && IsNoSpaceError(ex))
        {
            var dockerDiskPath = await GetDockerDiskPathAsync(cleanupSettings);
            var deletedCount = await CleanupOldUnusedImagesAsync(targetImageReference, cleanupSettings, dockerDiskPath, true);
            if (deletedCount == 0)
            {
                throw;
            }

            await CreateImageAsync(imagesCreateParameters, auth);
        }
    }

    private async Task CreateImageAsync(ImagesCreateParameters imagesCreateParameters, BaseAuthRequest auth)
    {
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

    private async Task EnsureImagePullDiskSpaceAsync(string targetImageReference, ImageCleanupSettings settings)
    {
        if (!settings.Enabled || settings.MinFreeSpaceBytes <= 0)
        {
            return;
        }

        var dockerDiskPath = await GetDockerDiskPathAsync(settings);
        if (HasEnoughFreeSpace(dockerDiskPath, settings.MinFreeSpaceBytes, out _, out _))
        {
            return;
        }

        await CleanupOldUnusedImagesAsync(targetImageReference, settings, dockerDiskPath, false);

        if (!HasEnoughFreeSpace(dockerDiskPath, settings.MinFreeSpaceBytes, out var availableFreeSpace, out var checkedPath))
        {
            throw new InvalidOperationException($"磁盘空间不足，路径 {checkedPath} 当前可用 {FormatBytes(availableFreeSpace)}，要求至少 {FormatBytes(settings.MinFreeSpaceBytes)}，且未找到足够可清理的未使用镜像。");
        }
    }

    private async Task<int> CleanupOldUnusedImagesAsync(string targetImageReference, ImageCleanupSettings settings, string dockerDiskPath, bool forceAtLeastOne)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        var usedImageIds = containers.Select(container => container.ImageID);
        var images = await GetImagesAsync();
        var candidates = DockerImageCleanupPlanner.SelectCleanupCandidates(images, usedImageIds, targetImageReference, settings.KeepRecentImages, settings.ProtectedImageTags);
        var deletedCount = 0;

        foreach (var image in candidates)
        {
            if ((!forceAtLeastOne || deletedCount > 0) && HasEnoughFreeSpace(dockerDiskPath, settings.MinFreeSpaceBytes, out _, out _))
            {
                break;
            }

            try
            {
                await _dockerClient.Images.DeleteImageAsync(image.ID, new ImageDeleteParameters { Force = false, NoPrune = false });
                deletedCount++;
            }
            catch (DockerApiException)
            {
                // 镜像可能在并发部署中刚被容器引用，跳过后继续尝试其他候选。
            }
        }

        return deletedCount;
    }

    private async Task<string> GetDockerDiskPathAsync(ImageCleanupSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.DockerDiskPath))
        {
            return settings.DockerDiskPath;
        }

        try
        {
            var systemInfo = await _dockerClient.System.GetSystemInfoAsync();
            if (!string.IsNullOrWhiteSpace(systemInfo.DockerRootDir))
            {
                return systemInfo.DockerRootDir;
            }
        }
        catch (DockerApiException)
        {
            // DockerRootDir 只是用于定位磁盘，读取失败时仍可用进程目录兜底。
        }

        // ponytail: 容器内未挂载 DockerRootDir 时只能检测当前文件系统；需要精确宿主机磁盘时挂载 Docker 数据目录并设置 DOCKER_ADMIN_DOCKER_DISK_PATH。
        return AppContext.BaseDirectory;
    }

    private static ImageCleanupSettings GetImageCleanupSettings()
    {
        return new ImageCleanupSettings
        {
            Enabled = GetBooleanEnvironment(ImageCleanupEnabledEnv, true),
            MinFreeSpaceBytes = GetLongEnvironment(MinFreeSpaceBytesEnv, DefaultMinFreeSpaceBytes),
            KeepRecentImages = GetIntEnvironment(KeepRecentImagesEnv, DefaultKeepRecentImages),
            DockerDiskPath = Environment.GetEnvironmentVariable(DockerDiskPathEnv),
            ProtectedImageTags = GetListEnvironment(ProtectedImageTagsEnv)
        };
    }

    private static bool HasEnoughFreeSpace(string path, long minFreeSpaceBytes, out long availableFreeSpace, out string checkedPath)
    {
        checkedPath = FindExistingPath(path);
        var root = Path.GetPathRoot(checkedPath);
        var driveInfo = new DriveInfo(string.IsNullOrWhiteSpace(root) ? checkedPath : root);
        availableFreeSpace = driveInfo.AvailableFreeSpace;
        return availableFreeSpace >= minFreeSpaceBytes;
    }

    private static string FindExistingPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return AppContext.BaseDirectory;
        }

        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        while (!Directory.Exists(fullPath))
        {
            var parent = Directory.GetParent(fullPath);
            if (parent == null)
            {
                return Path.GetPathRoot(fullPath) ?? AppContext.BaseDirectory;
            }

            fullPath = parent.FullName;
        }

        return fullPath;
    }

    private static bool GetBooleanEnvironment(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static long GetLongEnvironment(string name, long defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return long.TryParse(value, out var result) && result >= 0 ? result : defaultValue;
    }

    private static int GetIntEnvironment(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) && result >= 0 ? result : defaultValue;
    }

    private static IReadOnlyList<string> GetListEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
                   ? Array.Empty<string>()
                   : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsNoSpaceError(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("no space left on device", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private sealed class ImageCleanupSettings
    {
        public bool Enabled { get; init; }

        public long MinFreeSpaceBytes { get; init; }

        public int KeepRecentImages { get; init; }

        public string DockerDiskPath { get; init; }

        public IReadOnlyList<string> ProtectedImageTags { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// 删除镜像
    /// </summary>
    /// <param name="imageId">镜像ID</param>
    /// <param name="force">是否强制删除</param>
    public async Task RemoveImageAsync(string imageId, bool force = false)
    {
        var imageDeleteParameters = new ImageDeleteParameters
        {
            Force = force
        };
        await _dockerClient.Images.DeleteImageAsync(imageId, imageDeleteParameters);
    }
}
