using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PresentationLayer.Defaults;
using PresentationLayer.Extensions;
using PresentationLayer.Middleware;
using PresentationLayer.Routing;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EduChatbotDbContext>(opts =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new KeyNotFoundException("Could not find connection string.");
    opts.UseNpgsql(connStr, opts => { })
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(cfg =>
{
    cfg.Password.RequiredLength = 8;
    cfg.User.RequireUniqueEmail = true;
    cfg.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<EduChatbotDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = AuthenticationDefaults.LoginPath;
    opts.LogoutPath = AuthenticationDefaults.LogoutPath;
    opts.AccessDeniedPath = AuthenticationDefaults.AccessDeniedPath;
    opts.ReturnUrlParameter = AuthenticationDefaults.ReturnUrlParamName;
});

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
    pattern: "{controller:slugify=home}/{action:slugify=index}/{id?}");

await app.MigrateDb<EduChatbotDbContext>();

app.Run();
