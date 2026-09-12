using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Platform.Identity.DbMigrator;

class Program
{
    static async Task Main(string[] args)
    {
        // Cho phép DateTime Kind=Local khi dùng PostgreSQL (Npgsql)
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // Load .env trước khi tạo host
        LoadEnvFile();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
#if DEBUG
            .MinimumLevel.Override("Platform.Identity", LogEventLevel.Debug)
#else
            .MinimumLevel.Override("Platform.Identity", LogEventLevel.Information)
#endif
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        await CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .AddAppSettingsSecretsJson()
            .ConfigureLogging((context, logging) => logging.ClearProviders())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DbMigratorHostedService>();
            });

    private static void LoadEnvFile()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../.env"),
            Path.Combine(AppContext.BaseDirectory, "../../../.env"),
            Path.Combine(AppContext.BaseDirectory, "../../../../.env"),
        };

        foreach (var path in candidates)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                DotNetEnv.Env.Load(fullPath);
                Console.WriteLine($"[ENV] Loaded: {fullPath}");
                return;
            }
        }

        Console.WriteLine("[ENV] No .env file found (ok on production).");
    }
}