using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunWithMiddleware.Middlewares
{
  public class LoggingMiddleware
  {
    private readonly RequestDelegate _next;

    private const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} {Query} {QueryString} " +
                                           "responded {StatusCode} in {Elapsed:0.0000} ms";

    public LoggingMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<LoggingMiddleware> logger)
    {
      var start = Stopwatch.GetTimestamp();
      try
      {
        await _next(httpContext);
        
        var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

        var statusCode = httpContext.Response?.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error : LogLevel.Information;

        logger.Log(level, MessageTemplate,
          httpContext.Request.Method, httpContext.Request.Path,
          httpContext.Request.Query, httpContext.Request.QueryString,
          statusCode, elapsedMs
        );
      }
      // Исключение не будет поймано, поскольку `LogException()` вернёт false.
      // Такой подход нужен чтобы получить контекстную информацию об эксепшене
      catch (Exception ex) when (
        LogException(httpContext, logger, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex)
        )
      {
      }
    }

    private static bool LogException(HttpContext httpContext, ILogger logger, double elapsedMs, Exception ex)
    {
      var request = httpContext.Request;

      var result = logger
        .WithProperty("RequestHeaders", request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()))
        .WithProperty("RequestHost", request.Host)
        .WithProperty("RequestProtocol", request.Protocol);

      if (request.HasFormContentType)
      {
        result = result
          .WithProperty("RequestForm", request.Form.ToDictionary(v => v.Key, v => v.Value.ToString()));
      }
      
      result.LogError(ex, MessageTemplate, 
        httpContext.Request.Method, httpContext.Request.Path,
        httpContext.Request.Query, httpContext.Request.QueryString, 500, elapsedMs);

      return false;
    }

    private static double GetElapsedMilliseconds(long start, long stop)
    {
      return (stop - start) * 1000 / (double) Stopwatch.Frequency;
    }
  }

  public static class LoggingMiddlewareAppExtensions
  {
    public static IApplicationBuilder UseHttpRequestLogging(this IApplicationBuilder applicationBuilder)
    {
      return applicationBuilder.UseMiddleware<LoggingMiddleware>();
    }
  }
}