using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenHarbor.DAL;
using OpenHarbor.Models;
using OpenHarbor.Options;
using OpenHarbor.Services;

namespace OpenHarbor;

public class Startup(IConfiguration configuration, PluginServerOptions pluginOptions)
{
    private readonly PluginServerOptions _pluginOptions = pluginOptions;
    private readonly PluginManager _pluginManager = new();

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<PluginServerOptions>(configuration.GetSection(PluginServerOptions.SectionName));
        var maxRequestBodyBytes = _pluginOptions.MaxUploadBytes + 1024 * 1024;
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestBodyBytes);
        services.AddSingleton<PluginRuntimeState>();
        var timeProvider = TimeProvider.System;
        services.AddSingleton(timeProvider);
        var resolvedConnectionString = EnsurePathExists();
        void configureCatalogDatabase(DbContextOptionsBuilder options)
        {
            options.UseSqlite(resolvedConnectionString);
            options.AddInterceptors(new EntityTimestampInterceptor(timeProvider));
        }

        services.AddDbContext<PluginCatalogDbContext>((_, options) => configureCatalogDatabase(options));
        services.AddScoped<IPluginCatalogReader, PluginCatalogReader>();
        services.AddScoped<IPluginCatalogWriter, PluginCatalogWriter>();
        services.AddSingleton<IPluginPackageInstaller, PluginPackageInstaller>();
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
            dbContext.Database.Migrate();
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

        app.MapGet("/_host/plugin-ui.js", (PluginRuntimeState runtimeState) =>
        {
            var selectedPlugin = _pluginManager.LoadedPlugins.FirstOrDefault(
                plugin => plugin.Record.Id == runtimeState.SelectedDashboardPluginId);
            if (selectedPlugin?.Entry.Dashboard is null || string.IsNullOrWhiteSpace(selectedPlugin.Record.PublicFolderRelativePath))
                return (IResult)Results.NotFound();

            var runtimePath = Path.GetFullPath(Path.Combine(
                _pluginOptions.ManagedPluginRoot,
                selectedPlugin.Record.PublicFolderRelativePath.Replace('/', Path.DirectorySeparatorChar),
                "plugin-ui.js"));
            return File.Exists(runtimePath)
                ? Results.File(runtimePath, "text/javascript; charset=utf-8")
                : Results.NotFound();
        }).RequireAuthorization("Admin");

        app.MapGet("/", async (PluginRuntimeState runtimeState, IPluginCatalogReader catalogReader) =>
        {
            if (runtimeState.SelectedDashboardPluginId is not Guid selectedPluginId)
                return Results.Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>No dashboard provider is selected.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

            var record = await catalogReader.GetByIdAsync(selectedPluginId);
            var loadedPlugin = _pluginManager.LoadedPlugins.FirstOrDefault(plugin => plugin.Record.Id == selectedPluginId);
            if (record is null || !record.Enabled || loadedPlugin?.Entry.Dashboard is null || string.IsNullOrWhiteSpace(record.PublicFolderRelativePath))
                return Results.Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>The selected dashboard is unavailable.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

            var publicDirectory = Path.GetFullPath(Path.Combine(
                _pluginOptions.ManagedPluginRoot,
                record.PublicFolderRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            var dashboardFile = Path.Combine(publicDirectory, "index.html");
            if (!File.Exists(dashboardFile))
                return Results.Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>The selected dashboard has no public index.html.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

            var dashboardHtml = await File.ReadAllTextAsync(dashboardFile);
            var baseElement = $"<base href=\"/plugins/{record.RouteSubpath.Trim('/')}/\">";
            var dashboardIconSvg = loadedPlugin.Entry.Application?.IconSvg;
            var faviconHref = string.IsNullOrWhiteSpace(dashboardIconSvg)
                ? "/icon.svg"
                : $"data:image/svg+xml,{Uri.EscapeDataString(dashboardIconSvg)}";
            var faviconElement = $"<link rel=\"icon\" type=\"image/svg+xml\" href=\"{faviconHref}\">";
            dashboardHtml = dashboardHtml.Replace("<head>", $"<head>{baseElement}{faviconElement}", StringComparison.OrdinalIgnoreCase);
            return Results.Content(dashboardHtml, "text/html; charset=utf-8");
        }).RequireAuthorization("Admin");

        app.MapGet("/api/dashboard/applications", () => Results.Ok(
                _pluginManager.LoadedPlugins
                    .Where(plugin => plugin.Record.Enabled && plugin.Entry.Application is not null)
                    .Select(plugin => new
                    {
                        id = plugin.Record.Id,
                        name = plugin.Entry.Application!.Name,
                        iconSvg = plugin.Entry.Application.IconSvg,
                        launchMode = plugin.Entry.Application.LaunchMode,
                        routeSubpath = plugin.Record.RouteSubpath.Trim('/')
                    })))
            .RequireAuthorization("Admin");

        app.MapGet("/api/admin/status", async (PluginRuntimeState runtimeState, IPluginCatalogReader catalogReader) =>
        {
            var records = await catalogReader.GetAllAsync();
            return Results.Ok(new
            {
                restartPending = runtimeState.RestartPending,
                selectedDashboard = runtimeState.SelectedDashboardPluginId is null ? null : new
                {
                    id = runtimeState.SelectedDashboardPluginId,
                    name = runtimeState.SelectedDashboardPluginName,
                    routeSubpath = runtimeState.SelectedDashboardRouteSubpath
                },
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

    private string EnsurePathExists()
    {
        var connectionString = configuration.GetConnectionString("PluginCatalog")
            ?? throw new InvalidOperationException("Connection string 'PluginCatalog' is not configured.");
        var sqliteConnectionString = new SqliteConnectionStringBuilder(connectionString);
        if (sqliteConnectionString.Mode != SqliteOpenMode.Memory &&
            !string.IsNullOrWhiteSpace(sqliteConnectionString.DataSource) &&
            !string.Equals(sqliteConnectionString.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var databasePath = Path.GetFullPath(sqliteConnectionString.DataSource);
            var databaseDirectory = Path.GetDirectoryName(databasePath);
            if (databaseDirectory is not null)
                Directory.CreateDirectory(databaseDirectory);

            sqliteConnectionString.DataSource = databasePath;
        }

        return sqliteConnectionString.ToString();
    }
}