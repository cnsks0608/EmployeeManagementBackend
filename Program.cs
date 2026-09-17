using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Mappings;
using EmployeeManagement.Api.Services.EmployeeServices;
using EmployeeManagement.Api.Services.UserServices;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Services.CompanyServices;
using EmployeeManagement.Api.Services.LogServices;
using EmployeeManagement.Api.Services.ChatServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddControllers() // .NET'e "enum'ları JSON'a çevirirken sayı yerine ismini yaz" diyen hazır bir kütüphane sınıfı.
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IRequestLogService, RequestLogService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT Authentication ayarları - gelen tokenların nasıl doğrulanacağını belirtiyoruz
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>(); // Eğer middleware'i Authentication'dan önce koyarsak, her türlü istek (token geçersiz olsa bile, hatta login denemesi başarısız olsa bile) loglanır — çünkü middleware, Authentication reddetmeden önce devreye giriyor/// Eğer sonra koyarsak, Authentication reddettiği istekler hiç loglanmaz — çünkü Authentication middleware'i, geçersiz durumlarda isteği daha ileri göndermeyebilir.
app.UseExceptionHandler(_ => { });

// Authentication ve Authorization middleware'leri - SIRA ÖNEMLİ, Authentication önce gelmeli
app.UseAuthentication();  // gelen istekte token var mı diye kontrol eder, tokenın içindeki claimleri httpcontext adlı bir yere yerleştiriyor (ancak token yoksa veya geçersizse (token geçersizse httpcontexte claim koymaz) isteği reddetme gibi bir durum yok o controllerda yapılır)
app.UseAuthorization(); // [Authorize] ın çalışmasını sağlayan altyapı

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); -> gelen her isteği https e yönlendirmek için, http lullanacağım için gerek yok 


app.Run();