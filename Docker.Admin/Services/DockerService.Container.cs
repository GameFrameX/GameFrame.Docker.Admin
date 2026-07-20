using Docker.Admin.Models.Network;

namespace Docker.Admin.Services;

/// <summary>
/// Docker服务类，提供Docker容器和镜像管理功能
/// </summary>
public sealed partial class DockerService
{
    /// <summary>
    /// 获取所有容器列表
    /// </summary>
    /// <returns>容器列表</returns>
    /// <response code="200">成功返回容器列表</response>
    /// <response code="500">服务器错误</response>
    public async Task<IList<ContainerListResponse>> GetContainersAsync()
    {
        return await _dockerClient.Containers.ListContainersAsync(
                   new ContainersListParameters { All = true });
    }

    /// <summary>
    /// 获取容器日志
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器日志内容</returns>
    /// <response code="200">成功返回容器日志</response>
    /// <response code="404">容器未找到</response>
    /// <response code="500">服务器错误</response>
    public async Task<string> GetContainerLogsAsync(string containerId)
    {
        using var logStream = await _dockerClient.Containers.GetContainerLogsAsync(
                                  containerId,
                                  new ContainerLogsParameters
                                  {
                                      ShowStdout = true,
                                      ShowStderr = true,
                                      Tail = "100"
                                  });

        var (stdout, stderr) = await logStream.ReadOutputToEndAsync(CancellationToken.None);
        return stdout + stderr;
    }

    /// <summary>
    /// 启动容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    public async Task StartContainerAsync(string containerId)
    {
        await _dockerClient.Containers.StartContainerAsync(
            containerId,
            new ContainerStartParameters());
    }

    /// <summary>
    /// 停止容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    public async Task StopContainerAsync(string containerId)
    {
        await _dockerClient.Containers.StopContainerAsync(
            containerId,
            new ContainerStopParameters());
    }

    /// <summary>
    /// 获取容器统计信息
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器统计信息</returns>
    /// <exception cref="Exception">获取容器统计信息失败时抛出</exception>
    public async Task<ContainerStatsResponse> GetContainerStatsAsync(string containerId)
    {
        try
        {
            ContainerStatsResponse result = null;
            var progress = new Progress<ContainerStatsResponse>(stats => result = stats);

            await _dockerClient.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = false },
                progress,
                CancellationToken.None);

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get container stats for {containerId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 重启容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    public async Task RestartContainerAsync(string containerId)
    {
        await _dockerClient.Containers.RestartContainerAsync(
            containerId,
            new ContainerRestartParameters());
    }

    /// <summary>
    /// 删除容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <param name="force">是否强制删除</param>
    public async Task RemoveContainerAsync(string containerId, bool force = false)
    {
        await _dockerClient.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters
            {
                Force = force,
                RemoveVolumes = true
            });
    }

    /// <summary>
    /// 创建新容器
    /// </summary>
    /// <param name="image">镜像名称</param>
    /// <param name="name">容器名称</param>
    /// <param name="environmentVariables">环境变量字典</param>
    /// <param name="portBindings">端口映射字典</param>
    /// <param name="volumeBindings">卷映射字典</param>
    /// <param name="networkConfig">网络配置</param>
    /// <param name="labelsVariables">标签</param>
    /// <param name="command">命令列表</param>
    /// <returns>创建的容器ID</returns>
    public async Task<string> CreateContainerAsync(string image, string name, IDictionary<string, string> environmentVariables = null, IDictionary<string, string> portBindings = null, IDictionary<string, string> volumeBindings = null, NetworkConfig networkConfig = null, IDictionary<string, string> labelsVariables = null, IList<string> command = null)
    {
        await CreateNetwork(networkConfig);

        var createContainerParameters = new CreateContainerParameters
        {
            Image = image,
            Name = name,
            // 主机名
            HostConfig = new HostConfig()
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>(),
                Binds = new List<string>(),
                // 重启策略：除非手动停止，否则一直重启, 最大重试次数10次
                RestartPolicy = new RestartPolicy() { Name = RestartPolicyKind.UnlessStopped },
                NetworkMode = "bridge" // 显式设置网络模式为bridge，确保端口映射正常工作
            },
            // 暴露端口
            ExposedPorts = new Dictionary<string, EmptyStruct>(),
            // 健康检查
            Healthcheck = new HealthcheckConfig(),
            // 网络
            NetworkingConfig = new NetworkingConfig(),
            // 容器内的挂载点声明
            Volumes = new Dictionary<string, EmptyStruct>(),
            // 标签
            Labels = labelsVariables,
            // 命令
            Cmd = command,
            // 环境变量配置
            Env = ProcessEnvironmentVariables(environmentVariables),
        };

