# Docker 管理服务

基于 .NET 10.0 开发的 Docker 容器管理服务，提供 REST API 接口。

## 功能特点

- 通过 HTTP API 进行 Docker 容器管理
- 服务健康监控
- 安全的 Docker socket 访问
- 容器化部署方案

## 技术架构

### 整体设计

服务采用经典三层结构，对外通过 HTTP API 暴露能力，内部经 `Docker.DotNet` 客户端直接与本地 Docker 守护进程通信。Docker 端点按平台自动选择：Linux 下走 `unix:///var/run/docker.sock`，Windows 下走 `npipe://./pipe/docker_engine`，Docker Engine API 协议版本固定为 `v1.47`。

```
┌────────────────────────────────────────────────────┐
│            客户端 / 上游服务 (HTTP)                 │
└──────────────────────┬─────────────────────────────┘
                       │ REST API (容器内 8080，宿主机 20900)
┌──────────────────────▼─────────────────────────────┐
│   ASP.NET Core MVC (Program.cs 装配管线)             │
│   ├─ Filter：请求/响应统一日志、HttpJsonResult 封装 │
│   └─ Controllers：Containers / Image / NetWork     │
└──────────────────────┬─────────────────────────────┘
                       │ 依赖注入（DockerService 单例）
┌──────────────────────▼─────────────────────────────┐
│        Services：DockerService (partial class)      │
│   按领域拆分为 Container / Image / Network 三部分   │
│   + DockerImageCleanupPlanner（纯算法，便于测试）   │
└──────────────────────┬─────────────────────────────┘
                       │ Docker Engine API v1.47
┌──────────────────────▼─────────────────────────────┐
│      Docker.DotNet → docker.sock / npipe            │
└────────────────────────────────────────────────────┘
```

### 分层职责

| 层 | 位置 | 职责 |
|---|---|---|
| 入口与装配 | `Program.cs` | Serilog 日志、MVC、健康检查 `/health`、Swagger（仅开发环境）、`DockerService` 单例注册 |
| 控制器层 | `Controllers/` | 接收请求、异常兜底、统一包装 `HttpJsonResult`；不承载业务逻辑 |
| 服务层 | `Services/` | 封装 Docker 操作；`DockerService` 以 `partial` 形式按领域拆分到三个文件 |
| 算法层 | `Services/DockerImageCleanupPlanner.cs` | 镜像清理候选选择，纯函数、无副作用，便于单元测试 |
| 过滤器 | `Filter/` | 全局请求日志、响应日志（统一响应格式由 `HttpJsonResult` 承载） |
| 数据契约 | `Models/` | 请求/响应 DTO，按 `Container` / `Image` / `Network` 子目录组织 |

### 关键技术选型

- **运行时**：.NET 10.0，`LangVersion` 10，`ImplicitUsings` 开启，`Nullable` 关闭
- **Web 框架**：ASP.NET Core MVC（`AddControllers` + `[ApiController]`），非 Minimal API、非 Furion
- **Docker 客户端**：`Docker.DotNet` 3.125.15 + `Docker.DotNet.X509`，按平台自动选择 socket 端点
- **统一响应**：`GameFrameX.Foundation.Http.Normalization.HttpJsonResult`，所有接口统一返回该结构
- **日志**：Serilog（`Serilog.AspNetCore`）经 `GameFrameX.Foundation.Logger.LogHandler` 创建，`LogType = "DockerAdmin"`
- **API 文档**：Swashbuckle Swagger，仅 `Development` 环境启用，并加载 XML 注释（`GenerateDocumentationFile` 已开启）
- **JSON 序列化**：Newtonsoft.Json（通过 `GameFrameX.Foundation.Json` 封装）

### API 路由约定

控制器基类统一标注 `[Route("api/[controller]/[action]")]`，路由前缀由控制器类名（去掉 `Controller` 后缀）决定，方法名即 action（ASP.NET Core 路由匹配大小写不敏感）：

| 控制器 | 路由前缀 |
|---|---|
| `ContainersController` | `/api/containers` |
| `ImageController` | `/api/image` |
| `NetWorkController` | `/api/netWork` |

示例：`GET /api/containers/getList`、`POST /api/containers/start`、`POST /api/image/pull`、`POST /api/netWork/create`。

## 目录结构

