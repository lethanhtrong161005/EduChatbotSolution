using Business.Background;
using Business.Chunking;
using Business.Embedding;
using Business.ExternalPayment;
using Business.Parsing;
using Business.Services;
using DataAccess.Data;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Presentation.Extensions;
using Presentation.Filters;
using Presentation.Middleware;
using Presentation.RealtimeNotif;
using Presentation.Routing;
using Presentation.Settings;
using StackExchange.Redis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<EduChatAIDbContext>(opts =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new KeyNotFoundException("Could not find connection string.");
    opts.UseNpgsql(connStr, opts => opts.UseVector())
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

// ── Redis ──────────────────────────────────────────────────────
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

// ── Application Services ──────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddScoped<IDocumentIndexer, DocumentIndexer>();
builder.Services.AddSingleton<IDocumentParser, SimpleParser>();
builder.Services.AddSingleton<IDocumentChunker>(new FixedLengthChunker(chunkSize: 1000, overlap: 200));
builder.Services.AddSingleton<IOllamaApiClient, OllamaApiClient>(provider =>
{
    var opts = provider.GetRequiredService<IOptions<OllamaOptions>>().Value;
    return new OllamaApiClient(opts.Endpoint, opts.EmbeddingModel);
});
builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();

// ── Helper Services ───────────────────────────────────────────
builder.Services.AddTransient<IDocumentRealtimeNotifier, SignalRDocumentRealtimeNotifier>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

builder.Services.Configure<PaymentProviderOptions>(builder.Configuration.GetRequiredSection("PaymentProviders"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

// ── Identity Authentication ───────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.User.RequireUniqueEmail = true;
    opts.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<EduChatAIDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new KeyNotFoundException("Google ClientId is not configured.");
        opts.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new KeyNotFoundException("Google ClientSecret is not configured.");
        opts.CallbackPath = AuthenticationSettings.GoogleCallbackPath;
    });

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = AuthenticationSettings.LoginPath;
    opts.LogoutPath = AuthenticationSettings.LogoutPath;
    opts.AccessDeniedPath = AuthenticationSettings.AccessDeniedPath;
    opts.ReturnUrlParameter = AuthenticationSettings.ReturnUrlParamName;
    opts.ExpireTimeSpan = TimeSpan.FromHours(8);
    opts.SlidingExpiration = true;
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization();

// ── Real-time Web ──────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Background Serivces ──────────────────────────────────────────────
builder.Services.AddTransient<AutomaticRetryAttribute>();

builder.Services.AddHangfire((IServiceProvider provider, IGlobalConfiguration config) =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();

    config.UseFilter(provider.GetRequiredService<AutomaticRetryAttribute>());

    config.UsePostgreSqlStorage(
        options =>
        {
            options.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection"));
        });
});

builder.Services.AddHangfireServer();

// ── HTTP Pipeline ──────────────────────────────────────────────
builder.Services.AddScoped<CustomExceptionMiddleware>();

builder.Services.AddRouting(opts => opts.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Default", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Set-Cookie");
    });
});

builder.Services.AddControllersWithViews(opts =>
{
    opts.Conventions.Add(new RouteTokenTransformerConvention(
                            new SlugifyParameterTransformer()));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    await app.MigrateDb<EduChatAIDbContext>();
    await app.SeedDbAsync<EduChatAIDbContext>();
}
else
{
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<CustomExceptionMiddleware>();

app.UseRewriter(new RewriteOptions()
    .Add(new KebabCaseQueryParameterRule()));
app.UseRouting();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()],
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller:slugify=home}/{action:slugify=index}/{id?}");

app.MapHub<DocumentHub>("/documents/status");

app.Run();
