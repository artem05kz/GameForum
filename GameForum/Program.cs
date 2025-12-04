using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using GameForum.Data;
using GameForum.Model;
using GameForum.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:8080");

// Добавление сервисов
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
    options.UseMySql(cs, serverVersion);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<StatsService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.Cookie.Name = ".GameForum.auth";
        options.Cookie.HttpOnly = true;
    });

// локализация (ru, en)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages().AddViewLocalization();

var supportedCultures = new[] { new CultureInfo("ru-RU"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ru-RU");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// бд-редис сессия
var redisConn = builder.Configuration.GetValue<string>("Redis:Configuration");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    var mux = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.Configuration = redisConn;
        o.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "gameforum";
    });

    try
    {
        builder.Services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(mux, "DataProtection-Keys")
            .SetApplicationName("GameForum");
        Console.WriteLine("[DataProtection] Keys will be stored in Redis");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DataProtection] Failed to configure Redis key storage: {ex.Message}");
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".GameForum.session";
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ожидание готовности БД и создание схемы с ретраями
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    const int maxRetries = 20;
    var delay = TimeSpan.FromSeconds(3);
    var ready = false;
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            if (context.Database.CanConnect())
            {
                ready = true;
                break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Попытка {i}/{maxRetries} подключиться к БД не удалась", i, maxRetries);
        }
        Console.WriteLine($"Ожидание готовности БД... попытка {i}/{maxRetries}");
        Thread.Sleep(delay);
    }

    if (!ready)
    {
        logger.LogError("База данных недоступна после {maxRetries} попыток", maxRetries);
        throw new Exception("Database is not ready");
    }

    Console.WriteLine("БД доступна. Создание схемы при необходимости...");
    var created = context.Database.EnsureCreated();
    Console.WriteLine(created ? "База данных была создана" : "База данных уже существует");

    // Сиды

    if (!context.AuthUsers.Any())
    {
        Console.WriteLine("Добавление администратора...");
        var auth = new AuthService();
        auth.CreatePasswordHash("admin123", out var hash, out var salt);
        context.AuthUsers.Add(new AuthUser
        {
            Username = "admin",
            DisplayName = "Administrator",
            IsAdmin = true,
            PasswordHash = hash,
            PasswordSalt = salt
        });
    }

    context.SaveChanges();
    Console.WriteLine("Инициализация базы данных завершена");
}

    app.UseStaticFiles();
    app.UseRouting();
    app.UseRequestLocalization(app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value);
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();

