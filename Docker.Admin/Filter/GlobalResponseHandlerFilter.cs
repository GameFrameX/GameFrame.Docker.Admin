using GameFrameX.Foundation.Logger;

namespace Docker.Admin.Filter;

/// <summary>
/// 全局请求响应返回处理
/// </summary>
public sealed class GlobalResponseHandlerFilter : IActionFilter
{
    /// <summary>
    /// 
    /// </summary>
    public GlobalResponseHandlerFilter()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            LogHelper.Info($"resp ==> {controllerActionDescriptor.ControllerName}/{controllerActionDescriptor.ActionName} {context.ActionDescriptor.Id}: {JsonHelper.Serialize(objectResult.Value)}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
    }
}