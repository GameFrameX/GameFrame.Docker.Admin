@@ -0,0 +1,21 @@
﻿FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DockerAPI/DockerAPI.csproj", "DockerAPI/"]
RUN dotnet restore "DockerAPI/DockerAPI.csproj"
COPY . .
WORKDIR "/src/DockerAPI"
RUN dotnet build "DockerAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DockerAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
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
ENTRYPOINT ["dotnet", "DockerAPI.dll"]

# 挂载 Docker socket
VOLUME /var/run/docker.sock 