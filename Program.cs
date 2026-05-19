using InventoryPilot.Data;
using InventoryPilot.Models;
using InventoryPilot.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
static string RequiredAuthSetting(IConfiguration configuration, string key, string fallback) =>
    string.IsNullOrWhiteSpace(configuration[key]) ? fallback : configuration[key]!;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = RequiredAuthSetting(builder.Configuration, "Authentication:Google:ClientId", "replace-with-google-client-id");
        options.ClientSecret = RequiredAuthSetting(builder.Configuration, "Authentication:Google:ClientSecret", "replace-with-google-client-secret");
    })
    .AddFacebook(options =>
    {
        options.AppId = RequiredAuthSetting(builder.Configuration, "Authentication:Facebook:AppId", "replace-with-facebook-app-id");
        options.AppSecret = RequiredAuthSetting(builder.Configuration, "Authentication:Facebook:AppSecret", "replace-with-facebook-app-secret");
        options.Scope.Clear();
        options.Scope.Add("public_profile");
    });

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<InventoryPermissionService>();
builder.Services.AddSingleton<CustomIdComposer>();
builder.Services.AddSingleton<MarkdownRenderer>();
builder.Services.AddSingleton<UiText>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await AppSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
