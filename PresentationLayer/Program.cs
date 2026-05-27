using DataAccessLayer.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresentationLayer.Defaults;
using PresentationLayer.Extensions;
using PresentationLayer.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddScoped<CustomExceptionMiddleware>();

builder.Services.AddAuthentication()
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
    {
        opts.LoginPath = AuthenticationDefaults.LoginPath;
        opts.LogoutPath = AuthenticationDefaults.LogoutPath;
        opts.AccessDeniedPath = AuthenticationDefaults.AccessDeniedPath;
        opts.ReturnUrlParameter = AuthenticationDefaults.ReturnUrlParamName;
    });

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EduChatbotDbContext>(opts =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new KeyNotFoundException("Could not find connection string.");
    opts.UseNpgsql(connStr, x => x.UseVector())
        .UseSnakeCaseNamingConvention();
});

//builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

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
    pattern: "{controller:slugify=home}/{action:slugify=index}/{id?}");

await app.MigrateDb<EduChatbotDbContext>();

app.Run();