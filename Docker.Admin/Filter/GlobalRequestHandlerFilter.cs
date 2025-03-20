using GameFrameX.Foundation.Logger;

namespace Docker.Admin.Filter;

/// <summary>
/// 全局请求过滤器
/// </summary>
public sealed class GlobalRequestHandlerFilter : IActionFilter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is BaseController baseController)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            LogHelper.Info($"req ==> {controllerActionDescriptor.ControllerName}/{controllerActionDescriptor.ActionName} {context.ActionDescriptor.Id}: {JsonHelper.Serialize(context.ActionArguments)}");
        }
    }
}