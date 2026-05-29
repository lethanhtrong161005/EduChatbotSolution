using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Repositories.Implementation;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// 1. Configure Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.UseVector()));

// 2. Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 3. Register Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<DatabaseInitializationService>();

// 4. Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "EduChatAI.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 5. Add Authentication (Optional: for future use with identity)
builder.Services.AddAuthentication();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add session middleware
app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 6. Configure default route to Login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();