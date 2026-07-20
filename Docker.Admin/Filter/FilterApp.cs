namespace Docker.Admin.Filter;

/// <summary>
/// 
/// </summary>
public static class FilterApp
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterCollection"></param>
    public static void Init(FilterCollection filterCollection)
    {
        // API Key 鉴权
        filterCollection.Add<ApiKeyAuthFilter>();
        filterCollection.Add<GlobalRequestHandlerFilter>();
        // 异常
        // filterCollection.Add<GlobalExceptionHandlerFilter>();
        // 全局数据格式统一处理
        filterCollection.Add<GlobalResponseHandlerFilter>();
    }
}