using Business.Services.Account;
using Business.Services.AI;
using Business.Services.AI.Chat;
using Business.Services.AI.Indexing;
using Business.Services.AI.Indexing.Chunking;
using Business.Services.AI.Indexing.Embedding;
using Business.Services.AI.Indexing.Parsing;
using Business.Services.Documents;
using Business.Services.Documents.File;
using Business.Services.Subscriptions;
using Business.Services.Subscriptions.ExternalPayment;
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
using MimeDetective;
using MimeDetective.Storage;
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
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Database")
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

builder.Services.AddScoped<IAiConfigurationResolver, AiConfigurationResolver>();
builder.Services.AddScoped<IAiConfigurationAdminService, AiConfigurationAdminService>();

builder.Services.AddScoped<ISubjectReindexCoordinator, SubjectReindexCoordinator>();
builder.Services.AddSingleton<ISubjectReindexDispatcher, HangfireSubjectReindexDispatcher>();

builder.Services.AddScoped<IDocumentIndexingCoordinator, SingleRunDocumentIndexingCoordinator>();

builder.Services.AddSingleton<IDocumentParser, LocationAnnotatedParser>();

builder.Services.AddSingleton<IDocumentChunker, FixedLengthChunker>();
builder.Services.AddSingleton<IDocumentChunker, RecursiveSeparatorChunker>();
builder.Services.AddSingleton<IDocumentChunker, SentenceParagraphChunker>();
builder.Services.AddSingleton<IDocumentChunkerSelector, DocumentChunkerSelector>();

builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IEmbeddingGeneratorFactory, EmbeddingGeneratorFactory>();

builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();

builder.Services.AddScoped<IChatPersistenceService, ChatPersistenceService>();
builder.Services.AddScoped<IChatGenerationService, ChatGenerationService>();
builder.Services.AddScoped<IChatGenerationCoordinator, ChatGenerationCoordinator>();

builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();


// ── File Storage ──────────────────────────────────────
var supabaseOpts = builder.Configuration.GetSection("BlobStorage:Supabase").Get<SupabaseOptions>()
                   ?? throw new KeyNotFoundException("Supabase is not configured.");

builder.Services.AddSingleton(_ => new Supabase.Client(supabaseOpts.ApiUrl, supabaseOpts.ApiSecretKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
}));

builder.Services.Configure<FileStorageOptions>(opts =>
{
    opts.AppDirectory = AppConstants.AppDir;
    opts.FileDirectoryBuffer = AppConstants.FileDirBuffer;
    opts.FileDirectoryStaging = AppConstants.FileDirStaging;
    opts.FileDirectoryReceived = AppConstants.FileDirReceived;
    opts.FileDirectoryProcessing = AppConstants.FileDirProcessing;
    opts.FileDirectoryIndexed = AppConstants.FileDirIndexed;
    opts.FileDirectoryFailed = AppConstants.FileDirFailed;
});

builder.Services.AddKeyedScoped<IDurableStorageStrategy, LocalHardDriveDurableStorageStrategy>(DocumentStorageMethod.LocalHardDrive);
builder.Services.AddKeyedScoped<IDurableStorageStrategy, SupabaseDurableStorageStrategy>(DocumentStorageMethod.Supabase);
builder.Services.AddScoped<IDocumentStorageMethodResolver, DocumentStorageMethodResolver>();
builder.Services.AddScoped<IDocumentFileService, DocumentFileService>();
builder.Services.AddScoped<IDocumentFileReceptionService, DocumentFileReceptionService>();
builder.Services.AddScoped<DocumentPersistenceJob>();
builder.Services.AddSingleton<IStagingFileStore, LocalStagingFileStore>();
builder.Services.AddSingleton<ILocalFileBuffer, LocalFileBuffer>();

// ── File Validation ──────────────────────────────────────

var allDefinitions = MimeDetective.Definitions.DefaultDefinitions.All();

var extensions = AppConstants.AllowedExtensions.WithComparer(StringComparer.InvariantCultureIgnoreCase);

