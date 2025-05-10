using System.Text;
using System.Text.Json; // Added this using
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.IO; 
using System;
using System.Reflection; 
using System.Diagnostics; 
using LightBox.Core.Services.Interfaces;
using LightBox.Core.Services.Implementations;
using LightBox.Core.Models; // For ApplicationSettings, if needed directly, though likely not for instantiation here

namespace LightBox.WPF
{
    public partial class MainWindow : Window
    {
        private readonly ILoggingService _logger;
        private readonly IApplicationSettingsService _applicationSettingsService;
        private readonly IPluginService _pluginService;
        private readonly IWorkspaceService _workspaceService; // Added
        private LightBoxJsBridge? _jsBridge; // <--- Declare as a field

        public MainWindow()
        {
            // Instantiate services
            _logger = new SimpleFileLogger(); // Assuming default constructor or provide path
            _applicationSettingsService = new ApplicationSettingsService(_logger); // Pass logger
            _pluginService = new PluginManager(_applicationSettingsService, _logger); // Pass both services
            _workspaceService = new WorkspaceManager(_applicationSettingsService, _logger); // Added

            _logger.LogInfo("MainWindow constructor - Start, services instantiated.");
            InitializeComponent();
            _logger.LogInfo("MainWindow constructor - InitializeComponent completed.");
            InitializeWebViewAsync();
            _logger.LogInfo("MainWindow constructor - InitializeWebViewAsync called.");
            _logger.LogInfo("MainWindow constructor - End");
        }

        async void InitializeWebViewAsync()
        {
            _logger.LogInfo("InitializeWebViewAsync - Start");
            try
            {
                string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LightBox", "WebView2UserData");
                _logger.LogDebug($"InitializeWebViewAsync - User data folder: {userDataFolder}");
                Directory.CreateDirectory(userDataFolder);

                var environmentOptions = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--enable-features=OverlayScrollbar"
                };
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, environmentOptions);
                _logger.LogInfo("InitializeWebViewAsync - CoreWebView2Environment created.");
                
                await webView.EnsureCoreWebView2Async(environment); 
                _logger.LogInfo("InitializeWebViewAsync - EnsureCoreWebView2Async completed.");

