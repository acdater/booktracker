using System.Text;
using BookTracker.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Controllers with global camelCase JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

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

// TODO Story 1.2: Register AppDbContext
// TODO Story 1.2: Register IUserRepository / UserRepository
// TODO Story 1.3: Register IAuthService / AuthService
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
