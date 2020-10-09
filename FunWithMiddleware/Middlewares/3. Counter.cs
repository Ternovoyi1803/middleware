using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunWithMiddleware.Middlewares
{
  public class CounterMiddleware
  {
    private readonly RequestDelegate _next;
    private int _counter;

    public CounterMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CounterMiddleware> logger)
    {
      Interlocked.Increment(ref _counter);
      logger.LogInformation("Processed {Count} requests", _counter);

      var queue = new ConcurrentQueue<int>();

      while (true)
      {
        // int version = 0
        
        // 
        // Interlocked.Increment
      }

      // Вызов следующего мидлваре в конвеере
      await _next(context);
    }
  }
}