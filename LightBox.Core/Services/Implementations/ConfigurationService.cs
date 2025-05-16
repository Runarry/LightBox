using LightBox.Core.Models;
using LightBox.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Json.Schema;
using System.Linq;

namespace LightBox.Core.Services.Implementations
{
    /// <summary>
    /// Provides configuration management services for plugins.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private IPluginService _pluginService;
        private readonly ILoggingService _loggingService;
        private static readonly string TempConfigDir = Path.Combine(Path.GetTempPath(), "LightBox", "Plugins", "Temp");
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets or sets the plugin service to resolve circular dependency.
        /// </summary>
        public IPluginService PluginService
        {
            get { return _pluginService; }
            set { _pluginService = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="pluginService">The plugin service.</param>
        /// <param name="loggingService">The logging service.</param>
        public ConfigurationService(IPluginService pluginService, ILoggingService loggingService)
        {
            _pluginService = pluginService; // Might be null initially due to circular dependency
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            // Ensure temp directory exists
            if (!Directory.Exists(TempConfigDir))
            {
                Directory.CreateDirectory(TempConfigDir);
            }
        }

        /// <summary>
        /// Validates plugin configuration against the schema defined in the plugin's manifest.json
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <param name="configJson">The configuration JSON string to validate.</param>
        /// <returns>Validation result with information about success or failure.</returns>
        public ValidationResult ValidateConfiguration(string pluginId, string configJson)
        {
            try
            {
                _loggingService.LogDebug($"Validating configuration for plugin {pluginId}");
                
                // Get the plugin definition
                var pluginDefinition = _pluginService.GetPluginDefinitionByIdAsync(pluginId).GetAwaiter().GetResult();
                if (pluginDefinition == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Plugin with ID '{pluginId}' was not found."
                    };
                }

                // Check if config schema is defined
                if (pluginDefinition.ConfigSchema == null)
                {
                    // If no schema defined, any configuration is valid
                    _loggingService.LogWarning($"No config schema defined for plugin {pluginId}. Skipping validation.");
                    return new ValidationResult { IsValid = true };
                }

                // Parse the configuration
                try
                {
                    JsonDocument.Parse(configJson);
                }
                catch (JsonException ex)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Invalid JSON configuration: {ex.Message}"
                    };
                }

                // Convert JsonElement to JsonSchema
                var schemaJson = pluginDefinition.ConfigSchema.Value.GetRawText();
                var schema = JsonSchema.FromText(schemaJson);

                // Validate using JSON Schema
                var configDoc = JsonDocument.Parse(configJson);
                var validationResults = schema.Evaluate(configDoc.RootElement);

                if (!validationResults.IsValid)
                {
                    var errors = validationResults.Details
                        .Where(d => d.HasErrors)
                        .Select(e => e.Errors.Values.FirstOrDefault() ?? e.ToString())
                        .ToList();
                    
                    _loggingService.LogWarning($"Configuration for plugin {pluginId} is invalid: {string.Join(", ", errors)}");
                    
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Configuration does not conform to schema",
                        Errors = errors
                    };
                }
                else
                {
                    _loggingService.LogDebug($"Configuration for plugin {pluginId} is valid");
                    return new ValidationResult { IsValid = true };
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error validating configuration for plugin {pluginId}: {ex.Message}", ex);
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error validating configuration: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the default configuration for a plugin based on its schema.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <returns>The default configuration JSON string.</returns>
        public string GetDefaultConfiguration(string pluginId)
        {
            try
            {
                _loggingService.LogDebug($"Getting default configuration for plugin {pluginId}");
                
                // Get the plugin definition
                var pluginDefinition = _pluginService.GetPluginDefinitionByIdAsync(pluginId).GetAwaiter().GetResult();
                if (pluginDefinition == null)
                {
                    throw new ArgumentException($"Plugin with ID '{pluginId}' was not found.");
                }

                // Check if config schema is defined
                if (pluginDefinition.ConfigSchema == null)
                {
                    // Return empty object if no schema defined
                    _loggingService.LogWarning($"No config schema defined for plugin {pluginId}. Returning empty config.");
                    return "{}";
                }

                // Convert JsonElement to JsonSchema
                var schemaJson = pluginDefinition.ConfigSchema.Value.GetRawText();
                var schema = JsonSchema.FromText(schemaJson);

                // Build default configuration from schema
                var defaultConfig = BuildDefaultConfiguration(schema);

                // Serialize to JSON
                var defaultConfigJson = JsonSerializer.Serialize(defaultConfig, _jsonOptions);
                _loggingService.LogDebug($"Generated default configuration for plugin {pluginId}");
                
                return defaultConfigJson;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error generating default configuration for plugin {pluginId}: {ex.Message}", ex);
                // Return empty object on error
                return "{}";
            }
        }

        /// <summary>
        /// Resets the configuration of a plugin to its default values.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <returns>The default configuration JSON string.</returns>
        public string ResetConfiguration(string pluginId)
        {
            // Simply return the default configuration
            return GetDefaultConfiguration(pluginId);
        }

        /// <summary>
        /// Generates a temporary configuration file for an external process plugin.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <param name="instanceId">The ID of the plugin instance.</param>
        /// <param name="configJson">The configuration JSON string to write to the file.</param>
        /// <returns>The path to the generated configuration file.</returns>
        public string GenerateTempConfigFile(string pluginId, string instanceId, string configJson)
        {
            try
            {
                _loggingService.LogDebug($"Generating temporary configuration file for plugin instance {instanceId}");
                
                // Create a unique filename for this plugin instance
                var filePath = Path.Combine(TempConfigDir, $"instance-{instanceId}.json");

                // Write the configuration to the file
                File.WriteAllText(filePath, configJson);

                _loggingService.LogDebug($"Temporary configuration file generated at {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error generating temporary configuration file for plugin instance {instanceId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Builds a default configuration object based on a JSON schema.
        /// </summary>
        /// <param name="schema">The JSON schema.</param>
        /// <returns>A dictionary representing the default configuration.</returns>
        private IDictionary<string, object> BuildDefaultConfiguration(JsonSchema schema)
        {
            var result = new Dictionary<string, object>();

            // Get properties keyword from the schema
            var propertiesKeyword = schema.Keywords?.OfType<PropertiesKeyword>().FirstOrDefault();
            if (propertiesKeyword != null)
            {
                foreach (var property in propertiesKeyword.Properties)
                {
                    var propertyName = property.Key;
                    var propertySchema = property.Value;
                    
                    // Check if this property has a default value
                    var defaultKeyword = propertySchema.Keywords?.OfType<DefaultKeyword>().FirstOrDefault();
                    if (defaultKeyword != null && defaultKeyword.Value != null)
                    {
                        var defaultValue = JsonSerializer.Deserialize<object>(defaultKeyword.Value.ToJsonString());
                        result[propertyName] = defaultValue;
                    }
                    else
                    {
                        // No default value specified, determine by type
                        var typeKeyword = propertySchema.Keywords?.OfType<TypeKeyword>().FirstOrDefault();
                        if (typeKeyword != null)
                        {
                            var type = typeKeyword.Type.ToString().ToLowerInvariant();
                            
                            switch (type)
                            {
                                case "string":
                                    result[propertyName] = string.Empty;
                                    break;
                                case "number":
                                case "integer":
                                    result[propertyName] = 0;
                                    break;
                                case "boolean":
                                    result[propertyName] = false;
                                    break;
                                case "array":
                                    result[propertyName] = new object[0];
                                    break;
                                case "object":
                                    // For nested objects, recursively build defaults
                                    result[propertyName] = BuildDefaultConfiguration(propertySchema);
                                    break;
                                default:
                                    result[propertyName] = null;
                                    break;
                            }
                        }
                        else
                        {
                            // If type not specified, default to null
                            result[propertyName] = null;
                        }
                    }
                }
            }

            return result;
        }
    }
} 