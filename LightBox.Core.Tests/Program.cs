using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LightBox.Core.Services.Implementations;
using LightBox.Core.Services.Interfaces;
using LightBox.PluginContracts;
using Microsoft.Extensions.DependencyInjection;

namespace LightBox.Core.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("LightBox.Core.Tests - PluginManager Test Runner");
            Console.WriteLine("------------------------------------------------");

            // 配置服务
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // 创建和运行测试
            var tests = new PluginManagerTests(serviceProvider);
            await tests.RunAllTests();

            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Tests completed. Press any key to exit...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 配置和注册所需服务
            services.AddSingleton<ILoggingService, ConsoleLogger>();
            services.AddSingleton<IApplicationSettingsService, ConfigFileSettingsService>();
            services.AddSingleton<IPluginService, PluginManager>();
        }
    }

    /// <summary>
    /// 测试专用的控制台日志服务
    /// </summary>
    public class ConsoleLogger : ILoggingService
    {
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    LogDebug(message);
                    break;
                case LogLevel.Info:
                    LogInfo(message);
                    break;
                case LogLevel.Warning:
                    LogWarning(message, exception);
                    break;
                case LogLevel.Error:
                    LogError(message, exception);
                    break;
                default:
                    LogInfo(message);
                    break;
            }
        }

        public void LogError(string message, Exception exception = null)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            if (exception != null)
                Console.WriteLine($"  Exception: {exception.Message}");
            Console.ForegroundColor = originalColor;
        }

        public void LogWarning(string message, Exception exception = null)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] {message}");
            if (exception != null)
                Console.WriteLine($"  Exception: {exception.Message}");
            Console.ForegroundColor = originalColor;
        }

        public void LogWarning(string message)
        {
            LogWarning(message, null);
        }

        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogDebug(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[DEBUG] {message}");
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// 测试专用的应用设置服务
    /// </summary>
    public class ConfigFileSettingsService : IApplicationSettingsService
    {
        private Models.ApplicationSettings _settings;

        public ConfigFileSettingsService()
        {
            // 默认测试设置，主要是设置插件扫描目录
            _settings = new Models.ApplicationSettings
            {
                PluginScanDirectories = new List<string>
                {
                    // 确保路径正确指向TestPlugins目录
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TestPlugins", "PluginA")
                }
            };
        }

        public Task<Models.ApplicationSettings> LoadSettingsAsync()
        {
            return Task.FromResult(_settings);
        }

        public Task SaveSettingsAsync(Models.ApplicationSettings settings)
        {
            _settings = settings;
            return Task.CompletedTask;
        }
    }
} 