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
        services.AddSingleton<ILoadedPluginCatalogReader>(_pluginManager);
        services.AddSingleton<IPluginDataManager>(_pluginManager);
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
        services.AddControllers();

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
        app.MapControllers();
        _pluginManager.MapEndpoints(app);

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