using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DockerService>();

var app = builder.Build();

// 配置 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API 端点
// 获取容器列表
app.MapGet("/api/containers", async (DockerService docker) =>
    await docker.GetContainersAsync());

// 获取镜像列表
app.MapGet("/api/images", async (DockerService docker) =>
    await docker.GetImagesAsync());

// 获取容器日志
app.MapGet("/api/containers/{id}/logs", async (string id, DockerService docker) =>
    await docker.GetContainerLogsAsync(id));

// 启动容器
app.MapPost("/api/containers/{id}/start", async (string id, DockerService docker) =>
{
    await docker.StartContainerAsync(id);
    return Results.Ok();
});

// 停止容器
app.MapPost("/api/containers/{id}/stop", async (string id, DockerService docker) =>
{
    await docker.StopContainerAsync(id);
    return Results.Ok();
});

// 获取容器统计信息
app.MapGet("/api/containers/{id}/stats", async (string id, DockerService docker) =>
    await docker.GetContainerStatsAsync(id));

// 添加服务注册
builder.Services.AddSingleton<DockerComposeService>();

// 添加 Docker Compose API 端点
app.MapPost("/api/docker-compose", async (IFormFile file, DockerComposeService dockerCompose) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded");

    var filePath = Path.GetTempFileName();
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    await dockerCompose.ExecuteDockerComposeAsync(filePath);
    File.Delete(filePath);
    
    return Results.Ok();
});

app.Run();