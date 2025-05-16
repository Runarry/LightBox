using System;
using System.Text.Json;
using System.Threading.Tasks;
using LightBox.Core.Services.Interfaces;
using LightBox.Core.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace LightBox.Core.Tests
{
    public class ConfigurationServiceTests
    {
        private readonly IConfigurationService _configurationService;
        private readonly IPluginService _pluginService;
        private readonly ILoggingService _logger;

        public ConfigurationServiceTests(IServiceProvider serviceProvider)
        {
            _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
            _pluginService = serviceProvider.GetRequiredService<IPluginService>();
            _logger = serviceProvider.GetRequiredService<ILoggingService>();
        }

        public async Task RunAllTests()
        {
            _logger.LogInfo("Starting ConfigurationServiceTests...");

            await TestValidateConfiguration();
            await TestGetDefaultConfiguration();
            await TestResetConfiguration();
            await TestGenerateTempConfigFile();

            _logger.LogInfo("ConfigurationServiceTests completed.");
        }

        private async Task TestValidateConfiguration()
        {
            _logger.LogInfo("Testing ValidateConfiguration...");

            // First discover plugins
            var plugins = await _pluginService.DiscoverPluginsAsync();
            if (plugins.Count == 0)
            {
                _logger.LogError("No plugins found for testing.");
                return;
            }

            var pluginId = plugins[0].Id;
            _logger.LogInfo($"Using plugin {pluginId} for configuration tests.");

            // Test 1: Valid configuration
            var validConfig = JsonSerializer.Serialize(new
            {
                name = "Test Instance",
                enabled = true,
                refreshInterval = 30
            });

            var validationResult = _configurationService.ValidateConfiguration(pluginId, validConfig);
            _logger.LogInfo($"Valid configuration validation result: {validationResult.IsValid}");

            // Test 2: Invalid configuration (missing required field)
            var invalidConfig = JsonSerializer.Serialize(new
            {
                refreshInterval = 30
            });

            validationResult = _configurationService.ValidateConfiguration(pluginId, invalidConfig);
            _logger.LogInfo($"Invalid configuration validation result: {validationResult.IsValid}");
            if (!validationResult.IsValid)
            {
                _logger.LogInfo($"Error message: {validationResult.ErrorMessage}");
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogInfo($"- {error}");
                }
            }

            // Test 3: Invalid JSON
            validationResult = _configurationService.ValidateConfiguration(pluginId, "{invalid json}");
            _logger.LogInfo($"Invalid JSON validation result: {validationResult.IsValid}");
            if (!validationResult.IsValid)
            {
                _logger.LogInfo($"Error message: {validationResult.ErrorMessage}");
            }

            _logger.LogInfo("ValidateConfiguration test completed.");
        }

        private async Task TestGetDefaultConfiguration()
        {
            _logger.LogInfo("Testing GetDefaultConfiguration...");

            // First discover plugins
            var plugins = await _pluginService.DiscoverPluginsAsync();
            if (plugins.Count == 0)
            {
                _logger.LogError("No plugins found for testing.");
                return;
            }

            var pluginId = plugins[0].Id;
            var defaultConfig = _configurationService.GetDefaultConfiguration(pluginId);

            _logger.LogInfo($"Default configuration for plugin {pluginId}: {defaultConfig}");

            // Parse the default config to verify it's valid JSON
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(defaultConfig);
                _logger.LogInfo("Default configuration is valid JSON.");

                // Check if it has the expected properties
                if (configObj.TryGetProperty("name", out _))
                {
                    _logger.LogInfo("Default configuration has 'name' property.");
                }
                if (configObj.TryGetProperty("enabled", out _))
                {
                    _logger.LogInfo("Default configuration has 'enabled' property.");
                }
                if (configObj.TryGetProperty("refreshInterval", out _))
                {
                    _logger.LogInfo("Default configuration has 'refreshInterval' property.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Default configuration is not valid JSON: {ex.Message}");
            }

            _logger.LogInfo("GetDefaultConfiguration test completed.");
        }

        private async Task TestResetConfiguration()
        {
            _logger.LogInfo("Testing ResetConfiguration...");

            // First discover plugins
            var plugins = await _pluginService.DiscoverPluginsAsync();
            if (plugins.Count == 0)
            {
                _logger.LogError("No plugins found for testing.");
                return;
            }

            var pluginId = plugins[0].Id;
            var resetConfig = _configurationService.ResetConfiguration(pluginId);

            _logger.LogInfo($"Reset configuration for plugin {pluginId}: {resetConfig}");

            // Verify it's the same as the default configuration
            var defaultConfig = _configurationService.GetDefaultConfiguration(pluginId);
            if (resetConfig == defaultConfig)
            {
                _logger.LogInfo("Reset configuration matches default configuration.");
            }
            else
            {
                _logger.LogError("Reset configuration does not match default configuration.");
            }

            _logger.LogInfo("ResetConfiguration test completed.");
        }

        private async Task TestGenerateTempConfigFile()
        {
            _logger.LogInfo("Testing GenerateTempConfigFile...");

            // First discover plugins
            var plugins = await _pluginService.DiscoverPluginsAsync();
            if (plugins.Count == 0)
            {
                _logger.LogError("No plugins found for testing.");
                return;
            }

            var pluginId = plugins[0].Id;
            var instanceId = Guid.NewGuid().ToString();
            var configJson = JsonSerializer.Serialize(new
            {
                name = "Test Instance",
                enabled = true,
                refreshInterval = 30
            });

            var tempFilePath = _configurationService.GenerateTempConfigFile(pluginId, instanceId, configJson);
            _logger.LogInfo($"Temporary configuration file generated at: {tempFilePath}");

            // Verify the file exists
            if (System.IO.File.Exists(tempFilePath))
            {
                _logger.LogInfo("Temporary configuration file exists.");

                // Verify the content
                var fileContent = System.IO.File.ReadAllText(tempFilePath);
                _logger.LogInfo($"File content: {fileContent}");

                // Clean up
                System.IO.File.Delete(tempFilePath);
                _logger.LogInfo("Temporary configuration file deleted.");
            }
            else
            {
                _logger.LogError("Temporary configuration file does not exist.");
            }

            _logger.LogInfo("GenerateTempConfigFile test completed.");
        }
    }
} 