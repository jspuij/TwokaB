// <copyright file="BlazorWebView.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace BlazorWebView.iOS
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using CoreGraphics;
    using Foundation;
    using Newtonsoft.Json;
    using UIKit;
    using WebKit;

    /// <summary>
    /// An <see cref="IBlazorWebView"/> implementation for iOS.
    /// </summary>
    [Register("BlazorWebView")]
    [DesignTimeVisible(true)]
    [Category("New Controls")]
    public class BlazorWebView : UIControl, IWKScriptMessageHandler, IBlazorWebView
    {
        /// <summary>
        /// The initialization script for the callbacks.
        /// </summary>
        private const string InitScriptSource =
            @"window.__receiveMessageCallbacks = [];
	 		 window.__dispatchMessageCallback = function(message) {
			 	window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
			 };
			 window.external = {
			 	sendMessage: function(message) {
			 		window.webkit.messageHandlers.webwindowinterop.postMessage(message);
			 	},
			 	receiveMessage: function(callback) {
			 		window.__receiveMessageCallbacks.push(callback);
			 	}
			 };";

        /// <summary>
        /// The thread ID of the owner thread.
        /// </summary>
        private int ownerThreadId;

        /// <summary>
        /// The inner <see cref="WKWebView"/>.
        /// </summary>
        private WKWebView webView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorWebView"/> class.
        /// </summary>
        /// <param name="handle">A handle to an encapsulated native objet.</param>
        public BlazorWebView(IntPtr handle)
           : base(handle)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorWebView"/> class.
        /// </summary>
        public BlazorWebView()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorWebView"/> class.
        /// </summary>
        /// <param name="frameRect">A rectangle defining location and size.</param>
        [Export("initWithFrame:")]
        public BlazorWebView(CGRect frameRect)
            : base(frameRect)
        {
            this.Initialize();
        }

        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        public event EventHandler<string> OnWebMessageReceived;

        /// <summary>
        /// Invoked when a script message is received from a webpage.
        /// </summary>
        /// <param name="userContentController">The user content controller invoking the delegate method.</param>
        /// <param name="message">The script message received.</param>
        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            this.OnWebMessageReceived?.Invoke(this, (NSString)message.Body);
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
                this.InvokeOnMainThread(callback);
            }
        }

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            message = JsonConvert.ToString(message);

            WKJavascriptEvaluationResult wKJavascriptEvaluationResult =
                (o, e) =>
                {
                    if (e != null)
                    {
                        // TODO: Handle javascript error.
                        Console.WriteLine(e.ToString());
                    }
                };

            this.webView.EvaluateJavaScript($"__dispatchMessageCallback({message})", wKJavascriptEvaluationResult);
        }

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        public void NavigateToUrl(string url)
        {
            var nsUrl = NSUrl.FromString(url);
            var request = new NSUrlRequest(nsUrl);
            this.webView.LoadRequest(request);
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
#pragma warning disable CS0618 // Type or member is obsolete
                var alertview = new UIAlertView(title, message, null, "Cancel", null);
#pragma warning restore CS0618 // Type or member is obsolete
                alertview.Show();
            });
        }

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webvies.</param>
        public void Initialize(Action<WebViewOptions> configure)
        {
            var options = new WebViewOptions();
            configure.Invoke(options);

            var webConfig = new WKWebViewConfiguration();
            webConfig.UserContentController = new WKUserContentController();
            webConfig.UserContentController.AddUserScript(new WKUserScript((NSString)InitScriptSource, WKUserScriptInjectionTime.AtDocumentStart, true));
            webConfig.Preferences.SetValueForKey(NSNumber.FromBoolean(true), (NSString)"developerExtrasEnabled");
            webConfig.UserContentController.AddScriptMessageHandler(this, "webwindowinterop");

            foreach (var (schemeName, handler) in options.SchemeHandlers)
            {
                this.AddCustomScheme(webConfig, schemeName, handler);
            }

            this.webView = new WKWebView(this.Frame, webConfig);
            this.webView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
            this.AddSubview(this.webView);
            this.AutosizesSubviews = true;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            this.ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Adds a custom scheme handler to the collection of schemes to handle.
        /// </summary>
        /// <param name="webConfig">The webiew configuration.</param>
        /// <param name="scheme">The scheme to use.</param>
        /// <param name="requestHandler">The handler for the scheme.</param>
        private void AddCustomScheme(WKWebViewConfiguration webConfig, string scheme, ResolveWebResourceDelegate requestHandler)
        {
            var urlSchemeHandler = new UrlSchemeHandler(requestHandler);
            webConfig.SetUrlSchemeHandler(urlSchemeHandler, scheme);
        }
    }
}
