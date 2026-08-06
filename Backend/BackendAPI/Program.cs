using BackendAPI.Data;
using BackendAPI.Infrastructure;
using BackendAPI.Interfaces;
using BackendAPI.Jobs;
using BackendAPI.Models;
using BackendAPI.Repository;
using BackendAPI.Services;
using BackendAPI.Services.Llm;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using BackendAPI.Analytics;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddMvcOptions(options => options.Filters.Add<SocialExceptionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

builder.Services.AddAuthorization(options =>
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin")));

// ── Dapper context ────────────────────────────────────────────────────────
builder.Services.AddSingleton<DapperContext>();

// ── Repositories ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthRepository,          AuthRepository>();
builder.Services.AddScoped<IPollsRepository,         PollsRepository>();
builder.Services.AddScoped<IVotesRepository,         VotesRepository>();
builder.Services.AddScoped<IUsersRepository,         UsersRepository>();
builder.Services.AddScoped<INotificationsRepository, NotificationsRepository>();
builder.Services.AddScoped<IDashboardRepository,     DashboardRepository>();
builder.Services.AddScoped<ITrendingTopicRepository, TrendingTopicRepository>();
builder.Services.AddScoped<IChallengesRepository,    ChallengesRepository>();
builder.Services.AddScoped<IBusinessRepository,      BusinessRepository>();
builder.Services.AddScoped<IWellnessRepository,      WellnessRepository>();
builder.Services.AddScoped<IAchievementsRepository,  AchievementsRepository>();
builder.Services.AddScoped<IRewardRepository,        RewardRepository>();
builder.Services.AddScoped<IRewardService,           RewardService>();
builder.Services.AddScoped<ISocialRepository,        SocialRepository>();
builder.Services.AddScoped<IGameSessionsRepository,  GameSessionsRepository>();
builder.Services.AddSingleton<ISystemClock,           SystemClock>();

// ── Ingestion Services (US-03, US-04, US-05) ──────────────────────────────
builder.Services.AddHttpClient();
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.Section));
builder.Services.AddSingleton<IAnalyticsOutbox, AnalyticsOutbox>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddHostedService<AnalyticsDispatcher>();
builder.Services.AddHttpClient<IPushNotificationService, ExpoPushNotificationService>();
builder.Services.AddScoped<IRssIngestionService,     RssIngestionService>();
builder.Services.AddScoped<IYouTubeIngestionService, YouTubeIngestionService>();
builder.Services.AddScoped<IGNewsIngestionService,   GNewsIngestionService>();

// ── LLM Providers — all registered; active one chosen via PollGen:Provider config ─
builder.Services.AddScoped<ILlmProvider, OpenAiLlmProvider>();
builder.Services.AddScoped<ILlmProvider, AnthropicLlmProvider>();
builder.Services.AddScoped<ILlmProvider, CustomVmLlmProvider>();

// ── Poll Generation Service (US-07 enhanced: multi-provider) ─────────────
builder.Services.AddScoped<IPollGenerationService, PollGenerationService>();

// ── Hangfire Jobs — must be registered in DI so Hangfire can resolve them ─
builder.Services.AddScoped<IngestionJob>();
builder.Services.AddScoped<PollGenerationJob>();
builder.Services.AddScoped<RetentionNotificationJob>();

// ── Hangfire Dashboard Auth (US-11) ───────────────────────────────────────
builder.Services.AddSingleton<HangfireDashboardAuthFilter>();

// ── Hangfire (US-02) ──────────────────────────────────────────────────────
var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConn, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout     = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval          = TimeSpan.Zero,
        PrepareSchemaIfNecessary   = true   // auto-creates Hangfire schema tables
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;   // light footprint on App Service free/basic tier
});

// ── CORS — allow Next.js frontend ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Unread-Count");
    });
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pollify API v1"));
}

// Hangfire Dashboard — available in all environments.
// US-11: HangfireDashboardAuthFilter allows all in Dev, enforces Basic Auth in Prod.
var authFilter = app.Services.GetRequiredService<HangfireDashboardAuthFilter>();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { authFilter }
});

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Register recurring jobs (US-06, US-08) ───────────────────────────────
// Jobs are registered after the app is built so IRecurringJobManager is available.
using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // US-06: Ingest trending topics every 30 minutes
    recurringJobs.AddOrUpdate<IngestionJob>(
        "ingest-trending-topics",
        job => job.RunAsync(),
        builder.Configuration["Ingestion:Cron"] ?? "*/30 * * * *");

    // US-08: Generate polls from unprocessed topics every 35 minutes
    // (offset by 5 min so ingestion always runs first)
    recurringJobs.AddOrUpdate<PollGenerationJob>(
        "generate-polls-from-topics",
        job => job.RunAsync(),
        builder.Configuration["PollGeneration:Cron"] ?? "5/35 * * * *");

    if (builder.Configuration.GetValue<bool>("PollGeneration:RunOnStartup"))
    {
        BackgroundJob.Enqueue<PollGenerationJob>(job => job.RunAsync());
    }

    recurringJobs.AddOrUpdate<RetentionNotificationJob>(
        "create-retention-notifications",
        job => job.RunAsync(),
        "0 * * * *");
}

app.Run();
