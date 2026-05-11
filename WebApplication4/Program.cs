using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApplication4.Data;
using WebApplication4.Models;
using WebApplication4.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o =>
    {
        o.CommandTimeout(300);
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    options.SignIn.RequireConfirmedEmail = false;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromDays(30);

    options.SlidingExpiration = true;
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultAuthenticateScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultChallengeScheme =
            IdentityConstants.ApplicationScheme;
    })

    .AddGoogle(options =>
    {
        options.ClientId =
            Environment.GetEnvironmentVariable("Authentication__Google__ClientId")
            ?? builder.Configuration["Authentication:Google:ClientId"]!;

        options.ClientSecret =
            Environment.GetEnvironmentVariable("Authentication__Google__ClientSecret")
            ?? builder.Configuration["Authentication:Google:ClientSecret"]!;

        options.CallbackPath = "/signin-google";

        options.SaveTokens = true;
    })

    .AddGitHub(options =>
    {
        options.ClientId =
            Environment.GetEnvironmentVariable("Authentication__GitHub__ClientId")
            ?? builder.Configuration["Authentication:GitHub:ClientId"]!;

        options.ClientSecret =
            Environment.GetEnvironmentVariable("Authentication__GitHub__ClientSecret")
            ?? builder.Configuration["Authentication:GitHub:ClientSecret"]!;

        options.CallbackPath = "/signin-github";

        options.Scope.Add("user:email");

        options.SaveTokens = true;
    })

    .AddDiscord(options =>
    {
        options.ClientId =
            Environment.GetEnvironmentVariable("Authentication__Discord__ClientId")
            ?? builder.Configuration["Authentication:Discord:ClientId"]!;

        options.ClientSecret =
            Environment.GetEnvironmentVariable("Authentication__Discord__ClientSecret")
            ?? builder.Configuration["Authentication:Discord:ClientSecret"]!;

        options.CallbackPath = "/signin-discord";

        options.Scope.Add("identify");
        options.Scope.Add("email");

        options.SaveTokens = true;
    });

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("ru-RU"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture =
        new RequestCulture("ru-RU");

    options.SupportedCultures =
        supportedCultures;

    options.SupportedUICultures =
        supportedCultures;

    options.RequestCultureProviders.Insert(
        0,
        new QueryStringRequestCultureProvider());
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(
        LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

var app = builder.Build();
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseRequestLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

app.Run();