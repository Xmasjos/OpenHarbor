using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenHarbor.Data;
using OpenHarbor.Models;
using OpenHarbor.Options;
using OpenHarbor.Services;

namespace OpenHarbor;

public sealed class Startup(IConfiguration configuration)
{
    private readonly PluginServerOptions _pluginOptions =
        configuration.GetSection(PluginServerOptions.SectionName).Get<PluginServerOptions>() ?? new PluginServerOptions();
    private readonly PluginManager _pluginManager = new();

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<PluginServerOptions>(configuration.GetSection(PluginServerOptions.SectionName));
        services.AddSingleton<PluginRuntimeState>();
        var resolvedDataDirectory = Path.GetFullPath(_pluginOptions.DataDirectory);
        Directory.CreateDirectory(resolvedDataDirectory);
        var databasePath = Path.Combine(resolvedDataDirectory, _pluginOptions.DatabaseFileName);
        void configureCatalogDatabase(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={databasePath}");
        }

        services.AddDbContext<PluginCatalogDbContext>((_, options) => configureCatalogDatabase(options));
        services.AddScoped<IPluginCatalogReader, PluginCatalogReader>();
        services.AddScoped<IPluginCatalogWriter, PluginCatalogWriter>();
        services.AddSingleton<IPluginRecordNormalizer, PluginRecordNormalizer>();
        services.AddHostedService<PluginRuntimeStateInitializer>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Login";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
        });

        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToFolder("/Account");
        });

        var dbOptionsBuilder = new DbContextOptionsBuilder<PluginCatalogDbContext>();
        configureCatalogDatabase(dbOptionsBuilder);

        List<PluginRecord> enabledPlugins;
        using (var dbContext = new PluginCatalogDbContext(dbOptionsBuilder.Options))
        {
            dbContext.Database.EnsureCreated();
            enabledPlugins = dbContext.Plugins
                .AsNoTracking()
                .Where(plugin => plugin.Enabled)
                .ToList();
        }

        _pluginManager.RegisterEnabledPlugins(services, configuration, _pluginOptions, enabledPlugins);
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        _pluginManager.ConfigurePublicAssets(app, _pluginOptions);

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        _pluginManager.MapEndpoints(app);

        app.MapGet("/api/admin/status", async (PluginRuntimeState runtimeState, IPluginCatalogReader catalogReader) =>
        {
            var records = await catalogReader.GetAllAsync();
            return Results.Ok(new
            {
                restartPending = runtimeState.RestartPending,
                items = runtimeState.Statuses,
                totalRecords = records.Count
            });
        }).RequireAuthorization("Admin");

        app.MapGet("/api/admin/plugins", async (IPluginCatalogReader catalogReader) =>
            Results.Ok(await catalogReader.GetAllAsync()))
            .RequireAuthorization("Admin");

        app.MapPost("/api/admin/restart", (IHostApplicationLifetime lifetime) =>
        {
            lifetime.StopApplication();
            return Results.Ok(new { message = "Restart requested. The host will stop gracefully and must be relaunched by the configured supervisor." });
        }).RequireAuthorization("Admin");

    }
}