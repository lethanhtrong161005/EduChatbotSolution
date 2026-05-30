using BusinessLayer.Services.Implementations;
using BusinessLayer.Services.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.UnitOfWork;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresentationLayer.Defaults;
using PresentationLayer.Extensions;
using PresentationLayer.Middleware;
using PresentationLayer.Routing;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────
builder.Services.AddDbContext<EduChatbotDbContext>(opts =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new KeyNotFoundException("Could not find connection string.");
    opts.UseNpgsql(connStr)
        .UseSnakeCaseNamingConvention();
});

// ── Application Services ──────────────────────────────────────
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();

// ── Cookie Authentication (custom, no ASP.NET Identity) ───────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = AuthenticationDefaults.LoginPath;
        opts.LogoutPath = AuthenticationDefaults.LogoutPath;
        opts.AccessDeniedPath = AuthenticationDefaults.AccessDeniedPath;
        opts.ReturnUrlParameter = AuthenticationDefaults.ReturnUrlParamName;
        opts.ExpireTimeSpan = TimeSpan.FromHours(8);
        opts.SlidingExpiration = true;
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

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

builder.Services.AddRouting(opts => opts.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));

builder.Services.AddScoped<CustomExceptionMiddleware>();

builder.Services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseMiddleware<CustomExceptionMiddleware>();

app.UseHsts();
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller:slugify=account}/{action:slugify=login}/{id?}");

await app.MigrateDb<EduChatbotDbContext>();

app.Run();
