using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Platform.Identity;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // Cho phép DateTime Kind=Local khi dùng PostgreSQL (Npgsql)
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // Load .env từ root repo (ưu tiên) hoặc thư mục hiện tại
        LoadEnvFile();

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting Platform.Identity.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<IdentityHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void LoadEnvFile()
    {
        // Các vị trí có thể chứa .env (tùy chạy từ đâu)
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),                    // chạy từ root
            Path.Combine(Directory.GetCurrentDirectory(), "../../.env"),              // chạy từ src/Project
            Path.Combine(AppContext.BaseDirectory, "../../../.env"),                  // khi publish/bin
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