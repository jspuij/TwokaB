using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Support.V4.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using Android.Support.V7.App;
using Newtonsoft.Json;

namespace BlazorWebView.Android
{
    public class BlazorWebView : Fragment, IBlazorWebView
    {
        private WebView innerWebView;

        private const string InitScriptSource =
         @"window.__receiveMessageCallbacks = [];
	 		 window.__dispatchMessageCallback = function(message) {
			 	window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
			 };
			 window.external = {
			 	sendMessage: function(message) {
			 		webwindowinterop.PostMessage(message);
			 	},
			 	receiveMessage: function(callback) {
			 		window.__receiveMessageCallbacks.push(callback);
			 	}
			 };";

        public event EventHandler<string> OnWebMessageReceived;

        public void Initialize(Action<WebViewOptions> configure)
        {
            var options = new WebViewOptions();
            configure.Invoke(options);

            WebSettings webSettings = innerWebView.Settings;
            webSettings.JavaScriptEnabled = true;
            innerWebView.AddJavascriptInterface(new BlazorJavascriptInterface(this), "webwindowinterop");

            var resultCallBack = new ValueCallback<string>(s =>
            {
                if (!string.IsNullOrEmpty(s))
                {
                    Console.WriteLine(s);
                }
            });

            var blazorWebViewClient = new BlazorWebViewClient();
            blazorWebViewClient.PageStarted += (s, e) =>
            {
                innerWebView.EvaluateJavascript(InitScriptSource, resultCallBack);
            };

            innerWebView.SetWebViewClient(blazorWebViewClient);

            foreach (var (schemeName, handler) in options.SchemeHandlers)
            {
                blazorWebViewClient.AddCustomScheme(schemeName, handler);
            }
        }

        public void Invoke(Action callback)
        {
            this.Activity.RunOnUiThread(callback);
        }

        public void NavigateToUrl(string url)
        {
            innerWebView.LoadUrl(url);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            var view = inflater.Inflate(Resource.Layout.BlazorWebView, container, false);
            innerWebView = view.FindViewById<WebView>(Resource.Id.innerWebView);
            return view;
        }

        public void SendMessage(string message)
        {
            message = JsonConvert.ToString(message);

            var resultCallBack = new ValueCallback<string>(s =>
            {
                if (!string.IsNullOrEmpty(s))
                {
                    Console.WriteLine(s);
                }
            });

            innerWebView.EvaluateJavascript($"__dispatchMessageCallback({message})", resultCallBack);
        }

        public void ShowMessage(string title, string message)
        {
            this.Invoke(() =>
            {
                new AlertDialog.Builder(this.Context)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton(global::Android.Resource.String.Yes, (object sender, DialogClickEventArgs e) =>
                {
                })
                .SetIcon(global::Android.Resource.Drawable.IcDialogAlert)
                .Show();
            });
        }

        private class ValueCallback<TValue> : global::Java.Lang.Object, IValueCallback where TValue : class
        {
            private readonly Action<TValue> callback;

            public ValueCallback(Action<TValue> callback)
            {
                this.callback = callback;
            }

            public void OnReceiveValue(Java.Lang.Object value)
            {
                callback(value as TValue);
            }
        }

        internal void OnReceiveWebMessage(string message)
        {
            this.OnWebMessageReceived?.Invoke(this, message);
        }
    }
}