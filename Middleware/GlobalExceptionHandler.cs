using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;  // sitemde try catch ile yakalayamadığımız beklenmeyen bir hata olduğunda internal error ile kullanıcıya düzgün bir hata mesajı göndeririz 
            httpContext.Response.ContentType = "application/json";

            var errorResponse = new
            {
                message = "Beklenmeyen bir hata oluştu.",
                detail = exception.Message
            };

            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

            return true;
        }
    }
}