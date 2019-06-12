using System;
using Microsoft.AspNetCore.Mvc.Filters;
using NLog;

namespace demowebapi.Filters
{
    public class LogHttpRequestAttribute : ActionFilterAttribute
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.Debug("准备 Action Executing:" + context.HttpContext.Request.Path);
            base.OnActionExecuting(context);
        }
    }
}
