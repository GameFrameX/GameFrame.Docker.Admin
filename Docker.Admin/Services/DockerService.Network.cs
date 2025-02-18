using Docker.Admin.Models.Network;

namespace Docker.Admin.Services;

/// <summary>
/// Docker服务类，提供Docker容器和网络管理功能
/// </summary>
public sealed partial class DockerService
{
    /// <summary>
    /// 获取所有网络的列表
    /// </summary>
    /// <returns>网络响应列表</returns>
    public async Task<IList<NetworkResponse>> GetNetworks()
    {
        var networks = await _dockerClient.Networks.ListNetworksAsync();
        return networks;
    }

    /// <summary>
    /// 根据网络名称获取指定网络的信息
    /// </summary>
    /// <param name="name">网络名称</param>
    /// <returns>网络响应，如果未找到则返回默认值</returns>
    public async Task<NetworkResponse> GetNetwork(string name)
    {
        var networks = await _dockerClient.Networks.ListNetworksAsync();
        foreach (var network in networks)
        {
            if (network.Name == name)
            {
                return network;
            }
        }

        return default; // 如果未找到网络，返回默认值
    }

    /// <summary>
    /// 删除指定的网络
    /// </summary>
    /// <param name="deleteNetworkRequest">包含要删除的网络名称的请求</param>
    public async Task DeleteNetwork(DeleteNetworkRequest deleteNetworkRequest)
    {
        var networks = await _dockerClient.Networks.ListNetworksAsync();
        foreach (var network in networks)
        {
            if (network.Name == deleteNetworkRequest.Name)
            {
                await _dockerClient.Networks.DeleteNetworkAsync(network.ID);
                break; // 找到并删除后退出循环
            }
        }
    }

    /// <summary>
    /// 创建自定义网络
    /// </summary>
    /// <param name="networkConfig">网络配置，包括名称、子网等信息</param>
    public async Task CreateNetwork(NetworkConfig networkConfig)
    {
        if (networkConfig == null)
        {
            return;
        }

        var networks = await _dockerClient.Networks.ListNetworksAsync();
        bool isFind = false; // 标记是否找到同名网络
        foreach (var network in networks)
        {
            if (network.Name == networkConfig.Name)
            {
                if (networkConfig.IsRecreate) // 如果需要重建网络
                {
                    await _dockerClient.Networks.DeleteNetworkAsync(network.ID);
                }

                isFind = true; // 找到同名网络
                break; // 退出循环
            }
        }

        // 如果找到并需要重建，或者未找到同名网络，则创建新网络
        if ((isFind && networkConfig.IsRecreate) || !isFind)
        {
            await _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkConfig.Name,
                Driver = "bridge", // 使用桥接驱动
                IPAM = new IPAM
                {
                    Driver = "default", // 使用默认IPAM驱动
                    Config = new List<IPAMConfig>
                    {
                        new IPAMConfig
                        {
                            Subnet = networkConfig.Subnet + "/24", // 设置子网
                            Gateway = networkConfig.Gateway, // 设置网关
                        },
                    },
                },
            });
        }
    }
}