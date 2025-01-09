using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class DockerService
{
    private readonly DockerClient _dockerClient;

    public DockerService()
    {
        _dockerClient = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock")) // Linux
            // new Uri("npipe://./pipe/docker_engine")) // Windows
            .CreateClient();
    }

    // 获取容器列表
    public async Task<IList<ContainerListResponse>> GetContainersAsync()
    {
        return await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true });
    }

    // 获取镜像列表
    public async Task<IList<ImagesListResponse>> GetImagesAsync()
    {
        return await _dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = true });
    }

    // 获取容器日志
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

    // 启动容器
    public async Task StartContainerAsync(string containerId)
    {
        await _dockerClient.Containers.StartContainerAsync(
            containerId,
            new ContainerStartParameters());
    }

    // 停止容器
    public async Task StopContainerAsync(string containerId)
    {
        await _dockerClient.Containers.StopContainerAsync(
            containerId,
            new ContainerStopParameters());
    }

    // 获取容器统计信息
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
} 