using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;

namespace BlazorWebView.Android
{
    public class BlazorWebViewClient : WebViewClient
    {
        private readonly IDictionary<string, ResolveWebResourceDelegate> schemeHandlers = new Dictionary<string, ResolveWebResourceDelegate>();

        public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
        {
            // these schemes should proceed!
            if (schemeHandlers.ContainsKey(request.Url.Scheme))
            {
                return false;
            }
            return base.ShouldOverrideUrlLoading(view, request);
        }

        public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
        {
            if (schemeHandlers.TryGetValue(request.Url.Scheme, out var handler))
            {
                var stream = handler(request.Url.ToString(), out string contentType, out Encoding encoding);
                if (stream != null)
                {
                    var responseHeaders = new Dictionary<string, string>()
                    {
                        { "Cache-Control", "no-cache"},
                    };
                    return new WebResourceResponse(contentType, encoding.ToString(), 200, "OK", responseHeaders, stream);
                } else
                {
                    return new WebResourceResponse(contentType, "UTF-8", 404, "Not Found", null, null);
                }

            }
            return base.ShouldInterceptRequest(view, request);
        }

        internal void AddCustomScheme(string schemeName, ResolveWebResourceDelegate handler)
        {
            this.schemeHandlers.Add(schemeName, handler);
        }
    }
}