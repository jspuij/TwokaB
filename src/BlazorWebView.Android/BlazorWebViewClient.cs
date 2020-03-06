// <copyright file="BlazorWebViewClient.cs" company="Steve Sanderson and Jan-Willem Spuij">
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
    using global::Android.App;
    using global::Android.Content;
    using global::Android.Graphics;
    using global::Android.OS;
    using global::Android.Runtime;
    using global::Android.Views;
    using global::Android.Webkit;
    using global::Android.Widget;

    /// <summary>
    /// A <see cref="WebViewClient"/> implementation to handle events that happen
    /// on the inner Android <see cref="WebView"/>.
    /// </summary>
    public class BlazorWebViewClient : WebViewClient
    {
        private readonly IDictionary<string, ResolveWebResourceDelegate> schemeHandlers
            = new Dictionary<string, ResolveWebResourceDelegate>();

        /// <summary>
        /// Exposes an event that fires when the loading of a page is started, used to inject
        /// the <see cref="BlazorJavascriptInterface"/> object into the web view.
        /// </summary>
        public event EventHandler PageStarted;

        /// <summary>
        /// Give the host application a chance to take control when a URL is about to be loaded in the current WebView.
        /// If a WebViewClient is not provided, by default WebView will ask Activity Manager to choose the proper
        /// handler for the URL. If a WebViewClient is provided, returning true causes the current WebView to abort
        /// loading the URL, while returning false causes the WebView to continue loading the URL as usual.
        /// </summary>
        /// <param name="view">The WebView that is initiating the callback.</param>
        /// <param name="request">Object containing the details of the request.</param>
        /// <returns>true to cancel the current load, otherwise return false.</returns>
        public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
        {
            // these schemes should proceed!
            if (this.schemeHandlers.ContainsKey(request.Url.Scheme))
            {
                return false;
            }

            return base.ShouldOverrideUrlLoading(view, request);
        }

        /// <summary>
        /// Notify the host application of a resource request and allow the application to return the data.
        /// If the return value is null, the WebView will continue to load the resource as usual.
        /// Otherwise, the return response and data will be used.
        /// </summary>
        /// <param name="view">The WebView that is requesting the resource.</param>
        /// <param name="request">Object containing the details of the request.</param>
        /// <returns>
        /// A WebResourceResponse containing the response information or null
        /// if the WebView should load the resource itself.
        /// </returns>
        public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
        {
            if (this.schemeHandlers.TryGetValue(request.Url.Scheme, out var handler))
            {
                // handle the scheme and url by executing the handler.
                var stream = handler(request.Url.ToString(), out string contentType, out Encoding encoding);
                if (stream != null)
                {
                    // there is a result stream, prepare the response.
                    var responseHeaders = new Dictionary<string, string>()
                    {
                        { "Cache-Control", "no-cache" },
                    };
                    return new WebResourceResponse(contentType, encoding.ToString(), 200, "OK", responseHeaders, stream);
                }
                else
                {
                    // not found.
                    return new WebResourceResponse(contentType, "UTF-8", 404, "Not Found", null, null);
                }
            }

            return base.ShouldInterceptRequest(view, request);
        }

        /// <summary>
        /// Notify the host application that a page has started loading. This method is called once for each main frame
        /// load so a page with iframes or framesets will call onPageStarted one time for the main frame. This also
        /// means that onPageStarted will not be called when the contents of an embedded frame changes, i.e. clicking
        /// a link whose target is an iframe, it will also not be called for fragment navigations
        /// (navigations to #fragment_id).
        /// </summary>
        /// <param name="view">The WebView that is initiating the callback.</param>
        /// <param name="url">The url to be loaded.</param>
        /// <param name="favicon">The favicon for this page if it already exists in the database.</param>
        public override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
            this.PageStarted?.Invoke(this, new EventArgs());
            base.OnPageStarted(view, url, favicon);
        }

        /// <summary>
        /// Add a custom scheme with a handler to the webiew.
        /// </summary>
        /// <param name="schemeName">The scheme name to handle.</param>
        /// <param name="handler">The handler for the scheme.</param>
        internal void AddCustomScheme(string schemeName, ResolveWebResourceDelegate handler)
        {
            this.schemeHandlers.Add(schemeName, handler);
        }
    }
}