var scopedDefinitions = allDefinitions
    .ScopeExtensions(extensions)
    .TrimMeta()
    .TrimCategories()
    .TrimDescription()
    .ToImmutableArray();

var inspector = new ContentInspectorBuilder
{
    Definitions = scopedDefinitions,
}.Build();

builder.Services.AddSingleton(inspector);

var fileExtensionToMimeTypes = new FileExtensionToMimeTypeLookupBuilder()
{
    Definitions = scopedDefinitions,
}.Build();

var mimeTypes = extensions
    .Select(e => fileExtensionToMimeTypes.TryGetValue(e) ?? string.Empty)
    .Where(e => !string.IsNullOrEmpty(e))
    .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

builder.Services.Configure<FileValidationOptions>(opts =>
{
    opts.AllowedExtensions = extensions;
    opts.AllowedMimeType = mimeTypes;
});

builder.Services.AddSingleton<IDocumentFileValidator, DocumentFileValidator>();

// ── AI Model Providers ──────────────────────────────────────
var ollamaOpts = builder.Configuration.GetSection("AI:Ollama").Get<OllamaOptions>()
                 ?? throw new KeyNotFoundException("Ollama is not configured.");

var openRouterOpts = builder.Configuration.GetSection("AI:OpenRouter").Get<OpenRouterOptions>()
                     ?? throw new KeyNotFoundException("OpenRouter is not configured.");

var geminiOpts = builder.Configuration.GetSection("AI:Gemini").Get<GeminiOptions>()
                 ?? throw new KeyNotFoundException("OpenRouter is not configured.");

builder.Services.AddKeyedEmbeddingGenerator(
    EmbeddingModelName.BgeM3,
    new OllamaApiClient(ollamaOpts.Endpoint, EmbeddingModelName.BgeM3));

builder.Services.AddKeyedEmbeddingGenerator(
    EmbeddingModelName.NemotronEmbedVLFree,
    new OpenAI.Embeddings.EmbeddingClient(EmbeddingModelName.NemotronEmbedVLFree, new ApiKeyCredential(openRouterOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(openRouterOpts.Endpoint),
    }).AsIEmbeddingGenerator(defaultModelDimensions: openRouterOpts.DefaultEmbeddingDimensions));

builder.Services.AddKeyedEmbeddingGenerator(
    EmbeddingModelName.GeminiEmbedding2,
    new OpenAI.Embeddings.EmbeddingClient(EmbeddingModelName.GeminiEmbedding2, new ApiKeyCredential(geminiOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(geminiOpts.Endpoint),
    }).AsIEmbeddingGenerator(defaultModelDimensions: geminiOpts.DefaultEmbeddingDimensions));

builder.Services.AddKeyedChatClient(
    ChatModelName.Qwen3,
    new OllamaApiClient(ollamaOpts.Endpoint, ChatModelName.Qwen3));

builder.Services.AddKeyedChatClient(
    ChatModelName.OpenRouterFree,
    new OpenAI.Chat.ChatClient(ChatModelName.OpenRouterFree, new ApiKeyCredential(openRouterOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(openRouterOpts.Endpoint),
    }).AsIChatClient());

builder.Services.AddKeyedChatClient(
    ChatModelName.Gemini31ProPreview,
    new OpenAI.Chat.ChatClient(ChatModelName.Gemini31ProPreview, new ApiKeyCredential(geminiOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(geminiOpts.Endpoint),
    }).AsIChatClient());

builder.Services.AddKeyedChatClient(
    ChatModelName.Gemini35Flash,
    new OpenAI.Chat.ChatClient(ChatModelName.Gemini35Flash, new ApiKeyCredential(geminiOpts.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(geminiOpts.Endpoint),
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
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetRequiredSection("BlobStorage:Supabase"));

// ── Identity Authentication ───────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
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
builder.Services.AddScoped<IDocumentStatusRealtimeNotifier, SignalRDocumentStatusRealtimeNotifier>();

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

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/404");
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

