using Business.Services.Account;
using Business.Services.AI;
using Business.Services.AI.Chat;
using Business.Services.AI.Indexing;
using Business.Services.AI.Indexing.Chunking;
using Business.Services.AI.Indexing.Embedding;
using Business.Services.AI.Indexing.Parsing;
using Business.Services.Documents;
using Business.Services.ExternalPayment;
using Business.Services.SubscriptionPlan;
using DataAccess.Data;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Entities;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using Presentation.Background;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Middleware;
using Presentation.Options;
using Presentation.Realtime;
using Presentation.Routing;
using StackExchange.Redis;
using System.ClientModel;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
var connStrName = builder.Environment.IsDevelopment() ? "Container" : "Shared";

var connStr = builder.Configuration.GetConnectionString(connStrName)
              ?? throw new KeyNotFoundException("Connection string not configured.");

builder.Services.AddDbContext<EduChatAiDbContext>(opts =>
{
    opts.UseNpgsql(connStr, opts => opts.UseVector())
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddDbContext<DataProtectionDbContext>(opts =>
{
    opts.UseNpgsql(connStr)
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

// ── Redis ──────────────────────────────────────────────────────
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

// ── Application Services ──────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddScoped<IDocumentIndexer, DocumentIndexer>();
builder.Services.AddScoped<IDocumentStatusRealtimeNotifier, SignalRDocumentStatusRealtimeNotifier>();
builder.Services.AddSingleton<IDocumentParser, LocationAnnotatedParser>();
builder.Services.AddSingleton<IDocumentChunker>(new FixedLengthChunker(chunkSize: 1000, overlap: 200));
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IEmbeddingGeneratorFactory, EmbeddingGeneratorFactory>();

builder.Services.AddScoped<IAiConfigurationResolver, AiConfigurationResolver>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();

builder.Services.AddScoped<IChatPersistenceService, ChatPersistenceService>();
builder.Services.AddScoped<IChatGenerationService, ChatGenerationService>();
builder.Services.AddScoped<IChatGenerationCoordinator, ChatGenerationCoordinator>();
builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();

// ── AI Model Providers ──────────────────────────────────────
var ollamaOpts = builder.Configuration.GetSection("AI:Ollama").Get<OllamaOptions>()
                 ?? throw new KeyNotFoundException("Ollama is not configured.");

var openRouterOpts = builder.Configuration.GetSection("AI:OpenRouter").Get<OpenRouterOptions>()
                       ?? throw new KeyNotFoundException("OpenRouter is not configured.");

builder.Services.AddKeyedEmbeddingGenerator(
    EmbeddingModelName.BgeM3,
    new OllamaApiClient(ollamaOpts.Endpoint, EmbeddingModelName.BgeM3));

builder.Services.AddKeyedEmbeddingGenerator(
    EmbeddingModelName.NemotronEmbedVL_Free,
    new OpenAI.Embeddings.EmbeddingClient(EmbeddingModelName.NemotronEmbedVL_Free, new ApiKeyCredential(openRouterOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(openRouterOpts.Endpoint),
    }).AsIEmbeddingGenerator(defaultModelDimensions: openRouterOpts.DefaultEmbeddingDimensions));

builder.Services.AddKeyedChatClient(
    ChatModelName.Qwen3,
    new OllamaApiClient(ollamaOpts.Endpoint, ChatModelName.Qwen3));

builder.Services.AddKeyedChatClient(
    ChatModelName.OpenRouterFree,
    new OpenAI.Chat.ChatClient(ChatModelName.OpenRouterFree, new ApiKeyCredential(openRouterOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(openRouterOpts.Endpoint),
    }).AsIChatClient());

// ── Background Services ──────────────────────────────────────────────
builder.Services.AddHangfire((IServiceProvider provider, IGlobalConfiguration config) =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();

    config.UseFilterProvider(new HangfireRetryJobFilterProvider());

    config.UsePostgreSqlStorage(
        options =>
        {
            options.UseNpgsqlConnection(connStr);
        });
});

builder.Services.AddHangfireServer(opts =>
{
    opts.Queues = [.. HangfireConstants.Queues];
});

// ── Helper Services ───────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

builder.Services.Configure<PaymentProviderOptions>(builder.Configuration.GetRequiredSection("PaymentProviders"));

// ── Identity Authentication ───────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.User.RequireUniqueEmail = true;
    opts.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<EduChatAiDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = AuthenticationConstants.LoginPath;
    opts.LogoutPath = AuthenticationConstants.LogoutPath;
    opts.AccessDeniedPath = AuthenticationConstants.AccessDeniedPath;
    opts.ReturnUrlParameter = AuthenticationConstants.ReturnUrlParamName;
    opts.ExpireTimeSpan = TimeSpan.FromHours(8);
    opts.SlidingExpiration = true;
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DataProtectionDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new KeyNotFoundException("Google ClientId is not configured.");
        opts.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new KeyNotFoundException("Google ClientSecret is not configured.");
        opts.CallbackPath = AuthenticationConstants.GoogleCallbackPath;
    });

builder.Services.AddAuthorization();

// ── Real-time Web ──────────────────────────────────────────────
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddScoped<IResourceRealtimeNotifier, SignalRResourceRealtimeNotifier>();

// ── HTTP Pipeline ──────────────────────────────────────────────
builder.Services.AddScoped<CustomExceptionMiddleware>();

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Dev", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Set-Cookie", "Content-Encoding");
    });
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.Add(
        new PageRouteTransformerConvention(
            new SlugifyParameterTransformer()));
    options.Conventions.AddPageRoute("/Home/Index", string.Empty);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");

    await app.MigrateDbAsync<EduChatAiDbContext>();
    await app.SeedDbAsync<EduChatAiDbContext>();

    await app.MigrateDbAsync<DataProtectionDbContext>();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseMiddleware<CustomExceptionMiddleware>();

app.UseRewriter(new RewriteOptions()
    .Add(new KebabCaseQueryParameterRule()));

app.UseRouting();

app.UseCors("Dev");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
});

app.MapRazorPages();

app.MapHub<DocumentStatusHub>("/documents/status");
app.MapHub<AiChatHub>("/chat/answer");
app.MapHub<ResourceHub>("/resource");
app.MapHub<CommentHub>("/documents/comments");

app.Run();
