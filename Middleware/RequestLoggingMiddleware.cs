using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;


namespace EmployeeManagement.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)  // context -> o anki http isteğiyle ilgili her şeyi taşıyan kutu gibi
        {
            var httpMethod = context.Request.Method;   // GET, POST, PUT, DELETE / bunlar bir istekten o isteğin işlenmeden önce alınabilcek bilgiler 
            var path = context.Request.Path;            // /api/Employee/GetAllEmployees...

            await _next(context);  // işini halledip sıradaki kısma devreder, program.cs te sıraya yazdığımız middlewarelere 
            var statusCode = context.Response.StatusCode;  // istekle ilgili bütün işlemler gerçekleştikten sonra dönen responseun statuscode unu alırız 

            var username = context.User.Identity?.IsAuthenticated == true // giriş yapan kişinin kimlik bilgisi, gerçekten giriş yapmış mı
                ? context.User.Identity.Name // eğer evet ise → adını al, değilse → boş bırak
                : null;  // middleware Authentication'dan önce çalışıyorsa, bu satır kesinlikle _next()'ten sonra olmalı; sonra çalışıyorsa fark etmez 

            var log = new RequestLog
            {
                HttpMethod = httpMethod,
                Path = path,
                StatusCode = statusCode,
                Username = username,  // bu alanda username için istek gönderilirkenki headerdaki token okunuyor yani biz login olurken header kısmında henüz token olmadığı için login işleminde username null olarak geliyor
                CreatedAt = DateTime.UtcNow
            };

            dbContext.RequestLogs.Add(log);
            await dbContext.SaveChangesAsync();

        }
    }
}