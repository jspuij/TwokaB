using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using AppKit;
using CoreGraphics;
using Foundation;
using WebKit;
using Newtonsoft.Json;

namespace BlazorWebView.Mac
{
    [Register("BlazorWebView")]
    [DesignTimeVisible(true)]
    [Category("New Controls")]
    public class BlazorWebView : NSControl, IWKScriptMessageHandler, IBlazorWebView
    {
        private int ownerThreadId;

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

        private WKWebView webView;

        public event EventHandler<string> OnWebMessageReceived;

        public BlazorWebView(IntPtr p)
           : base(p)
        {
            Initialize();
        }

        public BlazorWebView()
        {
            Initialize();
        }

        void Initialize()
        {
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        [Export("initWithFrame:")]
        public BlazorWebView(CGRect frameRect) : base(frameRect)
        {
            // Init
            Initialize();
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            this.OnWebMessageReceived?.Invoke(this, (NSString)message.Body);
        }

        public void Invoke(Action callback)
        {
            InvokeOnMainThread(callback);
        }

        public void SendMessage(string message)
        {
            message = JsonConvert.ToString(message);
           
            WKJavascriptEvaluationResult wKJavascriptEvaluationResult =
                (o, e) =>
                {
                    if (e != null)
                    {
                        Console.WriteLine(e.ToString());
                    }
                };

            webView.EvaluateJavaScript($"__dispatchMessageCallback({message})", wKJavascriptEvaluationResult);
        }

        public void NavigateToUrl(string url)
        {
            var nsUrl = NSUrl.FromString(url);
            var request = new NSUrlRequest(nsUrl);
            webView.LoadRequest(request);
        }

        public void ShowMessage(string title, string message)
        {
            this.InvokeOnMainThread(() =>
            {
                var alert = new NSAlert();
                alert.Window.Title = title;
                alert.MessageText = message;
                alert.RunModal();
            });
        }

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
                AddCustomScheme(webConfig, schemeName, handler);
            }

            webView = new WKWebView(this.Frame, webConfig);
            webView.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
            this.AddSubview(webView);
            this.AutoresizesSubviews = true;
        }

        private void AddCustomScheme(WKWebViewConfiguration webConfig, string scheme, ResolveWebResourceDelegate requestHandler)
        {
            var urlSchemeHandler = new UrlSchemeHandler(requestHandler);
            webConfig.SetUrlSchemeHandler(urlSchemeHandler, scheme);
        }
    }
}
