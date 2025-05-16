using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"PluginB: Started with arguments: {string.Join(" ", args)}");

        string portStr = "8091"; // Default port
        string instanceId = "unknown_instance";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                portStr = args[i + 1];
            }
            if (args[i] == "--instance-id" && i + 1 < args.Length)
            {
                instanceId = args[i + 1];
            }
        }

        if (!int.TryParse(portStr, out int port))
        {
            port = 8091; // Fallback to default if parsing fails
            Console.WriteLine($"PluginB: Invalid port specified, using default {port}");
        }
        
        Console.WriteLine($"PluginB (Instance: {instanceId}): Attempting to listen on port {port}");

        HttpListener listener = new HttpListener();
        try
        {
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Listening on http://localhost:{port}/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Error starting listener on port {port}. {ex.Message}");
            return; // Exit if listener fails to start
        }
        
        // Handle Ctrl+C or termination signal for graceful shutdown
        Console.CancelKeyPress += (sender, eventArgs) => {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Stopping listener...");
            listener.Stop();
            listener.Close();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Stopped.");
            eventArgs.Cancel = true; // Prevent process termination by Ctrl+C if we want to do more cleanup
            Environment.Exit(0);
        };

        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = $"<HTML><BODY>Hello from PluginB (Instance: {instanceId})! Path: {request.Url.AbsolutePath}</BODY></HTML>";
                if (request.Url.AbsolutePath == "/ping")
                {
                    responseString = $"PluginB (Instance: {instanceId}) pong at {DateTime.Now}";
                }
                else if (request.Url.AbsolutePath == "/config")
                {
                    // Example: Read a config file passed via args or known location
                    // For simplicity, just returning a message
                    responseString = $"PluginB (Instance: {instanceId}) config endpoint. Args: {string.Join(" ", args)}";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                output.Close();
                Console.WriteLine($"PluginB (Instance: {instanceId}): Responded to {request.Url}");
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Operation Aborted (typically on Stop)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Listener stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): An error occurred: {ex.Message}");
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
            listener.Close();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Final shutdown.");
        }
    }
} 