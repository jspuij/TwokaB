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

namespace BlazorWebView.Android
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using global::Android.Content;
    using global::Android.OS;
    using global::Android.Runtime;
    using global::Android.Support.V4.App;
    using global::Android.Support.V7.App;
    using global::Android.Util;
    using global::Android.Views;
    using global::Android.Webkit;
    using global::Android.Widget;
    using Newtonsoft.Json;

    /// <summary>
    /// An <see cref="IBlazorWebView"/> implementation for Android.
    /// </summary>
    public class BlazorWebView : Fragment, IBlazorWebView
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
			 		webwindowinterop.PostMessage(message);
			 	},
			 	receiveMessage: function(callback) {
			 		window.__receiveMessageCallbacks.push(callback);
			 	}
			 };";

        /// <summary>
        /// The inner Android Webview.
        /// </summary>
        private WebView innerWebView;

        /// <summary>
        /// Event that is fired when a web message is received from javascript.
        /// </summary>
        public event EventHandler<string> OnWebMessageReceived;

        /// <summary>
        /// Initialize the BlazorWebView.
        /// </summary>
        /// <param name="configure">A delegate that is executed to configure the webvies.</param>
        public void Initialize(Action<WebViewOptions> configure)
        {
            var options = new WebViewOptions();
            configure.Invoke(options);

            WebSettings webSettings = this.innerWebView.Settings;
            webSettings.JavaScriptEnabled = true;
            this.innerWebView.AddJavascriptInterface(new BlazorJavascriptInterface(this), "webwindowinterop");

            var resultCallBack = new ValueCallback<string>(s =>
            {
                // TODO: Handle javascript errors nicer.
                if (!string.IsNullOrEmpty(s))
                {
                    Console.WriteLine(s);
                }
            });

            var blazorWebViewClient = new BlazorWebViewClient();
            blazorWebViewClient.PageStarted += (s, e) =>
            {
                this.innerWebView.EvaluateJavascript(InitScriptSource, resultCallBack);
            };

            this.innerWebView.SetWebViewClient(blazorWebViewClient);

            foreach (var (schemeName, handler) in options.SchemeHandlers)
            {
                blazorWebViewClient.AddCustomScheme(schemeName, handler);
            }
        }

        /// <summary>
        /// Invoke a callback on the UI thread.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        public void Invoke(Action callback)
        {
            this.Activity.RunOnUiThread(callback);
        }

        /// <summary>
        /// Navigate to the specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        public void NavigateToUrl(string url)
        {
            this.innerWebView.LoadUrl(url);
        }

        /// <summary>
        /// Called to have the fragment instantiate its user interface view.
        /// This is optional, and non-graphical fragments can return null (which is the default implementation).
        /// </summary>
        /// <param name="inflater">
        /// The LayoutInflater object that can be used to inflate any views in the fragment.
        /// </param>
        /// <param name="container">ViewGroup: If non-null, this is the parent view that the fragment's UI should
        /// be attached to. The fragment should not add the view itself, but this can be used to generate the
        /// LayoutParams of the view. This value may be null.
        /// </param>
        /// <param name="savedInstanceState">Bundle: If non-null, this fragment is being re-constructed from a
        /// previous saved state as given here.</param>
        /// <returns>Return the View for the fragment's UI, or null.</returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            var view = inflater.Inflate(Resource.Layout.BlazorWebView, container, false);
            this.innerWebView = view.FindViewById<WebView>(Resource.Id.innerWebView);
            return view;
        }

        /// <summary>
        /// Send a message to javascript.
        /// </summary>
        /// <param name="message">The message to send.</param>
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

            this.innerWebView.EvaluateJavascript($"__dispatchMessageCallback({message})", resultCallBack);
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

        /// <summary>
        /// Method that is called when a message is received from javascript.
        /// Invokes the Eventhandlers for <see cref="OnWebMessageReceived"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        internal void OnReceiveWebMessage(string message)
        {
            this.OnWebMessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// A callback class to handle receiving a value and calling a callback.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        private class ValueCallback<TValue> : global::Java.Lang.Object, IValueCallback
            where TValue : class
        {
            /// <summary>
            /// The callback to call with the value.
            /// </summary>
            private readonly Action<TValue> callback;

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueCallback{TValue}"/> class.
            /// </summary>
            /// <param name="callback">The callback to call.</param>
            public ValueCallback(Action<TValue> callback)
            {
                this.callback = callback;
            }

            /// <summary>
            /// Method that is called when a value is received.
            /// </summary>
            /// <param name="value">The value to receive.</param>
            public void OnReceiveValue(Java.Lang.Object value)
            {
                this.callback(value as TValue);
            }
        }
    }
}