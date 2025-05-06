using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.IO; 
using System;
using System.Reflection; 
using System.Diagnostics; 
using LightBox.Core.Services.Interfaces; 
using LightBox.Core.Services.Implementations; 

namespace LightBox.WPF
{
    public partial class MainWindow : Window
    {
        private readonly ILoggingService _logger; 

        public MainWindow()
        {
            _logger = new SimpleFileLogger(); 
            _logger.LogInfo("MainWindow constructor - Start");
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
                    
                    webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                    _logger.LogInfo("InitializeWebViewAsync - CoreWebView2InitializationCompleted event handler added.");

                    _logger.LogInfo("InitializeWebViewAsync - Attempting to add host object and open DevTools directly.");
                    try
                    {
                        webView.CoreWebView2.AddHostObjectToScript("lightboxBridge", new LightBoxJsBridgePlaceholder());
                        _logger.LogInfo("InitializeWebViewAsync - lightboxBridge added successfully.");
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
                        string indexPath = Path.Combine(solutionRootPath, "LightBox.WebViewUI", "dist", "index.html");
                        bool fileExists = File.Exists(indexPath);

                        _logger.LogInfo($"InitializeWebViewAsync - Attempting direct navigation to local file: {indexPath}");
                        _logger.LogInfo($"InitializeWebViewAsync - Local file exists: {fileExists}");
                        if (fileExists)
                        {
                            webView.CoreWebView2.Navigate($"file:///{indexPath.Replace('\\', '/')}");
                        }
                        else
                        {
                            _logger.LogWarning($"InitializeWebViewAsync - Local index.html not found at {indexPath}. Navigating to Bing.");
                            webView.CoreWebView2.Navigate("https://www.bing.com");
                        }
                    }
                    else
                    {
                         _logger.LogError("InitializeWebViewAsync - exePath is null or empty. Navigating to Bing.");
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

        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _logger.LogInfo($"WebView_CoreWebView2InitializationCompleted - Event Fired. IsSuccess: {e.IsSuccess}");
            if (e.IsSuccess)
            {
                if (webView.CoreWebView2 != null) 
                {
                    _logger.LogInfo("WebView_CoreWebView2InitializationCompleted - CoreWebView2 is not null (event).");
                }
                else
                {
                     _logger.LogError("WebView_CoreWebView2InitializationCompleted - CoreWebView2 is null in success branch (event).");
                }
            }
            else
            {
                _logger.LogError($"WebView_CoreWebView2InitializationCompleted - WebView2 Core Initialization failed (event): {e.InitializationException?.Message}", e.InitializationException);
            }
            _logger.LogInfo("WebView_CoreWebView2InitializationCompleted - End (event)");
        }
    }

    public class LightBoxJsBridgePlaceholder
    {
        public string Echo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"JSBridge Echo called with: {message}");
            return $"C# says: '{message}' received!";
        }
    }
}