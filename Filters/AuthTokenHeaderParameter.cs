using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

public class AuthTokenHeaderParameter : IOperationFilter
{
    public void Apply(Operation operation, OperationFilterContext context)
    {
        operation.Parameters = operation.Parameters ?? new List<IParameter>();
        //MemberAuthorizeAttribute 自定义的身份验证特性标记
        var isAuthor = operation != null && context != null;
        if (isAuthor)
        {
            //in query header
            operation.Parameters.Add(new NonBodyParameter()
            {
                Name = "Authorization",
                In = "header", //query formData ..
                Description = "身份验证 Authorization: Bearer tokenvalue",
                Required = false,
                Type = "string"
            });
        }
    }
}