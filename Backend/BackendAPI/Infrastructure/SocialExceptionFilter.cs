using BackendAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BackendAPI.Infrastructure;
public sealed class SocialExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch { SocialConflictException => 409, SocialForbiddenException => 403, SocialNotFoundException => 404, SocialRateLimitException => 429, _ => 0 };
        if (status == 0) return;
        context.Result = new ObjectResult(new { message = context.Exception.Message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
