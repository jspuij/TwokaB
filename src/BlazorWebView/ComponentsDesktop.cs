// <copyright file="ComponentsDesktop.cs" company="Steve Sanderson and Jan-Willem Spuij">
// Copyright 2020 Steve Sanderson and Jan-Willem Spuij
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace BlazorWebView
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BlazorWebView;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;
    using Microsoft.JSInterop.Infrastructure;

    /// <summary>
    /// A class that initializes Blazor using the specified <see cref="IBlazorWebView"/> implementation.
    /// </summary>
    public static class ComponentsDesktop
    {
        /// <summary>
        /// Gets a file extension content type provider.
        /// </summary>
        private static readonly FileExtensionContentTypeProvider FileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();

        /// <summary>
        /// Gets initial absolute Uri.
        /// </summary>
        internal static string InitialUriAbsolute { get; private set; }

        /// <summary>
        /// Gets absolute base Uri.
        /// </summary>
        internal static string BaseUriAbsolute { get; private set; }

        /// <summary>
        /// Gets desktop javscript runtime interop.
        /// </summary>
        internal static DesktopJSRuntime DesktopJSRuntime { get; private set; }

        /// <summary>
        /// Gets desktop renderer.
        /// </summary>
        internal static DesktopRenderer DesktopRenderer { get; private set; }

        /// <summary>
        /// Gets a reference to the blazor web view.
        /// </summary>
        internal static IBlazorWebView BlazorWebView { get; private set; }

        /// <summary>
        /// Gets a custom HTTP Scheme for the blazor app.
        /// </summary>
        private static string BlazorAppScheme
        {
            get
            {
                // On Windows, we can't use a custom scheme to host the initial HTML,
                // because webview2 won't let you do top-level navigation to such a URL.
                // On Linux/Mac, we must use a custom scheme, because their webviews
                // don't have a way to intercept http:// scheme requests.
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "http"
                    : "app";
            }
        }

        /// <summary>
        /// Runs Blazor using the specified <see cref="IBlazorWebView"/> implementation,
        /// using the path to the index.html host file and optionally a delegate to
        /// resolve the normal (non framework) resources.
        /// </summary>
        /// <typeparam name="TStartup">A startup class.</typeparam>
        /// <param name="blazorWebView">An <see cref="IBlazorWebView"/> implementation.</param>
        /// <param name="hostHtmlPath">The specified oath to the index.html file.</param>
        /// <param name="defaultResolveDelegate">An optional delegate to resolve non framework resources.</param>
        /// <returns>An <see cref="IDisposable "/> instance that can be used to cleanup Blazor.</returns>
        public static IDisposable Run<TStartup>(IBlazorWebView blazorWebView, string hostHtmlPath, ResolveWebResourceDelegate defaultResolveDelegate = null)
        {
            DesktopSynchronizationContext.UnhandledException += (sender, exception) =>
            {
                UnhandledException(exception);
            };

            if (defaultResolveDelegate == null)
            {
                var contentRootAbsolute = Path.GetDirectoryName(Path.GetFullPath(hostHtmlPath));

                defaultResolveDelegate = (string url, out string contentType, out Encoding encoding) =>
                {
                    // TODO: Only intercept for the hostname 'app' and passthrough for others
                    // TODO: Prevent directory traversal?
                    var appFile = Path.Combine(contentRootAbsolute, new Uri(url).AbsolutePath.Substring(1));
                    if (appFile == contentRootAbsolute)
                    {
                        appFile = hostHtmlPath;
                    }

                    contentType = GetContentType(appFile);

                    if (!File.Exists(appFile))
                    {
                        encoding = Encoding.Default;
                        return null;
                    }

                    return GetEncodingAndOpen(appFile, out encoding);
                };
            }

            BlazorWebView = blazorWebView;
            BlazorWebView.Initialize(options =>
            {
                options.SchemeHandlers.Add(BlazorAppScheme, defaultResolveDelegate);

                // framework:// is resolved as embedded resources
                options.SchemeHandlers.Add("framework", (string url, out string contentType, out Encoding encoding) =>
                {
                    contentType = GetContentType(url);
                    encoding = Encoding.UTF8;
                    return SupplyFrameworkFile(url);
                });
            });

            CancellationTokenSource appLifetimeCts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var ipc = new IPC(BlazorWebView);
                    await RunAsync<TStartup>(ipc, appLifetimeCts.Token);
                }
                catch (Exception ex)
                {
                    UnhandledException(ex);
                    throw;
                }
            });

            try
            {
                BlazorWebView.NavigateToUrl(BlazorAppScheme + "://app/");
            }
            catch
            {
                appLifetimeCts.Cancel();
                throw;
            }

            return new DelegateDisposable(() => appLifetimeCts.Cancel());
        }

        /// <summary>
        /// Gets the content type for the url.
        /// </summary>
        /// <param name="url">The url to use.</param>
        /// <returns>The content type.</returns>
        private static string GetContentType(string url)
        {
            if (FileExtensionContentTypeProvider.TryGetContentType(url, out string result))
            {
                return result;
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// Handles an unhandled exception in blazor.
        /// </summary>
        /// <param name="ex">The unhandled exception.</param>
        private static void UnhandledException(Exception ex)
        {
            BlazorWebView.ShowMessage("Error", $"{ex.Message}\n{ex.StackTrace}");
        }

        /// <summary>
        /// Runs blazor using the specified IPC implementation and a
        /// cancellationtoken that is triggered when the application should end.
        /// </summary>
        /// <typeparam name="TStartup">The type of the startup class.</typeparam>
        /// <param name="ipc">The IPC channel to communicate between blazor and javascript.</param>
        /// <param name="appLifetime">A cancellation token representing the application lifetime.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task RunAsync<TStartup>(IPC ipc, CancellationToken appLifetime)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true);

            DesktopJSRuntime = new DesktopJSRuntime(ipc);
            await PerformHandshakeAsync(ipc);
            AttachJsInterop(ipc, appLifetime);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configurationBuilder.Build());
            serviceCollection.AddLogging(configure => configure.AddConsole());
            serviceCollection.AddSingleton<NavigationManager>(DesktopNavigationManager.Instance);
            serviceCollection.AddSingleton<IJSRuntime>(DesktopJSRuntime);
            serviceCollection.AddSingleton<INavigationInterception, DesktopNavigationInterception>();
            serviceCollection.AddSingleton(BlazorWebView);

            var startup = new ConventionBasedStartup(Activator.CreateInstance(typeof(TStartup)));
            startup.ConfigureServices(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();
            var builder = new DesktopApplicationBuilder(services);
            startup.Configure(builder, services);

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            DesktopRenderer = new DesktopRenderer(services, ipc, loggerFactory);
            DesktopRenderer.UnhandledException += (sender, exception) =>
            {
                Console.Error.WriteLine(exception);
            };

            foreach (var rootComponent in builder.Entries)
            {
                _ = DesktopRenderer.AddComponentAsync(rootComponent.componentType, rootComponent.domElementSelector);
            }
        }

        /// <summary>
        /// A function to supply the framework file from an assembly resource stream.
        /// </summary>
        /// <param name="uri">The uri for the framework file.</param>
        /// <returns>A stream.</returns>
        private static Stream SupplyFrameworkFile(string uri)
        {
            switch (uri)
            {
                case "framework://blazor.desktop.js":
                    return typeof(ComponentsDesktop).Assembly.GetManifestResourceStream("BlazorWebView.blazor.desktop.js");
                default:
                    throw new ArgumentException($"Unknown framework file: {uri}");
            }
        }

        /// <summary>
        /// Performs the handshake with the javascript part of blazor.
        /// </summary>
        /// <param name="ipc">The IPC channel to communicate between blazor and javascript.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task PerformHandshakeAsync(IPC ipc)
        {
            var tcs = new TaskCompletionSource<object>();
            ipc.Once("components:init", args =>
            {
                var argsArray = (object[])args;
                InitialUriAbsolute = ((JsonElement)argsArray[0]).GetString();
                BaseUriAbsolute = ((JsonElement)argsArray[1]).GetString();

                tcs.SetResult(null);
            });

            await tcs.Task;
        }

        /// <summary>
        /// Attach the javascript interop functions.
        /// </summary>
        /// <param name="ipc">The ipc channel to use.</param>
        /// <param name="appLifetime">A cancellation token representing the application lifetime.</param>
        private static void AttachJsInterop(IPC ipc, CancellationToken appLifetime)
        {
            var desktopSynchronizationContext = new DesktopSynchronizationContext(appLifetime);
            SynchronizationContext.SetSynchronizationContext(desktopSynchronizationContext);

            ipc.On("BeginInvokeDotNetFromJS", args =>
            {
                desktopSynchronizationContext.Send(
                    state =>
                {
                    var argsArray = (object[])state;
                    DotNetDispatcher.BeginInvokeDotNet(
                        DesktopJSRuntime,
                        new DotNetInvocationInfo(
                            assemblyName: ((JsonElement)argsArray[1]).GetString(),
                            methodIdentifier: ((JsonElement)argsArray[2]).GetString(),
                            dotNetObjectId: ((JsonElement)argsArray[3]).GetInt64(),
                            callId: ((JsonElement)argsArray[0]).GetString()),
                        ((JsonElement)argsArray[4]).GetString());
                }, args);
            });

            ipc.On("EndInvokeJSFromDotNet", args =>
            {
                desktopSynchronizationContext.Send(
                    state =>
                {
                    var argsArray = (object[])state;
                    DotNetDispatcher.EndInvokeJS(
                        DesktopJSRuntime,
                        ((JsonElement)argsArray[2]).GetString());
                }, args);
            });
        }

        /// <summary>
        /// Gets the encoding for a file and opens it.
        /// </summary>
        /// <param name="filename">The filename for the file to open.</param>
        /// <param name="encoding">The detected encoding.</param>
        /// <returns>A stream that represents the file.</returns>
        private static Stream GetEncodingAndOpen(string filename, out Encoding encoding)
        {
            // Read the BOM
            var bom = new byte[4];
            var file = File.OpenRead(filename);
            file.Read(bom, 0, 4);
            file.Position = 0;

            // lets assume UTF8 is reasonably safe for web.
            encoding = Encoding.UTF8;

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
            {
                encoding = Encoding.UTF7;
            }
            else if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            {
                encoding = Encoding.UTF8;
            }
            else if (bom[0] == 0xff && bom[1] == 0xfe)
            {
                encoding = Encoding.Unicode;
            }
            else if (bom[0] == 0xfe && bom[1] == 0xff)
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
            {
                encoding = Encoding.UTF32;
            }

            return file;
        }

        /// <summary>
        /// Log a message from blazor.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private static void Log(string message)
        {
            var process = Process.GetCurrentProcess();
            Console.WriteLine($"[{process.ProcessName}:{process.Id}] out: " + message);
        }

        /// <summary>
        /// A class that implements <see cref="IDisposable"/> by calling a delegate.
        /// </summary>
        private sealed class DelegateDisposable : IDisposable
        {
            /// <summary>
            /// The delegate to call on disposal.
            /// </summary>
            private readonly Action action;

            /// <summary>
            /// Initializes a new instance of the <see cref="DelegateDisposable"/> class.
            /// </summary>
            /// <param name="action">The delegate to call on disposal.</param>
            public DelegateDisposable(Action action)
            {
                this.action = action;
            }

            /// <summary>
            ///  Performs application-defined tasks associated with freeing, releasing, or resetting
            ///  unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                this.action();
            }
        }
    }
}
