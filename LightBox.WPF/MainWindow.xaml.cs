using System.Text;
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

        public MainWindow()
        {
            // Instantiate services
            _logger = new SimpleFileLogger(); // Assuming default constructor or provide path
            _applicationSettingsService = new ApplicationSettingsService(_logger); // Pass logger
            _pluginService = new PluginManager(_applicationSettingsService, _logger); // Pass both services

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
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    
                    // Removed CoreWebView2InitializationCompleted event handler subscription

                    _logger.LogInfo("InitializeWebViewAsync - Attempting to add host object and open DevTools directly.");
                    try
                    {
                        var jsBridge = new LightBoxJsBridge(_applicationSettingsService, _pluginService, _logger);
                        webView.CoreWebView2.AddHostObjectToScript("lightboxBridge", jsBridge);
                        _logger.LogInfo("InitializeWebViewAsync - lightboxBridge (LightBoxJsBridge) added successfully.");
                        webView.CoreWebView2.OpenDevToolsWindow();
                        _logger.LogInfo("InitializeWebViewAsync - DevTools opened successfully.");
                    }
                    catch (Exception bridgeEx)
                    {
                        _logger.LogError("InitializeWebViewAsync - Error adding host object or opening DevTools directly.", bridgeEx);
                        MessageBox.Show($"Error setting up JSBridge/DevTools: {bridgeEx.Message}", "Bridge Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

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
                            webView.CoreWebView2.Navigate("https://lightbox.app.local/index.html");
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

        // WebView_CoreWebView2InitializationCompleted method removed as it's no longer used.
    }

    // LightBoxJsBridgePlaceholder class removed as it's replaced by LightBoxJsBridge.
}