        // 网络
        if (networkConfig != null)
        {
            createContainerParameters.NetworkingConfig.EndpointsConfig = new Dictionary<string, EndpointSettings>
            {
                {
                    networkConfig.Name, new EndpointSettings()
                },
            };
        }

        // 端口
        if (portBindings != null)
        {
            // 暴露且绑定端口
            foreach (var portBindingPair in portBindings)
            {
                // 绑定端口：Key是主机端口，Value是容器端口
                var hostPort = portBindingPair.Key;
                var containerPort = portBindingPair.Value;
                var containerPortKey = $"{containerPort}/tcp";

                // 验证端口号是否有效
                if (!int.TryParse(hostPort, out var hostPortNum) || hostPortNum <= 0 || hostPortNum > 65535)
                {
                    continue;
                }

                if (!int.TryParse(containerPort, out var containerPortNum) || containerPortNum <= 0 || containerPortNum > 65535)
                {
                    continue;
                }

                // 配置端口绑定 - 使用更明确的方式
                var portBindingConfig = new PortBinding
                {
                    HostPort = hostPort,
                    HostIP = "0.0.0.0", // 绑定到所有接口
                };

                // 确保PortBindings字典中有这个容器端口的条目
                if (!createContainerParameters.HostConfig.PortBindings.ContainsKey(containerPortKey))
                {
                    createContainerParameters.HostConfig.PortBindings[containerPortKey] = new List<PortBinding>();
                }

                createContainerParameters.HostConfig.PortBindings[containerPortKey].Add(portBindingConfig);

                // 暴露容器端口
                createContainerParameters.ExposedPorts[containerPortKey] = new EmptyStruct();
            }
        }

        // 卷配置
        if (volumeBindings != null && volumeBindings.Count > 0)
        {
            // 配置容器内的挂载点声明
            // 配置主机到容器的卷绑定
            foreach (var volumeBinding in volumeBindings)
            {
                // 添加容器内挂载点声明
                createContainerParameters.Volumes[volumeBinding.Value] = new EmptyStruct();

                // 添加主机到容器的卷绑定
                createContainerParameters.HostConfig.Binds.Add($"{volumeBinding.Key}:{volumeBinding.Value}");
            }
        }

