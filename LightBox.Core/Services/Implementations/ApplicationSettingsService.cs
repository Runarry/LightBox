using LightBox.Core.Models;
using LightBox.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Implementations
{
    public class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILoggingService _loggingService;
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        public ApplicationSettingsService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LightBox");
            _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        }

        public async Task<ApplicationSettings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ApplicationSettings>(json, _jsonSerializerOptions);
                    if (settings != null)
                    {
                        // Ensure default for LogFilePath if it's the placeholder
                        if (settings.LogFilePath == "Logs/lightbox-.log")
                        {
                            settings.LogFilePath = GetDefaultLogFilePath();
                        }
                        _loggingService.LogInfo($"Settings loaded from {_settingsFilePath}");
                        return settings;
                    }
                    _loggingService.LogWarning($"Failed to deserialize settings from {_settingsFilePath}. Returning default settings.");
                }
                else
                {
                    _loggingService.LogInfo($"Settings file not found at {_settingsFilePath}. Returning default settings.");
                }
            }
            catch (JsonException ex)
            {
                _loggingService.LogError($"Error deserializing settings from {_settingsFilePath}: {ex.Message}. Returning default settings.");
            }
            catch (IOException ex)
            {
                _loggingService.LogError($"Error reading settings file {_settingsFilePath}: {ex.Message}. Returning default settings.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"An unexpected error occurred while loading settings: {ex.Message}. Returning default settings.");
            }

            return GetDefaultSettings();
        }

        public async Task SaveSettingsAsync(ApplicationSettings settings)
        {
            if (settings == null)
            {
                _loggingService.LogError("Attempted to save null settings.");
                throw new ArgumentNullException(nameof(settings));
            }

            try
            {
                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                    _loggingService.LogInfo($"Created settings directory: {_settingsDirectory}");
                }

                var json = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json);
                _loggingService.LogInfo($"Settings saved to {_settingsFilePath}");
            }
            catch (IOException ex)
            {
                _loggingService.LogError($"Error writing settings file {_settingsFilePath}: {ex.Message}");
                // Potentially re-throw or handle more gracefully depending on requirements
                throw; 
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"An unexpected error occurred while saving settings: {ex.Message}");
                throw;
            }
        }

        private ApplicationSettings GetDefaultSettings()
        {
            return new ApplicationSettings
            {
                PluginScanDirectories = new List<string>(),
                LogFilePath = GetDefaultLogFilePath(),
                LogLevel = "Information",
                IpcApiPort = 0 
            };
        }

        private string GetDefaultLogFilePath()
        {
            // Ensure the Logs directory is relative to the settings/application data directory or an absolute path
            // For now, let's assume it's relative to where the application expects to write logs,
            // which might be the application's execution directory or a user-specific log directory.
            // The original "Logs/lightbox-.log" suggests a relative path.
            // If settings are in MyDocuments/LightBox, then Logs/ could be MyDocuments/LightBox/Logs
            var defaultLogDirectory = Path.Combine(_settingsDirectory, "Logs");
            return Path.Combine(defaultLogDirectory, $"lightbox-{DateTime.UtcNow:yyyyMMdd}.log");
        }
    }
}