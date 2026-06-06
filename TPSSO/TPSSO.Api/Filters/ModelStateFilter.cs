using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Taipi.Core.RQRS;

namespace TPSSO.Api.Filters;

/// <summary>
/// 自动拦截 ModelState 校验失败，统一返回 StatusResponseResult 格式
/// </summary>
public class ModelStateFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = string.Join("；", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            context.Result = new OkObjectResult(StatusResponseResult.BadRequest(errors));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
