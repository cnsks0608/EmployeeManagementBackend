using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace EmployeeManagement.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        // şifre içerebilecek key isimlerini burada topluyoruz, büyük/küçük harf duyarsız arayacağız
        private static readonly Regex PasswordFieldRegex = new Regex(
            "\"(password|newPassword|currentPassword|confirmPassword)\"\\s*:\\s*\"[^\"]*\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)  // context -> o anki http isteğiyle ilgili her şeyi taşıyan kutu gibi
        {
            var httpMethod = context.Request.Method;   // GET, POST, PUT, DELETE / bunlar bir istekten o isteğin işlenmeden önce alınabilcek bilgiler 
            var path = context.Request.Path;            // /api/Employee/GetAllEmployees...
            var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;
            var requestBody = await ReadAndMaskRequestBodyAsync(context.Request);

            await _next(context);  // işini halledip sıradaki kısma devreder, program.cs te sıraya yazdığımız middlewarelere 
            var statusCode = context.Response.StatusCode;  // istekle ilgili bütün işlemler gerçekleştikten sonra dönen responseun statuscode unu alırız 

            var username = context.User.Identity?.IsAuthenticated == true // giriş yapan kişinin kimlik bilgisi, gerçekten giriş yapmış mı
                ? context.User.Identity.Name // eğer evet ise → adını al, değilse → boş bırak
                : null;  // middleware Authentication'dan önce çalışıyorsa, bu satır kesinlikle _next()'ten sonra olmalı; sonra çalışıyorsa fark etmez 

            var log = new RequestLog
            {
                HttpMethod = httpMethod,
                Path = path,
                QueryString = queryString,
                RequestBody = requestBody,
                StatusCode = statusCode,
                Username = username,  // bu alanda username için istek gönderilirkenki headerdaki token okunuyor yani biz login olurken header kısmında henüz token olmadığı için login işleminde username null olarak geliyor
                CreatedAt = DateTime.UtcNow
            };

            dbContext.RequestLogs.Add(log);
            await dbContext.SaveChangesAsync();

        }

        // body'yi okur, şifre alanlarını maskeler, controller'ın body'yi hâlâ okuyabilmesi için stream'i başa sarar
        private async Task<string?> ReadAndMaskRequestBodyAsync(HttpRequest request)
        {
            // sadece JSON body'leri okuyoruz (dosya yükleme gibi diğer türleri okumaya çalışmıyoruz)
            if (!request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                return null;
            }

            if (request.ContentLength == null || request.ContentLength == 0)
            {
                return null;
            }

            request.EnableBuffering();  // body'nin birden fazla kez okunabilmesini sağlıyoruz

            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;  // controller'ın (model binding'in) body'yi baştan okuyabilmesi için sıfırlıyoruz

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            // şifre içeren alanları maskeliyoruz
            var maskedBody = PasswordFieldRegex.Replace(body, match =>
            {
                var fieldNameMatch = Regex.Match(match.Value, "\"([^\"]+)\"");
                var fieldName = fieldNameMatch.Value;
                return $"{fieldName}: \"***\"";
            });

            return maskedBody;
        }
    }
}