                if (webView.CoreWebView2 != null)
                {
                    _logger.LogInfo("InitializeWebViewAsync - CoreWebView2 is not null.");
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false; // Keep this
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false; // Keep this
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // Ensure DevTools are enabled
                    webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived; // Subscribe to WebMessageReceived
                    webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted; // Keep this for potential DevTools opening

                    // --- Remove AddHostObjectToScript logic ---
                    // Ensure _jsBridge is initialized here or somewhere accessible by the message handler
                    _jsBridge = new LightBoxJsBridge(_applicationSettingsService, _pluginService, _logger, _workspaceService); // Added _workspaceService

                    string? exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        string solutionRootPath = Path.GetFullPath(Path.Combine(exePath, "..", "..", "..", ".."));
                        string distPath = Path.Combine(solutionRootPath, "LightBox.WebViewUI", "dist");

                        if (Directory.Exists(distPath))
                        {
                            _logger.LogInfo($"InitializeWebViewAsync - Mapping lightbox.app.local to {distPath}");
                            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                                "lightbox.app.local", 
                                distPath,             
                                CoreWebView2HostResourceAccessKind.Allow 
                            );
                            _logger.LogInfo("InitializeWebViewAsync - Navigating to https://lightbox.app.local/index.html");
                            webView.CoreWebView2.Navigate("http://lightbox.app.local/index.html");
                        }
                        else
                        {
                            _logger.LogError($"InitializeWebViewAsync - distPath for SetVirtualHostNameToFolderMapping not found: {distPath}. Navigating to Bing.");
                            MessageBox.Show($"Frontend 'dist' directory not found at: {distPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            webView.CoreWebView2.Navigate("https://www.bing.com");
                        }
                    }
                    else
                    {
                         _logger.LogError("InitializeWebViewAsync - exePath is null or empty. Cannot set up virtual host. Navigating to Bing.");
                         MessageBox.Show("Application executable path could not be determined. Cannot load local frontend.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                         webView.CoreWebView2.Navigate("https://www.bing.com");
                    }
                }
                else
                {
                     _logger.LogError("InitializeWebViewAsync - CoreWebView2 is null after EnsureCoreWebView2Async.");
                     MessageBox.Show("CoreWebView2 is null after EnsureCoreWebView2Async.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("InitializeWebViewAsync - WebView2 initialization failed.", ex);
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _logger.LogInfo("InitializeWebViewAsync - End");
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _logger.LogInfo("CoreWebView2_NavigationCompleted - Navigation completed.");
            if (e.IsSuccess && webView?.CoreWebView2 != null)
            {
                 // Open DevTools after navigation if needed
                 webView.CoreWebView2.OpenDevToolsWindow();
                 _logger.LogInfo("CoreWebView2_NavigationCompleted - DevTools opened.");
            } else if (!e.IsSuccess) {
                 _logger.LogError($"CoreWebView2_NavigationCompleted - Navigation failed with error code: {e.WebErrorStatus}");
            }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Use WebMessageAsJson when JS sends an object via postMessage
            string messageJson = e.WebMessageAsJson;
            if (string.IsNullOrEmpty(messageJson))
            {
                _logger.LogWarning("Received empty or null web message JSON.");
                return;
            }

            _logger.LogDebug($"Received web message: {messageJson}");

            try
            {
                var message = JsonSerializer.Deserialize<WebMessageRequest>(messageJson);
                if (message == null || string.IsNullOrEmpty(message.Command))
                {
                    _logger.LogError("Failed to deserialize web message or command is missing.");
                    return;
                }

                string? resultJson = null;
                string? errorJson = null;

                try
                {
                    if (_jsBridge == null)
                    {
                         throw new InvalidOperationException("JSBridge is not initialized.");
                    }

                    switch (message.Command)
                    {
                        case "getApplicationSettings":
                            resultJson = await _jsBridge.GetApplicationSettings();
                            _logger.LogDebug($"GetApplicationSettings resultJson: {resultJson}"); // Added log
                            break;
                        case "saveApplicationSettings":
                            // Assuming SaveApplicationSettings now also returns Task or Task<string> for consistency or error reporting
                            await _jsBridge.SaveApplicationSettings(message.Payload ?? string.Empty);
                            resultJson = JsonSerializer.Serialize(new { success = true }); // Indicate success
                            break;
                        case "getAllPluginDefinitions":
                            resultJson = await _jsBridge.GetAllPluginDefinitions();
                            break;
                        // Workspace commands
                        case "getWorkspaces":
                            resultJson = await _jsBridge.GetWorkspaces();
                            break;
                        case "createWorkspace":
                            // Assuming payload for createWorkspace is: { "name": "string", "icon": "string" }
                            var createArgs = JsonSerializer.Deserialize<CreateWorkspaceArgs>(message.Payload ?? "{}");
                            if (createArgs == null || string.IsNullOrEmpty(createArgs.Name))
                                throw new ArgumentException("Invalid payload for createWorkspace. Name is required.");
                            resultJson = await _jsBridge.CreateWorkspace(createArgs.Name, createArgs.Icon ?? string.Empty);
                            break;
                        case "setActiveWorkspace":
                            // Assuming payload for setActiveWorkspace is: { "workspaceId": "string" }
                            var setActiveArgs = JsonSerializer.Deserialize<SetActiveWorkspaceArgs>(message.Payload ?? "{}");
                            if (setActiveArgs == null || string.IsNullOrEmpty(setActiveArgs.WorkspaceId))
                                throw new ArgumentException("Invalid payload for setActiveWorkspace. WorkspaceId is required.");
                            await _jsBridge.SetActiveWorkspace(setActiveArgs.WorkspaceId);
                            resultJson = JsonSerializer.Serialize(new { success = true });
                            break;
                        case "getActiveWorkspace":
                            resultJson = await _jsBridge.GetActiveWorkspace();
                            break;
                        case "updateWorkspace":
                            // Payload is the workspace JSON string
                            await _jsBridge.UpdateWorkspace(message.Payload ?? string.Empty);
                            resultJson = JsonSerializer.Serialize(new { success = true });
                            break;
                        case "deleteWorkspace":
                            // Assuming payload for deleteWorkspace is: { "workspaceId": "string" }
                             var deleteArgs = JsonSerializer.Deserialize<DeleteWorkspaceArgs>(message.Payload ?? "{}");
                            if (deleteArgs == null || string.IsNullOrEmpty(deleteArgs.WorkspaceId))
                                throw new ArgumentException("Invalid payload for deleteWorkspace. WorkspaceId is required.");
                            await _jsBridge.DeleteWorkspace(deleteArgs.WorkspaceId);
                            resultJson = JsonSerializer.Serialize(new { success = true });
                            break;
                        default:
                            throw new NotSupportedException($"Command '{message.Command}' is not supported.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing command '{message.Command}': {ex.Message}", ex);
                    errorJson = JsonSerializer.Serialize(new { message = ex.Message }); // Send back error message
                }

                // Send response back to JavaScript
                var response = new WebMessageResponse
                {
                    CallbackId = message.CallbackId,
                    Result = resultJson,
                    Error = errorJson
                };
                // Serialize the response object to a JSON string first
                string responseJson = JsonSerializer.Serialize(response);
                 _logger.LogDebug($"Sending web message response: {responseJson}");
                 // Pass the JSON string to PostWebMessageAsJson
                webView?.CoreWebView2?.PostWebMessageAsJson(responseJson);

            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"Error deserializing web message JSON: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error processing web message: {ex.Message}", ex);
            }
        }
    } // This closes the MainWindow class

    // Helper classes for WebMessage structure
    public class WebMessageRequest
    {
        public string Command { get; set; } = string.Empty;
        public string? Payload { get; set; } // JSON string for arguments
        public string CallbackId { get; set; } = string.Empty; // To match response with request
    }

    public class WebMessageResponse
    {
        public string CallbackId { get; set; } = string.Empty;
        public string? Result { get; set; } // JSON string of the result
        public string? Error { get; set; } // JSON string of the error details
    }

    // Helper classes for command payloads
    internal class CreateWorkspaceArgs
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }
    internal class SetActiveWorkspaceArgs
    {
        public string WorkspaceId { get; set; } = string.Empty;
    }
    internal class DeleteWorkspaceArgs
    {
        public string WorkspaceId { get; set; } = string.Empty;
    }

} // This closes the namespace
