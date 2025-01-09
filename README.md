# Docker 管理服务

基于 .NET 8.0 开发的 Docker 容器管理服务，提供 REST API 接口。

## 功能特点

- 通过 HTTP API 进行 Docker 容器管理
- 服务健康监控
- 安全的 Docker socket 访问
- 容器化部署方案

## 环境要求

- Docker Engine
- Docker Compose
- .NET 8.0 运行时 (本地开发需要)

## 配置说明

### Docker Compose 配置

服务默认运行在 20900 端口（映射自容器内的 8080 端口）。主要配置说明：

- 端口映射：20900:8080
- 自动重启：已启用
- 网络模式：bridge
- 关键挂载：docker.sock 用于与 Docker 守护进程通信

### Dockerfile 说明

- 基于 .NET 8.0 官方镜像构建
- 包含健康检查机制（每30秒检查一次）
- 已配置必要的 Docker 访问权限
- 支持多阶段构建，优化镜像大小

## 部署方法

1. 确保服务器已安装 Docker 和 Docker Compose
2. 克隆代码仓库
3. 在项目根目录执行：
   ```bash
   docker-compose up -d
   ```

## 注意事项

- 确保 docker.sock 挂载正确，这是与 Docker 守护进程通信的关键
- 服务需要具有足够的权限访问 Docker API
- 建议在生产环境中配置适当的安全措施

## 技术栈

- .NET 8.0
- Docker API
- RESTful API
- Docker Compose

