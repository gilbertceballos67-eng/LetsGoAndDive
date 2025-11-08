using LetdsGoAndDive.Data;
using Microsoft.AspNetCore.Identity;
using LetdsGoAndDive.Models;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Repositories;
using LetdsGoAndDive.Shared;
using LetdsGoAndDive.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides; // ✅ Needed for Render reverse proxy

var builder = WebApplication.CreateBuilder(args);

// ✅ Enable SignalR
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();

// ✅ Database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpContextAccessor();

// ✅ CORS — allow both localhost and Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://letsgoanddive.onrender.com",
                "http://localhost:7201" // ❗Use http (not https) for local dev
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ✅ Identity setup
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// ✅ Register repositories & services
builder.Services.AddTransient<IHomeRepository, HomeRepository>();
builder.Services.AddTransient<ICartRepository, CartRepository>();
builder.Services.AddTransient<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddTransient<IStockRepository, StockRepository>();
builder.Services.AddTransient<IItemTypeRepository, ItemTypeRepository>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddSession();

var app = builder.Build();

// ✅ Render reverse proxy headers (important for HTTPS and WebSockets)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ✅ Run migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedDefaultData(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ Middleware order matters for SignalR
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Must come BEFORE Authentication for SignalR cross-origin cookies
app.UseCors("CorsPolicy");
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ✅ SignalR Hub registration
app.MapHub<ChatHub>("/chathub", options =>
{
    // ✅ Allow all transports (Render supports WebSocket but fallback is good)
    options.Transports = HttpTransportType.WebSockets |
                         HttpTransportType.ServerSentEvents |
                         HttpTransportType.LongPolling;
});

// ✅ MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
