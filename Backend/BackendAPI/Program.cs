using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Repository;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Pollify API", Version = "v1" });
});

// Dapper context
builder.Services.AddSingleton<DapperContext>();

// Repositories
builder.Services.AddScoped<IPollsRepository, PollsRepository>();
builder.Services.AddScoped<IVotesRepository, VotesRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// CORS — allow Next.js frontend on localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pollify API v1"));
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.MapControllers();

app.Run();
