using Docker.Admin.Models.Network;
using Docker.DotNet;
using Docker.DotNet.Models;

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
        var logStream = await _dockerClient.Containers.GetContainerLogsAsync(
                            containerId,
                            new ContainerLogsParameters
                            {
                                ShowStdout = true,
                                ShowStderr = true,
                                Tail = "100"
                            });

        using var reader = new StreamReader(logStream);
        return await reader.ReadToEndAsync();
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
            using var stream = await _dockerClient.Containers.GetContainerStatsAsync(
                                   containerId,
                                   new ContainerStatsParameters { Stream = false },
                                   default);

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return System.Text.Json.JsonSerializer.Deserialize<ContainerStatsResponse>(content);
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
    /// <returns>创建的容器ID</returns>
    public async Task<string> CreateContainerAsync(string image, string name, IDictionary<string, string> environmentVariables = null, IDictionary<string, string> portBindings = null, IDictionary<string, string> volumeBindings = null, CreateNetworkRequest networkConfig = null)
    {
        var volumeList = new List<string>();
        if (volumeBindings != null)
        {
            foreach (var volumeBinding in volumeBindings)
            {
                volumeList.Add($"{volumeBinding.Key}:{volumeBinding.Value}");
            }
        }

        await CreateNetwork(networkConfig);

        var parameters = new CreateContainerParameters();
        parameters.Image = image;
        parameters.Name = name;

        if (environmentVariables != null)
        {
            parameters.Env = environmentVariables?.Select(kv => $"{kv.Key}={kv.Value}").ToList();
        }

        // 网络
        if (networkConfig != null)
        {
            parameters.NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    {
                        networkConfig.Name, new EndpointSettings()
                        {
                            IPAddress = networkConfig.IpAddress,
                        }
                    },
                },
            };
        }

        // 端口
        if (portBindings != null)
        {
            parameters.HostConfig = new HostConfig();
            parameters.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();

            foreach (var portBinding in portBindings)
            {
                // 确保端口映射的格式正确
                if (!parameters.HostConfig.PortBindings.ContainsKey(portBinding.Key))
                {
                    parameters.HostConfig.PortBindings[portBinding.Key] = new List<PortBinding>();
                }

                parameters.HostConfig.PortBindings[portBinding.Key].Add(new PortBinding { HostPort = portBinding.Value, HostIP = "0.0.0.0", });
            }

            // 暴露端口
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct>();

            foreach (var portBinding in portBindings)
            {
                parameters.ExposedPorts[portBinding.Key] = default;
            }
        }

        if (volumeList.Count > 0)
        {
            parameters.HostConfig.Binds = volumeList;
        }


        var response = await _dockerClient.Containers.CreateContainerAsync(parameters);

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
            using var stream = await _dockerClient.Containers.GetContainerStatsAsync(containerId, new ContainerStatsParameters { Stream = false }, CancellationToken.None);

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return System.Text.Json.JsonSerializer.Deserialize<ContainerStatsResponse>(content);
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
            if (string.IsNullOrEmpty(containerId))
            {
                throw new ArgumentException("Container ID cannot be empty", nameof(containerId));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("New name cannot be empty", nameof(newName));
            }

            await _dockerClient.Containers.RenameContainerAsync(
                containerId,
                new ContainerRenameParameters
                {
                    NewName = newName
                }, CancellationToken.None);
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
    /// <param name="registry">可选的镜像仓库地址</param>
    /// <param name="username">可选的用户名，用于身份验证</param>
    /// <param name="password">可选的密码，用于身份验证</param>
    /// <param name="name">容器名称</param>
    /// <param name="environmentVariables">环境变量字典，包含容器启动时需要的环境变量</param>
    /// <param name="portBindings">端口映射字典，定义容器端口与主机端口的映射关系</param>
    /// <param name="volumeBindings">卷映射字典，定义主机路径与容器路径的映射关系</param>
    /// <param name="networkConfig">可选的网络配置</param>
    /// <remarks>
    /// 此方法首先拉取指定的Docker镜像，然后创建一个新容器并启动它。
    /// </remarks>
    public async Task CreateAndStartContainer(string image, string tag, string registry, string username, string password, string name, IDictionary<string, string> environmentVariables = null, IDictionary<string, string> portBindings = null, IDictionary<string, string> volumeBindings = null, CreateNetworkRequest networkConfig = null)
    {
        await PullImageAsync(image, tag, registry, username, password);
        var containerId = await CreateContainerAsync($"{image}:{tag}", name, environmentVariables, portBindings, volumeBindings, networkConfig);
        await StartContainerAsync(containerId);
    }
}