```
GameFrame.Docker.Admin/
├── Docker.Admin/                        # 主服务项目
│   ├── Program.cs                       # 应用入口：日志、MVC、健康检查、Swagger、DI 装配
│   ├── BaseController.cs                # 控制器基类：统一路由 [api/[controller]/[action]]、注入 DockerService
│   ├── GlobalUsing.cs                   # 全局 using 引用
│   ├── Docker.Admin.csproj              # 项目文件与 NuGet 依赖
│   ├── Controllers/                     # HTTP 入口
│   │   ├── ContainersController.cs      # 容器：列表/启停/创建/删除/日志/统计/重命名等
│   │   ├── ImageController.cs           # 镜像：列表/拉取/删除
│   │   └── NetWorkController.cs         # 网络：列表/查询/创建/删除
│   ├── Services/                        # 业务封装层
│   │   ├── DockerService.cs             # partial 主类：DockerClient 初始化与端点选择
│   │   ├── DockerService.Container.cs   # 容器领域操作
│   │   ├── DockerService.Image.cs       # 镜像领域操作（含拉取前磁盘清理）
│   │   ├── DockerService.Network.cs     # 网络领域操作
│   │   └── DockerImageCleanupPlanner.cs # 镜像清理候选选择（纯算法，可单测）
│   ├── Filter/                          # MVC 过滤器
│   │   ├── FilterApp.cs                 # 过滤器统一注册入口
│   │   ├── GlobalRequestHandlerFilter.cs# 全局请求日志
│   │   └── GlobalResponseHandlerFilter.cs# 全局响应日志
│   ├── Models/                          # 请求/响应 DTO
│   │   ├── BaseRequest.cs               # Id 基类请求（镜像/容器/网络通用）
│   │   ├── BaseDeleteRequest.cs         # 含 IsForce 的删除请求
│   │   ├── BaseAuthRequest.cs           # 私有仓库认证信息（Registry/Username/Password/Email）
│   │   ├── Container/                   # 容器相关请求模型
│   │   ├── Image/                       # 镜像相关请求模型
│   │   └── Network/                     # 网络相关请求模型
│   └── Properties/
│       ├── AssemblyInfo.cs
│       └── launchSettings.json
├── Docker.Admin.Tests/                  # 单元测试项目（引用 Docker.Admin）
│   ├── Docker.Admin.Tests.csproj
│   └── Program.cs                       # 测试入口（镜像清理算法等）
├── Server.Docker.Admin.slnx             # 解决方案文件（.slnx 格式）
├── Dockerfile                           # 多阶段构建脚本
├── docker-compose.yml                   # 编排配置：端口、挂载、网络
├── .dockerignore
├── .gitignore
├── .github/workflows/                   # CI：contributors-readme、仓库同步
└── README.md
```

## 环境要求

- Docker Engine
- Docker Compose
- .NET 10.0 运行时 (本地开发需要)

## 配置说明

### Docker Compose 配置

服务默认运行在 20900 端口（映射自容器内的 8080 端口）。主要配置说明：

- 端口映射：20900:8080
- 自动重启：已启用
- 网络模式：bridge
- 关键挂载：docker.sock 用于与 Docker 守护进程通信

### 镜像拉取前磁盘保护

服务在拉取镜像前会检查可用磁盘空间。空间低于阈值时，会清理未被任何容器引用的旧镜像；清理顺序按镜像创建时间从旧到新执行，并保留同仓库最近的镜像用于回滚。

可用环境变量：

- `DOCKER_ADMIN_IMAGE_CLEANUP_ENABLED`：是否启用自动清理，默认 `true`
- `DOCKER_ADMIN_MIN_FREE_SPACE_BYTES`：拉取镜像前要求的最小可用空间，默认 `2147483648`（2 GB）
- `DOCKER_ADMIN_KEEP_RECENT_IMAGES`：每个镜像仓库保留的最近镜像数量，默认 `3`
- `DOCKER_ADMIN_PROTECTED_IMAGE_TAGS`：永不自动清理的完整镜像标签，多个值用英文逗号分隔
- `DOCKER_ADMIN_DOCKER_DISK_PATH`：用于检测磁盘空间的 Docker 数据目录路径；容器内无法访问宿主机 Docker 数据目录时，建议挂载该目录并显式配置此变量

### Dockerfile 说明

- 基于 .NET 10.0 官方镜像构建
- 包含健康检查机制（每30秒检查一次）
- 已配置必要的 Docker 访问权限
- 支持多阶段构建，优化镜像大小

### 接口鉴权

服务支持基于 API Key 的接口鉴权，防止未授权调用。配置后，除健康检查外所有接口都需要在请求头携带密钥。

> ⚠️ **必须配合 HTTPS 使用**：API Key 通过请求头明文传输，公网部署需由反向代理（Caddy/Nginx 等）或 Kestrel 证书终止 TLS，否则密钥会被中间人嗅探。仅启用 API Key 而不启用 HTTPS，在公网环境下不能视为安全。

可用环境变量：

- `DOCKER_ADMIN_API_KEY`：API 密钥。**未配置时鉴权关闭**，所有接口可匿名访问（仅适合内网或开发环境）

启用后，请求需携带请求头：

```
Authorization: Bearer <DOCKER_ADMIN_API_KEY>
```

调用示例：

```bash
curl -H "Authorization: Bearer $DOCKER_ADMIN_API_KEY" \
     -H "Content-Type: application/json" \
     -X POST http://your-host:20900/api/containers/start \
     -d '{"id":"abc123"}'
```

补充说明：

- 健康检查端点 `/health` 不受鉴权限制，Dockerfile 中的 `HEALTHCHECK` 无需携带密钥
- 启动时若未配置密钥，服务会输出 WARNING 日志提醒
- 密钥轮换：更新环境变量后重启服务即可
- CI/CD 场景下，密钥应存放在 CI/CD 的 Secrets 中通过环境变量注入，不要明文写入流水线配置

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

- .NET 10.0
- Docker API
- RESTful API
- Docker Compose
