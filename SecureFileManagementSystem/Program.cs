using Microsoft.EntityFrameworkCore;
using SecureFileManagementSystem.Data; // Replace with your actual namespace
using SecureFileManagementSystem.Hubs;
using SecureFileManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext for SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<PeerDirectoryService>();


// Optional: Add authentication services if you have login/identity
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//        .AddCookie(options => { /* Cookie options */ });

// Add CORS to allow P2P communication in local network
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalNetwork", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Optional: Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// add SignalR services
builder.Services.AddSignalR();

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowLocalNetwork"); // Allow cross-device requests in local network

app.UseSession();

app.UseAuthorization();

// Enable authentication
app.UseAuthentication();

// This maps the URL "/notificationHub" to your hub
app.MapHub<NotificationHub>("/notificationHub");

// Routing setup
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Optional: Map API controllers for P2P transfers if using [ApiController]
app.MapControllers();

app.Run();