        // 创建容器
        var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParameters);

        return response.ID;
    }

    /// <summary>
    /// 获取容器详细信息
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器详细信息</returns>
    /// <exception cref="DockerContainerNotFoundException">当容器不存在时抛出</exception>
    /// <exception cref="DockerApiException">Docker API调用失败时抛出</exception>
    public async Task<ContainerInspectResponse> InspectContainerAsync(string containerId)
    {
        return await _dockerClient.Containers.InspectContainerAsync(containerId);
    }

    /// <summary>
    /// 获取容器资源使用情况
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器资源使用统计信息</returns>
    /// <exception cref="Exception">获取容器资源信息失败时抛出，包含详细错误信息</exception>
    public async Task<ContainerStatsResponse> GetContainerResourcesAsync(string containerId)
    {
        try
        {
            ContainerStatsResponse result = null;
            var progress = new Progress<ContainerStatsResponse>(stats => result = stats);

            await _dockerClient.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = false },
                progress,
                CancellationToken.None);

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get container resources for {containerId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 重命名容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <param name="newName">新名称</param>
    /// <exception cref="ArgumentException">当容器ID或新名称为空时抛出</exception>
    /// <exception cref="Exception">重命名失败时抛出</exception>
    public async Task RenameContainerAsync(string containerId, string newName)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(containerId, nameof(containerId));
            ArgumentException.ThrowIfNullOrEmpty(newName, nameof(newName));

            var containerRenameParameters = new ContainerRenameParameters
            {
                NewName = newName
            };
            await _dockerClient.Containers.RenameContainerAsync(containerId, containerRenameParameters, CancellationToken.None);
        }
        catch (DockerContainerNotFoundException)
        {
            throw new Exception($"Container with ID {containerId} not found");
        }
        catch (DockerApiException ex)
        {
            throw new Exception($"Failed to rename container {containerId}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while renaming container {containerId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 暂停容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <exception cref="DockerContainerNotFoundException">当容器不存在时抛出</exception>
    /// <exception cref="DockerApiException">当Docker API调用失败时抛出</exception>
    /// <exception cref="Exception">其他错误时抛出</exception>
    public async Task PauseContainerAsync(string containerId)
    {
        await _dockerClient.Containers.PauseContainerAsync(containerId);
    }

    /// <summary>
    /// 恢复暂停的容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <exception cref="DockerContainerNotFoundException">当容器不存在时抛出</exception>
    /// <exception cref="DockerApiException">当Docker API调用失败时抛出</exception>
    /// <exception cref="Exception">其他错误时抛出</exception>
    public async Task UnpauseContainerAsync(string containerId)
    {
        await _dockerClient.Containers.UnpauseContainerAsync(containerId);
    }

    /// <summary>
    /// 创建并启动容器
    /// </summary>
    /// <param name="image">要使用的Docker镜像名称</param>
    /// <param name="tag">镜像标签，默认为latest</param>
    /// <param name="auth">可选的Docker仓库认证信息</param>
    /// <param name="name">容器名称</param>
    /// <param name="environmentVariables">环境变量字典，包含容器启动时需要的环境变量</param>
    /// <param name="portBindings">端口映射字典，定义容器端口与主机端口的映射关系</param>
    /// <param name="volumeBindings">卷映射字典，定义主机路径与容器路径的映射关系</param>
    /// <param name="networkConfig">可选的网络配置</param>
    /// <param name="labelsVariables">标签</param>
    /// <param name="command">命令列表</param>
    /// <remarks>
    /// 此方法首先拉取指定的Docker镜像，然后创建一个新容器并启动它。
    /// </remarks>
    public async Task<string> CreateAndStartContainer(string image, string tag, BaseAuthRequest auth, string name, IDictionary<string, string> environmentVariables = null, IDictionary<string, string> portBindings = null, IDictionary<string, string> volumeBindings = null, NetworkConfig networkConfig = null, IDictionary<string, string> labelsVariables = null, IList<string> command = null)
    {
        await PullImageAsync(image, tag, auth);
        var containerId = await CreateContainerAsync($"{image}:{tag}", name, environmentVariables, portBindings, volumeBindings, networkConfig, labelsVariables, command);
        await RestartContainerAsync(containerId);
        return containerId;
    }

    /// <summary>
    /// 处理环境变量，验证并格式化为Docker容器可用的格式
    /// </summary>
    /// <param name="environmentVariables">环境变量字典，包含键值对形式的环境变量。如果为null，则返回空列表</param>
    /// <returns>格式化后的环境变量字符串列表，格式为"KEY=VALUE"。如果输入为null或空，则返回空列表</returns>
    /// <exception cref="ArgumentException">当环境变量键为空、包含非法字符或值包含非法字符时抛出</exception>
    /// <remarks>
    /// 此方法执行以下验证和处理：
    /// <list type="bullet">
    /// <item><description>如果输入参数为null，直接返回空列表</description></item>
    /// <item><description>验证环境变量键不能为空或仅包含空白字符</description></item>
    /// <item><description>验证环境变量键不能包含等号(=)或空字符(\0)</description></item>
    /// <item><description>验证环境变量值不能包含空字符(\0)</description></item>
    /// <item><description>将null值转换为空字符串</description></item>
    /// <item><description>使用预分配容量的List&lt;string&gt;优化内存分配</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 正常使用
    /// var envVars = new Dictionary&lt;string, string&gt;
    /// {
    ///     { "APP_NAME", "my-app" },
    ///     { "DEBUG_MODE", "true" },
    ///     { "API_KEY", null } // 将被转换为空字符串
    /// };
    /// var result = ProcessEnvironmentVariables(envVars);
    /// // 结果: ["APP_NAME=my-app", "DEBUG_MODE=true", "API_KEY="]
    /// 
    /// // null 参数处理
    /// var emptyResult = ProcessEnvironmentVariables(null);
    /// // 结果: [] (空列表)
    /// </code>
    /// </example>
    private static List<string> ProcessEnvironmentVariables(IDictionary<string, string> environmentVariables)
    {
        // 如果参数为null，返回空列表
        if (environmentVariables == null)
        {
            return new List<string>();
        }

        // 使用对象池优化内存分配
        var envList = new List<string>(environmentVariables.Count);

        foreach (var envVar in environmentVariables)
        {
            // 验证环境变量键值对的有效性
            if (string.IsNullOrWhiteSpace(envVar.Key))
            {
                throw new ArgumentException("环境变量的键不能为空或仅包含空白字符", nameof(environmentVariables));
            }

            // 检查键名是否包含非法字符
            if (envVar.Key.Contains('=') || envVar.Key.Contains('\0'))
            {
                throw new ArgumentException($"环境变量键 '{envVar.Key}' 包含非法字符（= 或 \\0）", nameof(environmentVariables));
            }

            // 处理值为null的情况
            var value = envVar.Value ?? string.Empty;

            // 检查值是否包含非法字符
            if (value.Contains('\0'))
            {
                throw new ArgumentException($"环境变量 '{envVar.Key}' 的值包含非法字符（\\0）", nameof(environmentVariables));
            }

            // 构建环境变量字符串
            envList.Add($"{envVar.Key}={value}");
        }

        return envList;
    }
}