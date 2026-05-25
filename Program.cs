using Kvitoria.Data;
using Kvitoria.Extensions;
using Kvitoria.Models.Auth;
using Kvitoria.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();

        var connectionString = builder.Configuration.GetConnectionString("KvitoriaPostgres")
            ?? throw new InvalidOperationException("Connection string 'KvitoriaPostgres' is not configured.");

        builder.Services.AddDbContext<KvitoriaDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services
            .AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789._";
            })
            .AddEntityFrameworkStores<KvitoriaDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, Sha256PasswordHasher>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        builder.Services.AddKvitoriaServices();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Kvitoria.Database");

            await KvitoriaDbInitializer.InitializeAsync(
                scope.ServiceProvider,
                app.Configuration,
                logger);
        }

        await app.RunAsync();
    }
}
