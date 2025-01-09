# 使用 .NET 8.0 运行时作为基础镜像
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# 使用 .NET 8.0 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# 设置构建配置为Release模式
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# 复制项目文件并还原依赖
COPY ["Docker.Admin/Docker.Admin.csproj", "Docker.Admin/"]
RUN dotnet restore "Docker.Admin/Docker.Admin.csproj"
# 复制所有源代码
COPY . .
WORKDIR "/src/Docker.Admin"
# 构建应用
RUN dotnet build "Docker.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 发布阶段
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# 发布应用
RUN dotnet publish "Docker.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最终阶段：使用运行时镜像
FROM base AS final
WORKDIR /app
# 从发布阶段复制编译好的文件
COPY --from=publish /app/publish .

# 设置容器内用户权限，使其能访问 Docker socket
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd -g 999 docker \
    && usermod -aG docker root

# 暴露端口
EXPOSE 8080

# 添加健康检查：每30秒检查一次，超时时间3秒
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost/health || exit 1

# 设置入口点：运行 .NET 应用
ENTRYPOINT ["dotnet", "Docker.Admin.dll"]

# 挂载 Docker socket，用于与Docker守护进程通信
VOLUME /var/run/docker.sock 