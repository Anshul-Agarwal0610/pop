using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Repository;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Pollify API", Version = "v1" });
});

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── Dapper context ────────────────────────────────────────────────────────
builder.Services.AddSingleton<DapperContext>();

// ── Repositories ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthRepository,           AuthRepository>();
builder.Services.AddScoped<IPollsRepository,          PollsRepository>();
builder.Services.AddScoped<IVotesRepository,          VotesRepository>();
builder.Services.AddScoped<IUsersRepository,          UsersRepository>();
builder.Services.AddScoped<IDashboardRepository,      DashboardRepository>();
builder.Services.AddScoped<ITrendingTopicRepository,  TrendingTopicRepository>();

// ── CORS — allow Next.js frontend ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pollify API v1"));
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
