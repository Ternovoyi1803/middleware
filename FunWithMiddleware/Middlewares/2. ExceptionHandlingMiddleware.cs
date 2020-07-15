using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FunWithMiddleware.Middlewares
{
  public class HttpStatusCodeExceptionMiddleware
  {
    private readonly RequestDelegate _next;

    public HttpStatusCodeExceptionMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<HttpStatusCodeExceptionMiddleware> logger,
      IWebHostEnvironment hostingEnvironment)
    {
      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        if (context.Response.HasStarted)
        {
          logger.LogWarning("The response has already started, " +
                            "the http status code middleware will not be executed.");
          throw;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";

        object error;
        int statusCode;
        
        if (ex is ValidationException validation)
        {
          statusCode = StatusCodes.Status422UnprocessableEntity;
          error = new
          {
            error = validation.ValidationResult.ErrorMessage
          };
        }
        else
        {
          statusCode = StatusCodes.Status500InternalServerError;
          var isDevelopment = hostingEnvironment.IsDevelopment();
          
          error = new
          {
            error = isDevelopment ? ex.Message : "An error occurred during processing your request",
            stackTrace = isDevelopment ? ex.StackTrace : null
          };
        }

        context.Response.StatusCode = statusCode;
        var text = JsonSerializer.Serialize(error);
        
        await context.Response.WriteAsync(text);
      }
    }
  }

  // Extension method used to add the middleware to the HTTP request pipeline.
  public static class HttpStatusCodeExceptionMiddlewareExtensions
  {
    public static IApplicationBuilder UseHttpStatusCodeExceptionMiddleware(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<HttpStatusCodeExceptionMiddleware>();
    }
  }
}