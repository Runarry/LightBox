using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.IO; 
using System;

namespace LightBox.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); 
            InitializeWebViewAsync();
        }

        async void InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LightBox", "WebView2UserData");
                Directory.CreateDirectory(userDataFolder);

                var environmentOptions = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--enable-features=OverlayScrollbar"
                };
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, environmentOptions);
                
                await webView.EnsureCoreWebView2Async(environment); 

                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    // Removed: webView.CoreWebView2.Profile.DefaultBackgroundColor = DWEBVIEW2_DEFAULT_BACKGROUND_COLOR;
                    webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                }
                else
                {
                     MessageBox.Show("CoreWebView2 is null after EnsureCoreWebView2Async.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Changed object sender to object? sender to address CS8622
        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                if (webView.CoreWebView2 != null) 
                {
                    webView.CoreWebView2.AddHostObjectToScript("lightboxBridge", new LightBoxJsBridgePlaceholder());
                    // webView.CoreWebView2.Navigate("https://www.bing.com"); // For initial testing
                }
                else
                {
                     MessageBox.Show("CoreWebView2 is null in CoreWebView2InitializationCompleted.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"WebView2 Core Initialization failed: {e.InitializationException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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