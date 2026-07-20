using System.Security.Cryptography;
using System.Text;

namespace Docker.Admin.Filter;

/// <summary>
/// API Key 鉴权过滤器
/// 校验请求头 Authorization 中的 Bearer 令牌，密钥由环境变量 DOCKER_ADMIN_API_KEY 配置
/// </summary>
public sealed class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyEnvironmentVariable = "DOCKER_ADMIN_API_KEY";
    private const string AuthorizationHeader = "Authorization";
    private const string Scheme = "Bearer";

    /// <summary>
    /// 授权校验
    /// </summary>
    /// <param name="context">授权过滤器上下文</param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 未配置密钥时不启用鉴权，保持向后兼容
        var expectedKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (string.IsNullOrEmpty(expectedKey))
        {
            return;
        }

        var providedKey = ExtractBearerToken(context.HttpContext.Request);
        if (!IsValid(providedKey, expectedKey))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    private static string ExtractBearerToken(HttpRequest request)
    {
        var header = request.Headers[AuthorizationHeader].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var segments = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2 && string.Equals(segments[0], Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return segments[1];
        }

        return null;
    }

    private static bool IsValid(string providedKey, string expectedKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        // 常量时间比较，防止时序攻击
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
