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
            bool noWait = args.Length > 0 && args[0] == "--no-wait";
            
            Console.WriteLine("LightBox.Core.Tests - PluginManager Test Runner");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Configuring services...");

            // 配置服务
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            Console.WriteLine("Services configured.");
            Console.WriteLine("Starting tests...");

            try
            {
                // 创建和运行测试
                Console.WriteLine("Initializing PluginManagerTests...");
                var pluginManagerTests = new PluginManagerTests(serviceProvider);
                Console.WriteLine("Running PluginManagerTests...");
                await pluginManagerTests.RunAllTests();
                
                Console.WriteLine("Initializing ConfigurationServiceTests...");
                var configurationTests = new ConfigurationServiceTests(serviceProvider);
                Console.WriteLine("Running ConfigurationServiceTests...");
                await configurationTests.RunAllTests();

                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("Tests completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Test execution failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
            }

            if (!noWait)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("Setting up dependency injection...");
            
            // 配置和注册所需服务
            services.AddSingleton<ILoggingService, ConsoleLogger>();
            services.AddSingleton<IApplicationSettingsService, ConfigFileSettingsService>();
            
            // 首先创建ConfigurationService实例
            var configService = new ConfigurationService(null, new ConsoleLogger());
            services.AddSingleton<IConfigurationService>(configService);
            
            // 然后创建PluginManager并解决循环依赖
            var settingsService = new ConfigFileSettingsService();
            var loggingService = new ConsoleLogger();
            var pluginManager = new PluginManager(settingsService, loggingService, configService);
            
            // 手动解决循环依赖
            configService.PluginService = pluginManager;
            
            // 注册PluginManager实例
            services.AddSingleton<IPluginService>(pluginManager);
            
            Console.WriteLine("Services registration completed.");
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
            // 计算测试插件的绝对路径
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string testPluginsDir = Path.GetFullPath(Path.Combine(projectDir, "..", "TestPlugins"));
            string pluginADir = Path.Combine(testPluginsDir, "PluginA");
            
            Console.WriteLine($"Base directory: {baseDir}");
            Console.WriteLine($"Project directory: {projectDir}");
            Console.WriteLine($"TestPlugins directory: {testPluginsDir}");
            Console.WriteLine($"PluginA directory: {pluginADir}");
            
            // 检查目录是否存在
            if (Directory.Exists(pluginADir))
            {
                Console.WriteLine($"PluginA directory exists: {pluginADir}");
                
                // 检查manifest.json文件是否存在
                string manifestPath = Path.Combine(pluginADir, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    Console.WriteLine($"manifest.json exists: {manifestPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: manifest.json not found at: {manifestPath}");
                }
            }
            else
            {
                Console.WriteLine($"WARNING: PluginA directory does not exist: {pluginADir}");
            }

            // 默认测试设置，主要是设置插件扫描目录
            _settings = new Models.ApplicationSettings
            {
                PluginScanDirectories = new List<string>
                {
                    // 使用绝对路径，确保能找到测试插件
                    pluginADir
                }
            };
            
            Console.WriteLine($"Configured plugin scan directory: {pluginADir}");
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