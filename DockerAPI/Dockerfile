# 使用 SDK 镜像作为构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY ["DockerManager.csproj", "./"]
RUN dotnet restore

# 复制所有源代码
COPY . .

# 构建应用
RUN dotnet build -c Release -o /app/build

# 发布应用
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# 最终运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 设置容器内用户权限，使其能访问 Docker socket
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd -g 999 docker \
    && usermod -aG docker root

# 暴露端口
EXPOSE 80
EXPOSE 443

# 添加健康检查
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost/health || exit 1

# 设置入口点
ENTRYPOINT ["dotnet", "DockerManager.dll"]

# 挂载 Docker socket
VOLUME /var/run/docker.sock 