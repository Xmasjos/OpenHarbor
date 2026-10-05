using OpenHarbor;
using OpenHarbor.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var pluginOptions = builder.Configuration.GetSection(PluginServerOptions.SectionName).Get<PluginServerOptions>()
	?? new PluginServerOptions();
var maxRequestBodyBytes = pluginOptions.MaxUploadBytes + 1024 * 1024;

builder.WebHost.ConfigureKestrel(options =>
	options.Limits.MaxRequestBodySize = maxRequestBodyBytes);

var startup = new Startup(builder.Configuration, pluginOptions);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app);

app.Run();
