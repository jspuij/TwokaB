// <copyright file="BlazorOldEdgeWebView.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorWebView.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
    using Microsoft.Toolkit.Wpf.UI.Controls;

    /// <summary>
    /// A Blazor Webview Implementation for the old edge.
    /// </summary>
    public sealed class BlazorOldEdgeWebView : UserControl, IBlazorWebView
    {
        /// <summary>
        /// The initialization script for the callbacks.
        /// </summary>
        private const string InitScriptSource =
         @"window.__receiveMessageCallbacks = [];
             window.__dispatchMessageCallback = function(message) {
			 	window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
			 };
			 window.external.sendMessage = function(message) {
			 		window.external.notify(message);
			 	};
			 window.external.receiveMessage = function(callback) {
			 		window.__receiveMessageCallbacks.push(callback);
			 	};
			 ";

#pragma warning disable CS0618 // Type or member is obsolete
        private readonly WebView webview;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The thread id of the owner thread.
        /// </summary>
        private readonly int ownerThreadId;

        /// <summary>
        /// An uri to stream resolver for Edge.
        /// </summary>
        private readonly EdgeUriToStreamResolver uriToStreamResolver = new EdgeUriToStreamResolver();

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorOldEdgeWebView"/> class.
        /// </summary>
        public BlazorOldEdgeWebView()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            this.webview = new WebView();

            this.webview.ScriptNotify += this.Webview_ScriptNotify;

#pragma warning restore CS0618 // Type or member is obsolete
            this.Content = this.webview;
            this.ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        public event EventHandler<string> OnWebMessageReceived;

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webview.</param>
        public void Initialize(Action<WebViewOptions> configure)
        {
            var options = new WebViewOptions();
            configure.Invoke(options);

            this.webview.Navigate("about:blank");

            foreach (var (schemeName, handler) in options.SchemeHandlers)
            {
                this.uriToStreamResolver.AddSchemeHandler(schemeName, handler);
            }
        }

        /// <summary>
        /// Invoke a callback on the UI thread.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        public void Invoke(Action callback)
        {
            // If we're already on the UI thread, no need to dispatch
            if (Thread.CurrentThread.ManagedThreadId == this.ownerThreadId)
            {
                callback();
            }
            else
            {
                this.Dispatcher.Invoke(callback);
            }
        }

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        public void NavigateToUrl(string url)
        {
            var uri = new Uri(url);
            if (uri.Host == "app")
            {
                this.webview.Settings.IsScriptNotifyAllowed = true;
                this.webview.Settings.IsJavaScriptEnabled = true;
                this.webview.AddInitializeScript(InitScriptSource);
                var indexUri = new Uri(uri, "/index.html");
                this.webview.NavigateToLocalStreamUri(indexUri.MakeRelativeUri(uri), this.uriToStreamResolver);
            }
            else
            {
                this.webview.Navigate(uri);
            }
        }

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            this.webview.InvokeScript("__dispatchMessageCallback", message);
        }

        /// <summary>
        /// Show a native dialog for the platform with the specified message.
        /// </summary>
        /// <param name="title">The title to show.</param>
        /// <param name="message">The message to show.</param>
        public void ShowMessage(string title, string message)
        {
            this.Invoke(() =>
            {
                MessageBox.Show(message, title);
            });
        }

        /// <summary>
        /// Event handler for script notifications from Edge.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The event arguments.</param>
        private void Webview_ScriptNotify(object sender, WebViewControlScriptNotifyEventArgs eventArgs)
        {
            this.OnWebMessageReceived?.Invoke(this, eventArgs.Value);
        }

        /// <summary>
        /// Class that resolves an Uri to a stream.
        /// </summary>
        private class EdgeUriToStreamResolver : IUriToStreamResolver
        {
            private readonly Dictionary<string, ResolveWebResourceDelegate> schemeHandlers = new Dictionary<string, ResolveWebResourceDelegate>();

            /// <summary>
            /// Adds a scheme handler to the EdgeResolver.
            /// </summary>
            /// <param name="schemeName">The name of the scheme.</param>
            /// <param name="handler">The handler.</param>
            public void AddSchemeHandler(string schemeName, ResolveWebResourceDelegate handler)
            {
                this.schemeHandlers.Add(schemeName, handler);
            }

            /// <summary>
            /// Resolves the Uri to a stream or throws an Exception.
            /// </summary>
            /// <param name="uri">The uri to resolve.</param>
            /// <returns>A stream with content.</returns>
            public Stream UriToStream(Uri uri)
            {
                var defaultHandler = this.schemeHandlers["http"];
                foreach (var handler in this.schemeHandlers.Where(s => s.Value != defaultHandler))
                {
                    var hash = GetHashString(handler.Key);
                    if (uri.AbsolutePath.StartsWith(hash) || uri.AbsolutePath.StartsWith($"/{hash}"))
                    {
                        var newUri = uri.AbsolutePath.Replace($"/{hash}/", $"{handler.Key}://");
                        return this.Resolve(newUri, handler.Value);
                    }
                }

                return this.Resolve(new Uri(new Uri("http://app/"), uri.PathAndQuery).ToString(), defaultHandler);
            }

            /// <summary>
            /// Gets a hash string for a text.
            /// </summary>
            /// <param name="text">The text to hast.</param>
            /// <returns>A hash string.</returns>
            private static string GetHashString(string text)
            {
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    byte[] textData = Encoding.UTF8.GetBytes(text);
                    byte[] hash = sha.ComputeHash(textData);
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }

            /// <summary>
            /// Resolves an Uri to a stream.
            /// </summary>
            /// <param name="uri">The uri to resolve.</param>
            /// <param name="resolveWebResourceDelegate">The delegate to call.</param>
            /// <returns>A Stream.</returns>
            private Stream Resolve(string uri, ResolveWebResourceDelegate resolveWebResourceDelegate)
            {
                var result = resolveWebResourceDelegate(uri, out string contentType, out Encoding encoding);

                if (result == null)
                {
                    throw new ArgumentOutOfRangeException(uri.ToString());
                }

                if (contentType == "text/html")
                {
                    var finalStream = new MemoryStream((int)result.Length + 100);

                    using (var tempStream = new MemoryStream((int)result.Length + 100))
                    using (var reader = new StreamReader(result, encoding))
                    using (var writer = new StreamWriter(tempStream, encoding))
                    {
                        var defaultHandler = this.schemeHandlers["http"];

                        var content = reader.ReadToEnd();
                        foreach (var handler in this.schemeHandlers.Where(s => s.Value != defaultHandler))
                        {
                            var hash = GetHashString(handler.Key);
                            content = content.Replace($"{handler.Key}://", $"{hash}/");
                        }

                        writer.Write(content);
                        writer.Flush();
                        tempStream.Position = 0;
                        tempStream.CopyTo(finalStream);
                        finalStream.Position = 0;
                    }

                    result.Dispose();
                    result = finalStream;
                }

                return result;
            }
        }
    }
}
