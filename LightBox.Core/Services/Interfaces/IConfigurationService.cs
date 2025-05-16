using System.Collections.Generic;
using Json.Schema;

namespace LightBox.Core.Services.Interfaces
{
    /// <summary>
    /// Provides configuration management services for plugins.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Validates plugin configuration against the schema defined in the plugin's manifest.json
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <param name="configJson">The configuration JSON string to validate.</param>
        /// <returns>Validation result with information about success or failure.</returns>
        ValidationResult ValidateConfiguration(string pluginId, string configJson);

        /// <summary>
        /// Gets the default configuration for a plugin based on its schema.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <returns>The default configuration JSON string.</returns>
        string GetDefaultConfiguration(string pluginId);

        /// <summary>
        /// Resets the configuration of a plugin to its default values.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <returns>The default configuration JSON string.</returns>
        string ResetConfiguration(string pluginId);

        /// <summary>
        /// Generates a temporary configuration file for an external process plugin.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin.</param>
        /// <param name="instanceId">The ID of the plugin instance.</param>
        /// <param name="configJson">The configuration JSON string to write to the file.</param>
        /// <returns>The path to the generated configuration file.</returns>
        string GenerateTempConfigFile(string pluginId, string instanceId, string configJson);
    }

    /// <summary>
    /// Represents the result of a configuration validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of validation errors if validation failed.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
} 