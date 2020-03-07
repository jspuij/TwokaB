// <copyright file="UrlSchemeHandler.cs" company="Steve Sanderson and Jan-Willem Spuij">
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

namespace BlazorWebView.Mac
{
    using System;
    using System.Text;
    using Foundation;
    using WebKit;

    /// <summary>
    /// Handles urls belonging to single scheme.
    /// </summary>
    public class UrlSchemeHandler : NSObject, IWKUrlSchemeHandler
    {
        /// <summary>
        /// A reference to the requesthandler delegate.
        /// </summary>
        private ResolveWebResourceDelegate requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlSchemeHandler"/> class.
        /// </summary>
        /// <param name="requestHandler">A reference to the requesthandler delegate.</param>
        public UrlSchemeHandler(ResolveWebResourceDelegate requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// Starts a task that handles the retrieval of the resource.
        /// </summary>
        /// <param name="webView">The webview to use.</param>
        /// <param name="urlSchemeTask">The scheme task status object.</param>
        public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
            var url = urlSchemeTask.Request.Url;

            var stream = this.requestHandler(url.AbsoluteString, out string contentType, out Encoding encoding);

            NSDictionary headers = new NSMutableDictionary();
            headers.SetValueForKey((NSString)contentType, (NSString)"Content-Type");
            headers.SetValueForKey((NSString)"no-cache", (NSString)"Cache-Control");
            var response = new NSHttpUrlResponse(url, stream != null ? 200 : 404, "HTTP/1.1", headers);
            urlSchemeTask.DidReceiveResponse(response);
            if (stream != null)
            {
                urlSchemeTask.DidReceiveData(NSData.FromStream(stream));
            }

            urlSchemeTask.DidFinish();
        }

        /// <summary>
        /// Stops a task that handles the retrieval of the resource.
        /// </summary>
        /// <param name="webView">The webview to use.</param>
        /// <param name="urlSchemeTask">The scheme task status object.</param>
        public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
        }
    }
}
