using Business.Services;
using DataAccess.Data;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Presentation.Defaults;
using Presentation.Extensions;
using Presentation.Middleware;
using Presentation.Options;
using Presentation.Routing;
using StackExchange.Redis;
using System.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<EduChatbotDbContext>(opts =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new KeyNotFoundException("Could not find connection string.");
    opts.UseNpgsql(connStr)
        .UseSnakeCaseNamingConvention();
});

// ── Redis ──────────────────────────────────────────────────────
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

// ── Application Services ──────────────────────────────────────
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// ── Helper Services ───────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

builder.Services.Configure<PaymentServiceOptions>(builder.Configuration.GetRequiredSection("PaymentServices"));

// ── Identity Authentication ───────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.User.RequireUniqueEmail = true;
    opts.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<EduChatbotDbContext>()
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
    await app.MigrateDb<EduChatbotDbContext>();
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

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller:slugify=home}/{action:slugify=index}/{id?}");

app.Run();
