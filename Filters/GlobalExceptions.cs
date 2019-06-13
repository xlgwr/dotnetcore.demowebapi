using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NLog;

public class GlobalExceptions : IExceptionFilter

{
    private static Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IHostingEnvironment _env;

    public GlobalExceptions(IHostingEnvironment env)
    {
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {

        var json = new JsonErrorResponse();

        //这里面是自定义的操作记录日志

        if (context.Exception.GetType() == typeof(UserOperationException))
        {

            json.Message = context.Exception.Message;

            if (_env.IsDevelopment())
            {

                json.DevelopmentMessage = context.Exception.StackTrace;//堆栈信息

            }

            context.Result = new BadRequestObjectResult(json);//返回异常数据

        }
        else
        {

            json.Message = "发生了未知内部错误";

            if (_env.IsDevelopment())
            {
                json.DevelopmentMessage = context.Exception.StackTrace;//堆栈信息
            }
            context.Result = new InternalServerErrorObjectResult(json);
        }
        //采用log4net 进行错误日志记录
        _logger.Error(context.Exception,json.Message, null);

    }

}

public class InternalServerErrorObjectResult : ObjectResult
{

    public InternalServerErrorObjectResult(object value) : base(value)
    {

        StatusCode = StatusCodes.Status500InternalServerError;

    }

}

public class JsonErrorResponse
{

    /// <summary>
    /// 生产环境的消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 开发环境的消息
    /// </summary>
    public string DevelopmentMessage { get; set; }

}

/// <summary>
/// 操作日志
/// </summary>
public class UserOperationException : Exception
{

    public UserOperationException() { }

    public UserOperationException(string message) : base(message) { }

    public UserOperationException(string message, Exception innerException) : base(message, innerException) { }

}
