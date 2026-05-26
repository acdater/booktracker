using System.Text;
using BookTracker.Api.Data;
using BookTracker.Api.Middleware;
using BookTracker.Api.Repositories;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Controllers with global camelCase JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errorMessage = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault() ?? "Validation failed.";
        return new BadRequestObjectResult(new { error = errorMessage, code = "VALIDATION_ERROR" });
    };
});

// CORS — permissive for local development
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// JWT bearer authentication (wired now; enforced from Story 1.4)
var jwtSecret = builder.Configuration["JWT__Secret"] ?? "dev-placeholder-secret-change-before-use";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

// Swagger/OpenAPI — Development only
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

// TODO Story 2.1: Register IBookRepository, IUserBookRepository
// TODO Story 2.2: Register IBookService / BookService + IHttpClientFactory
// TODO Story 2.4: Register IShelfService / ShelfService
// TODO Story 4.1: Register IStatsService / StatsService

var app = builder.Build();

// ExceptionHandlingMiddleware — must be FIRST in pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
