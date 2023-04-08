using System.Reflection;
using ConsoleGPT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

// Create the host builder
var builder = Host.CreateDefaultBuilder(args);

// Load the configuration file and user secrets
//
// These need to be set either directly in the configuration.json file or in the user secrets. Details are in
// the configuration.json file.
#pragma warning disable CS8604
var configurationFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "configuration.json");
#pragma warning restore CS8604
builder.ConfigureAppConfiguration((builder) => builder
    .AddJsonFile(configurationFilePath)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>());

// Configure the services for the host
builder.ConfigureServices((context, services) =>
{
    // Setup configuration options
    var configurationRoot = context.Configuration;
    services.Configure<OpenAiServiceOptions>(configurationRoot.GetSection("OpenAI"));

    // Add Semantic Kernel
    services.AddSingleton<IKernel>(serviceProvider => Kernel.Builder.Build());

    // Add the primary hosted service to start the loop.
    services.AddHostedService<ConsoleGPTService>();
});

// Build and run the host. This keeps the app running using the HostedService.
var host = builder.Build();
await host.RunAsync();