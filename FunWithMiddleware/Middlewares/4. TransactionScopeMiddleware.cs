using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FunWithMiddleware.Middlewares
{
  public class TransactionScopeMiddleware
  {
    private readonly RequestDelegate _next;

    public TransactionScopeMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
      if (context.Request.Method == HttpMethods.Post)
      {
        await using var transaction = await db.Database.BeginTransactionAsync();
        
        await _next(context);
        await transaction.CommitAsync();
      }
      else
      {
        await _next(context);
      }
    }
  }
}